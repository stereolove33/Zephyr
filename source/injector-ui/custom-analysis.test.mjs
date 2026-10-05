import assert from "node:assert/strict";
import test from "node:test";
import { automaticLabels, preflightIssues, healthLabel } from "./custom-analysis.mjs";

test("content-derived champion and type do not need manual labels", () => {
  assert.deepEqual(automaticLabels({ derived: { champions: ["Ahri"], tags: ["champion-skin"] } }, { categories: [] }), { champion: "Ahri", type: "Skin", champions: ["Ahri"] });
});
test("readable font paths refine a coarse UI footprint", () => {
  assert.equal(automaticLabels({ derived: { tags: ["ui"] } }, { categories: ["Font"] }).type, "Font");
});
test("mixed packages and unknown packages remain explicit", () => {
  assert.equal(automaticLabels(null, { categories: ["Font", "UI"] }).type, "Mixed");
  assert.equal(automaticLabels(null, null).type, "Unknown");
});
test("resource conflicts block even when no champion is known", () => {
  assert.equal(preflightIssues({ conflicts: [{ firstMod: "a", secondMod: "b", resource: "123" }], verdicts: [] }).blocked, true);
});
test("fatal errors block but warnings alone do not", () => {
  assert.equal(preflightIssues({ conflicts: [], verdicts: [{ modId: "a", counts: { fatals: 1, warnings: 0 } }] }).blocked, true);
  assert.equal(preflightIssues({ conflicts: [], verdicts: [{ modId: "a", counts: { warnings: 2 } }] }).blocked, false);
});
test("an absent or incomplete check is not a healthy result", () => {
  assert.equal(healthLabel(null), "Not checked");
  assert.throws(() => preflightIssues({}), /Incomplete/);
});
