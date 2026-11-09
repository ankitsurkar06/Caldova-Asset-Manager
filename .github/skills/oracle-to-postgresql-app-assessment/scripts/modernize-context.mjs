import path from "node:path";
import { realpath } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { detectMigrationScope, extractEmbeddedSql } from "./detect-migration-scope.mjs";
import { indexPostgresDdlFolder, matchPostgresObjects } from "./index-postgres-ddl.mjs";
import {
  createBudget, integrationError, inspectPath, isMain, isWithin, MODERNIZE_SKILLS,
  parseArguments, readAdapterVersion, readLimitedText, requireDirectory,
  requireText, runCli, sha256, validateOutputDirectory, writeNewText,
} from "./modernize-common.mjs";
import { readSqlName, tokenizeSql } from "./postgres-ddl-parser.mjs";
import { resolveOptions } from "./target-write-policy.mjs";
import { maskSourceComments } from "./source-comment-mask.mjs";

const CONFIG_KEYS = new Set(["postgres-ddl-folder", "oracle-ddl-folder", "oracle-schema", "options"]);
const HOST_TEXT_EXTENSIONS = [".prefs", ".rest", ".http"];
const HOST_TEXT_FILE_NAMES = [".project", ".rest", ".http"];
const SQL_EXTENSIONS = new Set([
  ".sql", ".ddl", ".psql", ".pgsql", ".pls", ".plsql", ".pks", ".pkb", ".pck",
  ".bdy", ".spc", ".tpb", ".tps", ".plb", ".prc", ".fnc", ".vw", ".trg",
]);
const BUILTIN_CALLS = new Set([
  "case", "cast", "coalesce", "concat", "count", "decode", "exists", "extract", "greatest",
  "in", "least", "lower", "ltrim", "max", "min", "nullif", "nvl", "nvl2", "over",
  "round", "rtrim", "substr", "substring", "sum", "to_char", "to_date", "to_number",
  "trim", "upper", "values",
]);

export async function resolveModernizeConfig(workspacePath, overrides = {}, budget = createBudget()) {
  const workspace = await requireDirectory(workspacePath, "workspace-path");
  const configPath = path.join(workspace, ".github", "modernize", "oracle-postgres.json");
  let supplied = {};
  let configurationSource = "defaults";
  let configSha256 = null;
  if (await inspectPath(configPath, { allowMissing: true })) {
    const config = await readLimitedText(configPath, 64_000, budget);
    try {
      supplied = JSON.parse(config.text);
    } catch {
      throw integrationError("The optional Oracle/PostgreSQL repository configuration must be valid JSON.");
    }
    configurationSource = "repository-config";
    configSha256 = config.sha256;
  }
  for (const object of [supplied, overrides]) {
    if (!object || typeof object !== "object" || Array.isArray(object) ||
      Object.keys(object).some((key) => !CONFIG_KEYS.has(key))) {
      throw integrationError("Oracle/PostgreSQL configuration has an unknown or unsupported setting; live profiles and credentials are not accepted.");
    }
  }
  const values = { ...supplied, ...overrides };
  if (Object.keys(overrides).length) configurationSource = "adapter-override";
  if (values.options !== undefined && (!Array.isArray(values.options) ||
    values.options.some((option) => typeof option !== "string" || !/^[a-z][a-z-]{0,63}$/.test(option)))) {
    throw integrationError("Migration options must be an array of option names.");
  }
  let options;
  try {
    options = resolveOptions((values.options ?? []).join(","));
  } catch {
    throw integrationError("An unsupported migration option was supplied.");
  }
  if (options["create-persistent-ddl"] || options["test-target-writes"]) {
    throw integrationError("create-persistent-ddl and test-target-writes are unsupported in the offline Modernize edition.");
  }
  const folders = {};
  for (const key of ["postgres-ddl-folder", "oracle-ddl-folder"]) {
    if (!Object.hasOwn(values, key)) continue;
    const value = requireText(values[key], key);
    const trustedOverride = Object.hasOwn(overrides, key);
    if (!trustedOverride && (path.isAbsolute(value) || path.win32.parse(value).root)) {
      throw integrationError("Repository-config DDL folders must be workspace-relative and remain within the workspace.");
    }
    if (value.includes("://") || path.win32.parse(value).root.startsWith("\\\\") ||
      (path.win32.isAbsolute(value) && !path.isAbsolute(value))) {
      throw integrationError("DDL folders must be filesystem paths accessible to the current execution host.");
    }
    const candidate = path.resolve(workspace, trustedOverride ? value : value.replace(/[\\/]/g, path.sep));
    if (!trustedOverride && !isWithin(workspace, candidate)) {
      throw integrationError("Repository-config DDL folders must remain within the workspace.");
    }
    const folder = await requireDirectory(candidate, key);
    if (!trustedOverride && !isWithin(await realpath(workspace), await realpath(folder))) {
      throw integrationError("Repository-config DDL folders must remain within the workspace.");
    }
    if (isWithin(folder, workspace)) {
      throw integrationError("A dedicated DDL export folder cannot be the application workspace or its ancestor.");
    }
    if (isWithin(path.join(workspace, ".git"), folder)) {
      throw integrationError("Git metadata cannot be used as a DDL snapshot.");
    }
    folders[key] = folder;
  }
  if (folders["postgres-ddl-folder"] && folders["oracle-ddl-folder"] &&
    (isWithin(folders["postgres-ddl-folder"], folders["oracle-ddl-folder"]) ||
      isWithin(folders["oracle-ddl-folder"], folders["postgres-ddl-folder"]))) {
    throw integrationError("Oracle and PostgreSQL DDL exports must be separate, non-overlapping folders.");
  }
  if (values["oracle-schema"] !== undefined) {
    requireText(values["oracle-schema"], "oracle-schema");
    if (!folders["oracle-ddl-folder"]) throw integrationError("oracle-schema requires oracle-ddl-folder.");
  }
  return {
    workspace, configPath, configurationSource,
    configSha256: sha256(JSON.stringify([configSha256, overrides])),
    postgresDdlFolder: folders["postgres-ddl-folder"] ?? null,
    oracleDdlFolder: folders["oracle-ddl-folder"] ?? null,
    oracleSchema: values["oracle-schema"] ?? null,
    options,
  };
}

function sqlReferences(sql, budget) {
  const tokens = tokenizeSql(sql, { budget, maxTokens: 100_000 });
  const references = [];
  const add = (kind, parsed) => {
    if (!parsed || parsed.parts.length > 2) return;
    references.push({ kind, schema: parsed.parts.length === 2 ? parsed.parts[0] : null,
      name: parsed.parts.at(-1) });
  };
  for (let index = 0; index < tokens.length; index += 1) {
    const token = tokens[index];
    if (token.kind !== "word") continue;
    if (["from", "join", "update", "into"].includes(token.value)) {
      const parsed = readSqlName(tokens, index + 1);
      if (parsed && tokens[parsed.next]?.value !== "(") add("relation", parsed);
    }
    if (["call", "exec", "execute"].includes(token.value)) add("routine", readSqlName(tokens, index + 1));
    if (["nextval", "currval"].includes(token.value) && tokens[index + 1]?.value === "(" &&
      tokens[index + 2]?.kind === "string") {
      add("sequence", readSqlName(tokenizeSql(tokens[index + 2].value)));
    }
    const parsed = tokens[index - 1]?.value === "." ? null : readSqlName(tokens, index);
    if (parsed?.parts.length >= 2 && ["nextval", "currval"].includes(parsed.parts.at(-1))) {
      add("sequence", { parts: parsed.parts.slice(0, -1) });
    } else if (parsed && tokens[parsed.next]?.value === "(" &&
      !BUILTIN_CALLS.has(parsed.parts.at(-1)) &&
      (parsed.parts.length === 2 || ["select", "call"].includes(tokens[index - 1]?.value))) {
      add("routine", parsed);
    }
  }
  return references;
}

async function collectCodeReferences(workspace, scope, budget) {
  const references = new Map();
  const limitations = [];
  const add = (reference, sourcePath) => {
    if (!reference.name) return;
    const key = JSON.stringify([reference.kind, reference.schema, reference.name]);
    if (!references.has(key)) {
      if (references.size >= 2000) throw integrationError("Code-derived object reference limit exceeded.");
      references.set(key, { ...reference, sourcePaths: [] });
    }
    const sources = references.get(key).sourcePaths;
    if (!sources.includes(sourcePath)) sources.push(sourcePath);
  };
  for (const file of scope.scannedFileDigests) {
    budget.check();
    const source = await readLimitedText(path.join(workspace, file.path), 2_000_000, budget);
    if (source.sha256 !== file.sha256) {
      throw integrationError("Application source changed after scope detection; rerun against a stable workspace.");
    }
    const active = maskSourceComments(source.text, file.path);
    const sql = SQL_EXTENSIONS.has(path.extname(file.path).toLowerCase())
      ? [active] : extractEmbeddedSql(active, {
        includeRoutineCalls: true, rawStrings: path.extname(file.path).toLowerCase() === ".cs",
      });
    for (const context of sql) {
      let found;
      try {
        found = sqlReferences(context, budget);
      } catch (error) {
        if (error.code !== "MODERNIZE_INVALID_INPUT") throw error;
        limitations.push({ path: file.path, reason: "SQL reference extraction requires manual review of this source." });
        continue;
      }
      for (const reference of found) add(reference, file.path);
    }
    for (const annotation of active.matchAll(/\b(?:Table|ToTable)\s*\(\s*(?:name\s*=\s*)?"([^"\r\n]{1,256})"([^)\r\n]{0,500})\)/g)) {
      const schema = /\b[Ss]chema\s*=\s*"([^"\r\n]{1,256})"/.exec(annotation[2])?.[1];
      const parsed = readSqlName(tokenizeSql(annotation[1]));
      if (parsed?.parts.length === 1) add({ kind: "relation",
        schema: schema ? readSqlName(tokenizeSql(schema))?.parts[0] ?? null : null,
        name: parsed.parts[0] }, file.path);
    }
  }
  return { references: [...references.values()].sort((left, right) =>
    JSON.stringify(left).localeCompare(JSON.stringify(right))), limitations };
}

export async function collectModernizeContext(workspacePath, {
  overrides = {}, signal, maxDurationMs = 120_000, scopeLimits = {},
} = {}) {
  const budget = createBudget({ maxDurationMs, signal });
  const config = await resolveModernizeConfig(workspacePath, overrides, budget);
  const excludePaths = [
    path.join(config.workspace, ".github", "modernize"),
    path.join(config.workspace, ".github", "skills"),
    path.join(config.workspace, ".copilot", "skills"),
    path.join(config.workspace, ".claude", "skills"),
    config.postgresDdlFolder, config.oracleDdlFolder,
  ].filter((candidate) => candidate && candidate !== config.workspace && isWithin(config.workspace, candidate));
  const packageRoot = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
  if (packageRoot !== config.workspace && isWithin(config.workspace, packageRoot)) excludePaths.push(packageRoot);
  const scope = await detectMigrationScope(config.workspace, {
    ...scopeLimits, maxDurationMs: Math.min(30_000, maxDurationMs),
    captureBaseline: false, excludePaths: [...new Set(excludePaths)],
    additionalTextExtensions: HOST_TEXT_EXTENSIONS,
    additionalTextFileNames: HOST_TEXT_FILE_NAMES,
    onProgress: () => budget.check(),
  });
  budget.check();
  const findings = new Map();
  for (const finding of scope.activeOracleFindings) {
    findings.set(`${finding.path}\0${finding.category}`, { path: finding.path, category: finding.category });
  }
  const hasApplicationConsumers = [...scope.activeDatabaseFindings, ...scope.activeOracleFindings]
    .some((finding) => !SQL_EXTENSIONS.has(path.extname(finding.path).toLowerCase()));
  const scopeStatus = scope.skippedFiles.length || scope.status === "SCOPE_REVIEW_REQUIRED"
    ? "SCOPE_REVIEW_REQUIRED" : findings.size && hasApplicationConsumers ? "MIGRATION_REQUIRED" : "NOT_APPLICABLE";
  const languages = new Set();
  for (const file of scope.scannedFileDigests) {
    if (/\.(?:java|kt|kts)$/.test(file.path) || /(?:^|\/)(?:pom\.xml|build\.gradle)$/.test(file.path)) languages.add("java");
    if (/\.(?:cs|fs|vb|csproj|fsproj|vbproj)$/.test(file.path)) languages.add("dotnet");
  }
  const code = await collectCodeReferences(config.workspace, scope, budget);
  const snapshot = config.postgresDdlFolder
    ? await indexPostgresDdlFolder(config.postgresDdlFolder, { budget }) : null;
  const objectMatches = matchPostgresObjects(snapshot, code.references);
  const context = {
    schemaVersion: 1, edition: "modernize-offline", sourceVersion: await readAdapterVersion(),
    scopeStatus, applicable: scopeStatus === "MIGRATION_REQUIRED",
    configuration: {
      source: config.configurationSource, sha256: config.configSha256,
      postgresDdlSupplied: Boolean(config.postgresDdlFolder),
      oracleDdlSupplied: Boolean(config.oracleDdlFolder), options: config.options,
    },
    scope: {
      scannedFiles: scope.scannedFiles, inventorySha256: scope.inventoryDigest,
      hasApplicationConsumers,
      technologies: scope.detectedTechnologies,
      oracleFindings: [...findings.values()], uninspectedFiles: scope.skippedFiles,
      referenceLimitations: code.limitations,
    },
    postgresSnapshot: snapshot ? {
      status: snapshot.status, evidenceKind: snapshot.evidenceKind,
      snapshotSha256: snapshot.snapshotSha256, indexedFiles: snapshot.files.length,
      indexedObjects: snapshot.objects.length, limitations: snapshot.limitations,
      absenceMeans: snapshot.absenceMeans,
    } : { status: "NOT_SUPPLIED", absenceMeans: "unknown" },
    oracleDdl: {
      status: config.oracleDdlFolder ? "PROVIDED_NOT_INDEXED" : "NOT_SUPPLIED",
      purpose: "Optional source behavior evidence; inspect only matched objects during conversion.",
    },
    objectMatches,
    recommendedTaskSkills: scopeStatus === "MIGRATION_REQUIRED"
      ? [...languages].sort().map((language) => MODERNIZE_SKILLS[language]) : [],
    validation: {
      mode: "static-source-and-optional-ddl-snapshot",
      build: "not-run", tests: "not-run", coverage: "not-measured",
      docker: "not-run", liveTargetContract: "not-verified",
      liveTargetRuntime: "unsupported", persistentWrites: "unsupported", mcpCalls: 0,
    },
    elapsedMs: budget.elapsedMs(),
  };
  budget.check();
  if (Buffer.byteLength(JSON.stringify(context)) > 2_000_000) {
    throw integrationError("Offline context exceeds its size budget; narrow the application or DDL scope.");
  }
  return context;
}

export function adapterOverrides(args) {
  return Object.fromEntries(["postgres-ddl-folder", "oracle-ddl-folder", "oracle-schema"]
    .filter((key) => Object.hasOwn(args, `--${key}`)).map((key) => [key, args[`--${key}`]]));
}

if (isMain(import.meta.url)) {
  await runCli(async (signal) => {
    const args = parseArguments(process.argv.slice(2), [
      "--workspace-path", "--output-dir", "--postgres-ddl-folder", "--oracle-ddl-folder", "--oracle-schema",
    ], ["--workspace-path"]);
    const context = await collectModernizeContext(args["--workspace-path"], {
      overrides: adapterOverrides(args), signal,
    });
    const json = `${JSON.stringify(context, null, 2)}\n`;
    if (args["--output-dir"]) {
      const workspace = path.resolve(args["--workspace-path"]);
      const output = path.resolve(workspace, args["--output-dir"]);
      const hostRoot = path.join(workspace, ".github", "modernize");
      if (!isWithin(hostRoot, output) || isWithin(output, hostRoot)) {
        throw integrationError("Task evidence must be written beneath the Modernize plan work folder.");
      }
      const config = await resolveModernizeConfig(workspace, adapterOverrides(args));
      if (config.configSha256 !== context.configuration.sha256) {
        throw integrationError("Configuration changed during context collection; retry.");
      }
      await validateOutputDirectory(workspace, output, [config.postgresDdlFolder, config.oracleDdlFolder]);
      await writeNewText(output, "offline-context.json", json);
    } else process.stdout.write(json);
  });
}
