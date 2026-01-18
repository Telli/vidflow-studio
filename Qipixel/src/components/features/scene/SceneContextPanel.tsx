import { Scene } from "../../../data/mock-data";
import { ScrollArea } from "../../ui/scroll-area";
import { Badge } from "../../ui/badge";
import { Separator } from "../../ui/separator";
import { MapPin, Clock, Users, ArrowLeft } from "lucide-react";
import { Button } from "../../ui/button";
import { StatusBadge } from "../../common/StatusBadge";

interface SceneContextPanelProps {
  scene: Scene;
  onBack: () => void;
}

export function SceneContextPanel({ scene, onBack }: SceneContextPanelProps) {
  return (
    <div className="w-80 border-r border-zinc-800 bg-zinc-925 flex flex-col flex-shrink-0">
      {/* Back & Title Header (Merged into context panel for this layout or strictly separate?) 
          The Map says "SceneReviewPage > SceneContextPanel" and "SceneActionBar".
          Ideally Title is top level, but for sidebar layout, let's keep it here or above.
          I'll put the back button here as it acts as navigation context.
      */}
      <div className="h-14 border-b border-zinc-800 flex items-center px-4 gap-3 bg-zinc-950">
          <Button variant="ghost" size="icon" onClick={onBack} className="text-zinc-400 hover:text-zinc-100">
            <ArrowLeft className="w-5 h-5" />
          </Button>
          <div className="min-w-0">
             <div className="flex items-center gap-2">
                <span className="font-mono text-zinc-500 font-bold">SC{scene.number}</span>
                <StatusBadge status={scene.status} className="h-4 text-[10px] px-1" />
             </div>
             <div className="text-sm font-semibold text-zinc-100 truncate">{scene.title}</div>
          </div>
      </div>

      <div className="p-4 border-b border-zinc-800 bg-zinc-900/50">
        <h3 className="text-xs font-bold text-zinc-500 uppercase tracking-wider mb-1">Narrative Goal</h3>
        <p className="text-sm text-zinc-300 leading-relaxed">{scene.narrativeGoal}</p>
      </div>
      
      <ScrollArea className="flex-1">
        <div className="p-4 space-y-6">
          <div>
            <h3 className="text-xs font-bold text-zinc-500 uppercase tracking-wider mb-2">Emotional Beat</h3>
            <div className="p-3 bg-zinc-900 rounded border border-zinc-800 text-sm text-amber-500/90 font-medium">
              {scene.emotionalBeat}
            </div>
          </div>

          <div className="space-y-3">
            <div className="flex items-start gap-3">
              <MapPin className="w-4 h-4 text-zinc-500 mt-0.5" />
              <div>
                <div className="text-sm font-medium text-zinc-300">Location</div>
                <div className="text-sm text-zinc-400">{scene.location}</div>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <Clock className="w-4 h-4 text-zinc-500 mt-0.5" />
              <div>
                <div className="text-sm font-medium text-zinc-300">Time of Day</div>
                <div className="text-sm text-zinc-400">{scene.timeOfDay}</div>
              </div>
            </div>
          </div>

          <div>
            <h3 className="text-xs font-bold text-zinc-500 uppercase tracking-wider mb-3">Characters</h3>
            <div className="flex flex-wrap gap-2">
              {scene.characters.map(char => (
                <Badge key={char} variant="secondary" className="bg-zinc-800 text-zinc-300 hover:bg-zinc-700">
                  <Users className="w-3 h-3 mr-1 opacity-50" />
                  {char}
                </Badge>
              ))}
            </div>
          </div>

          <Separator className="bg-zinc-800" />
          
          <div>
            <h3 className="text-xs font-bold text-zinc-500 uppercase tracking-wider mb-2">Director's Notes</h3>
            <p className="text-sm text-zinc-400 italic">
              "Keep the camera tight on Elias. We need to feel the confinement of the space."
            </p>
          </div>
        </div>
      </ScrollArea>
    </div>
  );
}
