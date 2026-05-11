## Summary

Describe the change in maintainer terms. What behavior, documentation, tests, or tooling changed?

## Issue

Closes #

## Scope

- [ ] Runtime behavior
- [ ] UI or localization
- [ ] Save format, serialization, or migration
- [ ] Diagnostics or trace output
- [ ] Tests or test infrastructure
- [ ] Documentation/tooling only

## Compatibility

- Does this preserve existing upstream TLE save/config compatibility?
- Does this require a save-format version change?
- Does this change behavior for users who never enable the new feature?

## Verification

Paste exact commands and results.

- [ ] `npm.cmd test`
- [ ] `dotnet test --no-restore`
- [ ] `dotnet build TrafficLightsEnhancement\TrafficLightsEnhancement.csproj -c Release -p:DisablePostProcessors=true`
- [ ] In-game smoke test, if behavior changed:

## Diagnostics and Localization

- Does this add, remove, or rename diagnostics fields?
- Does this add or change user-facing strings?
- If localization files were touched, explain why direct translation changes are appropriate.

## Notes for Reviewers

Call out anything risky, intentionally deferred, or easier to review commit-by-commit.
