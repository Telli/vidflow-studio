Below is the **Technical PRD for VidFlow Studio Mode (v1)**.
This is written so it can be handed directly to **engineers, AI agents (Codex/Kiro/Cursor), or investors** and translated into code with minimal interpretation.

---

# 🎬 VidFlow Studio — Technical PRD (v1)

## 1. System Overview

VidFlow Studio is an **event-driven, agent-orchestrated creative system** for long-form video (5–20 min), built around **scene-level determinism, human approval gates, and non-destructive AI proposals**.

### Architectural North Star

> **Scene-first, event-driven, human-approved, agent-assisted.**

---

## 2. High-Level Architecture

```text
┌─────────────┐
│   Frontend  │  (Studio UI)
└──────┬──────┘
       │ REST / WebSocket
┌──────▼─────────────────────────┐
│        Application API          │
│  (Projects, Scenes, Reviews)   │
└──────┬─────────────────────────┘
       │ emits domain events
┌──────▼───────────┐
│   Event Store    │  (append-only)
└──────┬───────────┘
       │ subscribes
┌──────▼──────────────────────────┐
│  Agent Orchestration Engine     │
│  (deterministic pipeline)       │
└──────┬──────────────────────────┘
       │ calls
┌──────▼──────────┐   ┌───────────▼──────────┐
│ Agent Runtimes  │   │ Render / Media Stack  │
│ (LLMs)          │   │ (video generation)   │
└─────────────────┘   └──────────────────────┘
```

---

## 3. Tech Stack (Reference Implementation)

### Backend

* **ASP.NET Core 8+** (or Node.js equivalent)
* PostgreSQL (primary DB)
* JSONB for diffs and prompts
* Background workers (Hangfire / BullMQ / Temporal)
* Redis (optional caching, locks)

### Frontend

* React + TypeScript
* Zustand or Redux for state
* WebSockets / SignalR for agent updates
* Timeline + diff UI (custom)

### AI / Agents

* Pluggable LLM adapters:

  * OpenAI
  * Anthropic
  * Gemini
  * Local (Ollama / GGUF)
* Prompt templates versioned in code

### Media

* Animatic: low-res frames
* Scene render: per-scene
* Final stitch: ffmpeg / cloud renderer

---

## 4. Core Domain Services

### 4.1 Project Service

Responsibilities:

* Project lifecycle
* Runtime targets
* Permissions

Endpoints:

```http
POST /projects
GET /projects/{id}
PATCH /projects/{id}
```

---

### 4.2 Story Bible Service

Responsibilities:

* Versioned creative constraints
* Input to all agents

Endpoints:

```http
POST /projects/{id}/story-bible
GET /projects/{id}/story-bible
```

---

### 4.3 Scene Service (Critical)

Responsibilities:

* Scene CRUD
* Versioning
* Status transitions

Endpoints:

```http
POST /projects/{id}/scenes
GET /scenes/{id}
PATCH /scenes/{id}
POST /scenes/{id}/submit-review
POST /scenes/{id}/approve
POST /scenes/{id}/request-revision
```

Rules:

* Only `draft` scenes are editable
* Approval requires explicit human action
* Version increments on every change

---

### 4.4 Agent Proposal Service

Responsibilities:

* Store non-destructive agent output
* Diff management

Endpoints:

```http
GET /scenes/{id}/proposals
```

Rules:

* Proposals are append-only
* Never mutate scenes directly

---

### 4.5 Review Service

Responsibilities:

* Human feedback
* Approval governance

Endpoints:

```http
POST /scenes/{id}/reviews
```

---

## 5. Event System (Core Backbone)

### Event Store

* Append-only table
* Events are immutable
* System state can be rebuilt by replay

```json
{
  "eventType": "SceneApproved",
  "projectId": "...",
  "sceneId": "...",
  "payload": { "version": 3 },
  "emittedBy": "human",
  "timestamp": "..."
}
```

### Required Event Types

* SceneCreated
* SceneUpdated
* SceneSubmittedForReview
* SceneApproved
* SceneRevisionRequested
* AgentProposalCreated
* AgentRunFailed
* SceneRendered
* FinalRenderCompleted

---

## 6. Agent Orchestration Engine

### Execution Model

* One scene = one pipeline
* Agents run sequentially
* Triggered only by events

Pipeline:

```text
Writer → Director → Cinematographer → Editor → Producer
```

Showrunner runs **after approval**, cross-scene.

### Orchestration Rules

* Agents only run if scene.status == draft
* Stop pipeline on failure or constraint violation
* Human edits reset pipeline

---

## 7. Agent Runtime Interface

Each agent implements:

```ts
interface Agent {
  role: "writer" | "director" | ...
  run(input: AgentContext): AgentProposal | ConstraintViolation
}
```

AgentContext includes:

* Scene
* SceneScript
* Characters
* Story Bible
* Prior proposals

---

## 8. Cost & Safety Controls

### Budgets

* Per scene budget (cents)
* Per project budget

### Enforcement

* Producer Agent can block
* Orchestrator checks before every agent run

### Logging

* Cost per agent run stored in `scene_agent_runs`

---

## 9. Rendering Architecture

### Animatic

* Optional
* Low-cost, low-res
* Never blocks approval

### Scene Render

* Only from approved scenes
* Versioned by sceneId → version

### Stitching

* Uses StitchPlan
* Rejects unapproved scenes

---

## 10. Concurrency & Scaling

* Scenes processed independently
* Multiple scenes can run in parallel
* Single scene locked during orchestration
* Distributed workers supported

---

## 11. Security & Isolation

* Project-level isolation
* No prompt leakage across projects
* Prompt redaction for logs
* Signed URLs for render artifacts

---

## 12. Failure Handling

* Agent failure emits `AgentRunFailed`
* Scene flagged for human review
* No automatic retries

---

## 13. Observability

Required:

* Event replay tooling
* Per-scene execution timeline
* Agent latency & cost dashboards

---

## 14. MVP Build Order (Engineering)

1. DB schema + migrations
2. Event store + dispatcher
3. Scene service + approval gates
4. Agent orchestration engine
5. Single agent (Writer) end-to-end
6. Scene review UI
7. Stitch plan + placeholder render
8. Additional agents

---

## 15. Definition of Done (v1)

* A user can create a project
* Generate scenes
* See agent proposals
* Approve scenes
* Stitch and export a 5–10 min short
* Every decision is auditable and replayable

---

## Final Note (Strategic)

This technical design is **closer to a creative operating system than a video tool**.
You are building **Cursor for cinema**, not Runway with prompts.

