---
name: documentation-updater
description: Update the project's appropriate documentation when the user says "update documentation" or asks to document confirmed project changes. Verify every new or changed factual claim against source code, configuration, tests, direct tool output, or an explicit user requirement; never turn an inference into a fact.
---

# Documentation updater

Use this skill when the user asks to update documentation, especially with the
phrase "update documentation."

## Workflow

1. Inspect the current project instructions, documentation index, and relevant
   source files, tests, configuration, or other primary evidence for the
   requested change. Read the relevant existing documents before editing.
2. Determine which documentation files are actually affected. Update every
   relevant existing document, not just the index or the first matching file.
   Create a new document only when the subject does not fit an existing focused
   document.
3. Ground every new or changed factual statement in direct evidence:
   - Source code, tests, configuration, or checked-in data establish what the
     project implements.
   - Direct build, test, runtime, or inspection output establishes only what
     that result directly demonstrates.
   - An explicit user requirement establishes intended behavior, not proof that
     the behavior is implemented.
4. Keep implementation facts, requirements, design proposals, and investigation
   status distinct. Preserve useful existing `CONFIRMED`, `LIKELY`, and
   `UNKNOWN` labels. Do not promote a hypothesis, suggestive name, API
   signature, or absence of evidence into a confirmed behavior. Do not add
   unsupported claims to make the documents sound complete.
5. If a requested factual update cannot be verified, investigate further when
   practical. Otherwise leave the factual documentation unchanged and state
   what evidence is missing; do not guess.
6. Keep the documentation structure coherent. Put requirements and intended
   behavior in `docs/MODDING_REQUIREMENTS.md`, game type/API facts in
   `docs/GAME_DATA_REFERENCE.md`, UI investigation in
   `docs/UI_INTEGRATION_NOTES.md`, and dated probes, evidence grades, and open
   reverse-engineering questions in `docs/REVERSE_ENGINEERING_LOG.md`. Keep
   `docs/MODDING_CONTEXT.md` as the concise entry point and update its summary
   or links when the project's current confirmed conclusions change.
7. Update links and neighboring summaries when moving or changing material.
   Use repository-relative Markdown links between documents.
8. Review the final diff against the evidence and check that edited Markdown
   links resolve. Run documentation-specific checks if the repository provides
   them; otherwise, no build or test is needed for documentation-only edits.

## Output

Briefly report which documentation files changed, the confirmed information
added or corrected, and any requested facts left undocumented because evidence
was unavailable. Do not claim verification beyond the checks actually run.
