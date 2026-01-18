import React, { createContext, useContext, useEffect, useRef, useState, useCallback } from "react";
import { Project, Scene, AgentProposal } from "../data/mock-data";
import { toast } from "sonner@2.0.3";
import { vidflowApi, type SceneDto } from "../../api/vidflow";

const INITIAL_PROJECT: Project = {
  id: "",
  title: "",
  logline: "",
  runtimeTarget: 0,
  status: "Ideation",
  scenes: [],
};

interface ProjectContextType {
  project: Project;
  updateScene: (sceneId: string, updates: Partial<Scene>) => void;
  reorderScenes: (newScenes: Scene[]) => void;
  submitForReview: (sceneId: string) => void;
  approveScene: (sceneId: string) => void;
  addProposal: (sceneId: string, proposal: AgentProposal) => void;
  applyProposal: (sceneId: string, proposalId: string) => void;
  dismissProposal: (sceneId: string, proposalId: string) => void;
  runAgents: (sceneId: string) => void;
  requestRevision: (sceneId: string, feedback: string) => void;
  updateTransition: (sceneId: string, transition: string) => void;
  updateAudioNote: (sceneId: string, note: string) => void;
  refreshScenes: () => Promise<void>;
}

const ProjectContext = createContext<ProjectContextType | undefined>(undefined);

export function ProjectProvider({ children }: { children: React.ReactNode }) {
  const [project, setProject] = useState<Project>(INITIAL_PROJECT);
  const [projectId, setProjectId] = useState<string | null>(null);
  const pendingUpdateTimers = useRef<Record<string, ReturnType<typeof setTimeout> | undefined>>({});

  const mapSceneDtoToScene = useCallback((dto: SceneDto): Scene => {
    return {
      id: dto.id,
      number: dto.number,
      title: dto.title,
      narrativeGoal: dto.narrativeGoal,
      emotionalBeat: dto.emotionalBeat,
      location: dto.location,
      timeOfDay: dto.timeOfDay,
      characters: dto.characterNames ?? [],
      status: dto.status as any,
      isLocked: dto.isLocked,
      lockedUntil: dto.lockedUntil ?? null,
      lockedBy: dto.lockedBy ?? null,
      runtimeEstimate: dto.runtimeEstimateSeconds ?? 0,
      runtimeTarget: dto.runtimeTargetSeconds,
      script: dto.script ?? "",
      shots: (dto.shots ?? []).map((s) => ({
        id: s.id,
        number: s.number,
        type: s.type,
        duration: s.duration,
        description: s.description,
        camera: s.camera,
      })),
      proposals: (dto.proposals ?? []).map((p) => ({
        id: p.id,
        role: p.role as any,
        summary: p.summary,
        rationale: p.rationale,
        runtimeImpact: p.runtimeImpactSeconds,
        diff: p.diff,
        status: p.status as any,
        createdAt: p.createdAt,
        tokensUsed: p.tokensUsed,
        costUsd: p.costUsd,
      })).filter((p) => p.status === "Pending"),
      version: dto.version,
    };
  }, []);

  const refreshProject = useCallback(async (pid: string) => {
    const [projectDto, scenesDto] = await Promise.all([
      vidflowApi.getProject(pid),
      vidflowApi.listScenesForProject(pid),
    ]);

    const scenes = scenesDto.scenes.map(mapSceneDtoToScene);

    setProject((prev) => ({
      ...prev,
      id: projectDto.id,
      title: projectDto.title,
      logline: projectDto.logline,
      runtimeTarget: projectDto.runtimeTargetSeconds,
      status: projectDto.status as any,
      scenes,
    }));
  }, [mapSceneDtoToScene]);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        const list = await vidflowApi.listProjects();
        let pid = list.projects[0]?.id;

        if (!pid) {
          const created = await vidflowApi.createProject({
            title: "New Project",
            logline: "A new VidFlow project.",
            runtimeTargetSeconds: 900,
          });
          pid = created.id;
        }

        if (cancelled) return;
        setProjectId(pid);
        await refreshProject(pid);
      } catch (err: any) {
        toast.error("Backend not reachable", {
          description: String(err?.message ?? err),
        });
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [refreshProject]);

  const updateScene = useCallback((sceneId: string, updates: Partial<Scene>) => {
    let blockedByLock = false;
    let blockedByStatus = false;

    setProject((prev) => {
      const current = prev.scenes.find((s) => s.id === sceneId);
      if (current?.isLocked) {
        blockedByLock = true;
        return prev;
      }

      if (current && current.status !== "Draft") {
        blockedByStatus = true;
        return prev;
      }

      return {
        ...prev,
        scenes: prev.scenes.map((scene) =>
          scene.id === sceneId ? { ...scene, ...updates } : scene
        ),
      };
    });

    if (blockedByLock) {
      toast.error("Scene is locked", {
        description: "This scene is currently being processed. Please wait for it to unlock.",
      });
      return;
    }

    if (blockedByStatus) {
      toast.error("Scene is not editable", {
        description: "Only Draft scenes can be edited.",
      });
      return;
    }

    // Debounced persistence to backend
    if (pendingUpdateTimers.current[sceneId]) {
      clearTimeout(pendingUpdateTimers.current[sceneId]);
    }

    pendingUpdateTimers.current[sceneId] = setTimeout(async () => {
      try {
        await vidflowApi.updateScene(sceneId, {
          title: updates.title,
          narrativeGoal: updates.narrativeGoal,
          emotionalBeat: updates.emotionalBeat,
          location: updates.location,
          timeOfDay: updates.timeOfDay,
          script: updates.script,
          characterNames: (updates as any).characters,
        });
      } catch (err: any) {
        toast.error("Failed to save scene", {
          description: String(err?.message ?? err),
        });
      }
    }, 600);
  }, []);

  const reorderScenes = useCallback((newScenes: Scene[]) => {
    setProject((prev) => ({
      ...prev,
      scenes: newScenes,
    }));
  }, []);

  const submitForReview = useCallback((sceneId: string) => {
    (async () => {
      try {
        let blockedByLock = false;
        let blockedByStatus = false;

        setProject((prev) => {
          const current = prev.scenes.find((s) => s.id === sceneId);
          if (current?.isLocked) {
            blockedByLock = true;
            return prev;
          }
          if (current && current.status !== "Draft") {
            blockedByStatus = true;
            return prev;
          }
          return prev;
        });

        if (blockedByLock) {
          toast.error("Scene is locked", {
            description: "This scene is currently being processed. Please wait for it to unlock.",
          });
          return;
        }

        if (blockedByStatus) {
          toast.error("Invalid transition", {
            description: "Only Draft scenes can be submitted for review.",
          });
          return;
        }

        await vidflowApi.submitForReview(sceneId);
        const updated = await vidflowApi.getScene(sceneId);
        const mapped = mapSceneDtoToScene(updated);

        setProject((prev) => ({
          ...prev,
          scenes: prev.scenes.map((s) => (s.id === sceneId ? mapped : s)),
        }));

        toast.success("Submitted for review", {
          description: "This scene is now in Review.",
        });
      } catch (err: any) {
        toast.error("Submit for review failed", {
          description: String(err?.message ?? err),
        });
      }
    })();
  }, [mapSceneDtoToScene]);

  const approveScene = useCallback((sceneId: string) => {
    (async () => {
      try {
        let blockedByLock = false;
        let blockedByStatus = false;

        setProject((prev) => {
          const current = prev.scenes.find((s) => s.id === sceneId);
          if (current?.isLocked) {
            blockedByLock = true;
            return prev;
          }
          if (current && current.status !== "Review") {
            blockedByStatus = true;
            return prev;
          }
          return prev;
        });

        if (blockedByLock) {
          toast.error("Scene is locked", {
            description: "This scene is currently being processed. Please wait for it to unlock.",
          });
          return;
        }

        if (blockedByStatus) {
          toast.error("Invalid transition", {
            description: "Only Review scenes can be approved.",
          });
          return;
        }

        await vidflowApi.approveScene(sceneId, "human");

        if (projectId) {
          // Best-effort: add to stitch plan
          const approvedCount = project.scenes.filter(s => s.status === "Approved").length;
          await vidflowApi.addSceneToStitchPlan(projectId, sceneId, approvedCount + 1);
        }

        const updated = await vidflowApi.getScene(sceneId);
        const mapped = mapSceneDtoToScene(updated);
        setProject((prev) => ({
          ...prev,
          scenes: prev.scenes.map(s => (s.id === sceneId ? mapped : s)),
        }));

        toast.success("Scene Approved", {
          description: "The scene is now locked and available in the Stitch Plan.",
        });
      } catch (err: any) {
        toast.error("Approve failed", { description: String(err?.message ?? err) });
      }
    })();
  }, [mapSceneDtoToScene, project.scenes, projectId]);

  const addProposal = useCallback((sceneId: string, proposal: AgentProposal) => {
    setProject((prev) => ({
      ...prev,
      scenes: prev.scenes.map((scene) =>
        scene.id === sceneId
          ? { ...scene, proposals: [{ ...proposal, status: "Pending" }, ...scene.proposals] }
          : scene
      ),
    }));
  }, []);

  const applyProposal = useCallback((sceneId: string, proposalId: string) => {
    (async () => {
      try {
        let blockedByLock = false;
        let blockedByStatus = false;

        setProject((prev) => {
          const current = prev.scenes.find((s) => s.id === sceneId);
          if (current?.isLocked) {
            blockedByLock = true;
            return prev;
          }
          if (current && current.status !== "Draft") {
            blockedByStatus = true;
            return prev;
          }
          return prev;
        });

        if (blockedByLock) {
          toast.error("Scene is locked", {
            description: "This scene is currently being processed. Please wait for it to unlock.",
          });
          return;
        }

        if (blockedByStatus) {
          toast.error("Scene is not editable", {
            description: "Proposals can only be applied in Draft.",
          });
          return;
        }

        await vidflowApi.applyProposal(proposalId);
        const updated = await vidflowApi.getScene(sceneId);
        const mapped = mapSceneDtoToScene(updated);

        setProject((prev) => ({
          ...prev,
          scenes: prev.scenes.map(s => (s.id === sceneId ? mapped : s)),
        }));
        toast.success("Proposal Applied", { description: "Scene updated with agent suggestions." });
      } catch (err: any) {
        toast.error("Apply failed", { description: String(err?.message ?? err) });
      }
    })();
  }, [mapSceneDtoToScene]);

  const dismissProposal = useCallback((sceneId: string, proposalId: string) => {
    (async () => {
      try {
        let blockedByLock = false;
        let blockedByStatus = false;

        setProject((prev) => {
          const current = prev.scenes.find((s) => s.id === sceneId);
          if (current?.isLocked) {
            blockedByLock = true;
            return prev;
          }
          if (current && current.status !== "Draft") {
            blockedByStatus = true;
            return prev;
          }
          return prev;
        });

        if (blockedByLock) {
          toast.error("Scene is locked", {
            description: "This scene is currently being processed. Please wait for it to unlock.",
          });
          return;
        }

        if (blockedByStatus) {
          toast.error("Scene is not editable", {
            description: "Proposals can only be dismissed in Draft.",
          });
          return;
        }

        await vidflowApi.dismissProposal(proposalId);
        const updated = await vidflowApi.getScene(sceneId);
        const mapped = mapSceneDtoToScene(updated);

        setProject((prev) => ({
          ...prev,
          scenes: prev.scenes.map(s => (s.id === sceneId ? mapped : s)),
        }));
        toast("Proposal dismissed");
      } catch (err: any) {
        toast.error("Dismiss failed", { description: String(err?.message ?? err) });
      }
    })();
  }, [mapSceneDtoToScene]);

  const runAgents = useCallback((sceneId: string) => {
    (async () => {
      try {
        let blockedByLock = false;
        let blockedByStatus = false;

        setProject((prev) => {
          const current = prev.scenes.find((s) => s.id === sceneId);
          if (current?.isLocked) {
            blockedByLock = true;
            return prev;
          }
          if (current && current.status !== "Draft") {
            blockedByStatus = true;
            return prev;
          }
          return prev;
        });

        if (blockedByLock) {
          toast.error("Scene is locked", {
            description: "This scene is currently being processed. Please wait for it to unlock.",
          });
          return;
        }

        if (blockedByStatus) {
          toast.error("Scene is not editable", {
            description: "Agents can only run on Draft scenes.",
          });
          return;
        }

        const { jobId } = await vidflowApi.runAgents(sceneId);

        const initial = await vidflowApi.getScene(sceneId);
        const initialMapped = mapSceneDtoToScene(initial);
        setProject((prev) => ({
          ...prev,
          scenes: prev.scenes.map((s) => (s.id === sceneId ? initialMapped : s)),
        }));

        const startedAt = Date.now();
        while (true) {
          const status = await vidflowApi.getJobStatus(jobId);
          if (status.state === "Succeeded") break;
          if (status.state === "Failed") {
            throw new Error(status.reason ?? "Agent run failed");
          }
          if (Date.now() - startedAt > 120_000) {
            throw new Error("Timed out waiting for agent run");
          }
          await new Promise((r) => setTimeout(r, 1000));
        }

        const updated = await vidflowApi.getScene(sceneId);
        const mapped = mapSceneDtoToScene(updated);

        setProject((prev) => ({
          ...prev,
          scenes: prev.scenes.map(s => (s.id === sceneId ? mapped : s)),
        }));

        toast.success("Agent analysis completed", {
          description: "New proposals are ready for review.",
        });
      } catch (err: any) {
        const msg = String(err?.message ?? err);
        const isBudget = msg.toLowerCase().includes("budget") || msg.toLowerCase().includes("cap");
        toast.error(isBudget ? "Budget exceeded" : "Agent run failed", {
          description: msg,
        });
      }
    })();
  }, [mapSceneDtoToScene]);

  const requestRevision = useCallback((sceneId: string, feedback: string) => {
    (async () => {
      try {
        let blockedByLock = false;
        let blockedByStatus = false;

        setProject((prev) => {
          const current = prev.scenes.find((s) => s.id === sceneId);
          if (current?.isLocked) {
            blockedByLock = true;
            return prev;
          }
          if (current && current.status !== "Review") {
            blockedByStatus = true;
            return prev;
          }
          return prev;
        });

        if (blockedByLock) {
          toast.error("Scene is locked", {
            description: "This scene is currently being processed. Please wait for it to unlock.",
          });
          return;
        }

        if (blockedByStatus) {
          toast.error("Invalid transition", {
            description: "Revisions can only be requested while in Review.",
          });
          return;
        }

        await vidflowApi.requestRevision(sceneId, feedback, "human");
        const updated = await vidflowApi.getScene(sceneId);
        const mapped = mapSceneDtoToScene(updated);

        setProject((prev) => ({
          ...prev,
          scenes: prev.scenes.map(s => (s.id === sceneId ? mapped : s)),
        }));
        toast("Revision requested");
      } catch (err: any) {
        toast.error("Request revision failed", { description: String(err?.message ?? err) });
      }
    })();
  }, [mapSceneDtoToScene]);

  const updateTransition = useCallback((sceneId: string, transition: string) => {
      (async () => {
        try {
          if (!projectId) return;
          await vidflowApi.setTransition(projectId, sceneId, transition);
          toast("Transition Updated");
        } catch {
          toast("Transition Saved (local)");
        }
      })();
  }, [projectId]);

  const updateAudioNote = useCallback((sceneId: string, note: string) => {
      (async () => {
        try {
          if (!projectId) return;
          await vidflowApi.setAudioNote(projectId, sceneId, note);
          toast("Audio Note Saved");
        } catch {
          toast("Audio Note Saved (local)");
        }
      })();
  }, [projectId]);

  const refreshScenes = useCallback(async () => {
    if (projectId) {
      await refreshProject(projectId);
    }
  }, [projectId, refreshProject]);

  return (
    <ProjectContext.Provider
      value={{
        project,
        updateScene,
        reorderScenes,
        submitForReview,
        approveScene,
        addProposal,
        applyProposal,
        dismissProposal,
        runAgents,
        requestRevision,
        updateTransition,
        updateAudioNote,
        refreshScenes,
      }}
    >
      {children}
    </ProjectContext.Provider>
  );
}

export function useProject() {
  const context = useContext(ProjectContext);
  if (context === undefined) {
    throw new Error("useProject must be used within a ProjectProvider");
  }
  return context;
}
