# Localization Resource Audit

This audit maps the inherited localization/resource paths in TLE Extended so
future cleanup does not remove a path that is still used by the game, the React
UI, or a translation workflow.

## Current Runtime Path

`TrafficLightsEnhancement/Locale.json` is the active game-side localization
source.

`Mod.OnLoad()` registers:

```csharp
new LocaleHelper(modName + ".Locale.json").GetAvailableLanguages()
```

The project file embeds `Locale.json` and optional sibling files under
`TrafficLightsEnhancement/Locale/*.json`. In the current tree there are no
`Locale/*.json` sibling files, so the shipped game dictionary is the base
English `Locale.json` dictionary registered as `en-US`.

The active React UI also translates through Cities II's localization manager.
Source search shows UI components calling `useLocalization()` and translating
`UI.LABEL[...]` or `Tooltip.LABEL[...]` keys. New in-game UI text should
therefore be added to `Locale.json` first.

## Resource Inventory

| Resource surface | Loader | Current status | Notes |
| --- | --- | --- | --- |
| `TrafficLightsEnhancement/Locale.json` | `LocaleHelper` from `Mod.OnLoad()` | Active | Owns option labels, option descriptions, warnings, tooltips, and UI labels. |
| `TrafficLightsEnhancement/Locale/*.json` | `LocaleHelper` resource scan | Supported but currently absent | This is the expected place for embedded sibling dictionaries if the translation pipeline emits them. |
| `TrafficLightsEnhancement/Resources/Localisations/*.json` | `LocalisationUtils` | Embedded, but no production call site found | These inherited dictionaries are still packaged by `EmbeddedResource Include="Resources\**\*"`. Treat as legacy until a focused removal proves the package and Crowdin workflow do not need them. |
| `TrafficLightsEnhancement/Utils/LocalisationUtils.cs` | Direct construction only | No production caller found | Code search found the class and methods only in their own file. It can populate a `LocalizationDictionary`, but `Mod.OnLoad()` does not use it. |
| `TrafficLightsEnhancement/UI/src/mods/localisations/*.ts` | `mods/localisations/index.ts` | Bundled fallback dictionaries, no current consumer found outside the index | Current UI components use `useLocalization()`, not the TypeScript `getString()` helper. Do not add new strings only here. |
| `crowdin.yml` | Crowdin | Needs confirmation before broad translation work | The source pattern is `Locale*.json`; verify generated output lands where the project embeds it before relying on automated translation import/export. |

## Cleanup Opportunities

The following are plausible cleanup tasks, but none are safe as drive-by
deletions:

- Retire `LocalisationUtils` and `Resources/Localisations/*.json` after a
  focused build/package check confirms no runtime path, reflection hook, or
  translation workflow still depends on them.
- Either remove the unused TypeScript `getString()` dictionaries or give them a
  documented purpose. The current UI uses the game localization manager, so
  dual-maintaining these files is likely stale-work risk.
- Consolidate supported-locale lists. Locale identifiers are currently repeated
  in `LocaleHelper`, `LocalisationUtils`, and the TypeScript localization index.
- Add a source test that flags accidental reintroduction of UI text paths that
  bypass `Locale.json`.

## Risks

Deleting legacy resources too early could break inherited Crowdin output or a
runtime code path that is not obvious from static search. The safer sequence is:

1. Add or update tests that prove the active loader path.
2. Build the mod and inspect embedded resources.
3. Smoke test the options screen and main panel in-game.
4. Remove exactly one legacy path in a focused commit.

Until that sequence exists, `Locale.json` remains the live source of truth and
the inherited resources remain documented cleanup candidates.
