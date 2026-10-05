import { useDroppable } from "@dnd-kit/core";

import type { InstalledMod, LibraryFolder } from "@/lib/tauri";

import { FolderCard } from "./FolderCard";

interface DroppableFolderCardProps {
  folder: LibraryFolder;
  mods: InstalledMod[];
}

export function DroppableFolderCard({ folder, mods }: DroppableFolderCardProps) {
  const { setNodeRef, isOver } = useDroppable({
    id: `folder:${folder.id}`,
    data: { type: "folder", folderId: folder.id },
  });

  return (
    <div
      ref={setNodeRef}
      className={`h-full rounded-xl transition-all duration-150 ${
        isOver ? "scale-[1.02] ring-2 ring-accent-500" : ""
      }`}
    >
      <FolderCard folder={folder} mods={mods} />
    </div>
  );
}
