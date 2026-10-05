const pending = new Map();
const listeners = new Map();
let sequence = 0;

const host = globalThis.chrome?.webview;
if (host) host.addEventListener("message", ({ data }) => {
  if (data?.event) {
    for (const callback of listeners.get(data.event) || []) callback({ payload: data.payload });
    return;
  }
  const request = pending.get(data?.id);
  if (!request) return;
  clearTimeout(request.timeout);
  pending.delete(data.id);
  request.resolve(data.result);
});

export function invoke(command, args = {}) {
  if (!host) return Promise.reject(new Error("Abra o Zephyr pelo INICIAR.cmd."));
  return new Promise((resolve, reject) => {
    const id = ++sequence;
    const timeout = setTimeout(() => {
      pending.delete(id);
      reject(new Error("A operação demorou demais. Consulte menu.log e tente novamente."));
    }, 300000);
    pending.set(id, { resolve, reject, timeout });
    try { host.postMessage({ id, command, args }); }
    catch (error) { clearTimeout(timeout); pending.delete(id); reject(error); }
  });
}

export async function listen(event, callback) {
  if (!listeners.has(event)) listeners.set(event, new Set());
  listeners.get(event).add(callback);
  return () => listeners.get(event)?.delete(callback);
}

export async function open(options = {}) {
  const result = await invoke("dialog_open", options);
  if (result?.ok === true) return result.value;
  throw new Error(typeof result?.error === "string" ? result.error : JSON.stringify(result?.error));
}

globalThis.addEventListener?.("beforeunload", () => {
  for (const request of pending.values()) {
    clearTimeout(request.timeout);
    request.reject(new Error("A interface foi fechada."));
  }
  pending.clear();
});
