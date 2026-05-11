# Mod Option Descriptions Audit

This document records the audit of in-game mod option descriptions.

## Localization Path

The live options UI localization path is `Locale.json`, loaded from `Mod.OnLoad`
through `LocaleHelper(modName + ".Locale.json")`.

The older `Resources/Localisations/*.json` plus `LocalisationUtils` path still
exists in the repository, but it is not the path used by `Mod.OnLoad`.
Translation work should therefore target `Locale.json` and any future sibling
`Locale/*.json` files unless the localization system is intentionally
refactored.

## Description Key Convention

The Cities II options UI expects descriptions under keys shaped like:

```text
Options.OPTION_DESCRIPTION[...Settings.<OptionName>]
```

Many inherited entries used legacy `.tooltip` keys:

```text
Options.OPTION[...Settings.<OptionName>].tooltip
```

Those keys are kept for compatibility/readability, but they are not sufficient
for the current options hover description panel.

## Completed Fix

`Locale.json` now provides `Options.OPTION_DESCRIPTION[...]` entries for every
visible option in `Settings.cs`:

- `m_LocaleOption`
- `m_CompatibilityModeOption`
- `m_DefaultSplitPhasing`
- `m_DefaultAlwaysGreenKerbsideTurn`
- `m_DefaultExclusivePedestrian`
- `m_ShowTramSignalPriorityDiagnostics`
- `m_ForceNodeUpdate`
- `m_ComponentTypeToClear`
- `m_ClearSelectedComponent`
- `m_ReleaseChannel`
- `m_TleVersion`
- `m_LaneSystemVersion`
- `m_SuppressCanaryWarning`
- `m_MainPanelToggleKeyboardBinding`
- `m_MultiSelectEntityKeyboardBinding`
- `m_ResetBindings`

The Node test suite checks that each visible option has a label and non-empty
description, so new visible settings should fail tests until their hover text is
added.

## Translation Behavior

This fix only adds base static locale strings. Existing translation workflow can
carry those keys into other languages later. English fallback strings are
already common in inherited locale files, so this audit does not hand-translate
non-English strings.

## Follow-Up Work

Useful follow-ups:

- decide whether to remove legacy `.tooltip` keys after verifying no supported
  game version reads them
- reconcile the duplicate `Locale.json` and `Resources/Localisations` systems
- document the Crowdin/source-of-truth workflow for future localization edits
