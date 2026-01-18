import { Button } from "../../ui/button";
import { MessageSquare, Check } from "lucide-react";
import { Scene } from "../../../data/mock-data";
import { useProject } from "../../context/ProjectContext";
import { useState } from "react";
import { RevisionModal } from "../../modals/RevisionModal";

interface SceneActionBarProps {
  scene: Scene;
}

export function SceneActionBar({ scene }: SceneActionBarProps) {
  const { submitForReview, approveScene, requestRevision } = useProject();
  const [isRevisionModalOpen, setIsRevisionModalOpen] = useState(false);

  const canSubmitForReview = scene.status === "Draft" && !scene.isLocked;
  const canApprove = scene.status === "Review" && !scene.isLocked;
  const canRequestRevision = scene.status === "Review" && !scene.isLocked;

  return (
    <>
      <RevisionModal 
        open={isRevisionModalOpen} 
        onOpenChange={setIsRevisionModalOpen} 
        onSubmit={(feedback) => {
          requestRevision(scene.id, feedback);
          setIsRevisionModalOpen(false);
        }} 
      />
      
      <div className="h-16 border-t border-zinc-800 bg-zinc-900 px-6 flex items-center justify-between flex-shrink-0">
         <div className="flex items-center gap-4">
            <Button variant="ghost" className="text-zinc-400 hover:text-zinc-200">
               <MessageSquare className="w-4 h-4 mr-2" />
               Comments (0)
            </Button>
         </div>
         <div className="flex items-center gap-3">
            <Button variant="secondary" className="bg-zinc-800 text-zinc-300 hover:bg-zinc-700 border border-zinc-700">
              Save Manual Edit
            </Button>
            
            {scene.status === "Draft" && (
              <Button
                variant="secondary"
                disabled={!canSubmitForReview}
                onClick={() => submitForReview(scene.id)}
                className="bg-zinc-800 text-zinc-300 hover:bg-zinc-700 border border-zinc-700"
              >
                Submit for Review
              </Button>
            )}

            {scene.status === "Review" && (
              <Button
                variant="secondary"
                disabled={!canRequestRevision}
                onClick={() => setIsRevisionModalOpen(true)}
                className="bg-zinc-800 text-amber-500 hover:bg-zinc-700 border border-zinc-700 border-amber-900/30"
              >
                Request Revision
              </Button>
            )}

            {scene.status === "Review" && (
              <Button
                disabled={!canApprove}
                className="bg-teal-700 hover:bg-teal-600 text-white"
                onClick={() => approveScene(scene.id)}
              >
                <Check className="w-4 h-4 mr-2" />
                Approve Scene
              </Button>
            )}

            {scene.status === "Approved" && (
              <Button disabled className="bg-teal-900/50 text-teal-400 border border-teal-900 opacity-50 cursor-not-allowed">
                <Check className="w-4 h-4 mr-2" />
                Approved
              </Button>
            )}
         </div>
      </div>
    </>
  );
}
