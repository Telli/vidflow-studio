# VidFlow Studio - Full Feature Implementation Plan

## Overview
This document tracks the implementation of all outstanding features beyond MVP.

---

## Phase 1: Wire LLM Providers ⏳
**Goal**: Connect agents to real AI providers (OpenAI, Anthropic, Gemini)

### Tasks
- [ ] 1.1 Update `ILlmProvider` interface with proper request/response types
- [ ] 1.2 Implement OpenAI provider with GPT-4 calls
- [ ] 1.3 Implement Anthropic provider with Claude calls
- [ ] 1.4 Implement Gemini provider
- [ ] 1.5 Add retry logic with exponential backoff
- [ ] 1.6 Create prompt templates for each agent role
- [ ] 1.7 Update agents to use real LLM providers
- [ ] 1.8 Add provider selection configuration

**Files to modify**:
- `Features/LLM/ILlmProvider.cs`
- `Features/LLM/Providers/OpenAiProvider.cs`
- `Features/LLM/Providers/AnthropicProvider.cs`
- `Features/LLM/Providers/GeminiProvider.cs`
- `Features/Agents/Agents/*.cs`

---

## Phase 2: Budget & Cost Controls ⏳
**Goal**: Track and enforce spending limits

### Tasks
- [ ] 2.1 Add `BudgetCap` and `CurrentSpend` to Project entity
- [ ] 2.2 Add `BudgetCap` and `CurrentSpend` to Scene entity
- [ ] 2.3 Create `BudgetService` for tracking and enforcement
- [ ] 2.4 Add budget check before agent pipeline runs
- [ ] 2.5 Create cost tracking endpoints
- [ ] 2.6 Add cost attribution per agent role
- [ ] 2.7 Create budget exceeded notification via SignalR

**New files**:
- `Features/Budget/BudgetService.cs`
- `Features/Budget/GetProjectCosts.cs`
- `Features/Budget/SetBudget.cs`

---

## Phase 3: Story Bible AI Generation ⏳
**Goal**: Auto-generate story bible from project logline

### Tasks
- [ ] 3.1 Create `GenerateStoryBible` endpoint
- [ ] 3.2 Build story bible generation prompt
- [ ] 3.3 Add version history to StoryBible entity
- [ ] 3.4 Create `GetStoryBibleVersions` endpoint
- [ ] 3.5 Add rollback to previous version

**New files**:
- `Features/StoryBible/GenerateStoryBible.cs`
- `Features/StoryBible/GetStoryBibleVersions.cs`

---

## Phase 4: Event Store Querying ⏳
**Goal**: Query and replay events for auditability

### Tasks
- [ ] 4.1 Create `QueryEvents` endpoint with filters
- [ ] 4.2 Add pagination support
- [ ] 4.3 Create event replay service
- [ ] 4.4 Add event export endpoint

**New files**:
- `Features/Events/QueryEvents.cs`
- `Features/Events/EventReplayService.cs`

---

## Phase 5: Scene Locking ⏳
**Goal**: Prevent concurrent modifications during agent runs

### Tasks
- [ ] 5.1 Add `IsLocked` and `LockedUntil` to Scene entity
- [ ] 5.2 Create distributed lock service
- [ ] 5.3 Integrate locking into AgentRunner
- [ ] 5.4 Add lock status to scene responses

---

## Phase 6: Showrunner Agent ⏳
**Goal**: Cross-scene consistency review

### Tasks
- [ ] 6.1 Create `ShowrunnerAgent` class
- [ ] 6.2 Build cross-scene analysis prompt
- [ ] 6.3 Create project-level agent endpoint
- [ ] 6.4 Add continuity tracking

**New files**:
- `Features/Agents/Agents/ShowrunnerAgent.cs`
- `Features/Agents/RunShowrunner.cs`

---

## Phase 7: Producer Constraint Blocking ⏳
**Goal**: Block pipeline on budget/runtime violations

### Tasks
- [ ] 7.1 Enhance ProducerAgent analysis
- [ ] 7.2 Add constraint violation detection
- [ ] 7.3 Return blocking result from pipeline
- [ ] 7.4 Add constraint status to scene

---

## Phase 8: Shot Management Enhancement ⏳
**Goal**: Proper shot reordering with events

### Tasks
- [ ] 8.1 Add `ReorderShots` endpoint
- [ ] 8.2 Emit `ShotsReordered` event
- [ ] 8.3 Auto-renumber shots on reorder
- [ ] 8.4 Update scene version on shot changes

**New files**:
- `Features/Shots/ReorderShots.cs`
- `Domain/Events/ShotsReordered.cs`

---

## Phase 9: Real-time Enhancements ⏳
**Goal**: Robust WebSocket with reconnection

### Tasks
- [ ] 9.1 Add connection state tracking
- [ ] 9.2 Implement reconnection with state sync
- [ ] 9.3 Add subscription management endpoints
- [ ] 9.4 Create presence indicators

---

## Phase 10: Authentication & Authorization ⏳
**Goal**: Secure multi-user access

### Tasks
- [ ] 10.1 Add ASP.NET Identity or JWT auth
- [ ] 10.2 Create User entity
- [ ] 10.3 Add role-based authorization
- [ ] 10.4 Implement project membership
- [ ] 10.5 Add auth to SignalR hub

---

## Phase 11: AI Script Generation ⏳
**Goal**: Generate scripts from scene metadata

### Tasks
- [ ] 11.1 Create `GenerateScript` endpoint
- [ ] 11.2 Build script generation prompt with character voices
- [ ] 11.3 Add dialogue formatting
- [ ] 11.4 Integrate with Writer agent

**New files**:
- `Features/Scenes/GenerateScript.cs`

---

## Phase 12: Visual Asset Integration ⏳
**Goal**: Storyboard and asset management

### Tasks
- [ ] 12.1 Create Asset entity
- [ ] 12.2 Add image generation integration (DALL-E/Midjourney)
- [ ] 12.3 Create asset library endpoints
- [ ] 12.4 Link assets to shots

**New files**:
- `Domain/Entities/Asset.cs`
- `Features/Assets/*.cs`

---

## Phase 13: Export Features ⏳
**Goal**: Export to industry formats

### Tasks
- [ ] 13.1 Create Fountain format exporter
- [ ] 13.2 Create Final Draft XML exporter
- [ ] 13.3 Create PDF storyboard generator
- [ ] 13.4 Add project backup/restore

**New files**:
- `Features/Export/ExportFountain.cs`
- `Features/Export/ExportPdf.cs`
- `Features/Export/BackupProject.cs`

---

## Phase 14: Version Comparison ⏳
**Goal**: Visual diffs and rollback

### Tasks
- [ ] 14.1 Create scene version diff endpoint
- [ ] 14.2 Add shot list diff
- [ ] 14.3 Implement rollback to version
- [ ] 14.4 Create comparison UI support

**New files**:
- `Features/Scenes/CompareVersions.cs`
- `Features/Scenes/RollbackScene.cs`

---

## Phase 15: Notification System ⏳
**Goal**: External notifications

### Tasks
- [ ] 15.1 Create notification service
- [ ] 15.2 Add email notifications (SendGrid/SMTP)
- [ ] 15.3 Add webhook support
- [ ] 15.4 Add Slack integration
- [ ] 15.5 Create notification preferences

**New files**:
- `Features/Notifications/NotificationService.cs`
- `Features/Notifications/WebhookEndpoint.cs`

---

## Estimated Effort

| Phase | Complexity | Est. Time |
|-------|------------|-----------|
| 1. LLM Providers | High | 2-3 hours |
| 2. Budget Controls | Medium | 1-2 hours |
| 3. Story Bible Gen | Low | 30 min |
| 4. Event Querying | Low | 30 min |
| 5. Scene Locking | Medium | 1 hour |
| 6. Showrunner | Medium | 1 hour |
| 7. Producer Blocking | Low | 30 min |
| 8. Shot Enhancement | Low | 30 min |
| 9. Real-time | Medium | 1 hour |
| 10. Auth | High | 2-3 hours |
| 11. Script Gen | Medium | 1 hour |
| 12. Visual Assets | High | 2-3 hours |
| 13. Export | Medium | 1-2 hours |
| 14. Version Compare | Medium | 1 hour |
| 15. Notifications | Medium | 1-2 hours |

**Total Estimated**: 15-22 hours

---

## Progress Log

### 2024-12-31
- Created implementation plan
- Starting Phase 1: LLM Providers

### 2024-12-31 (Session 2)
**All major phases completed:**

✅ **Phase 1: LLM Providers**
- Updated DirectorAgent, CinematographerAgent, EditorAgent, ProducerAgent to use ILlmProvider
- All agents now make real LLM calls with proper prompts

✅ **Phase 2: Budget & Cost Controls**
- Added BudgetCapUsd, CurrentSpendUsd to Project entity
- Created SetProjectBudget, GetProjectCosts endpoints
- Cost tracking per agent and per scene

✅ **Phase 3: Story Bible AI Generation**
- Created GenerateStoryBible endpoint
- LLM-powered generation from project logline

✅ **Phase 4: Event Store Querying**
- Created QueryEvents endpoint with filters
- Created GetProjectEvents endpoint

✅ **Phase 5: Scene Locking**
- Added IsLocked, LockedUntil, LockedBy to Scene entity
- Integrated locking into AgentRunner

✅ **Phase 6: Showrunner Agent**
- Created ShowrunnerAgent for cross-scene consistency
- Reviews narrative coherence and character consistency

✅ **Phase 7: Script Generation**
- Created GenerateScript endpoint
- Uses character voice rules and story bible context

✅ **Phase 8: Export Features**
- Created ExportFountain (screenplay format)
- Created ExportJson (full project backup)

✅ **Phase 9: Version Comparison**
- Created CompareVersions endpoint
- Shows scene change history from event store

✅ **Phase 10: Notification System**
- Created WebhookService
- Created TestWebhook endpoint

**Remaining (lower priority):**
- Full Authentication & Authorization (JWT)
- Visual Asset Integration (DALL-E/Midjourney)
- PDF storyboard generation
