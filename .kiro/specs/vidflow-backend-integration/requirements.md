# Requirements Document

## Introduction

VidFlow Studio is an event-driven, agent-orchestrated creative system for producing 5–20 minute short films. This specification covers the implementation of a fully functioning ASP.NET Core 10 backend integrated with the existing React/TypeScript frontend, using Microsoft Agent Framework for multi-agent orchestration. The system follows a scene-first, review-first workflow where AI agents propose changes and humans approve them before rendering.

## Glossary

- **Project**: The top-level container for a film, including title, logline, runtime target, and status
- **Story_Bible**: Versioned creative constraints document containing themes, world rules, tone, visual style, and pacing rules
- **Character**: A defined entity with voice rules, arc, appearance constraints, and relationships
- **Scene**: The atomic unit of creation containing script, shots, status, version, and runtime budget
- **Shot**: A single camera setup within a scene with type, duration, description, and camera intent
- **Agent_Proposal**: A non-destructive suggestion from an AI agent with structured diffs
- **Agent_Role**: One of Writer, Director, Cinematographer, Editor, Producer, or Showrunner
- **Scene_Status**: One of Draft, Review, or Approved
- **Event_Store**: Append-only log of all domain events for auditability and replay
- **Stitch_Plan**: Ordered list of approved scenes with transitions and audio notes for final rendering
- **Render_Artifact**: Generated media output (animatic, scene render, or final film)
- **Orchestration_Engine**: The component that coordinates sequential agent execution per scene

## Requirements

### Requirement 1: Project Management

**User Story:** As a filmmaker, I want to create and manage film projects, so that I can organize my creative work with clear runtime targets and metadata.

#### Acceptance Criteria

1. WHEN a user creates a new project THEN THE Project_Service SHALL persist the project with title, logline, runtime target (300-1200 seconds), and initial status of "Ideation"
2. WHEN a user retrieves a project THEN THE Project_Service SHALL return all project metadata including computed metrics (total runtime, scene count, pending reviews)
3. WHEN a user updates project metadata THEN THE Project_Service SHALL validate runtime target bounds and emit a ProjectUpdated event
4. THE Project_Service SHALL enforce project-level isolation preventing cross-project data access
5. WHEN a project is created THEN THE Event_Store SHALL record a ProjectCreated event with timestamp and user attribution

### Requirement 2: Story Bible Management

**User Story:** As a filmmaker, I want to create and version my story bible, so that I can maintain consistent creative constraints across all scenes.

#### Acceptance Criteria

1. WHEN a user generates a story bible from a logline THEN THE Story_Bible_Service SHALL create a draft containing themes, world rules, tone, visual style, and pacing rules
2. WHEN a user edits the story bible THEN THE Story_Bible_Service SHALL create a new version and preserve the previous version
3. WHEN retrieving a story bible THEN THE Story_Bible_Service SHALL return the current version with access to version history
4. THE Story_Bible_Service SHALL provide the story bible as context input to all agent runs
5. WHEN a story bible version is created THEN THE Event_Store SHALL record a StoryBibleVersionCreated event

### Requirement 3: Character Studio

**User Story:** As a filmmaker, I want to create and manage characters with voice rules and arcs, so that dialogue generation maintains consistency.

#### Acceptance Criteria

1. WHEN a user creates a character THEN THE Character_Service SHALL persist name, role, archetype, description, traits, backstory, and relationships
2. WHEN a user updates a character THEN THE Character_Service SHALL version the character and emit a CharacterUpdated event
3. THE Character_Service SHALL provide character data as context to agents when processing scenes containing those characters
4. WHEN retrieving characters THEN THE Character_Service SHALL return all characters for a project with their current versions
5. THE Character_Service SHALL validate that character names are unique within a project

### Requirement 4: Scene Management

**User Story:** As a filmmaker, I want to create, edit, and manage scenes as atomic units, so that I can iterate on individual parts without affecting the whole film.

#### Acceptance Criteria

1. WHEN a user creates a scene THEN THE Scene_Service SHALL persist scene number, title, narrative goal, emotional beat, location, time of day, characters, and runtime target
2. WHEN a scene is created THEN THE Scene_Service SHALL set initial status to "Draft" and version to 1
3. WHEN a user edits a draft scene THEN THE Scene_Service SHALL increment the version and emit a SceneUpdated event
4. IF a user attempts to edit an approved scene THEN THE Scene_Service SHALL reject the edit and return an error
5. WHEN retrieving a scene THEN THE Scene_Service SHALL return all scene data including script, shots, proposals, and version history
6. THE Scene_Service SHALL calculate runtime estimates based on shot durations and script length

### Requirement 5: Scene Review Workflow

**User Story:** As a filmmaker, I want to submit scenes for review and approve or request revisions, so that I maintain quality control over the final output.

#### Acceptance Criteria

1. WHEN a user submits a scene for review THEN THE Scene_Service SHALL change status to "Review" and emit a SceneSubmittedForReview event
2. WHEN a user approves a scene THEN THE Scene_Service SHALL change status to "Approved", lock the scene from edits, and emit a SceneApproved event
3. WHEN a user requests revision with feedback THEN THE Scene_Service SHALL change status to "Draft", record the feedback, and emit a SceneRevisionRequested event
4. THE Scene_Service SHALL prevent status transitions that skip states (Draft cannot go directly to Approved)
5. WHEN a scene is approved THEN THE Scene_Service SHALL record the approving user and timestamp for audit

### Requirement 6: Agent Orchestration Engine

**User Story:** As a filmmaker, I want AI agents to analyze my scenes and propose improvements, so that I can benefit from automated creative assistance.

#### Acceptance Criteria

1. WHEN a scene is in Draft status THEN THE Orchestration_Engine SHALL execute agents in sequence: Writer → Director → Cinematographer → Editor → Producer
2. WHEN an agent completes analysis THEN THE Orchestration_Engine SHALL emit an AgentProposalCreated event with the proposal details
3. IF an agent run fails THEN THE Orchestration_Engine SHALL emit an AgentRunFailed event and halt the pipeline
4. THE Orchestration_Engine SHALL provide each agent with scene data, story bible, characters, and prior proposals as context
5. WHEN a human edits a scene THEN THE Orchestration_Engine SHALL reset and re-run the agent pipeline
6. THE Orchestration_Engine SHALL enforce scene-level locking during pipeline execution to prevent concurrent modifications
7. WHEN the Producer agent detects a constraint violation (budget, runtime) THEN THE Orchestration_Engine SHALL block further processing and flag the scene

### Requirement 7: Agent Proposal Management

**User Story:** As a filmmaker, I want to view, apply, or dismiss agent proposals, so that I can selectively incorporate AI suggestions.

#### Acceptance Criteria

1. WHEN an agent creates a proposal THEN THE Proposal_Service SHALL store the proposal with role, summary, rationale, runtime impact, and structured diff
2. WHEN a user applies a proposal THEN THE Proposal_Service SHALL merge the diff into the scene, increment version, and remove the proposal from pending
3. WHEN a user dismisses a proposal THEN THE Proposal_Service SHALL mark it as dismissed without modifying the scene
4. THE Proposal_Service SHALL never mutate scenes directly; all changes require explicit user action
5. WHEN retrieving proposals THEN THE Proposal_Service SHALL return proposals grouped by agent role with timestamps

### Requirement 8: Shot List Management

**User Story:** As a filmmaker, I want to manage shot lists for each scene, so that I can plan camera work and visual storytelling.

#### Acceptance Criteria

1. WHEN a user adds a shot THEN THE Shot_Service SHALL persist shot number, type, duration, description, and camera intent
2. WHEN shots are reordered THEN THE Shot_Service SHALL update shot numbers sequentially and emit a ShotsReordered event
3. THE Shot_Service SHALL calculate total shot duration and compare against scene runtime target
4. WHEN a shot is modified THEN THE Shot_Service SHALL increment the parent scene version

### Requirement 9: Stitch Plan Assembly

**User Story:** As a filmmaker, I want to assemble approved scenes into a stitch plan, so that I can prepare for final rendering.

#### Acceptance Criteria

1. WHEN a user creates a stitch plan THEN THE Stitch_Service SHALL only accept scenes with "Approved" status
2. IF a user attempts to add an unapproved scene THEN THE Stitch_Service SHALL reject the addition with an error
3. WHEN a user sets transition notes between scenes THEN THE Stitch_Service SHALL persist the transition type and notes
4. WHEN a user sets audio notes for a scene THEN THE Stitch_Service SHALL persist the audio/music direction
5. THE Stitch_Service SHALL calculate projected total runtime from all included scenes
6. WHEN all scenes are approved THEN THE Stitch_Service SHALL enable the final render action

### Requirement 10: Rendering Pipeline

**User Story:** As a filmmaker, I want to render animatics, scene renders, and final films, so that I can produce viewable output.

#### Acceptance Criteria

1. WHEN a user requests an animatic THEN THE Render_Service SHALL generate a low-resolution preview without blocking scene approval
2. WHEN a user requests a scene render THEN THE Render_Service SHALL only process approved scenes and emit a SceneRendered event
3. WHEN a user requests final render THEN THE Render_Service SHALL process the stitch plan and emit a FinalRenderCompleted event
4. THE Render_Service SHALL store render artifacts with source version references (sceneId → version)
5. WHEN a scene is re-rendered THEN THE Render_Service SHALL not force re-render of other scenes
6. THE Render_Service SHALL track render status (queued, processing, completed, failed) and provide progress updates

### Requirement 11: Event Store and Auditability

**User Story:** As a filmmaker, I want all actions to be logged and auditable, so that I can track changes and replay system state.

#### Acceptance Criteria

1. THE Event_Store SHALL persist all domain events in an append-only manner
2. THE Event_Store SHALL record event type, project ID, entity ID, payload, emitter (human/agent), and timestamp for each event
3. WHEN replaying events THEN THE Event_Store SHALL reconstruct entity state accurately
4. THE Event_Store SHALL support querying events by project, entity, type, and time range
5. THE Event_Store SHALL never delete or modify existing events

### Requirement 12: LLM Provider Integration

**User Story:** As a system administrator, I want to configure multiple LLM providers, so that agents can use the best available models.

#### Acceptance Criteria

1. THE LLM_Service SHALL support OpenAI, Anthropic, and Gemini through native Microsoft Agent Framework adapters
2. WHEN a provider is not natively supported THEN THE LLM_Service SHALL use direct HttpClient calls with configurable endpoints
3. THE LLM_Service SHALL implement retry logic with exponential backoff for transient failures
4. THE LLM_Service SHALL track token usage and cost per agent run
5. THE LLM_Service SHALL support prompt template versioning in code

### Requirement 13: Real-time Updates

**User Story:** As a filmmaker, I want to see agent activity and updates in real-time, so that I can monitor progress without refreshing.

#### Acceptance Criteria

1. WHEN an agent starts processing THEN THE WebSocket_Service SHALL push a status update to connected clients
2. WHEN an agent creates a proposal THEN THE WebSocket_Service SHALL push the proposal to the scene's subscribers
3. WHEN a scene status changes THEN THE WebSocket_Service SHALL notify all project subscribers
4. THE WebSocket_Service SHALL support reconnection with state synchronization
5. THE WebSocket_Service SHALL authenticate connections using the same auth as REST endpoints

### Requirement 14: Cost and Budget Controls

**User Story:** As a filmmaker, I want to set and enforce budgets, so that I can control AI processing costs.

#### Acceptance Criteria

1. WHEN a project is created THEN THE Budget_Service SHALL allow setting a project-level budget cap
2. WHEN a scene is created THEN THE Budget_Service SHALL allow setting a scene-level budget cap
3. WHEN an agent run would exceed budget THEN THE Budget_Service SHALL block the run and notify the user
4. THE Budget_Service SHALL track cumulative cost per scene and per project
5. THE Budget_Service SHALL provide cost attribution per agent role

### Requirement 15: API Integration with Frontend

**User Story:** As a frontend developer, I want well-documented REST APIs, so that I can integrate the backend with the existing React UI.

#### Acceptance Criteria

1. THE API_Gateway SHALL expose RESTful endpoints matching the existing frontend data contracts
2. THE API_Gateway SHALL return JSON responses compatible with the TypeScript interfaces in mock-data.ts
3. THE API_Gateway SHALL implement proper HTTP status codes (200, 201, 400, 401, 403, 404, 500)
4. THE API_Gateway SHALL support CORS for local development
5. THE API_Gateway SHALL provide OpenAPI/Swagger documentation
6. WHEN the frontend calls an endpoint THEN THE API_Gateway SHALL respond within 200ms for cached data

