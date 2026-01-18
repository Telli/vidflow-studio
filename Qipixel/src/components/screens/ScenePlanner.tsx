import { useState, useRef, useCallback, useEffect } from "react";
import { Scene } from "../data/mock-data";
import { Button } from "../ui/button";
import { StatusBadge } from "../common/StatusBadge";
import { GripVertical, MoreVertical, Plus } from "lucide-react";
import { cn } from "../ui/utils";
import { useDrag, useDrop } from "react-dnd";
import { useProject } from "../context/ProjectContext";

interface DragItem {
  index: number;
  id: string;
  actIndex: number;
}

interface DraggableSceneProps {
  scene: Scene;
  index: number;
  actIndex: number;
  onOpenScene?: (id: string) => void;
  moveScene: (dragIndex: number, hoverIndex: number, sourceActIndex: number, targetActIndex: number) => void;
  onDragEnd: () => void;
}

// Draggable Scene Component
function DraggableScene({ 
  scene, 
  index, 
  actIndex, 
  onOpenScene, 
  moveScene,
  onDragEnd
}: DraggableSceneProps) {
  const ref = useRef<HTMLDivElement>(null);
  const handleRef = useRef<HTMLDivElement>(null);
  
  const [{ handlerId }, drop] = useDrop<DragItem, void, { handlerId: string | symbol | null }>({
    accept: 'SCENE',
    collect(monitor) {
      return {
        handlerId: monitor.getHandlerId(),
      };
    },
    hover(item: DragItem, monitor) {
      if (!ref.current) {
        return;
      }
      
      const dragIndex = item.index;
      const hoverIndex = index;
      const sourceActIndex = item.actIndex;
      const targetActIndex = actIndex;

      // Don't replace items with themselves
      if (dragIndex === hoverIndex && sourceActIndex === targetActIndex) {
        return;
      }

      const hoverBoundingRect = ref.current?.getBoundingClientRect();
      const hoverMiddleY = (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
      const clientOffset = monitor.getClientOffset();
      const hoverClientY = (clientOffset as any).y - hoverBoundingRect.top;

      if (dragIndex < hoverIndex && hoverClientY < hoverMiddleY) {
        return;
      }
      if (dragIndex > hoverIndex && hoverClientY > hoverMiddleY) {
        return;
      }

      moveScene(dragIndex, hoverIndex, sourceActIndex, targetActIndex);

      item.index = hoverIndex;
      item.actIndex = targetActIndex;
    },
  });

  const [{ isDragging }, drag, preview] = useDrag({
    type: 'SCENE',
    item: () => {
      return { id: scene.id, index, actIndex };
    },
    end: (item, monitor) => {
        onDragEnd();
    },
    collect: (monitor) => ({
      isDragging: monitor.isDragging(),
    }),
  });

  drag(handleRef);
  preview(drop(ref));

  return (
    <div 
      ref={ref}
      data-handler-id={handlerId}
      className={cn(
        "group flex items-center gap-4 bg-zinc-900 border border-zinc-800 rounded-lg p-3 hover:border-zinc-700 transition-colors",
        isDragging ? "opacity-30 border-dashed border-zinc-600" : ""
      )}
    >
      <div ref={handleRef} className="cursor-grab text-zinc-600 hover:text-zinc-400 active:cursor-grabbing p-1">
        <GripVertical className="w-5 h-5" />
      </div>
      
      <div className="w-12 h-12 flex items-center justify-center bg-zinc-950 rounded border border-zinc-800 font-mono text-zinc-400 font-bold">
        {scene.number}
      </div>

      <div className="flex-1 min-w-0 cursor-pointer" onClick={() => onOpenScene && onOpenScene(scene.id)}>
        <div className="flex items-center gap-3 mb-1">
          <h4 className="font-medium text-zinc-200 truncate hover:text-amber-500 transition-colors">
            {scene.title}
          </h4>
          <StatusBadge status={scene.status} className="h-5 text-[10px] px-1.5" />
        </div>
        <div className="flex items-center gap-4 text-xs text-zinc-500">
          <span>{scene.location} • {scene.timeOfDay}</span>
          <span className="truncate max-w-[300px]">{scene.narrativeGoal}</span>
        </div>
      </div>

      <div className="flex items-center gap-6 px-4 border-l border-zinc-800">
        <div className="text-right">
          <div className="text-sm font-mono text-zinc-300">
            {Math.floor(scene.runtimeEstimate / 60)}:{(scene.runtimeEstimate % 60).toString().padStart(2, '0')}
          </div>
          <div className={cn(
            "text-[10px]",
            scene.runtimeEstimate > scene.runtimeTarget ? "text-red-400" : "text-zinc-500"
          )}>
            {scene.runtimeEstimate > scene.runtimeTarget ? `+${scene.runtimeEstimate - scene.runtimeTarget}s` : "On target"}
          </div>
        </div>
        
        <Button variant="ghost" size="icon" className="h-8 w-8 text-zinc-500 hover:text-zinc-300">
          <MoreVertical className="w-4 h-4" />
        </Button>
      </div>
    </div>
  );
}

export function ScenePlanner() {
  const { project, reorderScenes } = useProject();
  const [acts, setActs] = useState<{name: string, scenes: Scene[]}[]>([]);
  
  // Use a ref to keep track of the latest acts state for the drag end callback
  const actsRef = useRef(acts);

  useEffect(() => {
    // Initial sync
    const sceneList = [...project.scenes];
    const newActs = [
        { name: "Act I: The Setup", scenes: sceneList.slice(0, 1) },
        { name: "Act II: The Confrontation", scenes: sceneList.slice(1, 3) },
        { name: "Act III: The Resolution", scenes: sceneList.slice(3) },
    ];
    setActs(newActs);
    actsRef.current = newActs;
  }, [project.scenes]);

  const moveScene = useCallback((dragIndex: number, hoverIndex: number, sourceActIndex: number, targetActIndex: number) => {
    setActs((prevActs) => {
      const newActs = JSON.parse(JSON.stringify(prevActs));
      const sourceScenes = newActs[sourceActIndex].scenes;
      const [movedScene] = sourceScenes.splice(dragIndex, 1);
      
      const targetScenes = newActs[targetActIndex].scenes;
      targetScenes.splice(hoverIndex, 0, movedScene);
      
      actsRef.current = newActs; // Update ref synchronously
      return newActs;
    });
  }, []);

  const handleDragEnd = useCallback(() => {
      const flattenedScenes = actsRef.current.flatMap(act => act.scenes);
      // We only want to trigger update if the order actually changed? 
      // Diffing might be expensive, so we just blindly update. 
      // To prevent infinite loop with the useEffect above, we might need to check.
      // But `reorderScenes` will update `project` which triggers `useEffect`.
      // The `useEffect` will re-set `acts`.
      // If `flattenedScenes` is same as `project.scenes` (by value), React state update might bail out, 
      // but `project.scenes` is a new array every time context updates.
      // It's fine for now, acts will just refresh.
      reorderScenes(flattenedScenes);
  }, [reorderScenes]);

  return (
    <div className="p-8 max-w-5xl mx-auto space-y-8 animate-in fade-in duration-500">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-zinc-100">Scene Planner</h1>
          <p className="text-zinc-400 mt-1">Organize the narrative structure and pacing. Drag to reorder.</p>
        </div>
        <div className="text-right">
          <div className="text-2xl font-bold text-zinc-100 font-mono">
            {Math.floor(project.scenes.reduce((acc, s) => acc + s.runtimeEstimate, 0) / 60)}m
            <span className="text-lg text-zinc-500 font-sans font-normal ml-2">/ {project.runtimeTarget / 60}m Target</span>
          </div>
        </div>
      </div>

      <div className="space-y-8">
        {acts.map((act, actIndex) => (
          <div key={actIndex} className="space-y-4">
            <div className="flex items-center justify-between text-zinc-500 px-2">
              <h3 className="text-sm font-semibold uppercase tracking-wider">{act.name}</h3>
              <span className="text-xs">{act.scenes.reduce((acc, s) => acc + s.runtimeEstimate, 0)}s est.</span>
            </div>
            
            <div className="space-y-2 min-h-[50px] bg-zinc-900/10 rounded-lg p-1 border border-transparent transition-colors hover:border-zinc-800/50">
              {act.scenes.map((scene, index) => (
                <DraggableScene 
                  key={scene.id} 
                  scene={scene} 
                  index={index} 
                  actIndex={actIndex}
                  moveScene={moveScene}
                  onDragEnd={handleDragEnd}
                />
              ))}
              <Button 
                variant="ghost" 
                className="w-full border border-dashed border-zinc-800 text-zinc-500 hover:bg-zinc-900 hover:text-zinc-300 h-10"
              >
                <Plus className="w-4 h-4 mr-2" /> Add Scene to Act {actIndex + 1}
              </Button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
