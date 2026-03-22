# README Fork Rewrite Design

## Goal

Replace the inherited upstream-oriented `README.md` with a fork-first document for `Cities2-TLE-TSP`.

## Context

The current README still reads like the original `Traffic Lights Enhancement` project page. That makes it unclear which behavior belongs to upstream TLE and which behavior was added in this fork. The most important fork-specific addition is Transit Signal Priority (TSP), and the README should describe it precisely without overstating what has been validated.

## Design Decisions

1. Lead with fork identity.
   The README should immediately state that this repository is a fork, name the upstream repository, and explain that this fork periodically incorporates upstream fixes rather than contributing directly back.

2. Document confirmed fork behavior only.
   Claims should be grounded in the current repository state: code, UI bindings, tests, and recent fork commits. If a behavior is inherited from upstream but not re-verified here, the README should either omit it or label it as inherited context rather than a confirmed fork-specific guarantee.

3. Split TSP documentation into confirmed behavior and uncertainty.
   The TSP section should explain what the code clearly implements, then explicitly call out what is still uncertain, under-validated, or not yet exposed in the UI.

4. Keep upstream docs as references, not the main story.
   Existing files like `GUIDE.md` and `BUILD.md` can be referenced when still useful, but the root README should stand on its own as the authoritative overview for this fork.

## Target README Structure

1. Title and fork summary
2. Status / upstream relationship
3. What this fork changes
4. Transit Signal Priority
   - Confirmed behavior
   - How it works
   - Known limits and uncertainty
5. Build / installation notes
6. References to inherited upstream docs
7. Acknowledgements

## Content Rules

* Prefer exact descriptions over marketing language
* Name uncertainty explicitly
* Do not imply this fork is distributed through the same channels as upstream unless verified
* Keep warnings that still apply, but rewrite them so they describe this fork rather than the original project
