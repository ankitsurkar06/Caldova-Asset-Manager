---
name: validate-rulebook-compliance
description: Validate rulebook compliance by mapping rulebook rules to plan tasks
---

# Validate Rulebook Compliance

Map each rulebook rule to the tasks in the modernization plan and produce a compliance summary.

## User Input

- tasks-json-path (Mandatory): Path to the tasks.json file
- compliance-output-path (Mandatory): Path to write the compliance markdown summary

## Workflow

1. Read the tasks from ${tasks-json-path}
2. Read the rulebook files provided as attachments
3. For each rulebook rule, determine its status:
   - **COVERED**: at least one task addresses the rule and its approach is consistent with the rule
   - **VIOLATED**: a task's approach conflicts with the rule — for example, it targets a service, framework version or library that `targets.md` does not approve, introduces a technology or pattern that `policies.md` prohibits, or omits an element that `policies.md` requires. Quote the conflicting task detail in the Task column.
   - **NOT COVERED**: no task addresses the rule and no task conflicts with it
4. Write a markdown summary to ${compliance-output-path} with the following format:

## Rulebook Compliance

| Rulebook | Rule | Status | Task |
|----------|------|--------|------|
| targets.md | brief rule summary | ✅ COVERED | 001 |
| targets.md | another rule | ⛔ VIOLATED | 003 — targets Azure Service Bus, not in approved services |
| policies.md | another rule | ❌ NOT COVERED | - |

**COVERED: X/Y  ·  VIOLATED: V/Y  ·  NOT COVERED: Z/Y**

Include all rules from all rulebook files. A VIOLATED status takes precedence over COVERED when the same rule is both addressed by one task and conflicted by another. Only write the markdown file, do not modify any other files.
