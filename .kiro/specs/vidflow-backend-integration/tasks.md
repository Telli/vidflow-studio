# Implementation Plan: VidFlow Studio Backend Integration

## Overview

This implementation plan breaks down the VidFlow Studio backend into discrete coding tasks using Vertical Slice Architecture in a single ASP.NET Core 10 Web API project. The system deliberately avoids over-engineering (no CQRS, MediatR, or repository abstractions). Entity Framework Core is used directly via DbContext. Agent orchestration uses Microsoft Agent Framework as a deterministic role-based runner that produces proposals only and never mutates state.

## Tasks

- [x] 1. Project Setup and Infrastructure
  - [x] 1.1 Create ASP.NET Core 10 Web API project
    - Create VidFlow.Api project with minimal API structure
    - Configure project for vertical slice architecture
    - Set up appsettings.json with PostgreSQL connection string
    - _Requirements: 15.1_

  - [x] 1.2 Configure Entity Framework Core with PostgreSQL
    - Add Npgsql.EntityFrameworkCore.PostgreSQL package
    - Create VidFlowDbContext with DbSet configurations
    - Configure JSONB column mappings for complex types
    - _Requirements: 11.1_

  - [x] 1.3 Create database migrations for core schema
    - Create Projects table migration
    - Create Scenes table migration with status enum
    - Create Shots table migration
    - Create AgentProposals table migration
    - Create EventStore table migration (append-only)
    - Create Characters table migration
    - Create StoryBibles table migration
    - Create StitchPlans table migration
    - _Requirements: 1.1, 4.1, 7.1, 8.1, 11.1_

  - [x] 1.4 Set up Program.cs with service registration
    - Register DbContext with scoped lifetime
    - Configure SignalR for real-time updates
    - Configure global exception handling
    - _Requirements: 15.1_

- [-] 2. Domain Layer Implementation
  - [x] 2.1 Create domain entities and value objects
    - Implement Project entity with status enum
    - Implement Scene entity with status transitions
    - Implement Shot entity
    - Implement AgentProposal entity with role enum
    - Implement Character entity with relationships
    - Implement StoryBible entity
    - Implement StitchPlan entity with entries
    - _Requirements: 1.1, 3.1, 4.1, 7.1, 8.1_

  - [x] 2.2 Write property test for entity creation completeness
    - **Property 1: Entity Creation Completeness**
    - **Validates: Requirements 1.1, 3.1, 4.1, 7.1, 8.1**

  - [x] 2.3 Implement domain events
    - Create base DomainEvent record
    - Implement ProjectCreated, ProjectUpdated events
    - Implement SceneCreated, SceneUpdated, SceneSubmittedForReview events
    - Implement SceneApproved, SceneRevisionRequested events
    - Implement AgentProposalCreated, AgentRunFailed events
    - Implement SceneRendered, FinalRenderCompleted events
    - _Requirements: 1.5, 5.1, 5.2, 5.3, 6.2, 6.3, 10.2, 10.3_

  - [x] 2.4 Implement scene status state machine
    - Create SceneStatus enum (Draft, Review, Approved)
    - Implement valid transition validation logic
    - Throw InvalidStatusTransitionException for invalid transitions
    - _Requirements: 5.4_

  - [x] 2.5 Write property test for scene status state machine
    - **Property 15: Scene Status State Machine**
    - **Validates: Requirements 5.4**

  - [x] 2.6 Implement domain exceptions
    - Create DomainException base class
    - Implement SceneNotEditableException
    - Implement InvalidStatusTransitionException
    - Implement SceneNotApprovedException
    - Implement BudgetExceededException
    - Implement DuplicateCharacterNameException
    - Implement ConcurrentModificationException
    - _Requirements: 4.4, 5.4, 9.2, 14.3, 3.5, 6.6_

- [x] 3. Checkpoint - Domain Layer Complete
  - Ensure all domain entities compile
  - Ensure all domain events are defined
  - Ensure status state machine is implemented
  - Ask the user if questions arise

- [x] 4. Event Store Implementation
  - [x] 4.1 Create EventStoreEntry entity
    - Implement EventStoreEntry entity
    - Implement FromDomainEvent factory method
    - _Requirements: 11.1, 11.2_

  - [x] 4.2 Write property test for event store immutability
    - **Property 5: Event Store Immutability**
    - **Validates: Requirements 11.1, 11.5**

  - [x] 4.3 Write property test for event replay round-trip
    - **Property 6: Event Replay Round-Trip**
    - **Validates: Requirements 11.3**

- [x] 5. Feature Slices - Projects
  - [x] 5.1 Implement CreateProject feature slice
    - Create Request/Response records
    - Implement endpoint handler with validation
    - Emit ProjectCreated event to event store
    - _Requirements: 1.1, 1.5_

  - [x] 5.2 Implement GetProject feature slice
    - Return project with computed metrics (total runtime, scene count, pending reviews)
    - _Requirements: 1.2_

  - [x] 5.3 Implement UpdateProject feature slice
    - Validate runtime target bounds
    - Emit ProjectUpdated event
    - _Requirements: 1.3_

  - [x] 5.4 Implement ListProjects feature slice
    - _Requirements: 15.1_

- [x] 6. Feature Slices - Scenes
  - [x] 6.1 Implement CreateScene feature slice
    - Set initial status to Draft and version to 1
    - Emit SceneCreated event
    - _Requirements: 4.1, 4.2_

  - [x] 6.2 Implement GetScene feature slice
    - Return scene with shots, proposals, and version
    - _Requirements: 4.5_

  - [x] 6.3 Implement UpdateScene feature slice
    - Enforce edit restrictions on approved scenes
    - Increment version on edit
    - Emit SceneUpdated event
    - _Requirements: 4.3, 4.4_

  - [x] 6.4 Implement SubmitForReview feature slice
    - Change status to Review
    - Emit SceneSubmittedForReview event
    - _Requirements: 5.1_

  - [x] 6.5 Implement ApproveScene feature slice
    - Change status to Approved, lock scene
    - Record approving user and timestamp
    - Emit SceneApproved event
    - _Requirements: 5.2, 5.5_

  - [x] 6.6 Implement RequestRevision feature slice
    - Change status to Draft, record feedback
    - Emit SceneRevisionRequested event
    - _Requirements: 5.3_

- [ ] 7. Checkpoint - Core Feature Slices Complete
  - Test all endpoints with Swagger UI
  - Run all property tests
  - Ask the user if questions arise

- [x] 8. Feature Slices - Supporting Entities
  - [x] 8.1 Implement Character feature slices
    - CreateCharacter with uniqueness validation
    - GetCharacters for project
    - UpdateCharacter with versioning
    - _Requirements: 3.1, 3.2, 3.4, 3.5_

  - [x] 8.2 Implement StoryBible feature slices
    - CreateStoryBible
    - UpdateStoryBible with versioning
    - GetStoryBible
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 8.3 Implement Shot feature slices
    - AddShot
    - UpdateShot with parent scene version increment
    - ReorderShots with sequential numbering
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [x] 8.4 Implement Proposal feature slices
    - GetProposals grouped by role
    - ApplyProposal with scene update
    - DismissProposal
    - _Requirements: 7.1, 7.2, 7.3, 7.5_

- [x] 9. Feature Slices - Stitch Plan
  - [x] 9.1 Implement StitchPlan feature slices
    - GetStitchPlan
    - AddSceneToStitchPlan with approval validation
    - SetTransition
    - SetAudioNote
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

- [ ] 10. Checkpoint - All Feature Slices Complete
  - Ensure all feature slices compile and pass tests
  - Run full property test suite
  - Ask the user if questions arise

- [x] 11. LLM Provider Integration
  - [x] 11.1 Create LLM provider interface and base classes
    - Create ILlmProvider interface
    - Create LlmRequest and LlmResponse records
    - Implement retry logic with exponential backoff
    - _Requirements: 12.3_

  - [x] 11.2 Implement OpenAI provider
    - Implement CompleteAsync
    - Implement token counting and cost tracking
    - _Requirements: 12.1, 12.4_

  - [x] 11.3 Implement Anthropic provider
    - Implement CompleteAsync using HttpClient
    - Implement token counting and cost tracking
    - _Requirements: 12.1, 12.2, 12.4_

  - [x] 11.4 Implement Gemini provider
    - Implement CompleteAsync using HttpClient
    - Implement token counting and cost tracking
    - _Requirements: 12.1, 12.2, 12.4_

- [x] 12. Agent Runner (Deterministic)
  - [x] 12.1 Create agent interface and context
    - Create ICreativeAgent interface
    - Create AgentContext record
    - Create AgentPipelineResult type
    - _Requirements: 6.4_

  - [x] 12.2 Implement AgentRunner
    - Execute agents in fixed sequence: Writer → Director → Cinematographer → Editor → Producer
    - Collect proposals without state mutation
    - Halt pipeline on failure
    - _Requirements: 6.1, 6.3_

  - [x] 12.3 Implement WriterAgent
    - Define prompt template for script analysis
    - Implement AnalyzeAsync method (returns proposal only)
    - _Requirements: 6.1_

  - [x] 12.4 Implement DirectorAgent
    - Define prompt template for blocking and emotional beats
    - Implement AnalyzeAsync method (returns proposal only)
    - _Requirements: 6.1_

  - [x] 12.5 Implement CinematographerAgent
    - Define prompt template for shot composition
    - Implement AnalyzeAsync method (returns proposal only)
    - _Requirements: 6.1_

  - [x] 12.6 Implement EditorAgent
    - Define prompt template for pacing analysis
    - Implement AnalyzeAsync method (returns proposal only)
    - _Requirements: 6.1_

  - [x] 12.7 Implement ProducerAgent
    - Define prompt template for budget/constraint checking
    - Implement constraint violation detection (returns proposal only)
    - _Requirements: 6.1, 6.7_

- [ ] 13. Checkpoint - Agent System Complete
  - Test agent pipeline end-to-end
  - Verify agents produce proposals only (no state mutation)
  - Ask the user if questions arise

- [x] 14. Real-time Updates (SignalR)
  - [x] 14.1 Create AgentActivityHub
    - Implement JoinProjectGroup/LeaveProjectGroup
    - Implement JoinSceneGroup/LeaveSceneGroup
    - _Requirements: 13.1, 13.2, 13.3_

  - [x] 14.2 Integrate hub notifications with AgentRunner
    - Notify on agent started/completed/failed
    - Notify on proposal created
    - Notify on scene status changed
    - _Requirements: 13.1, 13.2, 13.3_

- [x] 15. Project Isolation and API Configuration
  - [x] 15.1 Configure global exception handling
    - Map domain exceptions to HTTP status codes
    - Return ApiErrorResponse format
    - _Requirements: 15.3_

  - [x] 15.2 Configure CORS and Swagger
    - Enable CORS for local development
    - Configure Swagger/OpenAPI documentation
    - _Requirements: 15.4, 15.5_

- [ ] 16. Missing Property-Based Tests
  - [ ] 16.1 Write property test for version increment on edit
    - **Property 3: Version Increment on Edit**
    - **Validates: Requirements 2.2, 4.3, 8.4**

  - [ ] 16.2 Write property test for status-based operation gates
    - **Property 4: Status-Based Operation Gates**
    - **Validates: Requirements 4.4, 5.4, 9.1, 9.2, 10.2**

  - [ ] 16.3 Write property test for event emission on state changes
    - **Property 2: Event Emission on State Changes**
    - **Validates: Requirements 1.3, 1.5, 2.5, 3.2, 5.1, 5.2, 5.3, 6.2, 6.3, 10.2, 10.3**

  - [ ] 16.4 Write property test for character name uniqueness
    - **Property 16: Character Name Uniqueness**
    - **Validates: Requirements 3.5**

  - [ ] 16.5 Write property test for shot number sequentiality
    - **Property 17: Shot Number Sequentiality**
    - **Validates: Requirements 8.2**

  - [ ] 16.6 Write property test for proposal non-mutation invariant
    - **Property 8: Proposal Non-Mutation Invariant**
    - **Validates: Requirements 7.3, 7.4**

  - [ ] 16.7 Write property test for runtime calculation accuracy
    - **Property 13: Runtime Calculation Accuracy**
    - **Validates: Requirements 4.6, 8.3, 9.5**

  - [ ] 16.8 Write property test for agent context completeness
    - **Property 7: Agent Context Completeness**
    - **Validates: Requirements 2.4, 3.3, 6.4**

  - [ ] 16.9 Write property test for agent pipeline sequence
    - **Property 10: Agent Pipeline Sequence**
    - **Validates: Requirements 6.1**

  - [ ] 16.10 Write property test for project isolation
    - **Property 9: Project Isolation**
    - **Validates: Requirements 1.4**

  - [ ] 16.11 Write property test for API contract compatibility
    - **Property 14: API Contract Compatibility**
    - **Validates: Requirements 15.1, 15.2**

- [ ] 17. Remaining Implementation Tasks
  - [ ] 17.1 Implement project-level access control
    - Ensure all queries filter by project ID
    - _Requirements: 1.4_

  - [ ] 17.2 Implement scene locking for concurrency
    - Acquire lock before pipeline execution
    - Release lock on completion or cancellation
    - _Requirements: 6.6_

  - [ ] 17.3 Write property test for concurrency locking
    - **Property 11: Concurrency Locking**
    - **Validates: Requirements 6.6**

  - [ ] 17.4 Add budget enforcement to agent pipeline
    - Block agent runs that would exceed budget caps
    - Implement cost tracking per scene and project
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5_

  - [ ] 17.5 Write property test for budget enforcement
    - **Property 12: Budget Enforcement**
    - **Validates: Requirements 14.3**

- [ ] 18. Checkpoint - Core Backend Complete
  - Run complete property test suite (all 17 properties)
  - Test all API endpoints with Swagger UI
  - Verify agent pipeline works end-to-end
  - Ask the user if questions arise

## Notes

- Single Web API project with Vertical Slice Architecture ✅
- No CQRS, MediatR, or repository abstractions - EF Core DbContext used directly ✅
- Agents are deterministic role-based runners that produce proposals only ✅
- Agents NEVER mutate application state - all changes require explicit human action ✅
- Core backend implementation is ~85% complete
- Missing: 11 property-based tests, project isolation, concurrency locking, budget enforcement
- All 17 correctness properties from the design document need to be covered by property tests
- Property tests use FsCheck with 100+ iterations each
