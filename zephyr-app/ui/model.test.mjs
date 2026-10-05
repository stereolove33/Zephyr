import assert from "node:assert/strict";
import test from "node:test";
import { unwrap, needsCustomStart, officialLabel, stopAllLoaders, toggleLoaders } from "./model.mjs";

test("an IPC failure is never treated as successful loading", () => {
  assert.throws(() => unwrap({ ok: false, error: { code: "access_denied" } }), /access_denied/);
  assert.throws(() => unwrap(undefined), /Unexpected loader response/);
  assert.deepEqual(unwrap({ ok: true, value: { running: true } }), { running: true });
});
test("Start prepares selected customs before the official loader", () => {
  assert.equal(needsCustomStart([{ enabled: true }], { running: false }), true);
  assert.equal(needsCustomStart([{ enabled: false }], { running: false }), false);
  assert.equal(needsCustomStart([{ enabled: true }], { running: true }), false);
});
test("an alive loader can still have failed its injection", () => {
  assert.equal(officialLabel("ERROR|OpenProcess failed", true), "Injection failed");
  assert.equal(officialLabel("WAITING|Game", true), "Waiting for game");
  assert.equal(officialLabel("LOADED|DLL", true), "DLL loaded");
  assert.equal(officialLabel("", false), "Stopped");
});

test("stopping still attempts the official loader when custom stop fails", async () => {
  const calls = [];
  await assert.rejects(stopAllLoaders(async command => {
    calls.push(command);
    if (command === "stop_patcher") throw new Error("custom stop failed");
  }), /custom stop failed/);
  assert.deepEqual(calls, ["stop_patcher", "plugin:injector|stop_official"]);
});

test("primary action starts the official loader even when customs are already active", async () => {
  for (const [official, custom] of [[true, false], [false, true], [true, true], [false, false]]) {
    const calls = [];
    await toggleLoaders({ running: official }, { running: custom }, async () => calls.push("start"), async () => calls.push("stop"));
    assert.deepEqual(calls, [official ? "stop" : "start"]);
  }
});
