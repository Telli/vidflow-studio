import { Project, Scene } from "../data/mock-data";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "../ui/card";
import { Button } from "../ui/button";
import { StatusBadge } from "../common/StatusBadge";
import { Clock, Plus, Bot, ChevronRight } from "lucide-react";
import { Progress } from "../ui/progress";
import { useProject } from "../context/ProjectContext";

interface DashboardProps {
  // project: Project; // Removed as it's now in context
  // BUT to keep props compatible if passed from parent, we can keep it optional or just ignore it
  // Ideally we refactor parent to not pass it.
  // We'll update interface to allow optional project or just ignore props.
  project?: Project;
  onOpenScene: (sceneId: string) => void;
}

export function Dashboard({ onOpenScene }: DashboardProps) {
  const { project } = useProject();
  
  const totalRuntime = project.scenes.reduce((acc, s) => acc + s.runtimeEstimate, 0);
  const progress = Math.min((totalRuntime / project.runtimeTarget) * 100, 100);

  return (
    <div className="p-8 space-y-8 max-w-7xl mx-auto animate-in fade-in duration-500">
      {/* Header Section */}
      <div className="flex justify-between items-start">
        <div className="space-y-1">
          <div className="flex items-center gap-3">
            <h1 className="text-3xl font-bold tracking-tight text-zinc-100">{project.title}</h1>
            <StatusBadge status={project.status} />
          </div>
          <p className="text-zinc-400 max-w-2xl text-lg">{project.logline}</p>
        </div>
        <div className="flex gap-3">
          <Button variant="outline" className="border-zinc-700 hover:bg-zinc-800 text-zinc-300">
            Project Settings
          </Button>
          <Button className="bg-amber-600 hover:bg-amber-700 text-white">
            <Plus className="w-4 h-4 mr-2" />
            New Scene
          </Button>
        </div>
      </div>

      {/* Metrics Bar */}
      <div className="grid grid-cols-4 gap-4">
        <Card className="bg-zinc-900 border-zinc-800">
          <CardContent className="p-6">
            <div className="text-sm font-medium text-zinc-500 mb-2">Total Runtime</div>
            <div className="text-2xl font-bold text-zinc-100 flex items-baseline gap-2">
              {Math.floor(totalRuntime / 60)}m {totalRuntime % 60}s
              <span className="text-sm font-normal text-zinc-500">/ {Math.floor(project.runtimeTarget / 60)}m target</span>
            </div>
            <Progress value={progress} className="h-1 mt-4 bg-zinc-800" />
          </CardContent>
        </Card>
        
        <Card className="bg-zinc-900 border-zinc-800">
          <CardContent className="p-6">
            <div className="text-sm font-medium text-zinc-500 mb-2">Scene Count</div>
            <div className="text-2xl font-bold text-zinc-100">{project.scenes.length}</div>
            <div className="text-xs text-zinc-500 mt-1">4 Acts Structured</div>
          </CardContent>
        </Card>

        <Card className="bg-zinc-900 border-zinc-800">
          <CardContent className="p-6">
            <div className="text-sm font-medium text-zinc-500 mb-2">Pending Reviews</div>
            <div className="text-2xl font-bold text-amber-500">
              {project.scenes.filter(s => s.status === 'Review').length}
            </div>
            <div className="text-xs text-zinc-500 mt-1">Requires attention</div>
          </CardContent>
        </Card>

        <Card className="bg-zinc-900 border-zinc-800">
          <CardContent className="p-6">
            <div className="text-sm font-medium text-zinc-500 mb-2">Agent Activity</div>
            <div className="text-2xl font-bold text-zinc-100">
                {project.scenes.reduce((acc, s) => acc + s.proposals.length, 0)}
            </div>
            <div className="text-xs text-zinc-500 mt-1">Total Proposals</div>
          </CardContent>
        </Card>
      </div>

      {/* Scene Grid */}
      <div className="space-y-4">
        <h2 className="text-xl font-semibold text-zinc-200">Scenes</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {project.scenes.map((scene) => (
            <Card 
              key={scene.id} 
              className="bg-zinc-900 border-zinc-800 hover:border-zinc-700 transition-colors cursor-pointer group"
              onClick={() => onOpenScene(scene.id)}
            >
              <CardHeader className="pb-3">
                <div className="flex justify-between items-start mb-1">
                  <span className="text-xs font-mono text-zinc-500 uppercase tracking-wider">Scene {scene.number}</span>
                  <StatusBadge status={scene.status} />
                </div>
                <CardTitle className="text-lg text-zinc-100 group-hover:text-amber-500 transition-colors">
                  {scene.title}
                </CardTitle>
                <CardDescription className="line-clamp-2 text-zinc-500">
                  {scene.narrativeGoal}
                </CardDescription>
              </CardHeader>
              <CardContent className="pb-3">
                <div className="flex gap-2 flex-wrap mb-4">
                  {scene.characters.map(char => (
                    <span key={char} className="text-xs px-2 py-1 bg-zinc-800 text-zinc-400 rounded-full border border-zinc-700">
                      {char}
                    </span>
                  ))}
                </div>
                <div className="flex items-center gap-4 text-xs text-zinc-400">
                  <div className="flex items-center gap-1">
                    <Clock className="w-3 h-3" />
                    {Math.floor(scene.runtimeEstimate / 60)}:{(scene.runtimeEstimate % 60).toString().padStart(2, '0')}
                  </div>
                  {scene.proposals.length > 0 && (
                    <div className="flex items-center gap-1 text-amber-500">
                      <Bot className="w-3 h-3" />
                      {scene.proposals.length} Proposals
                    </div>
                  )}
                </div>
              </CardContent>
              <CardFooter className="pt-3 border-t border-zinc-800/50 flex justify-end">
                <Button variant="ghost" size="sm" className="h-8 text-zinc-400 hover:text-zinc-100 p-0 hover:bg-transparent">
                  Open Scene <ChevronRight className="w-4 h-4 ml-1" />
                </Button>
              </CardFooter>
            </Card>
          ))}
          
          {/* Create New Scene Card */}
          <Card className="bg-zinc-900/30 border-zinc-800 border-dashed hover:border-zinc-700 hover:bg-zinc-900/50 transition-all cursor-pointer flex items-center justify-center min-h-[200px]">
            <div className="flex flex-col items-center text-zinc-500 gap-2">
              <div className="w-10 h-10 rounded-full bg-zinc-800 flex items-center justify-center">
                <Plus className="w-5 h-5" />
              </div>
              <span className="font-medium">Create Scene</span>
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
