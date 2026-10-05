import { useCallback, useMemo } from "react";

import type { Settings, WadBlocklistEntry } from "@/lib/tauri";

export interface UseWadBlocklistResult {
  blocklist: WadBlocklistEntry[];
  /** Append an entry. Returns `false` if an equivalent entry already exists. */
  add: (entry: WadBlocklistEntry) => boolean;
  removeAt: (index: number) => void;
  clear: () => void;
}

/**
 * CRUD surface for the `wadBlocklist` slice of `Settings`. Every mutation calls
 * `onSave` with the full updated settings object — the same write-through
 * pattern used by the other settings sections.
 *
 * `add` treats two entries as equivalent when they share the same `kind` and a
 * case-insensitive value match. Exact and Regex entries with the same literal
 * value are allowed to coexist because they block different things.
 */
export function useWadBlocklist(
  settings: Settings,
  onSave: (settings: Settings) => void,
): UseWadBlocklistResult {
  const blocklist = useMemo(() => settings.wadBlocklist ?? [], [settings.wadBlocklist]);

  const replace = useCallback(
    (next: WadBlocklistEntry[]) => {
      onSave({ ...settings, wadBlocklist: next });
    },
    [settings, onSave],
  );

  const add = useCallback(
    (entry: WadBlocklistEntry): boolean => {
      const needle = entry.value.toLowerCase();
      const isDuplicate = blocklist.some(
        (e) => e.kind === entry.kind && e.value.toLowerCase() === needle,
      );
      if (isDuplicate) return false;
      replace([...blocklist, entry]);
      return true;
    },
    [blocklist, replace],
  );

  const removeAt = useCallback(
    (index: number) => {
      replace(blocklist.filter((_, i) => i !== index));
    },
    [blocklist, replace],
  );

  const clear = useCallback(() => {
    replace([]);
  }, [replace]);

  return { blocklist, add, removeAt, clear };
}
