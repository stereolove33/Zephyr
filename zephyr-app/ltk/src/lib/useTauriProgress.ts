import { listen, type UnlistenFn } from "@tauri-apps/api/event";
import { useCallback, useEffect, useMemo, useState } from "react";

interface TauriProgressOptions {
  terminalStages?: string[];
  clearDelay?: number;
}

interface TauriProgressResult<T> {
  progress: T | null;
  clear: () => void;
}

const DEFAULT_TERMINAL_STAGES = ["complete", "error"];

/**
 * Subscribe to a Tauri backend progress event and auto-clear after a terminal stage.
 *
 * The payload type `T` must have a `stage` string field. When `stage` matches one
 * of the `terminalStages` (default: `["complete", "error"]`), progress is cleared
 * after `clearDelay` ms (default: 1000).
 */
export function useTauriProgress<T extends { stage: string }>(
  eventName: string,
  options?: TauriProgressOptions,
): TauriProgressResult<T> {
  const [progress, setProgress] = useState<T | null>(null);

  const terminalStages = useMemo(
    () => options?.terminalStages ?? DEFAULT_TERMINAL_STAGES,
    [options?.terminalStages],
  );
  const clearDelay = options?.clearDelay ?? 1000;

  useEffect(() => {
    let unlisten: UnlistenFn | null = null;
    let timeoutId: ReturnType<typeof setTimeout> | null = null;
    let mounted = true;

    listen<T>(eventName, (event) => {
      setProgress(event.payload);

      if (terminalStages.includes(event.payload.stage)) {
        if (timeoutId !== null) clearTimeout(timeoutId);
        timeoutId = setTimeout(() => {
          if (mounted) setProgress(null);
        }, clearDelay);
      }
    }).then((fn) => {
      if (!mounted) {
        fn();
        return;
      }
      unlisten = fn;
    });

    return () => {
      mounted = false;
      if (unlisten) unlisten();
      if (timeoutId !== null) clearTimeout(timeoutId);
    };
  }, [eventName, terminalStages, clearDelay]);

  const clear = useCallback(() => setProgress(null), []);

  return { progress, clear };
}
