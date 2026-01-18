import { useState, useEffect } from "react";
import { Project, Scene, AgentProposal } from "../data/mock-data";
import { Button } from "../ui/button";
import { StatusBadge } from "../common/StatusBadge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "../ui/tabs";
import { ScrollArea } from "../ui/scroll-area";
import { Badge } from "../ui/badge";
import { Separator } from "../ui/separator";
import { RevisionModal } from "../modals/RevisionModal";
import { Slider } from "../ui/slider";
import { Textarea } from "../ui/textarea";
import { 
  ArrowLeft, Clock, MapPin, Users, History, 
  Bot, Check, X, MessageSquare, Play, Video, 
  FileText, Image as ImageIcon, ChevronDown, ChevronRight,
  Volume2, Maximize2, Pause, SkipBack, SkipForward, Loader2
} from "lucide-react";
import { cn } from "../ui/utils";
import { toast } from "sonner@2.0.3";
import { useProject } from "../context/ProjectContext";

interface SceneWorkspaceProps {
  sceneId: string;
  onBack: () => void;
}

export function SceneWorkspace({ sceneId, onBack }: SceneWorkspaceProps) {
  const { project, updateScene, approveScene, addProposal, applyProposal } = useProject();
  const scene = project.scenes.find(s => s.id === sceneId);
  
  if (!scene) return null;

  const [activeTab, setActiveTab] = useState("script");
  const [isRevisionModalOpen, setIsRevisionModalOpen] = useState(false);
  
  // Agent Simulation State
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  
  // Video Player State
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const duration = scene.runtimeEstimate;

  // Mock Agent Analysis
  const handleRunAnalysis = () => {
    setIsAnalyzing(true);
    setTimeout(() => {
      const newProposal: AgentProposal = {
        id: `p-new-${Date.now()}`,
        role: "Editor",
        summary: "Pacing Adjustment",
        rationale: "The current cut feels dragging in the middle section. Suggest trimming the reaction shots.",
        runtimeImpact: -8,
        diff: "Trimmed shots 3-5 by 2s each. Added cross-dissolve to smooth transition."
      };
      addProposal(scene.id, newProposal);
      setIsAnalyzing(false);
      toast.success("Agent analysis complete", {
        description: "New proposal added by Editor Agent.",
      });
    }, 2500);
  };

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
    <div className="flex flex-col h-screen bg-zinc-950 overflow-hidden animate-in fade-in duration-300">
      <RevisionModal 
        open={isRevisionModalOpen} 
        onOpenChange={setIsRevisionModalOpen} 
        onSubmit={() => setIsRevisionModalOpen(false)} 
      />

      {/* Top Bar */}
      <div className="h-14 border-b border-zinc-800 flex items-center justify-between px-4 bg-zinc-950 flex-shrink-0">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" onClick={onBack} className="text-zinc-400 hover:text-zinc-100">
            <ArrowLeft className="w-5 h-5" />
          </Button>
          <div className="flex items-center gap-3">
            <span className="font-mono text-zinc-500 font-bold text-lg">{scene.number}</span>
            <span className="font-semibold text-zinc-100 text-lg">{scene.title}</span>
            <StatusBadge status={scene.status} />
          </div>
        </div>
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 px-3 py-1.5 bg-zinc-900 rounded-md border border-zinc-800">
             <Clock className="w-4 h-4 text-zinc-500" />
             <span className="text-sm font-mono text-zinc-300">
               {Math.floor(scene.runtimeEstimate / 60)}:{(scene.runtimeEstimate % 60).toString().padStart(2, '0')}
             </span>
             <span className="text-xs text-zinc-500">/ {scene.runtimeTarget}s</span>
          </div>
          <Button variant="ghost" size="sm" className="text-zinc-400">
            <History className="w-4 h-4 mr-2" />
            v{scene.version}
          </Button>
          {scene.status !== 'Approved' && (
              <Button 
                  className="bg-teal-700 hover:bg-teal-600 text-white"
                  onClick={() => approveScene(scene.id)}
              >
                <Check className="w-4 h-4 mr-2" />
                Approve Scene
              </Button>
          )}
          {scene.status === 'Approved' && (
              <Button disabled className="bg-teal-900/50 text-teal-400 border border-teal-900">
                <Check className="w-4 h-4 mr-2" />
                Approved
              </Button>
          )}
        </div>
      </div>

      {/* Main Layout */}
      <div className="flex-1 flex overflow-hidden">
        
        {/* LEFT PANEL: Context */}
        <div className="w-80 border-r border-zinc-800 bg-zinc-925 flex flex-col flex-shrink-0">
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

        {/* CENTER PANEL: Content */}
        <div className="flex-1 flex flex-col min-w-0 bg-zinc-950">
          <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col">
            <div className="border-b border-zinc-800 px-4 bg-zinc-950">
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
            </div>

            <div className="flex-1 bg-zinc-950 relative">
              <TabsContent value="script" className="h-full m-0 p-0 absolute inset-0">
                <ScrollArea className="h-full">
                  <div className="max-w-3xl mx-auto py-12 px-8 min-h-full bg-zinc-950">
                    <Textarea 
                        className="w-full h-[80vh] bg-transparent border-0 text-zinc-300 font-mono text-base resize-none focus-visible:ring-0 leading-relaxed p-0"
                        value={scene.script}
                        onChange={(e) => updateScene(scene.id, { script: e.target.value })}
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

          {/* Bottom Action Bar */}
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
                <Button 
                  variant="secondary" 
                  onClick={() => setIsRevisionModalOpen(true)}
                  className="bg-zinc-800 text-amber-500 hover:bg-zinc-700 border border-zinc-700 border-amber-900/30"
                >
                  Request Revision
                </Button>
             </div>
          </div>
        </div>

        {/* RIGHT PANEL: Agents */}
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
                        <Button size="sm" className="flex-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-300 h-8 border border-zinc-700">
                          Dismiss
                        </Button>
                        <Button 
                            size="sm" 
                            className="flex-1 bg-zinc-100 hover:bg-white text-zinc-900 h-8 font-semibold"
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
                   onClick={handleRunAnalysis}
                 >
                   <Bot className="w-4 h-4 mr-2" />
                   Run New Analysis
                 </Button>
              )}
            </div>
          </ScrollArea>
        </div>
      </div>
    </div>
  );
}
