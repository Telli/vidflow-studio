import { useState, useEffect } from "react";
import { Scene } from "../../../data/mock-data";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "../../ui/tabs";
import { ScrollArea } from "../../ui/scroll-area";
import { Textarea } from "../../ui/textarea";
import { Badge } from "../../ui/badge";
import { Button } from "../../ui/button";
import { Slider } from "../../ui/slider";
import { Separator } from "../../ui/separator";
import { 
    FileText, 
    Image as ImageIcon, 
    Video, 
    Play, 
    Pause, 
    SkipBack, 
    SkipForward, 
    Volume2, 
    Maximize2 
} from "lucide-react";
import { useProject } from "../../context/ProjectContext";

interface SceneContentPanelProps {
  scene: Scene;
}

export function SceneContentPanel({ scene }: SceneContentPanelProps) {
  const { updateScene } = useProject();
  const [activeTab, setActiveTab] = useState("script");
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const duration = scene.runtimeEstimate;

  const canEdit = scene.status === "Draft" && !scene.isLocked;

  // Mock Video Progress
  useEffect(() => {
    let interval: NodeJS.Timeout;
    if (isPlaying) {
      interval = setInterval(() => {
        setCurrentTime(prev => {
          if (prev >= duration) {
            setIsPlaying(false);
            return 0;
          }
          return prev + 1;
        });
      }, 1000);
    }
    return () => clearInterval(interval);
  }, [isPlaying, duration]);

  const formatTime = (seconds: number) => {
    const m = Math.floor(seconds / 60);
    const s = Math.floor(seconds % 60);
    const ms = Math.floor((seconds % 1) * 100); 
    return `00:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}:${ms.toString().padStart(2, '0')}`;
  };

  return (
    <div className="flex-1 flex flex-col min-w-0 bg-zinc-950">
      <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col">
        <div className="border-b border-zinc-800 px-4 bg-zinc-950 flex items-center justify-between">
          <TabsList className="h-12 bg-transparent space-x-2">
            <TabsTrigger value="script" className="data-[state=active]:bg-zinc-900 data-[state=active]:text-zinc-100 text-zinc-500 rounded-b-none border-b-2 border-transparent data-[state=active]:border-amber-600 rounded-none px-4 pb-3 pt-3">
              <FileText className="w-4 h-4 mr-2" /> Script
            </TabsTrigger>
            <TabsTrigger value="storyboard" className="data-[state=active]:bg-zinc-900 data-[state=active]:text-zinc-100 text-zinc-500 rounded-b-none border-b-2 border-transparent data-[state=active]:border-amber-600 rounded-none px-4 pb-3 pt-3">
              <ImageIcon className="w-4 h-4 mr-2" /> Storyboard
            </TabsTrigger>
            <TabsTrigger value="animatic" className="data-[state=active]:bg-zinc-900 data-[state=active]:text-zinc-100 text-zinc-500 rounded-b-none border-b-2 border-transparent data-[state=active]:border-amber-600 rounded-none px-4 pb-3 pt-3">
              <Video className="w-4 h-4 mr-2" /> Animatic
            </TabsTrigger>
          </TabsList>
          
          <div className="flex items-center gap-2 text-xs text-zinc-500">
             <span>Runtime: {Math.floor(scene.runtimeEstimate / 60)}m {scene.runtimeEstimate % 60}s</span>
          </div>
        </div>

        <div className="flex-1 bg-zinc-950 relative">
          <TabsContent value="script" className="h-full m-0 p-0 absolute inset-0">
            <ScrollArea className="h-full">
              <div className="max-w-3xl mx-auto py-12 px-8 min-h-full bg-zinc-950">
                <Textarea 
                    className="w-full h-[80vh] bg-transparent border-0 text-zinc-300 font-mono text-base resize-none focus-visible:ring-0 leading-relaxed p-0"
                    value={scene.script}
                    readOnly={!canEdit}
                    onChange={(e) => {
                      if (!canEdit) return;
                      updateScene(scene.id, { script: e.target.value });
                    }}
                    spellCheck={false}
                />
              </div>
            </ScrollArea>
          </TabsContent>
          
          <TabsContent value="storyboard" className="h-full m-0 p-8 overflow-auto absolute inset-0">
            <div className="grid grid-cols-2 lg:grid-cols-3 gap-6">
              {scene.shots.length > 0 ? scene.shots.map((shot) => (
                <div key={shot.id} className="group bg-zinc-900 border border-zinc-800 rounded-lg overflow-hidden hover:border-zinc-700 transition-all">
                  <div className="aspect-video bg-zinc-950 flex items-center justify-center relative">
                    <span className="text-zinc-700 font-bold text-4xl opacity-20">{shot.number}</span>
                    <Badge className="absolute top-2 left-2 bg-zinc-900/80 text-zinc-400 border-zinc-700">{shot.type}</Badge>
                    <Badge className="absolute bottom-2 right-2 bg-zinc-900/80 text-amber-500 border-zinc-700">{shot.duration}</Badge>
                  </div>
                  <div className="p-3">
                    <p className="text-sm text-zinc-300 font-medium mb-1">{shot.description}</p>
                    <p className="text-xs text-zinc-500 italic">{shot.camera}</p>
                  </div>
                </div>
              )) : (
                <div className="col-span-full flex flex-col items-center justify-center py-20 text-zinc-500">
                   <ImageIcon className="w-12 h-12 mb-4 opacity-20" />
                   <p>No shots generated yet.</p>
                   <Button variant="outline" className="mt-4 border-zinc-700">Generate Shot List</Button>
                </div>
              )}
            </div>
          </TabsContent>

          <TabsContent value="animatic" className="h-full m-0 flex flex-col items-center justify-center absolute inset-0 bg-black">
             {/* Video Player Container */}
             <div className="w-full h-full flex flex-col">
                <div className="flex-1 flex items-center justify-center bg-zinc-950 relative group">
                   {/* Mock Video Content */}
                   <div className="w-[80%] aspect-video bg-zinc-900 relative overflow-hidden rounded-sm shadow-2xl border border-zinc-800">
                      <div className="absolute inset-0 flex items-center justify-center">
                         <div className="text-center">
                            <Video className="w-16 h-16 text-zinc-800 mb-4 mx-auto" />
                            <span className="text-zinc-700 font-mono text-sm uppercase tracking-widest">
                               Preview (Animatic Mode)
                            </span>
                         </div>
                      </div>
                      <div className="absolute bottom-4 right-4 text-xs font-mono text-zinc-500 border border-zinc-800 px-2 py-1 rounded bg-black/50">
                         Not Final Render
                      </div>
                      
                      {/* Play Overlay */}
                      {!isPlaying && (
                         <div className="absolute inset-0 flex items-center justify-center bg-black/30 cursor-pointer" onClick={() => setIsPlaying(true)}>
                            <div className="w-16 h-16 rounded-full bg-white/10 backdrop-blur-sm border border-white/20 flex items-center justify-center hover:bg-white/20 transition-all hover:scale-105">
                               <Play className="w-8 h-8 text-white ml-1 fill-white" />
                            </div>
                         </div>
                      )}
                   </div>
                </div>

                {/* Player Controls */}
                <div className="h-16 bg-zinc-900 border-t border-zinc-800 px-6 flex flex-col justify-center gap-2">
                   <div className="flex items-center gap-4">
                      <span className="text-xs font-mono text-zinc-400 w-20">{formatTime(currentTime)}</span>
                      <Slider 
                         value={[currentTime]} 
                         max={duration} 
                         step={1} 
                         onValueChange={(val) => setCurrentTime(val[0])}
                         className="flex-1" 
                      />
                      <span className="text-xs font-mono text-zinc-500 w-20 text-right">{formatTime(duration)}</span>
                   </div>
                   
                   <div className="flex items-center justify-between">
                      <div className="flex items-center gap-4">
                         <Button variant="ghost" size="icon" className="h-8 w-8 text-zinc-400 hover:text-zinc-100" onClick={() => setCurrentTime(0)}>
                            <SkipBack className="w-4 h-4 fill-current" />
                         </Button>
                         <Button 
                            variant="ghost" 
                            size="icon" 
                            className="h-8 w-8 text-zinc-200 hover:text-white"
                            onClick={() => setIsPlaying(!isPlaying)}
                         >
                            {isPlaying ? <Pause className="w-5 h-5 fill-current" /> : <Play className="w-5 h-5 fill-current" />}
                         </Button>
                         <Button variant="ghost" size="icon" className="h-8 w-8 text-zinc-400 hover:text-zinc-100" onClick={() => setCurrentTime(duration)}>
                            <SkipForward className="w-4 h-4 fill-current" />
                         </Button>
                         
                         <Separator orientation="vertical" className="h-4 bg-zinc-700 mx-2" />
                         
                         <div className="flex items-center gap-2 group">
                            <Volume2 className="w-4 h-4 text-zinc-400" />
                            <div className="w-20">
                               <Slider defaultValue={[75]} max={100} step={1} className="w-full" />
                            </div>
                         </div>
                      </div>

                      <div className="flex items-center gap-2">
                         <Button variant="ghost" size="sm" className="text-xs text-zinc-500 hover:text-zinc-300 font-mono">
                            1080p
                         </Button>
                         <Button variant="ghost" size="icon" className="h-8 w-8 text-zinc-400 hover:text-zinc-100">
                            <Maximize2 className="w-4 h-4" />
                         </Button>
                      </div>
                   </div>
                </div>
             </div>
          </TabsContent>
        </div>
      </Tabs>
    </div>
  );
}
