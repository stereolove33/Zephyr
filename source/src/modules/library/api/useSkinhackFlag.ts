import { useMemo, useState } from "react";

import type { InstalledMod } from "@/lib/tauri";
import { checkModForSkinhack } from "@/modules/library/utils/skinhackCheck";

export function useSkinhackFlag(mod: InstalledMod) {
  const [infoOpen, setInfoOpen] = useState(false);
  const flag = useMemo(() => checkModForSkinhack(mod), [mod]);
  const isFlagged = flag != null;
  const reason = flag?.reason ?? "";

  return { isFlagged, reason, infoOpen, setInfoOpen };
}
