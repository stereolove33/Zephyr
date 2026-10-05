import test from "node:test";
import assert from "node:assert/strict";

const messages = [];
let receive;
globalThis.chrome = { webview: {
  addEventListener(name, callback) { assert.equal(name, "message"); receive = callback; },
  postMessage(message) { messages.push(message); },
} };
const bridge = await import("./bridge.mjs");

test("overlapping native replies resolve the matching requests", async () => {
  const first = bridge.invoke("plugin:injector|official_status");
  const second = bridge.invoke("get_patcher_status");
  const a = messages.at(-2), b = messages.at(-1);
  receive({ data: { id: b.id, result: { ok: true, value: { phase: "building" } } } });
  receive({ data: { id: a.id, result: { ok: true, value: { running: false } } } });
  assert.deepEqual(await first, { ok: true, value: { running: false } });
  assert.deepEqual(await second, { ok: true, value: { phase: "building" } });
});

test("native failures survive the adapter instead of becoming success", async () => {
  const result = bridge.invoke("start_patcher");
  receive({ data: { id: messages.at(-1).id, result: { ok: false, error: "overlay failed" } } });
  assert.deepEqual(await result, { ok: false, error: "overlay failed" });
});

test("file dialog cancellation and multiple paths remain distinct", async () => {
  const cancelled = bridge.open({ multiple: true });
  receive({ data: { id: messages.at(-1).id, result: { ok: true, value: null } } });
  assert.equal(await cancelled, null);
  const files = bridge.open({ multiple: true });
  receive({ data: { id: messages.at(-1).id, result: { ok: true, value: ["D:\\a.modpkg", "D:\\b.fantome"] } } });
  assert.deepEqual(await files, ["D:\\a.modpkg", "D:\\b.fantome"]);
});

test("patcher errors reach listeners until they unsubscribe", async () => {
  const events = [];
  const remove = await bridge.listen("patcher-error", event => events.push(event.payload));
  receive({ data: { event: "patcher-error", payload: "host failed" } });
  remove();
  receive({ data: { event: "patcher-error", payload: "second" } });
  assert.deepEqual(events, ["host failed"]);
});
