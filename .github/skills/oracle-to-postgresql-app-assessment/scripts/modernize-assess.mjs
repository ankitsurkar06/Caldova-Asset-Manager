import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  adapterOverrides, collectModernizeContext, resolveModernizeConfig,
} from "./modernize-context.mjs";
import {
  createBudget, integrationError, inspectPath, isMain, parseArguments, runCli,
  validateOutputDirectory, writeNewText,
} from "./modernize-common.mjs";

function escapeHtml(value) {
  return String(value ?? "").replaceAll("&", "&amp;").replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#39;");
}

function escapeMarkdown(value) {
  return escapeHtml(value).replaceAll("|", "&#124;").replace(/[\r\n]+/g, " ")
    .replace(/[\\`*_[\]()]/g, "\\$&");
}

function displayName(object) {
  return `${object.schema == null ? "(schema unresolved)" : object.schema}.${object.name}` +
    (object.identityArguments ? `(${object.identityArguments.join(", ")})` : "");
}

function sections(context) {
  const review = context.scopeStatus === "SCOPE_REVIEW_REQUIRED";
  const snapshot = context.postgresSnapshot;
  const matching = context.objectMatches.filter((entry) => entry.status === "snapshot-candidate").length;
  const unresolved = context.objectMatches.filter((entry) => entry.status !== "snapshot-candidate").length;
  const tables = [
    {
      title: "Scope and applicability",
      headers: ["Area", "Evidence"],
      rows: [
        ["Assessment result", review ? "Action needed: incomplete static scope; do not start conversion." :
          "Convertible candidates identified; conversion and runtime validation have not run."],
        ["Source files inspected", context.scope.scannedFiles],
        ["Detected technologies", context.scope.technologies.map((entry) => entry.name).join(", ") || "Not identified"],
        ["Oracle hotspot entries", context.scope.oracleFindings.length],
        ["Uninspected files", context.scope.uninspectedFiles.length],
        ["SQL reference review limitations", context.scope.referenceLimitations.length],
      ],
    },
    {
      title: "Oracle application hotspots",
      headers: ["Application path", "Oracle evidence category"],
      rows: context.scope.oracleFindings.map((finding) => [finding.path, finding.category]),
    },
    {
      title: "PostgreSQL snapshot grounding",
      text: snapshot.status === "NOT_SUPPLIED"
        ? "PostgreSQL DDL snapshot not supplied. Target object compatibility has not been verified."
        : "This is a supplied DDL snapshot, not a live database inventory. A candidate is not proof of deployment or compatibility. Absence means only not found in this export. Unqualified names and routine overloads require review.",
      headers: ["Area", "Evidence"],
      rows: [
        ["Snapshot status", snapshot.status],
        ["Indexed DDL files", snapshot.indexedFiles ?? "Not applicable"],
        ["Indexed object declarations", snapshot.indexedObjects ?? "Not applicable"],
        ["Code-derived references with one candidate", matching],
        ["Unresolved, absent, or ambiguous references", unresolved],
        ["Snapshot content digest", snapshot.snapshotSha256 ?? "Not supplied"],
        ["Oracle DDL source", context.oracleDdl.status],
      ],
    },
    {
      title: "Code-derived object reconciliation",
      headers: ["Source paths", "Reference kind", "Code reference", "Snapshot result", "Candidate contracts"],
      rows: context.objectMatches.map((entry) => [
        entry.reference.sourcePaths.join(", "), entry.reference.kind,
        `${entry.reference.schema ?? "(unqualified)"}.${entry.reference.name}`, entry.status,
        entry.candidates.map((object) => `${object.kind} ${displayName(object)}`).join("; ") || "None established",
      ]),
    },
    {
      title: "Matched snapshot contract details",
      headers: ["Object", "Columns or routine arguments", "Source evidence", "Limitations"],
      rows: [...new Map(context.objectMatches.flatMap((entry) => entry.candidates)
        .map((object) => [JSON.stringify([object.kind, object.schema, object.name, object.identityArguments]), object])).values()]
        .map((object) => [
          `${object.kind} ${displayName(object)}`,
          object.columns?.map((column) => `${column.name}: ${column.type}${column.nullable ? "" : " NOT NULL"}`).join("; ") ??
            object.arguments?.map((argument) => `${argument.mode} ${argument.name ?? "(unnamed)"}: ${argument.type}`).join("; ") ??
            object.baseType ?? "Review the matched DDL for additional semantics.",
          `${object.source.relativePath}, line ${object.sourceLine}, SHA256 ${object.source.sha256}`,
          object.limitations.join("; ") || "Only declared structure indexed; runtime behavior is unverified.",
        ]),
    },
    {
      title: "Scope and parser limitations",
      headers: ["Evidence path", "Limitation"],
      rows: [
        ...context.scope.uninspectedFiles.map((entry) => [entry.path, entry.reason]),
        ...context.scope.referenceLimitations.map((entry) => [entry.path, entry.reason]),
        ...(snapshot.limitations ?? []).map((entry) => [entry.relativePath, entry.reason]),
      ],
      text: "Reference extraction is static and conservative, not a full SQL/ORM binder. Review dynamic SQL, CTE aliases, indirect routine calls, mapping conventions, and runtime search paths during conversion.",
    },
    {
      title: "Validation boundaries",
      headers: ["Gate", "Result"],
      rows: [
        ["Source assessment", review ? "Incomplete: see scope limitations" : "Static inspection only"],
        ["Build and packaging", "Not run"],
        ["Unit and integration tests", "Not run"],
        ["Code coverage", "Not measured"],
        ["Docker PostgreSQL validation", "Not run"],
        ["Authoritative live target contract", "Not verified"],
        ["Authoritative live target runtime and writes", "Unsupported in this edition"],
        ["PostgreSQL MCP calls", 0],
      ],
    },
    {
      title: "Recommended next steps",
      headers: ["Owner", "Action"],
      rows: review ? [["Application owner", "Resolve every uninspected source path before starting conversion."]] : [
        ["Application owner", "Create and review a Modernize plan; this assessment does not automatically create tasks."],
        ["Application owner", context.recommendedTaskSkills.length ?
          `Verify exact task skill selection: ${context.recommendedTaskSkills.join(", ")}.` :
          "No Java/.NET conversion adapter applies; use the standalone skill for other supported languages."],
        ["Database owner", snapshot.status === "NOT_SUPPLIED"
          ? "Optionally supply a schema-only PostgreSQL DDL folder for offline grounding."
          : "Confirm the export's scope, age, schema names, and unresolved object candidates."],
        ["Application owner", "Run application builds, discovered tests, packaging checks, and isolated Docker production-path tests during conversion."],
        ["Database owner", "Validate the deployed target and production runtime separately; snapshot and Docker results do not establish live readiness."],
      ],
    },
    {
      title: "Execution evidence",
      headers: ["Field", "Value"],
      rows: [
        ["Edition", context.edition],
        ["Source plugin version", context.sourceVersion],
        ["Configuration source", context.configuration.source],
        ["Configuration digest", context.configuration.sha256],
        ["Application inventory digest", context.scope.inventorySha256],
        ["Static collection duration (milliseconds)", context.elapsedMs],
        ["Workspace source modifications", "None"],
        ["Action options", "Assessment executes no optional migration actions"],
      ],
    },
  ];
  return tables;
}

export function renderModernizeAssessment(context, format) {
  if (!["html", "markdown"].includes(format)) {
    throw integrationError("report-format must be html or markdown.");
  }
  const title = "Oracle to PostgreSQL Application Readiness";
  const summary = "Offline assessment of Oracle-dependent application code. Findings are grounded in source evidence and, when supplied, extracted PostgreSQL DDL. No live connections, database writes, builds, tests, or Docker execution were performed.";
  const renderTable = (section) => {
    const rows = section.rows.slice(0, 500);
    const truncated = section.rows.length > rows.length
      ? `Showing ${rows.length} of ${section.rows.length} entries; use task context collection for the complete bounded manifest.` : "";
    if (format === "markdown") {
      return [
        `## ${section.title}`, section.text ?? "",
        `| ${section.headers.join(" | ")} |`,
        `| ${section.headers.map(() => "---").join(" | ")} |`,
        ...(rows.length ? rows : [section.headers.map((_, index) => index === 0 ? "None recorded" : "Not applicable")])
          .map((row) => `| ${row.map(escapeMarkdown).join(" | ")} |`),
        truncated,
      ].filter(Boolean).join("\n");
    }
    return `<section><h2>${section.title}</h2>${section.text ? `<p>${escapeHtml(section.text)}</p>` : ""}` +
      `<table><thead><tr>${section.headers.map((header) => `<th>${escapeHtml(header)}</th>`).join("")}</tr></thead>` +
      `<tbody>${rows.length ? rows.map((row) =>
        `<tr>${row.map((value) => `<td>${escapeHtml(value)}</td>`).join("")}</tr>`).join("") :
        `<tr><td colspan="${section.headers.length}">None recorded</td></tr>`}</tbody></table>` +
      `${truncated ? `<p>${escapeHtml(truncated)}</p>` : ""}</section>`;
  };
  const content = sections(context).map(renderTable).join("\n\n");
  if (format === "markdown") return `# ${title}\n\n## Executive summary\n\n${summary}\n\n${content}\n`;
  return `<!doctype html>\n<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">` +
    `<title>${title}</title><style>body{font:16px system-ui,sans-serif;margin:2rem;max-width:100rem;color:#202124}` +
    `table{border-collapse:collapse;width:100%;margin:1rem 0}th,td{border:1px solid #bbb;padding:.5rem;text-align:left;vertical-align:top;overflow-wrap:anywhere}` +
    `th{background:#f2f4f7}h2{margin-top:2rem}</style></head><body><h1>${title}</h1>` +
    `<h2>Executive summary</h2><p>${summary}</p>${content}</body></html>\n`;
}

export async function runModernizeAssessment({
  workspacePath, outputDirectory, reportFormat, overrides = {}, signal, maxDurationMs = 540_000,
}) {
  const budget = createBudget({ maxDurationMs, signal });
  if (!["html", "markdown"].includes(reportFormat)) {
    throw integrationError("report-format must be html or markdown.");
  }
  const config = await resolveModernizeConfig(workspacePath, overrides, budget);
  const packageRoot = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
  const output = await validateOutputDirectory(config.workspace, outputDirectory,
    [config.postgresDdlFolder, config.oracleDdlFolder, packageRoot]);
  const entryName = reportFormat === "html" ? "index.html" : "index.md";
  if (await inspectPath(path.join(output, entryName), { allowMissing: true })) {
    throw integrationError("The requested report entry point already exists; use an empty output directory.");
  }
  const context = await collectModernizeContext(config.workspace, {
    overrides, signal, maxDurationMs: Math.min(maxDurationMs, 120_000),
  });
  if (config.configSha256 !== context.configuration.sha256) {
    throw integrationError("Configuration changed during assessment; retry.");
  }
  budget.check();
  if (context.scopeStatus === "NOT_APPLICABLE") {
    return { status: "skipped", reason: "No active Oracle application evidence was found." };
  }
  const report = renderModernizeAssessment(context, reportFormat);
  if (Buffer.byteLength(report) > 2_000_000) throw integrationError("The assessment report exceeds its size budget.");
  budget.check();
  await writeNewText(output, entryName, report);
  return { status: "completed", reason: context.scopeStatus === "SCOPE_REVIEW_REQUIRED"
    ? "The report records incomplete scope requiring review before conversion."
    : "The offline application assessment report was generated." };
}

if (isMain(import.meta.url)) {
  await runCli(async (signal) => {
    const args = parseArguments(process.argv.slice(2), [
      "--workspace-path", "--assessment-output-dir", "--report-format",
      "--postgres-ddl-folder", "--oracle-ddl-folder", "--oracle-schema",
    ], ["--workspace-path", "--assessment-output-dir", "--report-format"]);
    const result = await runModernizeAssessment({
      workspacePath: args["--workspace-path"], outputDirectory: args["--assessment-output-dir"],
      reportFormat: args["--report-format"], overrides: adapterOverrides(args), signal,
    });
    process.stdout.write(`${JSON.stringify(result)}\n`);
  });
}
