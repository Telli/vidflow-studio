import { useState, useEffect } from "react";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "../components/ui/card";
import { Button } from "../components/ui/button";
import { StatusBadge } from "../components/common/StatusBadge";
import { Clock, Plus, Bot, ChevronRight, DollarSign, AlertTriangle, Loader2 } from "lucide-react";
import { Progress } from "../components/ui/progress";
import { useProject } from "../components/context/ProjectContext";
import { Scene } from "../data/mock-data";
import { vidflowApi, ProjectCostsResponse } from "../api/vidflow";
import { CreateSceneModal } from "../components/modals/CreateSceneModal";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "../components/ui/dialog";
import { Input } from "../components/ui/input";
import { Label } from "../components/ui/label";
import { toast } from "sonner@2.0.3";

interface SceneCardProps {
  scene: Scene;
  onClick: () => void;
}

function SceneCard({ scene, onClick }: SceneCardProps) {
  return (
    <Card 
      className="bg-zinc-900 border-zinc-800 hover:border-zinc-700 transition-colors cursor-pointer group"
      onClick={onClick}
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
          {scene.characters.map((char: string) => (
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
  );
}

interface ProjectDashboardPageProps {
  onOpenScene: (sceneId: string) => void;
}

export function ProjectDashboardPage({ onOpenScene }: ProjectDashboardPageProps) {
  const { project, refreshScenes } = useProject();
  const [costs, setCosts] = useState<ProjectCostsResponse | null>(null);
  const [costsLoading, setCostsLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showBudgetModal, setShowBudgetModal] = useState(false);
  const [budgetCapInput, setBudgetCapInput] = useState<string>("");

  const totalRuntime = project.scenes.reduce((acc, s) => acc + s.runtimeEstimate, 0);
  const progress = Math.min((totalRuntime / project.runtimeTarget) * 100, 100);
  const nextSceneNumber = String(project.scenes.length + 1);

  useEffect(() => {
    async function loadCosts() {
      setCostsLoading(true);
      try {
        const costsData = await vidflowApi.getProjectCosts(project.id);
        setCosts(costsData);
      } catch (err) {
        console.error("Failed to load project costs:", err);
      } finally {
        setCostsLoading(false);
      }
    }
    loadCosts();
  }, [project.id]);

  useEffect(() => {
    if (!showBudgetModal) return;
    setBudgetCapInput(String(costs?.budgetCapUsd ?? ""));
  }, [showBudgetModal, costs?.budgetCapUsd]);

  async function handleSaveBudget() {
    const parsed = Number(budgetCapInput);
    if (!Number.isFinite(parsed) || parsed < 0) {
      toast.error("Invalid budget", { description: "Enter a non-negative number." });
      return;
    }

    try {
      await vidflowApi.setProjectBudget(project.id, parsed);
      const refreshed = await vidflowApi.getProjectCosts(project.id);
      setCosts(refreshed);
      setShowBudgetModal(false);
      toast.success("Budget updated", { description: `Budget cap set to $${parsed.toFixed(2)}` });
    } catch (err: any) {
      toast.error("Failed to update budget", { description: String(err?.message ?? err) });
    }
  }

  async function handleSceneCreated() {
    await refreshScenes();
  }

  return (
    <div className="p-8 space-y-8 max-w-7xl mx-auto animate-in fade-in duration-500">
      <Dialog open={showBudgetModal} onOpenChange={setShowBudgetModal}>
        <DialogContent className="bg-zinc-900 border-zinc-800 text-zinc-100 sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Set Project Budget</DialogTitle>
            <DialogDescription className="text-zinc-500">
              Set a USD cap for agent runs. When exceeded, agents will stop and the run will fail.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-2">
            <Label className="text-zinc-300">Budget cap (USD)</Label>
            <Input
              type="number"
              min={0}
              step={0.01}
              value={budgetCapInput}
              onChange={(e) => setBudgetCapInput(e.target.value)}
              className="bg-zinc-950 border-zinc-800 text-zinc-200"
            />
          </div>

          <DialogFooter>
            <Button variant="ghost" onClick={() => setShowBudgetModal(false)} className="text-zinc-400 hover:text-zinc-200">
              Cancel
            </Button>
            <Button onClick={handleSaveBudget} className="bg-amber-600 hover:bg-amber-700 text-white">
              Save
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* ProjectHeader */}
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
          <Button
            className="bg-amber-600 hover:bg-amber-700 text-white"
            onClick={() => setShowCreateModal(true)}
          >
            <Plus className="w-4 h-4 mr-2" />
            New Scene
          </Button>
        </div>
      </div>

      {/* Metrics (Could be own component) */}
      <div className="grid grid-cols-5 gap-4">
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

        <Card className="bg-zinc-900 border-zinc-800">
          <CardContent className="p-6">
            <div className="text-sm font-medium text-zinc-500 mb-2 flex items-center gap-1">
              <DollarSign className="w-3 h-3" />
              Budget
            </div>
            {costsLoading ? (
              <div className="flex items-center justify-center py-2">
                <Loader2 className="w-5 h-5 animate-spin text-zinc-600" />
              </div>
            ) : costs ? (
              <>
                <div className={`text-2xl font-bold ${costs.budgetUtilizationPercent > 80 ? 'text-red-500' : costs.budgetUtilizationPercent > 50 ? 'text-amber-500' : 'text-green-500'}`}>
                  ${costs.currentSpendUsd.toFixed(2)}
                </div>
                <div className="text-xs text-zinc-500 mt-1 flex items-center gap-1">
                  {costs.budgetCapUsd > 0 ? (
                    <>
                      / ${costs.budgetCapUsd.toFixed(2)} cap
                      {costs.budgetUtilizationPercent > 80 && (
                        <AlertTriangle className="w-3 h-3 text-red-500" />
                      )}
                    </>
                  ) : (
                    "No budget set"
                  )}
                </div>
                {costs.budgetCapUsd > 0 && (
                  <Progress
                    value={Math.min(costs.budgetUtilizationPercent, 100)}
                    className={`h-1 mt-4 bg-zinc-800 ${costs.budgetUtilizationPercent > 80 ? '[&>div]:bg-red-500' : costs.budgetUtilizationPercent > 50 ? '[&>div]:bg-amber-500' : '[&>div]:bg-green-500'}`}
                  />
                )}
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowBudgetModal(true)}
                  className="mt-4 border-zinc-700 hover:bg-zinc-800 text-zinc-300 w-full"
                >
                  Set Budget
                </Button>
              </>
            ) : (
              <div className="text-2xl font-bold text-zinc-600">—</div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* SceneSummaryGrid */}
      <div className="space-y-4">
        <h2 className="text-xl font-semibold text-zinc-200">Scenes</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {project.scenes.map((scene) => (
            <SceneCard 
              key={scene.id} 
              scene={scene}
              onClick={() => onOpenScene(scene.id)}
            />
          ))}
          
          {/* Create New Scene Card */}
          <Card
            className="bg-zinc-900/30 border-zinc-800 border-dashed hover:border-zinc-700 hover:bg-zinc-900/50 transition-all cursor-pointer flex items-center justify-center min-h-[200px]"
            onClick={() => setShowCreateModal(true)}
          >
            <div className="flex flex-col items-center text-zinc-500 gap-2">
              <div className="w-10 h-10 rounded-full bg-zinc-800 flex items-center justify-center">
                <Plus className="w-5 h-5" />
              </div>
              <span className="font-medium">Create Scene</span>
            </div>
          </Card>
        </div>
      </div>

      <CreateSceneModal
        open={showCreateModal}
        onOpenChange={setShowCreateModal}
        projectId={project.id}
        nextSceneNumber={nextSceneNumber}
        onSceneCreated={handleSceneCreated}
      />
    </div>
  );
}
