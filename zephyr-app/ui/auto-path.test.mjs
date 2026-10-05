import test from "node:test";
import assert from "node:assert/strict";
import { detectFirstLeaguePath } from "./auto-path.mjs";

test("first launch detects and saves the game folder without changing preferences", async () => {
  let saved, marked = 0;
  const initial = { leaguePath: null, autoRun: false, minimizeToTray: true };
  const result = await detectFirstLeaguePath(initial, {
    attempted: false, markAttempted: () => marked++,
    detect: async () => "D:\\Riot Games\\League of Legends",
    save: async settings => { saved = settings; },
  });
  assert.equal(result.state, "detected");
  assert.equal(marked, 1);
  assert.deepEqual(saved, { ...initial, leaguePath: "D:\\Riot Games\\League of Legends" });
  assert.equal(initial.leaguePath, null);
});

test("an existing game folder is never overwritten", async () => {
  const settings = { leaguePath: "C:\\custom\\League", autoRun: true };
  const result = await detectFirstLeaguePath(settings, {
    attempted: false, markAttempted: () => assert.fail("Must not mark"),
    detect: async () => assert.fail("Must not detect"), save: async () => assert.fail("Must not save"),
  });
  assert.equal(result.state, "configured");
  assert.equal(result.settings, settings);
});

test("automatic detection runs only once", async () => {
  const result = await detectFirstLeaguePath({ leaguePath: null }, {
    attempted: true, detect: async () => assert.fail("Must not detect"),
  });
  assert.equal(result.state, "skipped");
});

test("a missing installation leaves manual setup available", async () => {
  const result = await detectFirstLeaguePath({ leaguePath: null }, {
    attempted: false, markAttempted() {}, detect: async () => null,
    save: async () => assert.fail("Must not save"),
  });
  assert.equal(result.state, "not-found");
  assert.equal(result.settings.leaguePath, null);
});

test("detection or save failures do not prevent startup", async () => {
  for (const failing of ["detect", "save"]) {
    const result = await detectFirstLeaguePath({ leaguePath: null }, {
      attempted: false, markAttempted() {},
      detect: async () => { if (failing === "detect") throw new Error("Unavailable"); return "C:\\League"; },
      save: async () => { throw new Error("Unavailable"); },
    });
    assert.equal(result.state, "failed");
    assert.equal(result.settings.leaguePath, null);
  }
});
