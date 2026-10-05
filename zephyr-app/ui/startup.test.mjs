import test from "node:test";
import assert from "node:assert/strict";
import { createStartupTransition } from "./startup.mjs";

test("fast startup holds the same logo for 1.5 seconds before fading", () => {
  let clock = 0, queued, faded = 0;
  const transition = createStartupTransition({
    now: () => clock, schedule: (callback, delay) => { queued = { callback, delay }; },
    reveal: () => faded++,
  });
  clock = 500;
  transition.handedOver(500);
  clock = 700;
  transition.finish();
  assert.equal(queued.delay, 800);
  assert.equal(faded, 0);
  queued.callback();
  assert.equal(faded, 1);
});

test("slow startup waits for readiness without restarting the 1.5 second hold", () => {
  let delay;
  const transition = createStartupTransition({ now: () => 2400, schedule: (_, value) => { delay = value; }, reveal() {} });
  transition.handedOver(2400);
  transition.finish();
  assert.equal(delay, 0);
});

test("repeated readiness signals never restart the splash or its fade", () => {
  let scheduled = 0;
  const transition = createStartupTransition({ now: () => 0, schedule: () => scheduled++, reveal() {} });
  transition.finish();
  transition.finish();
  assert.equal(scheduled, 1);
});
