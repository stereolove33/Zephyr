import { type RefObject, useEffect } from "react";

/**
 * Call `handler` when a mousedown fires outside the element referenced by `ref`.
 *
 * Pass `enabled=false` to skip attaching the listener (e.g., while the popup is
 * closed) so it doesn't add overhead to every click on the page.
 */
export function useClickOutside(
  ref: RefObject<HTMLElement | null>,
  handler: () => void,
  enabled = true,
) {
  useEffect(() => {
    if (!enabled) return;
    function onMouseDown(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        handler();
      }
    }
    document.addEventListener("mousedown", onMouseDown);
    return () => document.removeEventListener("mousedown", onMouseDown);
  }, [ref, handler, enabled]);
}
