import { useDeferredValue, useMemo } from "react";

import { countRegexMatches, isValidRegex } from "./wadRegex";

export interface RegexPreview {
  /** `true` when the pattern is empty or compiles as a valid regex. */
  valid: boolean;
  /**
   * Number of WADs matched by the pattern. `null` when the pattern is empty,
   * invalid, or the WAD list is not yet available.
   */
  matchCount: number | null;
}

/**
 * Live validation + match-count preview for a user-entered regex pattern.
 *
 * The match count is computed against a deferred copy of the pattern so fast
 * typing doesn't block the input — React renders the stale count while the
 * deferred value catches up.
 */
export function useRegexPreview(
  pattern: string,
  availableWads: string[] | undefined,
): RegexPreview {
  const trimmed = pattern.trim();
  const valid = trimmed === "" ? true : isValidRegex(trimmed);
  const deferredTrimmed = useDeferredValue(trimmed);

  const matchCount = useMemo(() => {
    if (!trimmed || !valid || !availableWads) return null;
    return countRegexMatches(deferredTrimmed, availableWads);
  }, [deferredTrimmed, trimmed, valid, availableWads]);

  return { valid, matchCount };
}
