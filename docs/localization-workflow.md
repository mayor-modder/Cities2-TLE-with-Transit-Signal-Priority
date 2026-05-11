# Localization Workflow

This document records the current localization ownership model for TLE
Extended. It is maintainer-facing: use it before adding option labels,
descriptions, tooltips, or translated UI text.

## Live Source Of Truth

`TrafficLightsEnhancement/Locale.json` is the live source of truth for game
localization entries in the current codebase.

`Mod.OnLoad()` registers dictionaries from
`new LocaleHelper(modName + ".Locale.json").GetAvailableLanguages()`. The
project embeds:

- `Locale.json`
- optional sibling locale files under `TrafficLightsEnhancement/Locale/*.json`

`LocaleHelper` loads the base `Locale.json` as `en-US` and then scans embedded
resources whose names share the same base name. No sibling locale files exist at
the moment, so the current shipped dictionary is the base English file.

`Locale.json` owns these live key families:

- `Options.SECTION`, `Options.TAB`, and `Options.GROUP`
- `Options.OPTION[...]` labels
- `Options.OPTION_DESCRIPTION[...]` hover descriptions
- `Options.WARNING[...]` warning text
- `Tooltip.LABEL[...]`
- `UI.LABEL[...]`

Current React UI components call Cities II's `useLocalization()` and translate
`UI.LABEL[...]` or `Tooltip.LABEL[...]` keys from the game localization manager.
They do not use the older TypeScript `getString()` helper as their primary text
source.

## Option Labels And Descriptions

For every visible setting in `Settings.cs`, add or maintain both:

```text
Options.OPTION[C2VM.TrafficLightsEnhancement.C2VM.TrafficLightsEnhancement.Mod.Settings.<OptionName>]
Options.OPTION_DESCRIPTION[C2VM.TrafficLightsEnhancement.C2VM.TrafficLightsEnhancement.Mod.Settings.<OptionName>]
```

`Options.OPTION_DESCRIPTION[...]` is required for the current options hover
description panel. Legacy `.tooltip` keys are not sufficient for that panel.

The Node test suite checks the visible options list against `Locale.json`.
Whenever a new visible option is added, update the test list if needed and add a
non-empty base description.

## Legacy `.tooltip` Keys

Many inherited entries also include:

```text
Options.OPTION[...Settings.<OptionName>].tooltip
```

Do not remove these keys in drive-by localization work. They appear to be legacy
fallbacks rather than the current hover-description path, but removal should
wait for an explicit supported-game-version check and an in-game smoke test that
confirms no supported options UI still reads them.

New visible settings must have `Options.OPTION_DESCRIPTION[...]`. Adding a
matching `.tooltip` key is acceptable for consistency, but it should not be
treated as the authoritative description.

## Legacy Resource Localisations

`TrafficLightsEnhancement/Resources/Localisations/*.json` and
`TrafficLightsEnhancement/Utils/LocalisationUtils.cs` are still present and
embedded through `Resources/**/*`, but they are not the active path registered by
`Mod.OnLoad()`.

Current code search shows no production caller constructing `LocalisationUtils`
or calling `AddToDictionary()`. The class still points at embedded resources
named like:

```text
C2VM.TrafficLightsEnhancement.Resources.Localisations.<locale>.json
```

Treat this path as a cleanup candidate, not a translation source of truth.
Removing it should be a focused PR that deletes the unused class/resources,
updates build resources, and verifies mod options plus in-game UI labels in a
supported game build.

## TypeScript Localisation Files

`TrafficLightsEnhancement/UI/src/mods/localisations/*.ts` contains inherited
fallback dictionaries and a `getString()` helper. Current UI components mostly
translate through Cities II's localization manager instead, using keys supplied
by `Locale.json`.

Do not add new user-facing strings only to the TypeScript locale files. Add live
keys to `Locale.json` first. If a future UI path intentionally returns to the
TypeScript dictionaries, document that decision and add tests that prove those
files are used.

## Translation Workflow

Base strings should be written in `Locale.json`. Non-English strings should flow
through the project's translation workflow rather than ad-hoc hand translation
during feature work.

The current `crowdin.yml` lists `Locale*.json` as the source pattern. Before
turning on broad translation updates, verify that Crowdin output lands in the
embedded sibling-locale location expected by the project:

```text
TrafficLightsEnhancement/Locale/*.json
```

English fallback strings are already common in inherited locale files. Do not
block feature or hardening work on hand-translating every locale, but keep new
base strings clear and stable so they are ready for Crowdin.
