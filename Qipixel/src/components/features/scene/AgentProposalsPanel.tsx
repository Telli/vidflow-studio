import { useState } from "react";
import { Scene, AgentProposal } from "../../../data/mock-data";
import { Badge } from "../../ui/badge";
import { Button } from "../../ui/button";
import { ScrollArea } from "../../ui/scroll-area";
import { Bot, Loader2 } from "lucide-react";
import { cn } from "../../ui/utils";
import { useProject } from "../../context/ProjectContext";
import { toast } from "sonner@2.0.3";

interface AgentProposalsPanelProps {
  scene: Scene;
}

export function AgentProposalsPanel({ scene }: AgentProposalsPanelProps) {
  const { applyProposal, dismissProposal, runAgents } = useProject();
  const [isAnalyzing, setIsAnalyzing] = useState(false);

  const canEdit = scene.status === "Draft" && !scene.isLocked;

  const handleRunAnalysis = async () => {
    setIsAnalyzing(true);
    try {
      runAgents(scene.id);
      toast.success("Agent analysis started", {
        description: "Agents are analyzing the scene.",
      });
    } finally {
      setTimeout(() => setIsAnalyzing(false), 1200);
    }
  };

  return (
    <div className="w-[380px] border-l border-zinc-800 bg-zinc-925 flex flex-col flex-shrink-0">
      <div className="p-4 border-b border-zinc-800 flex items-center justify-between bg-zinc-900/50">
        <h3 className="text-sm font-semibold text-zinc-200">Agent Proposals</h3>
        <Badge variant="outline" className="bg-amber-900/20 text-amber-500 border-amber-900/50">
          {scene.proposals.length} New
        </Badge>
      </div>

      <ScrollArea className="flex-1 bg-zinc-900/20">
        <div className="p-4 space-y-4">
          {/* Agent Analysis Loading State */}
          {isAnalyzing && (
             <div className="bg-zinc-900/50 border border-zinc-800 border-dashed rounded-lg p-6 flex flex-col items-center justify-center text-center animate-in fade-in zoom-in-95 duration-300">
                <Loader2 className="w-8 h-8 text-amber-500 animate-spin mb-3" />
                <h4 className="text-zinc-300 font-medium text-sm">Agents Analyzing Scene...</h4>
                <p className="text-zinc-500 text-xs mt-1">Reviewing pacing against narrative goals.</p>
             </div>
          )}

          {scene.proposals.length === 0 && !isAnalyzing ? (
            <div className="text-center py-10 px-4">
              <Bot className="w-10 h-10 text-zinc-700 mx-auto mb-3" />
              <p className="text-sm text-zinc-500">No pending proposals.</p>
              <p className="text-xs text-zinc-600 mt-1">Agents will suggest improvements based on narrative goals.</p>
              <Button 
                variant="outline" 
                className="mt-4 border-zinc-700 text-zinc-400 hover:text-zinc-200 w-full"
                disabled={!canEdit}
                onClick={handleRunAnalysis}
              >
                Run Analysis
              </Button>
            </div>
          ) : (
            scene.proposals.map((proposal) => (
              <div key={proposal.id} className="bg-zinc-900 border border-zinc-800 rounded-lg overflow-hidden shadow-sm hover:border-zinc-600 transition-colors animate-in slide-in-from-right-5 duration-300">
                <div className="p-3 border-b border-zinc-800 flex items-center justify-between bg-zinc-900">
                  <div className="flex items-center gap-2">
                    <Badge variant="secondary" className={cn(
                      "text-[10px] font-medium border",
                      proposal.role === 'Director' ? "bg-purple-900/20 text-purple-300 border-purple-900/30" :
                      proposal.role === 'Editor' ? "bg-indigo-900/20 text-indigo-300 border-indigo-900/30" :
                      proposal.role === 'Cinematographer' ? "bg-blue-900/20 text-blue-300 border-blue-900/30" :
                      "bg-zinc-800 text-zinc-300 border-zinc-700"
                    )}>
                      {proposal.role}
                    </Badge>
                    <span className="text-xs font-mono text-zinc-500">{proposal.runtimeImpact > 0 ? `+${proposal.runtimeImpact}s` : `${proposal.runtimeImpact}s`}</span>
                  </div>
                  <span className="text-[10px] text-zinc-600">Just now</span>
                </div>
                <div className="p-4">
                  <h4 className="text-sm font-semibold text-zinc-200 mb-1">{proposal.summary}</h4>
                  <p className="text-xs text-zinc-400 mb-3 leading-relaxed">{proposal.rationale}</p>
                  
                  <div className="bg-black/40 rounded p-2 text-xs font-mono text-zinc-300 border-l-2 border-amber-500 mb-4">
                    {proposal.diff}
                  </div>

                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      className="flex-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-300 h-8 border border-zinc-700"
                      disabled={!canEdit}
                      onClick={() => dismissProposal(scene.id, proposal.id)}
                    >
                      Dismiss
                    </Button>
                    <Button 
                        size="sm" 
                        className="flex-1 bg-zinc-100 hover:bg-white text-zinc-900 h-8 font-semibold"
                        disabled={!canEdit}
                        onClick={() => applyProposal(scene.id, proposal.id)}
                    >
                      Apply
                    </Button>
                  </div>
                </div>
              </div>
            ))
          )}
          
          {/* Add Analysis Button at bottom if list is not empty */}
          {scene.proposals.length > 0 && !isAnalyzing && (
             <Button 
               variant="ghost" 
               className="w-full text-zinc-500 hover:text-amber-500 hover:bg-zinc-900 border border-dashed border-zinc-800"
               disabled={!canEdit}
               onClick={handleRunAnalysis}
             >
               <Bot className="w-4 h-4 mr-2" />
               Run New Analysis
             </Button>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}
