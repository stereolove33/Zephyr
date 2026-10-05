import { useMemo } from "react";
import { match } from "ts-pattern";

import type { WadBlocklistEntry } from "@/lib/tauri";

export type BlocklistSortKey = "nameAsc" | "nameDesc" | "kind";

export interface DecoratedBlocklistEntry {
  entry: WadBlocklistEntry;
  /** Index into the source `blocklist` array, preserved across filter/sort. */
  originalIndex: number;
}

/**
 * Filter `blocklist` by the trimmed lower-cased `search` substring and sort
 * the result. Original indices are preserved so the UI can call `removeAt`
 * with the correct index from the source array.
 *
 * Sorting is presentational only — the stored order is never mutated.
 */
export function useBlocklistView(
  blocklist: WadBlocklistEntry[],
  search: string,
  sortKey: BlocklistSortKey,
): DecoratedBlocklistEntry[] {
  return useMemo(() => {
    const decorated: DecoratedBlocklistEntry[] = blocklist.map((entry, originalIndex) => ({
      entry,
      originalIndex,
    }));

    const lowerSearch = search.trim().toLowerCase();
    const filtered = lowerSearch
      ? decorated.filter((d) => d.entry.value.toLowerCase().includes(lowerSearch))
      : decorated;

    return [...filtered].sort((a, b) =>
      match(sortKey)
        .with("nameAsc", () => a.entry.value.localeCompare(b.entry.value))
        .with("nameDesc", () => b.entry.value.localeCompare(a.entry.value))
        .with("kind", () => {
          if (a.entry.kind === b.entry.kind) {
            return a.entry.value.localeCompare(b.entry.value);
          }
          return a.entry.kind === "exact" ? -1 : 1;
        })
        .exhaustive(),
    );
  }, [blocklist, search, sortKey]);
}
