# Design Document: VidFlow Studio Backend Integration

## Overview

This design document describes the architecture and implementation of the VidFlow Studio backend - an event-driven, agent-orchestrated creative system built with ASP.NET Core 10. The backend integrates with the existing React/TypeScript frontend to provide a complete scene-first, review-first workflow for producing 5-20 minute short films.

The system uses **Vertical Slice Architecture** in a single Web API project, prioritizing clarity, auditability, and low overhead over architectural purity. It deliberately avoids over-engineering patterns like CQRS, MediatR, and repository abstractions. Entity Framework Core is used directly via DbContext.

AI agent orchestration uses Microsoft Agent Framework strictly as a **deterministic role-based runner**. Agents produce structured proposals only and never mutate application state.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         React Frontend                               │
│  (Existing Qipixel UI - TypeScript/React)                           │
└─────────────────────────────┬───────────────────────────────────────┘
                              │ REST API / WebSocket (SignalR)
┌─────────────────────────────▼───────────────────────────────────────┐
│                 ASP.NET Core 10 Web API (Single Project)             │
│                      Vertical Slice Architecture                     │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │                    Feature Slices                                ││
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐           ││
│  │  │ Projects │ │  Scenes  │ │Characters│ │StoryBible│           ││
│  │  │  Slice   │ │  Slice   │ │  Slice   │ │  Slice   │           ││
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘           ││
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐           ││
│  │  │Proposals │ │  Shots   │ │StitchPlan│ │ Render   │           ││
│  │  │  Slice   │ │  Slice   │ │  Slice   │ │  Slice   │           ││
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘           ││
│  └─────────────────────────────────────────────────────────────────┘│
│                              │                                       │
│  ┌──────────────────────────▼──────────────────────────────────────┐│
│  │              Shared Infrastructure                               ││
│  │  VidFlowDbContext │ Domain Entities │ Domain Events             ││
│  └─────────────────────────────────────────────────────────────────┘│
└─────────────────────────────┬───────────────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────────────┐
│                    External Dependencies                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────────┐  │
│  │ PostgreSQL  │  │  SignalR    │  │  Microsoft Agent Framework  │  │
│  │  (EF Core)  │  │  (Real-time)│  │  (Deterministic Runner)     │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

### Agent Orchestration Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│              Agent Pipeline (Deterministic Role-Based Runner)        │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │                    Scene Pipeline                                ││
│  │  ┌────────┐  ┌──────────┐  ┌───────────────┐  ┌────────┐  ┌────┐││
│  │  │ Writer │→ │ Director │→ │Cinematographer│→ │ Editor │→ │Prod│││
│  │  └────────┘  └──────────┘  └───────────────┘  └────────┘  └────┘││
│  │       │           │               │               │          │   ││
│  │       ▼           ▼               ▼               ▼          ▼   ││
│  │  [Proposal]  [Proposal]      [Proposal]      [Proposal] [Proposal]│
│  │  (No state mutation - proposals only)                            ││
│  └─────────────────────────────────────────────────────────────────┘│
│                              │                                       │
│  ┌───────────────────────────▼─────────────────────────────────────┐│
│  │              Microsoft Agent Framework                           ││
│  │  ┌─────────────────────────────────────────────────────────────┐││
│  │  │ AgentRunner (Sequential, Deterministic)                     │││
│  │  │ - Executes agents in fixed order                            │││
│  │  │ - Collects proposals without state mutation                 │││
│  │  │ - Halts on failure                                          │││
│  │  └─────────────────────────────────────────────────────────────┘││
│  └─────────────────────────────────────────────────────────────────┘│
│                              │                                       │
│  ┌───────────────────────────▼─────────────────────────────────────┐│
│  │                    LLM Provider Layer                            ││
│  │  ┌─────────┐  ┌───────────┐  ┌────────┐  ┌───────────────────┐  ││
│  │  │ OpenAI  │  │ Anthropic │  │ Gemini │  │ HttpClient (Other)│  ││
│  │  └─────────┘  └───────────┘  └────────┘  └───────────────────┘  ││
│  └─────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────┘
```

### Design Principles

1. **Vertical Slice Architecture**: Each feature is self-contained with its own endpoint, handler, and data access
2. **No CQRS/MediatR**: Direct method calls, no message bus overhead
3. **No Repository Pattern**: EF Core DbContext used directly in feature handlers
4. **No Unit of Work Abstraction**: EF Core's built-in change tracking is sufficient
5. **Deterministic Agent Runner**: Agents execute in fixed sequence, produce proposals only
6. **Agents Never Mutate State**: All changes require explicit human action via proposal application

## Project Structure

```
VidFlow.Api/
├── Program.cs
├── appsettings.json
├── Data/
│   ├── VidFlowDbContext.cs
│   └── Migrations/
├── Domain/
│   ├── Entities/
│   │   ├── Project.cs
│   │   ├── Scene.cs
│   │   ├── Shot.cs
│   │   ├── Character.cs
│   │   ├── StoryBible.cs
│   │   ├── AgentProposal.cs
│   │   └── StitchPlan.cs
│   ├── Enums/
│   │   ├── SceneStatus.cs
│   │   ├── AgentRole.cs
│   │   └── ProposalStatus.cs
│   ├── Events/
│   │   └── DomainEvents.cs
│   └── Exceptions/
│       └── DomainExceptions.cs
├── Features/
│   ├── Projects/
│   │   ├── CreateProject.cs
│   │   ├── GetProject.cs
│   │   ├── UpdateProject.cs
│   │   └── ListProjects.cs
│   ├── Scenes/
│   │   ├── CreateScene.cs
│   │   ├── GetScene.cs
│   │   ├── UpdateScene.cs
│   │   ├── SubmitForReview.cs
│   │   ├── ApproveScene.cs
│   │   └── RequestRevision.cs
│   ├── Proposals/
│   │   ├── GetProposals.cs
│   │   ├── ApplyProposal.cs
│   │   └── DismissProposal.cs
│   ├── Characters/
│   ├── StoryBible/
│   ├── Shots/
│   ├── StitchPlan/
│   └── Agents/
│       ├── RunAgentPipeline.cs
│       ├── AgentRunner.cs
│       └── Agents/
│           ├── WriterAgent.cs
│           ├── DirectorAgent.cs
│           ├── CinematographerAgent.cs
│           ├── EditorAgent.cs
│           └── ProducerAgent.cs
├── Hubs/
│   └── AgentActivityHub.cs
└── Shared/
    ├── ApiErrorResponse.cs
    └── GlobalExceptionHandler.cs
```

## Components and Interfaces

### Feature Slice Pattern

Each feature is implemented as a self-contained slice with endpoint, request/response types, and handler logic:

```csharp
// Example: Features/Scenes/CreateScene.cs
public static class CreateScene
{
    public record Request(
        string Number,
        string Title,
        string NarrativeGoal,
        string EmotionalBeat,
        string Location,
        string TimeOfDay,
        int RuntimeTargetSeconds,
        List<string>? CharacterNames = null);

    public record Response(Guid Id, string Number, string Title, string Status, int Version);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId}/scenes", Handler)
           .WithName("CreateScene")
           .WithTags("Scenes");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        Request request,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return Results.NotFound();

        var scene = Scene.Create(
            projectId,
            request.Number,
            request.Title,
            request.NarrativeGoal,
            request.EmotionalBeat,
            request.Location,
            request.TimeOfDay,
            request.RuntimeTargetSeconds,
            request.CharacterNames);

        db.Scenes.Add(scene);
        
        // Append domain events to event store
        foreach (var evt in scene.DomainEvents)
        {
            db.EventStore.Add(EventStoreEntry.FromDomainEvent(evt, projectId, scene.Id));
        }
        scene.ClearDomainEvents();

        await db.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/scenes/{scene.Id}",
            new Response(scene.Id, scene.Number, scene.Title, scene.Status.ToString(), scene.Version));
    }
}
```

### Agent Runner (Deterministic)

```csharp
public class AgentRunner
{
    private readonly IEnumerable<ICreativeAgent> _agents;
    private readonly VidFlowDbContext _db;
    private readonly IHubContext<AgentActivityHub> _hub;

    // Agents are injected in fixed order: Writer, Director, Cinematographer, Editor, Producer
    public AgentRunner(
        IEnumerable<ICreativeAgent> agents,
        VidFlowDbContext db,
        IHubContext<AgentActivityHub> hub)
    {
        _agents = agents.OrderBy(a => GetAgentOrder(a.Role));
        _db = db;
        _hub = hub;
    }

    public async Task<AgentPipelineResult> RunPipelineAsync(Guid sceneId, CancellationToken ct)
    {
        var scene = await _db.Scenes
            .Include(s => s.Shots)
            .Include(s => s.Proposals)
            .FirstOrDefaultAsync(s => s.Id == sceneId, ct);

        if (scene is null || scene.Status != SceneStatus.Draft)
            return AgentPipelineResult.Failed(AgentRole.Writer, "Scene not found or not in Draft status");

        var storyBible = await _db.StoryBibles.FirstOrDefaultAsync(sb => sb.ProjectId == scene.ProjectId, ct);
        var characters = await _db.Characters.Where(c => c.ProjectId == scene.ProjectId).ToListAsync(ct);
        var proposals = new List<AgentProposal>();

        foreach (var agent in _agents)
        {
            await _hub.Clients.Group($"scene-{sceneId}").SendAsync("AgentStarted", sceneId, agent.Role.ToString(), ct);

            try
            {
                var context = new AgentContext(scene, storyBible, characters, proposals);
                var proposal = await agent.AnalyzeAsync(context, ct);

                if (proposal is not null)
                {
                    proposals.Add(proposal);
                    _db.AgentProposals.Add(proposal);
                    await _hub.Clients.Group($"scene-{sceneId}").SendAsync("ProposalCreated", sceneId, proposal, ct);
                }

                await _hub.Clients.Group($"scene-{sceneId}").SendAsync("AgentCompleted", sceneId, agent.Role.ToString(), ct);
            }
            catch (Exception ex)
            {
                await _hub.Clients.Group($"scene-{sceneId}").SendAsync("AgentFailed", sceneId, agent.Role.ToString(), ex.Message, ct);
                return AgentPipelineResult.Failed(agent.Role, ex.Message);
            }
        }

        await _db.SaveChangesAsync(ct);
        return AgentPipelineResult.Succeeded(proposals);
    }

    private static int GetAgentOrder(AgentRole role) => role switch
    {
        AgentRole.Writer => 1,
        AgentRole.Director => 2,
        AgentRole.Cinematographer => 3,
        AgentRole.Editor => 4,
        AgentRole.Producer => 5,
        _ => 99
    };
}
```

### Agent Interface

```csharp
public interface ICreativeAgent
{
    AgentRole Role { get; }
    Task<AgentProposal?> AnalyzeAsync(AgentContext context, CancellationToken ct);
}

public record AgentContext(
    Scene Scene,
    StoryBible? StoryBible,
    IEnumerable<Character> Characters,
    IEnumerable<AgentProposal> PriorProposals);

public record AgentPipelineResult
{
    public bool Success { get; init; }
    public IReadOnlyList<AgentProposal> Proposals { get; init; } = [];
    public AgentRole? FailedAtRole { get; init; }
    public string? ErrorMessage { get; init; }

    public static AgentPipelineResult Succeeded(IEnumerable<AgentProposal> proposals)
        => new() { Success = true, Proposals = proposals.ToList() };

    public static AgentPipelineResult Failed(AgentRole role, string error)
        => new() { Success = false, FailedAtRole = role, ErrorMessage = error };
}
```

### SignalR Hub

```csharp
public class AgentActivityHub : Hub
{
    public async Task JoinProjectGroup(Guid projectId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{projectId}");

    public async Task LeaveProjectGroup(Guid projectId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project-{projectId}");

    public async Task JoinSceneGroup(Guid sceneId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"scene-{sceneId}");

    public async Task LeaveSceneGroup(Guid sceneId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"scene-{sceneId}");
}
```

## Data Models

### Domain Entities

```csharp
public class Project
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Logline { get; private set; } = string.Empty;
    public int RuntimeTargetSeconds { get; private set; }
    public ProjectStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ICollection<Scene> Scenes { get; private set; } = [];
    public ICollection<Character> Characters { get; private set; } = [];
    public StoryBible? StoryBible { get; private set; }
    public StitchPlan? StitchPlan { get; private set; }

    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Project() { }

    public static Project Create(string title, string logline, int runtimeTargetSeconds)
    {
        if (runtimeTargetSeconds < 300 || runtimeTargetSeconds > 1200)
            throw new ArgumentOutOfRangeException(nameof(runtimeTargetSeconds));

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = title,
            Logline = logline,
            RuntimeTargetSeconds = runtimeTargetSeconds,
            Status = ProjectStatus.Ideation,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        project._domainEvents.Add(new ProjectCreated(project.Id, title, logline, runtimeTargetSeconds));
        return project;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public enum ProjectStatus { Ideation, Writing, Production, Locked }
```

```csharp
public class Scene
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string NarrativeGoal { get; private set; } = string.Empty;
    public string EmotionalBeat { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public string TimeOfDay { get; private set; } = string.Empty;
    public SceneStatus Status { get; private set; }
    public int RuntimeEstimateSeconds { get; private set; }
    public int RuntimeTargetSeconds { get; private set; }
    public string Script { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    public List<string> CharacterNames { get; private set; } = [];
    public ICollection<Shot> Shots { get; private set; } = [];
    public ICollection<AgentProposal> Proposals { get; private set; } = [];

    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Scene() { }

    public static Scene Create(Guid projectId, string number, string title, string narrativeGoal,
        string emotionalBeat, string location, string timeOfDay, int runtimeTargetSeconds,
        IEnumerable<string>? characterNames = null)
    {
        var scene = new Scene
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Number = number,
            Title = title,
            NarrativeGoal = narrativeGoal,
            EmotionalBeat = emotionalBeat,
            Location = location,
            TimeOfDay = timeOfDay,
            Status = SceneStatus.Draft,
            RuntimeTargetSeconds = runtimeTargetSeconds,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CharacterNames = characterNames?.ToList() ?? []
        };

        scene._domainEvents.Add(new SceneCreated(scene.Id, projectId, number, title));
        return scene;
    }

    public void Update(string? script = null, string? title = null)
    {
        if (Status == SceneStatus.Approved)
            throw new SceneNotEditableException(Id, Status);

        if (script != null) Script = script;
        if (title != null) Title = title;
        Version++;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new SceneUpdated(Id, Version));
    }

    public void SubmitForReview()
    {
        if (Status != SceneStatus.Draft)
            throw new InvalidStatusTransitionException(Status, SceneStatus.Review);
        Status = SceneStatus.Review;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new SceneSubmittedForReview(Id));
    }

    public void Approve(string approvedBy)
    {
        if (Status != SceneStatus.Review)
            throw new InvalidStatusTransitionException(Status, SceneStatus.Approved);
        Status = SceneStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new SceneApproved(Id, approvedBy, Version));
    }

    public void RequestRevision(string feedback, string requestedBy)
    {
        if (Status != SceneStatus.Review)
            throw new InvalidStatusTransitionException(Status, SceneStatus.Draft);
        Status = SceneStatus.Draft;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new SceneRevisionRequested(Id, feedback, requestedBy));
    }

    public static bool IsValidTransition(SceneStatus from, SceneStatus to) => (from, to) switch
    {
        (SceneStatus.Draft, SceneStatus.Review) => true,
        (SceneStatus.Review, SceneStatus.Approved) => true,
        (SceneStatus.Review, SceneStatus.Draft) => true,
        _ => false
    };

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public enum SceneStatus { Draft, Review, Approved }
```

```csharp
public class Shot
{
    public Guid Id { get; private set; }
    public Guid SceneId { get; private set; }
    public int Number { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Duration { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Camera { get; private set; } = string.Empty;

    private Shot() { }

    public static Shot Create(Guid sceneId, int number, string type, string duration, string description, string camera)
        => new() { Id = Guid.NewGuid(), SceneId = sceneId, Number = number, Type = type, Duration = duration, Description = description, Camera = camera };

    public int GetDurationSeconds() => int.TryParse(Duration.TrimEnd('s', 'S'), out var s) ? s : 0;
}

public class AgentProposal
{
    public Guid Id { get; private set; }
    public Guid SceneId { get; private set; }
    public AgentRole Role { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string Rationale { get; private set; } = string.Empty;
    public int RuntimeImpactSeconds { get; private set; }
    public string Diff { get; private set; } = string.Empty;
    public ProposalStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int TokensUsed { get; private set; }
    public decimal CostUsd { get; private set; }

    private AgentProposal() { }

    public static AgentProposal Create(Guid sceneId, AgentRole role, string summary, string rationale,
        int runtimeImpactSeconds, string diff, int tokensUsed = 0, decimal costUsd = 0)
        => new()
        {
            Id = Guid.NewGuid(),
            SceneId = sceneId,
            Role = role,
            Summary = summary,
            Rationale = rationale,
            RuntimeImpactSeconds = runtimeImpactSeconds,
            Diff = diff,
            Status = ProposalStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            TokensUsed = tokensUsed,
            CostUsd = costUsd
        };

    public void Apply() => Status = ProposalStatus.Applied;
    public void Dismiss() => Status = ProposalStatus.Dismissed;
}

public enum AgentRole { Writer, Director, Cinematographer, Editor, Producer, Showrunner }
public enum ProposalStatus { Pending, Applied, Dismissed }

public class Character
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string Archetype { get; private set; } = string.Empty;
    public string Age { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Backstory { get; private set; } = string.Empty;
    public List<string> Traits { get; private set; } = [];
    public List<CharacterRelationship> Relationships { get; private set; } = [];
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Character() { }

    public static Character Create(Guid projectId, string name, string role, string archetype,
        string age, string description, string backstory,
        IEnumerable<string>? traits = null, IEnumerable<CharacterRelationship>? relationships = null)
        => new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name,
            Role = role,
            Archetype = archetype,
            Age = age,
            Description = description,
            Backstory = backstory,
            Traits = traits?.ToList() ?? [],
            Relationships = relationships?.ToList() ?? [],
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}

public record CharacterRelationship(string Name, string Type, string Note);

public class StoryBible
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Themes { get; private set; } = string.Empty;
    public string WorldRules { get; private set; } = string.Empty;
    public string Tone { get; private set; } = string.Empty;
    public string VisualStyle { get; private set; } = string.Empty;
    public string PacingRules { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private StoryBible() { }

    public static StoryBible Create(Guid projectId, string themes, string worldRules, string tone, string visualStyle, string pacingRules)
        => new() { Id = Guid.NewGuid(), ProjectId = projectId, Themes = themes, WorldRules = worldRules, Tone = tone, VisualStyle = visualStyle, PacingRules = pacingRules, Version = 1, CreatedAt = DateTime.UtcNow };
}

public class StitchPlan
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public List<StitchPlanEntry> Entries { get; private set; } = [];
    public int TotalRuntimeSeconds { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private StitchPlan() { }

    public static StitchPlan Create(Guid projectId)
        => new() { Id = Guid.NewGuid(), ProjectId = projectId, UpdatedAt = DateTime.UtcNow };
}

public record StitchPlanEntry(Guid SceneId, int Order, string? TransitionType, string? TransitionNotes, string? AudioNotes);
```

### Domain Events

```csharp
public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string EmittedBy { get; init; } = "human";
}

public record ProjectCreated(Guid ProjectId, string Title, string Logline, int RuntimeTarget) : DomainEvent;
public record ProjectUpdated(Guid ProjectId, string Title, string Logline, int RuntimeTarget) : DomainEvent;
public record SceneCreated(Guid SceneId, Guid ProjectId, string Number, string Title) : DomainEvent;
public record SceneUpdated(Guid SceneId, int NewVersion) : DomainEvent;
public record SceneSubmittedForReview(Guid SceneId) : DomainEvent;
public record SceneApproved(Guid SceneId, string ApprovedBy, int Version) : DomainEvent;
public record SceneRevisionRequested(Guid SceneId, string Feedback, string RequestedBy) : DomainEvent;
public record AgentProposalCreated(Guid ProposalId, Guid SceneId, AgentRole Role, string Summary) : DomainEvent;
public record AgentRunFailed(Guid SceneId, AgentRole Role, string Error) : DomainEvent;
public record SceneRendered(Guid SceneId, Guid ArtifactId, int Version) : DomainEvent;
public record FinalRenderCompleted(Guid ProjectId, Guid ArtifactId) : DomainEvent;
public record StoryBibleVersionCreated(Guid StoryBibleId, Guid ProjectId, int Version) : DomainEvent;
public record CharacterUpdated(Guid CharacterId, Guid ProjectId, int Version) : DomainEvent;
```

### Event Store Entry

```csharp
public class EventStoreEntry
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid? ProjectId { get; private set; }
    public Guid? EntityId { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public string EmittedBy { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }

    private EventStoreEntry() { }

    public static EventStoreEntry FromDomainEvent(DomainEvent evt, Guid? projectId, Guid? entityId)
        => new()
        {
            Id = evt.EventId,
            EventType = evt.GetType().Name,
            ProjectId = projectId,
            EntityId = entityId,
            Payload = JsonSerializer.Serialize(evt, evt.GetType()),
            EmittedBy = evt.EmittedBy,
            Timestamp = evt.Timestamp
        };
}
```

### DbContext

```csharp
public class VidFlowDbContext : DbContext
{
    public VidFlowDbContext(DbContextOptions<VidFlowDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Scene> Scenes => Set<Scene>();
    public DbSet<Shot> Shots => Set<Shot>();
    public DbSet<AgentProposal> AgentProposals => Set<AgentProposal>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<StoryBible> StoryBibles => Set<StoryBible>();
    public DbSet<StitchPlan> StitchPlans => Set<StitchPlan>();
    public DbSet<EventStoreEntry> EventStore => Set<EventStoreEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Scene>()
            .Property(s => s.CharacterNames)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Character>()
            .Property(c => c.Traits)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Character>()
            .Property(c => c.Relationships)
            .HasColumnType("jsonb");

        modelBuilder.Entity<StitchPlan>()
            .Property(sp => sp.Entries)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Character>()
            .HasIndex(c => new { c.ProjectId, c.Name })
            .IsUnique();

        modelBuilder.Entity<Scene>()
            .HasIndex(s => new { s.ProjectId, s.Number })
            .IsUnique();
    }
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

These properties will be validated using property-based testing with the `FsCheck` library for .NET.

### Property 1: Entity Creation Completeness

*For any* valid entity creation request (Project, Scene, Character, Shot, Proposal), the persisted entity SHALL contain all required fields with correct values matching the input.

**Validates: Requirements 1.1, 3.1, 4.1, 7.1, 8.1**

### Property 2: Event Emission on State Changes

*For any* state-changing operation (create, update, approve, reject, submit), the Event_Store SHALL contain a corresponding domain event with correct event type, entity ID, and timestamp.

**Validates: Requirements 1.3, 1.5, 2.5, 3.2, 5.1, 5.2, 5.3, 6.2, 6.3, 10.2, 10.3**

### Property 3: Version Increment on Edit

*For any* entity that supports versioning (Scene, Character, StoryBible), editing the entity SHALL result in the version number incrementing by exactly 1.

**Validates: Requirements 2.2, 4.3, 8.4**

### Property 4: Status-Based Operation Gates

*For any* operation that requires a specific entity status (editing requires Draft, rendering requires Approved, stitch plan requires Approved), attempting the operation with an incorrect status SHALL result in rejection with an appropriate error.

**Validates: Requirements 4.4, 5.4, 9.1, 9.2, 10.2**

### Property 5: Event Store Immutability

*For any* sequence of operations on the Event_Store, the count of events SHALL only increase (never decrease), and the content of existing events SHALL never change.

**Validates: Requirements 11.1, 11.5**

### Property 6: Event Replay Round-Trip

*For any* entity with recorded events, replaying all events from the Event_Store SHALL reconstruct an entity state equivalent to the current persisted state.

**Validates: Requirements 11.3**

### Property 7: Agent Context Completeness

*For any* agent execution, the AgentContext provided to the agent SHALL contain the current scene data, story bible, all characters appearing in the scene, and all prior proposals for that scene.

**Validates: Requirements 2.4, 3.3, 6.4**

### Property 8: Proposal Non-Mutation Invariant

*For any* agent proposal creation or dismissal, the associated scene's script, shots, and version SHALL remain unchanged until a user explicitly applies the proposal.

**Validates: Requirements 7.3, 7.4**

### Property 9: Project Isolation

*For any* two distinct projects, accessing data (scenes, characters, proposals) from one project SHALL never return data belonging to the other project.

**Validates: Requirements 1.4**

### Property 10: Agent Pipeline Sequence

*For any* complete agent pipeline execution on a draft scene, the agents SHALL execute in the exact order: Writer → Director → Cinematographer → Editor → Producer, with each agent receiving proposals from all previously executed agents.

**Validates: Requirements 6.1**

### Property 11: Concurrency Locking

*For any* scene undergoing agent pipeline execution, concurrent modification attempts SHALL be blocked until the pipeline completes or is cancelled.

**Validates: Requirements 6.6**

### Property 12: Budget Enforcement

*For any* agent run that would cause cumulative cost to exceed the scene or project budget cap, the run SHALL be blocked before execution and the user SHALL be notified.

**Validates: Requirements 14.3**

### Property 13: Runtime Calculation Accuracy

*For any* scene with shots, the calculated runtime estimate SHALL equal the sum of all shot durations, and the stitch plan total runtime SHALL equal the sum of all included scene runtimes.

**Validates: Requirements 4.6, 8.3, 9.5**

### Property 14: API Contract Compatibility

*For any* API response containing Project, Scene, Shot, or Proposal data, the JSON structure SHALL be deserializable into the corresponding TypeScript interfaces defined in the frontend.

**Validates: Requirements 15.1, 15.2**

### Property 15: Scene Status State Machine

*For any* scene, the status transitions SHALL follow the valid state machine: Draft → Review → Approved, or Review → Draft (revision). Direct transitions from Draft → Approved SHALL be rejected.

**Validates: Requirements 5.4**

### Property 16: Character Name Uniqueness

*For any* project, attempting to create a character with a name that already exists in that project SHALL result in rejection with an appropriate error.

**Validates: Requirements 3.5**

### Property 17: Shot Number Sequentiality

*For any* scene after shot reordering, the shot numbers SHALL be sequential starting from 1 with no gaps.

**Validates: Requirements 8.2**

## Error Handling

### Domain Exceptions

```csharp
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    protected DomainException(string errorCode, string message) : base(message) => ErrorCode = errorCode;
}

public class SceneNotEditableException : DomainException
{
    public SceneNotEditableException(Guid sceneId, SceneStatus status)
        : base("SCENE_NOT_EDITABLE", $"Scene {sceneId} cannot be edited in status {status}") { }
}

public class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(SceneStatus from, SceneStatus to)
        : base("INVALID_STATUS_TRANSITION", $"Cannot transition from {from} to {to}") { }
}

public class SceneNotApprovedException : DomainException
{
    public SceneNotApprovedException(Guid sceneId)
        : base("SCENE_NOT_APPROVED", $"Scene {sceneId} must be approved for this operation") { }
}

public class BudgetExceededException : DomainException
{
    public BudgetExceededException(Guid entityId, decimal currentCost, decimal budget)
        : base("BUDGET_EXCEEDED", $"Operation would exceed budget. Current: {currentCost}, Budget: {budget}") { }
}

public class DuplicateCharacterNameException : DomainException
{
    public DuplicateCharacterNameException(string name, Guid projectId)
        : base("DUPLICATE_CHARACTER_NAME", $"Character '{name}' already exists in project {projectId}") { }
}

public class ConcurrentModificationException : DomainException
{
    public ConcurrentModificationException(Guid sceneId)
        : base("CONCURRENT_MODIFICATION", $"Scene {sceneId} is currently being processed by agents") { }
}
```

### Global Exception Handler

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (statusCode, response) = exception switch
        {
            DomainException de => (StatusCodes.Status400BadRequest, new ApiErrorResponse(de.ErrorCode, de.Message)),
            KeyNotFoundException => (StatusCodes.Status404NotFound, new ApiErrorResponse("NOT_FOUND", "Resource not found")),
            _ => (StatusCodes.Status500InternalServerError, new ApiErrorResponse("INTERNAL_ERROR", "An unexpected error occurred"))
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(response, ct);
        return true;
    }
}

public record ApiErrorResponse(string ErrorCode, string Message, IDictionary<string, string[]>? ValidationErrors = null);
```

## Testing Strategy

### Dual Testing Approach

- **Unit tests**: Verify specific examples, edge cases, and error conditions
- **Property-based tests**: Verify universal properties hold across all valid inputs using FsCheck (100+ iterations each)

### Test Organization

```
VidFlow.Api.Tests/
├── Unit/
│   ├── Domain/
│   │   ├── SceneTests.cs
│   │   └── SceneStatusTransitionTests.cs
│   └── Features/
│       ├── CreateSceneTests.cs
│       └── ApproveSceneTests.cs
├── Properties/
│   ├── Generators/
│   │   ├── SceneGenerator.cs
│   │   └── ProjectGenerator.cs
│   ├── EntityCreationProperties.cs
│   ├── StatusGateProperties.cs
│   └── AgentPipelineProperties.cs
└── Integration/
    └── DatabaseIntegrationTests.cs
```

### Property Test Configuration

```csharp
[assembly: Properties(MaxTest = 100, QuietOnSuccess = true)]
```
