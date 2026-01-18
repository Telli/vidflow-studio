import { Badge } from "../ui/badge";
import { cn } from "../ui/utils";
import { SceneStatus } from "../data/mock-data";

export function StatusBadge({ status, className }: { status: SceneStatus | string, className?: string }) {
  const getVariant = (s: string) => {
    switch (s) {
      case "Approved": return "bg-teal-900/50 text-teal-200 border-teal-800 hover:bg-teal-900/70";
      case "Review": return "bg-amber-900/50 text-amber-200 border-amber-800 hover:bg-amber-900/70";
      case "Draft": return "bg-zinc-800 text-zinc-400 border-zinc-700 hover:bg-zinc-800";
      case "Locked": return "bg-purple-900/50 text-purple-200 border-purple-800 hover:bg-purple-900/70";
      default: return "bg-zinc-800 text-zinc-400 border-zinc-700";
    }
  };

  return (
    <Badge variant="outline" className={cn("font-normal border", getVariant(status), className)}>
      {status}
    </Badge>
  );
}
