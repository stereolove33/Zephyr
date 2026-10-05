export async function detectFirstLeaguePath(settings, { attempted, markAttempted, detect, save }) {
  if (typeof settings.leaguePath === "string" && settings.leaguePath.trim())
    return { settings, state: "configured" };
  if (attempted) return { settings, state: "skipped" };
  try { markAttempted(); } catch { /* Storage can be unavailable. */ }
  try {
    const detected = await detect();
    if (typeof detected !== "string" || !detected.trim()) return { settings, state: "not-found" };
    const updated = { ...settings, leaguePath: detected.trim() };
    await save(updated);
    return { settings: updated, state: "detected" };
  } catch {
    // Keep the initial setup usable; Browse and Auto-detect remain available.
    return { settings, state: "failed" };
  }
}
