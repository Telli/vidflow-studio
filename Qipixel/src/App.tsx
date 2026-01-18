import { useEffect, useState } from "react";
import { DndProvider } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import { Toaster } from "./components/ui/sonner";
import { ProjectProvider, useProject } from "./components/context/ProjectContext";
import { AuthProvider, useAuth } from "./components/context/AuthContext";
import { AppShell } from "./components/layout/AppShell";
import { ProjectDashboardPage } from "./routes/ProjectDashboardPage";
import { SceneReviewPage } from "./routes/SceneReviewPage";
import { StitchPlanPage } from "./routes/StitchPlanPage";
import { StoryBiblePage } from "./routes/StoryBiblePage";
import { ScenePlanner } from "./components/screens/ScenePlanner";
import { ExportScreen } from "./components/screens/ExportScreen";
import { AgentActivityOverlay } from "./components/overlays/AgentActivityOverlay";
import { AuthScreen } from "./components/auth/AuthScreen";
import { Loader2 } from "lucide-react";

function AppContent() {
  const { project } = useProject();
  const [currentRoute, setCurrentRoute] = useState("dashboard");
  const [currentSceneId, setCurrentSceneId] = useState(() => project.scenes[0]?.id ?? "");
  const [isActivityOpen, setIsActivityOpen] = useState(false);

  useEffect(() => {
    if (!currentSceneId && project.scenes.length > 0) {
      setCurrentSceneId(project.scenes[0].id);
    }
  }, [currentSceneId, project.scenes]);

  const handleNavigate = (route: string) => {
    if (route === 'activity') {
        setIsActivityOpen(true);
    } else {
        setCurrentRoute(route);
    }
  };

  const handleOpenScene = (sceneId: string) => {
    setCurrentSceneId(sceneId);
    setCurrentRoute("scene-review");
  };

  return (
    <AppShell currentRoute={currentRoute} onNavigate={handleNavigate}>
        <AgentActivityOverlay 
          open={isActivityOpen} 
          onOpenChange={setIsActivityOpen} 
          project={project} 
        />
        
        {/* Router Switch */}
        {(currentRoute === "dashboard" || currentRoute === "projects") && (
            <ProjectDashboardPage onOpenScene={handleOpenScene} />
        )}
        
        {currentRoute === "planner" && (
            <ScenePlanner />
        )}
        
        {currentRoute === "scene-review" && currentSceneId && (
            <SceneReviewPage 
                sceneId={currentSceneId} 
                onBack={() => setCurrentRoute("dashboard")} 
            />
        )}
        
        {(currentRoute === "bible" || currentRoute === "characters") && (
            <StoryBiblePage />
        )}

        {currentRoute === "stitch" && (
            <StitchPlanPage />
        )}
        
        {currentRoute === "renders" && (
            <ExportScreen project={project} />
        )}
    </AppShell>
  );
}

function AuthenticatedApp() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen bg-zinc-950 flex items-center justify-center">
        <Loader2 className="w-8 h-8 text-amber-500 animate-spin" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <AuthScreen />;
  }

  return (
    <ProjectProvider>
      <AppContent />
    </ProjectProvider>
  );
}

export default function App() {
  return (
    <DndProvider backend={HTML5Backend}>
      <AuthProvider>
        <Toaster />
        <AuthenticatedApp />
      </AuthProvider>
    </DndProvider>
  );
}
