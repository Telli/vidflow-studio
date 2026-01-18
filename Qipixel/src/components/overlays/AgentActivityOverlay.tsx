import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from "../ui/sheet";
import { ScrollArea } from "../ui/scroll-area";
import { Badge } from "../ui/badge";
import { Activity, CheckCircle, AlertTriangle, Clock } from "lucide-react";
import { Project } from "../data/mock-data";

interface AgentActivityOverlayProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  project: Project;
}

export function AgentActivityOverlay({ open, onOpenChange, project }: AgentActivityOverlayProps) {
  // Mock activity log
  const activities = [
    { id: 1, agent: "Director Agent", action: "Analyzed Scene 2 pacing", status: "success", time: "2m ago", cost: "$0.02" },
    { id: 2, agent: "Cinematographer Agent", action: "Generated shot list for Scene 3", status: "success", time: "5m ago", cost: "$0.15" },
    { id: 3, agent: "Writer Agent", action: "Refining dialogue in Scene 2", status: "processing", time: "Now", cost: "..." },
    { id: 4, agent: "Continuity Agent", action: "Flagged prop mismatch in Scene 1", status: "warning", time: "1h ago", cost: "$0.01" },
  ];

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="bg-zinc-900 border-l border-zinc-800 text-zinc-100 w-[400px]">
        <SheetHeader className="mb-6">
          <SheetTitle className="text-zinc-100 flex items-center gap-2">
            <Activity className="w-5 h-5 text-amber-500" />
            Agent Activity Log
          </SheetTitle>
          <SheetDescription className="text-zinc-500">
            Real-time tracking of agent inference and costs.
          </SheetDescription>
        </SheetHeader>

        <ScrollArea className="h-[calc(100vh-120px)] pr-4">
          <div className="space-y-6">
             <div className="relative pl-4 border-l border-zinc-800 space-y-8">
                {activities.map((item) => (
                   <div key={item.id} className="relative">
                      <div className="absolute -left-[21px] top-1 bg-zinc-900 border border-zinc-800 rounded-full p-1">
                         {item.status === 'success' && <CheckCircle className="w-3 h-3 text-teal-500" />}
                         {item.status === 'processing' && <Activity className="w-3 h-3 text-amber-500 animate-pulse" />}
                         {item.status === 'warning' && <AlertTriangle className="w-3 h-3 text-red-500" />}
                      </div>
                      
                      <div className="flex justify-between items-start mb-1">
                         <span className="text-xs font-bold text-zinc-300">{item.agent}</span>
                         <span className="text-[10px] text-zinc-500 font-mono">{item.time}</span>
                      </div>
                      <p className="text-sm text-zinc-400 mb-2">{item.action}</p>
                      <div className="flex items-center gap-2">
                         <Badge variant="outline" className="text-[10px] h-5 border-zinc-800 text-zinc-500 font-mono">
                            {item.cost}
                         </Badge>
                      </div>
                   </div>
                ))}
             </div>
          </div>
        </ScrollArea>
      </SheetContent>
    </Sheet>
  );
}
