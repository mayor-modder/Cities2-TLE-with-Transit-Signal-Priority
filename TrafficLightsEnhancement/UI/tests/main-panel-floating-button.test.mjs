import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";

const rootDir = process.cwd();
const mainPanelPath = join(rootDir, "src", "mods", "components", "main-panel", "index.tsx");
const mainPanelSource = readFileSync(mainPanelPath, "utf8");

test("main panel uses the custom floating button click path", () => {
  assert.match(
    mainPanelSource,
    /import FloatingButton from ['"]\.\.\/\.\.\/components\/common\/floating-button['"];/
  );
  assert.match(
    mainPanelSource,
    /<FloatingButton[\s\S]*show=\{showFloatingButton\}[\s\S]*onClick=\{floatingButtonClickHandler\}/
  );
  assert.doesNotMatch(
    mainPanelSource,
    /<Button[\s\S]*variant=['"]floating['"]/
  );
});
