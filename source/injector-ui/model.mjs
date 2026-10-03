export function unwrap(result) {
  if (result?.ok === true) return result.value;
  if (result?.ok === false) throw new Error(JSON.stringify(result.error));
  throw new Error("Unexpected loader response.");
}

export function needsCustomStart(mods, status) {
  return mods.some((mod) => mod.enabled) && !status.running;
}

export function officialLabel(message = "", running = false) {
  if (message.startsWith("ERROR|")) return "Injection failed";
  if (message.startsWith("LOADED|")) return "DLL loaded";
  if (message.startsWith("LOADING|")) return "Loading";
  return running ? "Waiting for game" : "Stopped";
}

export async function stopAllLoaders(api) {
  // Attempt both stops even if one loader fails to stop.
  const results = await Promise.allSettled([
    api("stop_patcher"),
    api("plugin:injector|stop_official"),
  ]);
  const failure = results.find(result => result.status === "rejected");
  if (failure) throw failure.reason;
}

export async function toggleLoaders(official, custom, start, stop) {
  if (official.running) {
    await stop();
    return;
  }
  await start();
}
