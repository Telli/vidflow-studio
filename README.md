# VidFlow Studio Mode

A scene-first, review-first creative IDE for producing 5–20 minute "Hollywood/indie" short films using an agentic workflow.

## Overview

VidFlow Studio Mode generates **story bible → characters → scene scripts → shot lists → animatics → scene renders → stitched final**, with explicit human approvals at each gate. Agents propose changes; humans approve; renders happen only from approved scenes.

## North Star

A filmmaker can ship a coherent 8–12 minute short in one sitting, with professional structure, continuity, and pacing.

## Architecture

- **Event-driven, agent-orchestrated creative system**
- **Scene-level determinism, human approval gates, non-destructive AI proposals**
- **Role-based agents** (Writer/Director/DP/Editor/Producer/Showrunner)
- **Versioning + diffs + audit logs**

## Tech Stack

### Backend
- ASP.NET Core 8+
- PostgreSQL
- Background workers (Hangfire)
- Redis (caching)

### Frontend
- React + TypeScript
- Zustand/Redux for state
- WebSockets/SignalR for agent updates

### AI/Agents
- Pluggable LLM adapters (OpenAI, Anthropic, Gemini, Local)
- Versioned prompt templates

## Project Structure

```
VidFlow Studio Design/
├── PRD                           # Product Requirements Document
├── Technical PRD for VidFlow.md  # Technical specifications
├── VidFlow/                      # Backend API (.NET)
│   ├── VidFlow.Api/
│   ├── VidFlow.Api.Tests/
│   └── Dockerfile
├── Qipixel/                      # Frontend (React)
│   ├── src/
│   ├── build/
│   └── package.json
└── .kiro/                        # Agent specifications
```

## Core Features

- **Project Setup**: Create projects with runtime targets and style presets
- **Story Bible Builder**: Generate and version creative constraints
- **Character Studio**: Create characters with arcs and voice rules
- **Scene Planner**: Generate outlines with runtime budgets
- **Scene Review Workspace**: Core review interface with agent proposals
- **Agent System**: Role-based agents with deterministic orchestration
- **Rendering Pipeline**: Animatic → scene render → final stitch
- **Stitching & Assembly**: Combine approved scenes into final film

## Getting Started

### Prerequisites
- .NET 8+
- Node.js 18+
- PostgreSQL
- Redis (optional)

### Backend Setup
```bash
cd VidFlow
dotnet restore
dotnet run --project VidFlow.Api
```

### Frontend Setup
```bash
cd Qipixel
npm install
npm start
```

## MVP Scope (v1)

- Project setup
- Story bible + characters
- Scene planner
- Scene review workspace
- Agent proposals (writer/director/dp/editor/producer)
- Approval gating
- Stitch plan + final render

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request for review

## License

[License to be determined]

---

**VidFlow Studio - Cursor for cinema, not Runway with prompts.**
