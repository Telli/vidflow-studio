import { useProject } from "../components/context/ProjectContext";
import { SceneContextPanel } from "../components/features/scene/SceneContextPanel";
import { SceneContentPanel } from "../components/features/scene/SceneContentPanel";
import { AgentProposalsPanel } from "../components/features/scene/AgentProposalsPanel";
import { SceneActionBar } from "../components/features/scene/SceneActionBar";

interface SceneReviewPageProps {
  sceneId: string;
  onBack: () => void;
}

export function SceneReviewPage({ sceneId, onBack }: SceneReviewPageProps) {
  const { project } = useProject();
  const scene = project.scenes.find(s => s.id === sceneId);
  
  if (!scene) return null;

  return (
    <div className="flex flex-col h-full bg-zinc-950 overflow-hidden animate-in fade-in duration-300">
      {/* 
        The top header was previously in SceneWorkspace. 
        In the new layout, SceneContextPanel has a back button and title header.
        However, the layout requested is:
        <SceneReviewLayout>
          <SceneContextPanel />
          <SceneContentPanel />
          <AgentProposalsPanel />
        </SceneReviewLayout>
        <SceneActionBar />
        
        So the main horizontal flex container is the "SceneReviewLayout".
      */}
      
      <div className="flex-1 flex overflow-hidden">
        <SceneContextPanel scene={scene} onBack={onBack} />
        <SceneContentPanel scene={scene} />
        <AgentProposalsPanel scene={scene} />
      </div>
      
      <SceneActionBar scene={scene} />
    </div>
  );
}
