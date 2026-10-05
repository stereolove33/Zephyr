export function createStartupTransition({ now = () => performance.now(), schedule = setTimeout, reveal }) {
  let visibleSince = now();
  let finishing = false;
  return {
    handedOver(elapsedMilliseconds = 0) {
      const elapsed = Number.isFinite(elapsedMilliseconds) ? Math.max(0, elapsedMilliseconds) : 0;
      visibleSince = now() - elapsed;
    },
    finish() {
      if (finishing) return;
      finishing = true;
      schedule(reveal, Math.max(0, 1500 - (now() - visibleSince)));
    },
  };
}
