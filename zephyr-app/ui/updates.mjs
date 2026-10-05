// Updates are notices only; downloading and installing remain user actions.
export function createUpdateController({ check, openRelease, render, translate }) {
  let state = { state: "idle" };
  let pending;
  function refresh() {
    const t = translate;
    const available = state.state === "available";
    const messages = {
      checking: "Checking for updates...",
      current: "You are using the latest version.",
      unavailable: "Could not check for updates. Try again later.",
      available: "A new version of Zephyr is available:",
    };
    render({
      available,
      checking: state.state === "checking",
      title: t("Update available"),
      message: available ? `${t(messages.available)} ${state.version}` : t(messages[state.state] || ""),
      downloadLabel: t("View update"),
      checkLabel: t("Check for updates"),
    });
  }
  async function runCheck() {
    if (pending) return pending;
    state = { state: "checking" };
    refresh();
    pending = (async () => {
      try {
        const result = await check();
        state = ["available", "current", "unavailable"].includes(result?.state)
          ? result : { state: "unavailable" };
      } catch {
        state = { state: "unavailable" };
      }
      refresh();
    })();
    try { await pending; }
    finally { pending = undefined; }
  }
  return {
    refresh,
    check: runCheck,
    async open() { if (state.state === "available") await openRelease(); },
  };
}
