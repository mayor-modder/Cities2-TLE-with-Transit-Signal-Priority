import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const source = (path) => readFile(new URL(`../${path}`, import.meta.url), "utf8");
const repoSource = (path) => readFile(new URL(`../../${path}`, import.meta.url), "utf8");

test("main panel data exposes tram signal priority state", async () => {
  const general = await source("src/mods/general.ts");

  assert.match(general, /tramSignalPriority\?\s*:\s*\{/);
  assert.match(general, /isVisible:\s*boolean/);
  assert.match(general, /isEnabled:\s*boolean/);
  assert.match(general, /isEditable:\s*boolean/);
  assert.match(general, /statusLabel\?:\s*string/);
  assert.match(general, /diagnostics\?:\s*\{/);
  assert.match(general, /summary\?:\s*\{\s*label:\s*string,\s*value:\s*string\s*\}/);
  assert.match(general, /events\?:\s*Array<\{\s*sequence:\s*number,\s*label:\s*string,\s*value:\s*string\s*\}>/);
  assert.match(general, /rows:\s*Array<\{\s*label:\s*string,\s*value:\s*string\s*\}>/);
});

test("bindings exposes the tram signal priority toggle trigger", async () => {
  const bindings = await source("src/bindings.ts");

  assert.match(bindings, /toggleTramSignalPriority\s*=\s*triggers\.create<\[boolean\]>\("ToggleTramSignalPriority"\)/);
});

test("main panel renders only tram signal priority controls", async () => {
  const content = await source("src/mods/components/main-panel/content.tsx");
  const panelStart = content.indexOf("TramSignalPriority");

  assert.notEqual(panelStart, -1);
  const panelEnd = content.indexOf("{mainData.hasLaneDirectionTool", panelStart);
  const panelSource = panelEnd === -1 ? content.slice(panelStart) : content.slice(panelStart, panelEnd);

  assert.match(panelSource, /EnableTramSignalPriority/);
  assert.match(panelSource, /TramSignalPriorityDiagnostics/);
  assert.doesNotMatch(panelSource, /bus/i);
  assert.doesNotMatch(panelSource, /source/i);
  assert.doesNotMatch(panelSource, /public[-\s]?car|publicCar/i);
});

test("tram signal priority diagnostics are gated by a mod option", async () => {
  const settings = await repoSource("Settings.cs");
  const uiBindings = await repoSource("Systems/UI/UISystem.UIBIndings.cs");
  const uiSystem = await repoSource("Systems/UI/UISystem.cs");
  const locale = await repoSource("Locale.json");
  const content = await source("src/mods/components/main-panel/content.tsx");

  assert.match(settings, /public\s+bool\s+m_ShowTramSignalPriorityDiagnostics\s*\{\s*get;\s*set;\s*\}/);
  assert.match(settings, /m_ShowTramSignalPriorityDiagnostics\s*=\s*false/);
  assert.match(uiBindings, /m_ShowTramSignalPriorityDiagnostics\s*\?\s*GetTramSignalPriorityDiagnostics\(m_SelectedEntity,\s*tspSettings\)/);
  assert.match(uiBindings, /diagnostics\s*=\s*tspDiagnostics/);
  assert.match(uiSystem, /ShouldRefreshMainPanelForDiagnostics\(\)/);
  assert.match(uiSystem, /m_MainPanelState\s*==\s*MainPanelState\.Main/);
  assert.match(content, /mainData\.tramSignalPriority\.diagnostics/);
  assert.match(content, /mainData\.tramSignalPriority\.diagnostics\.summary/);
  assert.match(content, /mainData\.tramSignalPriority\.diagnostics\.events/);
  assert.match(locale, /TSPDiagnosticsRequest/);
  assert.match(locale, /TSPDiagnosticsCurrentGroup/);
  assert.match(locale, /TSPDiagnosticsCurveApproach/);
  assert.match(locale, /TSPDiagnosticsDecision/);
});

test("backend provides tram signal priority summary and event history", async () => {
  const uiBindings = await repoSource("Systems/UI/UISystem.UIBIndings.cs");
  const uiSystem = await repoSource("Systems/UI/UISystem.cs");
  const locale = await repoSource("Locale.json");

  assert.match(uiBindings, /GetTspDiagnosticsSummary/);
  assert.match(uiBindings, /GetTspDiagnosticsEvents/);
  assert.match(uiBindings, /ShouldRecordTspDiagnosticsEvent/);
  assert.match(uiBindings, /RecordTspDiagnosticsEvent/);
  assert.match(uiBindings, /summary\s*=\s*GetTspDiagnosticsSummary/);
  assert.match(uiBindings, /events\s*=\s*GetTspDiagnosticsEvents/);
  assert.match(uiSystem, /m_TspDiagnosticsEvents/);
  assert.match(locale, /TSPDiagnosticsSummary/);
  assert.match(locale, /TSPDiagnosticsEvents/);
});

test("backend writes selected tram signal priority diagnostics to a trace file", async () => {
  const uiBindings = await repoSource("Systems/UI/UISystem.UIBIndings.cs");

  assert.match(uiBindings, /TspDiagnosticsTraceFileName/);
  assert.match(uiBindings, /C2VM\.TrafficLightsEnhancement\.TspDiagnostics\.jsonl/);
  assert.match(uiBindings, /WriteTspDiagnosticsTraceEvent/);
  assert.match(uiBindings, /Application\.persistentDataPath/);
  assert.match(uiBindings, /TspDiagnosticsTraceFileLock/);
  assert.match(uiBindings, /RotateTspDiagnosticsTraceFileIfNeeded/);
  assert.match(uiBindings, /TspDiagnosticsTraceMaxRotatedFiles/);
  assert.match(uiBindings, /PruneTspDiagnosticsTraceFiles/);
  assert.match(uiBindings, /FileMode\.Append/);
});

test("backend trace writes follow selected diagnostics event filtering", async () => {
  const uiBindings = await repoSource("Systems/UI/UISystem.UIBIndings.cs");
  const eventsStart = uiBindings.indexOf("private ArrayList GetTspDiagnosticsEvents");
  const eventsEnd = uiBindings.indexOf("private void PruneTspDiagnosticsEvents", eventsStart);
  const eventsSource = uiBindings.slice(eventsStart, eventsEnd);

  assert.notEqual(eventsStart, -1);
  assert.notEqual(eventsEnd, -1);
  assert.match(eventsSource, /bool\s+shouldRecordEvent\s*=\s*signatureChanged\s*&&\s*ShouldRecordTspDiagnosticsEvent\(history,\s*hasRuntimeDebug\s*\|\|\s*hasDecisionTrace\)/);
  assert.match(eventsSource, /if\s*\(\s*signatureChanged\s*\)/);
  assert.match(eventsSource, /if\s*\(\s*shouldRecordEvent\s*\)/);
  assert.ok(eventsSource.indexOf("bool shouldRecordEvent") < eventsSource.indexOf("WriteTspDiagnosticsTraceEvent"));
  assert.ok(eventsSource.indexOf("bool shouldRecordEvent") < eventsSource.indexOf("RecordTspDiagnosticsEvent"));
});

test("static locale provides descriptions for visible mod options", async () => {
  const locale = JSON.parse(await repoSource("Locale.json"));
  const optionPrefix =
    "Options.OPTION[C2VM.TrafficLightsEnhancement.C2VM.TrafficLightsEnhancement.Mod.Settings.";
  const descriptionPrefix =
    "Options.OPTION_DESCRIPTION[C2VM.TrafficLightsEnhancement.C2VM.TrafficLightsEnhancement.Mod.Settings.";
  const visibleOptions = [
    "m_LocaleOption",
    "m_CompatibilityModeOption",
    "m_DefaultSplitPhasing",
    "m_DefaultAlwaysGreenKerbsideTurn",
    "m_DefaultExclusivePedestrian",
    "m_ShowTramSignalPriorityDiagnostics",
    "m_ForceNodeUpdate",
    "m_ComponentTypeToClear",
    "m_ClearSelectedComponent",
    "m_ReleaseChannel",
    "m_TleVersion",
    "m_LaneSystemVersion",
    "m_SuppressCanaryWarning",
    "m_MainPanelToggleKeyboardBinding",
    "m_MultiSelectEntityKeyboardBinding",
    "m_ResetBindings",
  ];

  for (const option of visibleOptions) {
    const optionKey = `${optionPrefix}${option}]`;
    const descriptionKey = `${descriptionPrefix}${option}]`;

    assert.equal(typeof locale[optionKey], "string", `${option} needs a label`);
    assert.equal(typeof locale[descriptionKey], "string", `${option} needs a description`);
    assert.notEqual(locale[descriptionKey].trim(), "", `${option} description cannot be empty`);
    assert.doesNotMatch(locale[descriptionKey], /^Options\.OPTION_DESCRIPTION/, `${option} description cannot be a raw localization key`);
  }
});

test("custom phase vehicle weights expose bicycle weight control", async () => {
  const subPanel = await source("src/mods/components/custom-phase-tool/main-panel/sub-panel.tsx");
  const locale = JSON.parse(await repoSource("Locale.json"));

  assert.match(subPanel, /keyName="BicycleWeight"/);
  assert.match(subPanel, /label="BicycleWeight"/);
  assert.match(subPanel, /value=\{data\.bicycleWeight\}/);
  assert.match(subPanel, /Tooltip\.LABEL\[C2VM\.TrafficLightsEnhancement\.BicycleWeight\]/);
  assert.equal(locale["UI.LABEL[C2VM.TrafficLightsEnhancement.BicycleWeight]"], "Bicycle Weight");
  assert.equal(
    typeof locale["Tooltip.LABEL[C2VM.TrafficLightsEnhancement.BicycleWeight]"],
    "string");
});

test("backend toggle removes tram signal priority settings when disabled", async () => {
  const uiBindings = await repoSource("Systems/UI/UISystem.UIBIndings.cs");
  const toggleStart = uiBindings.indexOf("protected void ToggleTramSignalPriority(bool enabled)");

  assert.notEqual(toggleStart, -1);
  const toggleEnd = uiBindings.indexOf("protected void CallMainPanelUpdatePosition", toggleStart);
  const toggleSource = toggleEnd === -1 ? uiBindings.slice(toggleStart) : uiBindings.slice(toggleStart, toggleEnd);

  assert.match(toggleSource, /if\s*\(!enabled\)/);
  assert.match(toggleSource, /EntityManager\.RemoveComponent<TransitSignalPrioritySettings>\(m_SelectedEntity\)/);
  assert.match(toggleSource, /settings\.m_Enabled\s*=\s*true/);
});

test("tool removal clears tram signal priority runtime components", async () => {
  const toolSystem = await repoSource("Systems/Tool/ToolSystem.cs");
  const helperStart = toolSystem.indexOf("private void RemoveTransitSignalPriorityComponents(Entity entity)");

  assert.notEqual(helperStart, -1);
  const helperEnd = toolSystem.indexOf("private void", helperStart + 1);
  const helperSource = helperEnd === -1 ? toolSystem.slice(helperStart) : toolSystem.slice(helperStart, helperEnd);

  assert.match(helperSource, /RemoveComponent<TransitSignalPrioritySettings>/);
  assert.match(helperSource, /RemoveComponent<TransitSignalPriorityRequest>/);
  assert.match(helperSource, /RemoveComponent<TransitSignalPriorityRuntimeDebugInfo>/);
  assert.match(helperSource, /RemoveComponent<TransitSignalPriorityDecisionTrace>/);

  const removalStart = toolSystem.indexOf("EntityManager.RemoveComponent<CustomTrafficLights>(m_RaycastResult)");
  const removalEnd = toolSystem.indexOf("EntityManager.AddComponentData(m_RaycastResult", removalStart);
  const removalSource = removalEnd === -1 ? toolSystem.slice(removalStart) : toolSystem.slice(removalStart, removalEnd);

  assert.match(removalSource, /RemoveTransitSignalPriorityComponents\(m_RaycastResult\)/);
});

test("tram signal priority settings reserve public car priority without persisting group propagation", async () => {
  const settings = await repoSource("Components/TransitSignalPrioritySettings.cs");
  const normalizeStart = settings.indexOf("public void Normalize()");
  const serializeStart = settings.indexOf("public void Serialize");
  const deserializeStart = settings.indexOf("public void Deserialize");

  assert.notEqual(normalizeStart, -1);
  assert.notEqual(serializeStart, -1);
  assert.notEqual(deserializeStart, -1);

  const normalizeSource = settings.slice(normalizeStart, serializeStart);
  const serializeSource = settings.slice(serializeStart, deserializeStart);
  const deserializeSource = settings.slice(deserializeStart);

  assert.doesNotMatch(normalizeSource, /m_AllowPublicCarRequests\s*=/);
  assert.match(serializeSource, /writer\.Write\(2\)/);
  assert.match(serializeSource, /writer\.Write\(m_AllowPublicCarRequests\)/);
  assert.doesNotMatch(serializeSource, /m_AllowGroupPropagation/);
  assert.match(deserializeSource, /if\s*\(version\s*==\s*1\)/);
  assert.doesNotMatch(deserializeSource, /reader\.Read\(out m_AllowGroupPropagation\)/);
});
