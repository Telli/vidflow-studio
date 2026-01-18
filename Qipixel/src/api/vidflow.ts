type HttpMethod = "GET" | "POST" | "PUT" | "DELETE";

// When running behind Vite proxy, use relative URLs (empty base)
// Set VITE_API_BASE_URL for direct backend access (e.g., production)
const DEFAULT_API_BASE_URL = "";

export function getApiBaseUrl(): string {
  const env = (import.meta as any).env;
  return (env?.VITE_API_BASE_URL as string | undefined) ?? DEFAULT_API_BASE_URL;
}

type RequestOptions = {
  method?: HttpMethod;
  body?: unknown;
  signal?: AbortSignal;
};

class HttpError extends Error {
  constructor(
    public status: number,
    public statusText: string,
    message: string
  ) {
    super(message);
    this.name = "HttpError";
  }

  get isRetryable(): boolean {
    // Retry on network errors, rate limits, and server errors
    return this.status === 429 || this.status === 503 || this.status === 504;
  }
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// Auth token storage
const AUTH_TOKEN_KEY = "vidflow_access_token";

export function getAuthToken(): string | null {
  return localStorage.getItem(AUTH_TOKEN_KEY);
}

export function setAuthToken(token: string): void {
  localStorage.setItem(AUTH_TOKEN_KEY, token);
}

export function clearAuthToken(): void {
  localStorage.removeItem(AUTH_TOKEN_KEY);
}

async function request<T>(
  path: string,
  options: RequestOptions = {}
): Promise<T> {
  const url = `${getApiBaseUrl()}${path}`;

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  const token = getAuthToken();
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const res = await fetch(url, {
    method: options.method ?? "GET",
    headers,
    body: options.body ? JSON.stringify(options.body) : undefined,
    signal: options.signal,
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new HttpError(res.status, res.statusText, `HTTP ${res.status} ${res.statusText}: ${text}`);
  }

  if (res.status === 204) {
    return undefined as T;
  }

  return (await res.json()) as T;
}

async function requestWithRetry<T>(
  path: string,
  options: RequestOptions = {},
  maxRetries = 3
): Promise<T> {
  let lastError: Error | null = null;

  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      return await request<T>(path, options);
    } catch (err) {
      lastError = err as Error;

      // Don't retry if it's the last attempt
      if (attempt === maxRetries - 1) break;

      // Only retry on retryable errors
      if (err instanceof HttpError && !err.isRetryable) break;

      // For network errors (non-HttpError), retry
      if (!(err instanceof HttpError)) {
        // Exponential backoff: 1s, 2s, 4s
        await delay(Math.pow(2, attempt) * 1000);
        continue;
      }

      // Retry with exponential backoff
      await delay(Math.pow(2, attempt) * 1000);
    }
  }

  throw lastError ?? new Error("Request failed after retries");
}

export type ProjectSummaryDto = {
  id: string;
  title: string;
  logline: string;
  runtimeTargetSeconds: number;
  status: string;
  createdAt: string;
  updatedAt: string;
  totalRuntimeSeconds: number;
  sceneCount: number;
  pendingReviewCount: number;
};

export type ListProjectsResponse = { projects: ProjectSummaryDto[] };

export type CreateProjectRequest = {
  title: string;
  logline: string;
  runtimeTargetSeconds: number;
};

export type CreateProjectResponse = {
  id: string;
  title: string;
  logline: string;
  runtimeTargetSeconds: number;
  status: string;
  createdAt: string;
};

export type GetProjectResponse = {
  id: string;
  title: string;
  logline: string;
  runtimeTargetSeconds: number;
  status: string;
  createdAt: string;
  updatedAt: string;
  totalRuntimeSeconds: number;
  sceneCount: number;
  pendingReviewCount: number;
};

export type SceneDto = {
  id: string;
  projectId: string;
  number: string;
  title: string;
  narrativeGoal: string;
  emotionalBeat: string;
  location: string;
  timeOfDay: string;
  status: string;
  runtimeEstimateSeconds: number;
  runtimeTargetSeconds: number;
  script: string;
  version: number;
  createdAt: string;
  updatedAt: string;
  isLocked: boolean;
  lockedUntil?: string | null;
  lockedBy?: string | null;
  characterNames: string[];
  shots: Array<{
    id: string;
    number: number;
    type: string;
    duration: string;
    description: string;
    camera: string;
  }>;
  proposals: Array<{
    id: string;
    role: string;
    summary: string;
    rationale: string;
    runtimeImpactSeconds: number;
    diff: string;
    status: string;
    createdAt: string;
    tokensUsed: number;
    costUsd: number;
  }>;
};

export type ListScenesForProjectResponse = { scenes: SceneDto[] };

export type GetSceneResponse = SceneDto;

export type CreateSceneRequest = {
  number: string;
  title: string;
  narrativeGoal: string;
  emotionalBeat: string;
  location: string;
  timeOfDay: string;
  runtimeTargetSeconds: number;
  characterNames?: string[];
};

export type CreateSceneResponse = {
  id: string;
  number: string;
  title: string;
  status: string;
  version: number;
  createdAt: string;
};

export type UpdateSceneRequest = {
  title?: string;
  narrativeGoal?: string;
  emotionalBeat?: string;
  location?: string;
  timeOfDay?: string;
  script?: string;
  characterNames?: string[];
};

export type RunAgentsResponse = {
  success: boolean;
  proposals: Array<{
    id: string;
    role: string;
    summary: string;
    rationale: string;
    runtimeImpactSeconds: number;
    diff: string;
    status: string;
    createdAt: string;
    tokensUsed: number;
    costUsd: number;
  }>;
  failedAtRole?: string;
  errorMessage?: string;
};

export type RunAgentsJobResponse = {
  jobId: string;
  sceneId: string;
};

export type JobStatusResponse = {
  jobId: string;
  state: string;
  createdAt?: string;
  lastStateChangedAt?: string;
  reason?: string;
};

// Render types
export type RenderType = "Animatic" | "Scene" | "Final";
export type RenderStatus = "Queued" | "Processing" | "Completed" | "Failed";

export type RenderJobDto = {
  id: string;
  sceneId?: string;
  type: RenderType;
  status: RenderStatus;
  progressPercent: number;
  artifactPath?: string;
  createdAt: string;
  completedAt?: string;
};

export type ListRenderJobsResponse = {
  projectId: string;
  jobs: RenderJobDto[];
};

export type RenderJobResponse = {
  jobId: string;
  sceneId: string;
  type: RenderType;
  status: RenderStatus;
  createdAt: string;
};

// Budget types
export type AgentCostBreakdown = {
  role: string;
  proposalCount: number;
  totalTokens: number;
  totalCostUsd: number;
};

export type SceneCostBreakdown = {
  sceneId: string;
  sceneTitle: string;
  proposalCount: number;
  totalCostUsd: number;
};

export type ProjectCostsResponse = {
  projectId: string;
  budgetCapUsd: number;
  currentSpendUsd: number;
  remainingBudget: number;
  budgetUtilizationPercent: number;
  costsByAgent: AgentCostBreakdown[];
  costsByScene: SceneCostBreakdown[];
  totalProposals: number;
  totalTokensUsed: number;
};

// Story Bible types
export type StoryBibleResponse = {
  id: string;
  projectId: string;
  themes: string;
  worldRules: string;
  tone: string;
  visualStyle: string;
  pacingRules: string;
  version: number;
  createdAt: string;
};

// Character types
export type CharacterRelationshipDto = {
  name: string;
  type: string;
  note: string;
};

export type CharacterDto = {
  id: string;
  name: string;
  role: string;
  archetype: string;
  age: string;
  description: string;
  backstory: string;
  traits: string[];
  relationships: CharacterRelationshipDto[];
  version: number;
  createdAt: string;
  updatedAt: string;
};

export type CreateCharacterRequest = {
  name: string;
  role: string;
  archetype?: string;
  age?: string;
  description?: string;
  backstory?: string;
  traits?: string[];
};

// Asset types
export type AssetType = "Storyboard" | "Animatic" | "Render" | "Audio";
export type AssetStatus = "Pending" | "Generating" | "Ready" | "Failed";

// Auth types
export type RegisterRequest = {
  email: string;
  password: string;
};

export type RegisterResponse = {
  id: string;
  email: string;
  token: string;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type LoginResponse = {
  id: string;
  email: string;
  token: string;
};

export type MeResponse = {
  id: string;
  email: string;
};

export type AssetDto = {
  id: string;
  projectId: string;
  sceneId?: string;
  shotId?: string;
  name: string;
  type: AssetType;
  status: AssetStatus;
  url?: string;
  createdAt: string;
};

export const vidflowApi = {
  listProjects: () => request<ListProjectsResponse>("/api/projects"),
  createProject: (body: CreateProjectRequest) => request<CreateProjectResponse>("/api/projects", { method: "POST", body }),
  getProject: (projectId: string) => request<GetProjectResponse>(`/api/projects/${projectId}`),

  listScenesForProject: (projectId: string) => request<ListScenesForProjectResponse>(`/api/projects/${projectId}/scenes`),
  getScene: (sceneId: string) => request<GetSceneResponse>(`/api/scenes/${sceneId}`),
  createScene: (projectId: string, body: CreateSceneRequest) => request<CreateSceneResponse>(`/api/projects/${projectId}/scenes`, { method: "POST", body }),
  updateScene: (sceneId: string, body: UpdateSceneRequest) => request<any>(`/api/scenes/${sceneId}`, { method: "PUT", body }),

  submitForReview: (sceneId: string) => request<any>(`/api/scenes/${sceneId}/submit-for-review`, { method: "POST" }),
  approveScene: (sceneId: string, approvedBy: string) => request<any>(`/api/scenes/${sceneId}/approve`, { method: "POST", body: { approvedBy } }),
  requestRevision: (sceneId: string, feedback: string, requestedBy: string) => request<any>(`/api/scenes/${sceneId}/request-revision`, { method: "POST", body: { feedback, requestedBy } }),

  addShot: (sceneId: string, body: { type: string; duration: string; description: string; camera: string }) => request<any>(`/api/scenes/${sceneId}/shots`, { method: "POST", body }),

  runAgents: (sceneId: string) => request<RunAgentsJobResponse>(`/api/scenes/${sceneId}/run-agents`, { method: "POST" }),

  getJobStatus: (jobId: string) => request<JobStatusResponse>(`/api/jobs/${jobId}`),

  applyProposal: (proposalId: string) => request<any>(`/api/proposals/${proposalId}/apply`, { method: "POST" }),
  dismissProposal: (proposalId: string) => request<any>(`/api/proposals/${proposalId}/dismiss`, { method: "POST" }),

  addSceneToStitchPlan: (projectId: string, sceneId: string, order: number) =>
    request<any>(`/api/projects/${projectId}/stitch-plan/scenes`, { method: "POST", body: { sceneId, order } }),

  setTransition: (projectId: string, sceneId: string, transitionType: string, transitionNotes?: string) =>
    request<any>(`/api/projects/${projectId}/stitch-plan/scenes/${sceneId}/transition`, { method: "PUT", body: { transitionType, transitionNotes } }),

  setAudioNote: (projectId: string, sceneId: string, audioNotes: string) =>
    request<any>(`/api/projects/${projectId}/stitch-plan/scenes/${sceneId}/audio`, { method: "PUT", body: { audioNotes } }),

  // Export endpoints
  exportStoryboardPdfUrl: (projectId: string) => `${getApiBaseUrl()}/api/projects/${projectId}/export/storyboard.pdf`,

  // Render endpoints
  listRenderJobs: (projectId: string) => request<ListRenderJobsResponse>(`/api/projects/${projectId}/render-jobs`),
  getRenderJob: (jobId: string) => request<RenderJobDto>(`/api/render-jobs/${jobId}`),
  requestAnimatic: (sceneId: string) => request<RenderJobResponse>(`/api/scenes/${sceneId}/render-animatic`, { method: "POST" }),
  requestSceneRender: (sceneId: string) => request<RenderJobResponse>(`/api/scenes/${sceneId}/render`, { method: "POST" }),
  requestFinalRender: (projectId: string) => request<RenderJobResponse>(`/api/projects/${projectId}/render-final`, { method: "POST" }),

  // Budget endpoints
  getProjectCosts: (projectId: string) => request<ProjectCostsResponse>(`/api/projects/${projectId}/costs`),
  setProjectBudget: (projectId: string, budgetCapUsd: number) =>
    request<any>(`/api/projects/${projectId}/budget`, { method: "PUT", body: { budgetCapUsd } }),

  // Story Bible endpoints
  getStoryBible: (projectId: string) => request<StoryBibleResponse>(`/api/projects/${projectId}/story-bible`),
  generateStoryBible: (projectId: string) => request<StoryBibleResponse>(`/api/projects/${projectId}/story-bible/generate`, { method: "POST" }),

  // Characters endpoints
  getCharacters: (projectId: string) => request<CharacterDto[]>(`/api/projects/${projectId}/characters`),
  createCharacter: (projectId: string, body: CreateCharacterRequest) =>
    request<CharacterDto>(`/api/projects/${projectId}/characters`, { method: "POST", body }),

  // Assets endpoints
  listAssets: (projectId: string) => request<AssetDto[]>(`/api/projects/${projectId}/assets`),
  getAsset: (assetId: string) => request<AssetDto>(`/api/assets/${assetId}`),
  createStoryboardAsset: (projectId: string, sceneId: string, shotId: string) =>
    request<AssetDto>(`/api/projects/${projectId}/assets/storyboard`, { method: "POST", body: { sceneId, shotId } }),

  // Auth endpoints
  register: (body: RegisterRequest) => request<RegisterResponse>("/api/auth/register", { method: "POST", body }),
  login: (body: LoginRequest) => request<LoginResponse>("/api/auth/login", { method: "POST", body }),
  me: () => request<MeResponse>("/api/auth/me"),
};
