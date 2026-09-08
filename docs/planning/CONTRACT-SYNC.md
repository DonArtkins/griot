# Contract synchronization before commit or push

Run `python3 scripts/check-contract-sync.py` from the repository root on the
feature branch. It compares the complete branch and working tree to the merge
base with `main`; `--base <commit>` selects another review base. A nonzero exit
blocks commit/push. Run the owning system's build and tests separately.

## Required semantic review

For each changed route, schema field, enum, token, setting, port or service:

1. Identify the owning feature spec and approved design/amendment.
2. Compare the final implementation and meaningful test evidence to every
   acceptance criterion. Record implemented, planned or blocked status explicitly.
3. Search all seven systems, root context, planning, research, docs, agent and
   skill instructions, diagram sources, deployment examples and API collections.
4. Correct stale references and update dependent specs in this same branch.
   A link to the current contract does not justify leaving contradictory prose.
5. Update affected progress trackers and the changelog before pushing. Retain
   historical session notes as history and add corrections with current evidence.

The executable gate catches duplicate feature IDs, known stale auth contracts,
missing auth implementation/schema markers and missing branch documentation
coverage for systems with changed production code. It is a static guard; it
cannot prove arbitrary semantic equivalence or replace integration tests and
the human design approval required by AGENTS.md. Add checks for new contracts
as they are introduced. Do not claim all repository features are implemented
because the guard passes.

Feature 07's review matrix and test evidence are in
[the repair log](FEATURE-07-AUTH-REPAIR.md).
