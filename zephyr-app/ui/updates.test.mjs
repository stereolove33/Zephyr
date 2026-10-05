import test from "node:test";
import assert from "node:assert/strict";
import { createUpdateController } from "./updates.mjs";

test("update notice follows language changes and opens only on user action", async () => {
  let view, language = "en", opened = 0;
  const updates = createUpdateController({
    check: async () => ({ state: "available", version: "1.0.10" }),
    openRelease: async () => { opened++; },
    translate: text => language === "en" ? text : `pt:${text}`,
    render: result => { view = result; },
  });
  await updates.check();
  assert.equal(view.available, true);
  assert.match(view.message, /1\.0\.10$/);
  assert.equal(opened, 0);
  language = "pt";
  updates.refresh();
  assert.match(view.title, /^pt:/);
  await updates.open();
  assert.equal(opened, 1);
});

test("concurrent checks share one request and clear the checking state", async () => {
  let resolve, requests = 0, view;
  const updates = createUpdateController({
    check: () => { requests++; return new Promise(done => { resolve = done; }); },
    openRelease: async () => assert.fail("No update"),
    translate: text => text, render: result => { view = result; },
  });
  const first = updates.check(), second = updates.check();
  assert.equal(requests, 1);
  assert.equal(view.checking, true);
  resolve({ state: "current" });
  await Promise.all([first, second]);
  assert.equal(view.available, false);
  assert.equal(view.checking, false);
  await updates.open();
});

test("offline checks remain non-blocking and allow retry", async () => {
  let online = false, view;
  const updates = createUpdateController({
    check: async () => { if (!online) throw new Error("Offline"); return { state: "current" }; },
    openRelease: async () => assert.fail("No update"),
    translate: text => text, render: result => { view = result; },
  });
  await updates.check();
  assert.equal(view.available, false);
  assert.match(view.message, /Try again later/);
  await updates.open();
  online = true;
  await updates.check();
  assert.equal(view.message, "You are using the latest version.");
});
