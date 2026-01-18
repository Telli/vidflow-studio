import { Button } from "../ui/button";
import { Badge } from "../ui/badge";
import { Input } from "../ui/input";
import { Music, ArrowRight, AlertCircle, Film } from "lucide-react";
import { useProject } from "../context/ProjectContext";

export function StitchPlan() {
  const { project, updateTransition, updateAudioNote } = useProject();
  
  // In a real implementation, transitions and audio notes would be part of the data model
  // Since we don't have those fields in Scene, we can simulate them by persisting to a sidecar state in context
  // or just fire-and-forget for this prototype.
  // The context exposes updateTransition/updateAudioNote which just logs for now.

  const approvedScenes = project.scenes.filter(s => s.status === 'Approved');
  const allApproved = project.scenes.every(s => s.status === 'Approved');
  const totalRuntime = approvedScenes.reduce((acc, s) => acc + s.runtimeEstimate, 0);

  return (
    <div className="flex flex-col h-screen bg-zinc-950 animate-in fade-in duration-500">
      <div className="p-8 pb-4 border-b border-zinc-800 flex items-center justify-between">
         <div>
            <h1 className="text-3xl font-bold text-zinc-100">Stitch Plan</h1>
            <p className="text-zinc-400 mt-1">Assemble the final cut from approved scenes.</p>
         </div>
         <div className="text-right">
            <div className="text-xl font-bold text-zinc-100 font-mono">
               {Math.floor(totalRuntime / 60)}m {(totalRuntime % 60).toString().padStart(2, '0')}s
            </div>
            <div className="text-xs text-zinc-500">Projected Runtime</div>
         </div>
      </div>

      <div className="flex-1 flex overflow-hidden">
        {/* Timeline List */}
        <div className="flex-1 overflow-auto p-8 max-w-4xl mx-auto space-y-6">
           {approvedScenes.length === 0 ? (
             <div className="text-center py-20 border border-dashed border-zinc-800 rounded-lg bg-zinc-900/20">
               <Film className="w-12 h-12 mx-auto text-zinc-700 mb-4" />
               <h3 className="text-zinc-300 font-medium">No Approved Scenes</h3>
               <p className="text-zinc-500 mt-1">Approve scenes in the Workspace to add them here.</p>
             </div>
           ) : (
             approvedScenes.map((scene, index) => (
               <div key={scene.id} className="relative pl-8 border-l-2 border-zinc-800 last:border-0 pb-12 last:pb-0">
                  <div className="absolute -left-[9px] top-0 w-4 h-4 rounded-full bg-zinc-800 border-2 border-zinc-950 ring-2 ring-zinc-900"></div>
                  
                  {/* Scene Card */}
                  <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-4 mb-4 relative group">
                     <div className="flex justify-between items-start mb-2">
                        <div className="flex items-center gap-3">
                           <Badge variant="outline" className="bg-teal-950/30 text-teal-500 border-teal-900/50">
                              Scene {scene.number}
                           </Badge>
                           <h3 className="font-medium text-zinc-200">{scene.title}</h3>
                        </div>
                        <span className="font-mono text-xs text-zinc-500">{scene.runtimeEstimate}s</span>
                     </div>
                     <p className="text-sm text-zinc-500 line-clamp-1">{scene.narrativeGoal}</p>
                     
                     <div className="mt-4 flex gap-4">
                        <div className="flex-1">
                           <label className="text-xs text-zinc-500 uppercase font-bold mb-1.5 block">Music / Audio Note</label>
                           <div className="flex gap-2">
                              <Music className="w-4 h-4 text-zinc-600 mt-2" />
                              <Input 
                                placeholder="E.g. Fade in ominous drone..." 
                                className="bg-zinc-950 border-zinc-800 h-8 text-xs text-zinc-300 placeholder:text-zinc-700"
                                onBlur={(e) => updateAudioNote(scene.id, e.target.value)}
                              />
                           </div>
                        </div>
                     </div>
                  </div>

                  {/* Transition Connector */}
                  {index < approvedScenes.length - 1 && (
                     <div className="flex items-center gap-4 ml-4">
                        <div className="w-0.5 h-8 bg-zinc-800"></div>
                        <div className="flex-1">
                           <div className="flex items-center gap-2 mb-1">
                              <ArrowRight className="w-3 h-3 text-zinc-600" />
                              <span className="text-[10px] uppercase font-bold text-zinc-500">Transition</span>
                           </div>
                           <Input 
                              placeholder="Cut to..." 
                              className="bg-zinc-950/50 border-zinc-800 h-7 text-xs w-64 text-zinc-400 placeholder:text-zinc-800"
                              onBlur={(e) => updateTransition(scene.id, e.target.value)}
                           />
                        </div>
                     </div>
                  )}
               </div>
             ))
           )}
           
           {!allApproved && (
              <div className="mt-12 p-4 bg-amber-900/10 border border-amber-900/30 rounded-lg flex items-start gap-3">
                 <AlertCircle className="w-5 h-5 text-amber-600 flex-shrink-0" />
                 <div>
                    <h4 className="text-sm font-medium text-amber-500">Stitch Plan Incomplete</h4>
                    <p className="text-xs text-amber-700 mt-1">
                       You have {project.scenes.length - approvedScenes.length} scenes pending approval. 
                       The full film cannot be rendered until all scenes are approved.
                    </p>
                 </div>
              </div>
           )}
        </div>

        {/* Action Panel */}
        <div className="w-80 border-l border-zinc-800 bg-zinc-900/50 p-6 flex flex-col">
           <div className="mb-8">
              <h3 className="text-sm font-medium text-zinc-200 mb-4">Export Preview</h3>
              <div className="aspect-video bg-zinc-950 rounded border border-zinc-800 flex items-center justify-center mb-2">
                 <span className="text-zinc-700 text-xs">Preview</span>
              </div>
              <div className="text-xs text-zinc-500 text-center">
                 Total Runtime: {Math.floor(totalRuntime / 60)}m {totalRuntime % 60}s
              </div>
           </div>

           <div className="mt-auto space-y-3">
              <Button disabled={!allApproved} className="w-full bg-amber-600 hover:bg-amber-700 text-white disabled:opacity-50 disabled:cursor-not-allowed">
                 {allApproved ? "Render Final Film" : "Waiting for Approvals"}
              </Button>
              <Button variant="outline" className="w-full border-zinc-700 text-zinc-400 hover:text-zinc-200">
                 Export Stitch Plan PDF
              </Button>
           </div>
        </div>
      </div>
    </div>
  );
}
