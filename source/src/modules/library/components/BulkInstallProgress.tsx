import { Progress } from "@/components";
import type { InstallProgress } from "@/lib/tauri";

interface BulkInstallProgressProps {
  progress: InstallProgress | null;
}

export function BulkInstallProgress({ progress }: BulkInstallProgressProps) {
  if (!progress) {
    return (
      <Progress.Root value={null} label="Preparing import...">
        <Progress.Track>
          <Progress.Indicator />
        </Progress.Track>
      </Progress.Root>
    );
  }

  return (
    <>
      <Progress.Root
        value={progress.total > 0 ? (progress.current / progress.total) * 100 : 0}
        label={`${progress.current} / ${progress.total}`}
      >
        <Progress.Track>
          <Progress.Indicator />
        </Progress.Track>
      </Progress.Root>
      <p className="truncate text-sm text-surface-400">{progress.currentFile}</p>
    </>
  );
}
