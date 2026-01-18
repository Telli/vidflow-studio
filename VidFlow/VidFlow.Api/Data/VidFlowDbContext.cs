using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Domain.Entities;

namespace VidFlow.Api.Data;

/// <summary>
/// Entity Framework Core DbContext for VidFlow Studio.
/// Uses PostgreSQL with JSONB for complex types.
/// </summary>
public class VidFlowDbContext : DbContext
{
    public VidFlowDbContext(DbContextOptions<VidFlowDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Scene> Scenes => Set<Scene>();
    public DbSet<Shot> Shots => Set<Shot>();
    public DbSet<AgentProposal> AgentProposals => Set<AgentProposal>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<StoryBible> StoryBibles => Set<StoryBible>();
    public DbSet<StitchPlan> StitchPlans => Set<StitchPlan>();
    public DbSet<EventStoreEntry> EventStore => Set<EventStoreEntry>();
    public DbSet<RenderJob> RenderJobs => Set<RenderJob>();
    public DbSet<LlmInteraction> LlmInteractions => Set<LlmInteraction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureProject(modelBuilder);
        ConfigureScene(modelBuilder);
        ConfigureShot(modelBuilder);
        ConfigureAgentProposal(modelBuilder);
        ConfigureCharacter(modelBuilder);
        ConfigureStoryBible(modelBuilder);
        ConfigureStitchPlan(modelBuilder);
        ConfigureEventStore(modelBuilder);
        ConfigureRenderJob(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigureAssets(modelBuilder);
        ConfigureLlmInteraction(modelBuilder);
    }

    private static void ConfigureAssets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.SceneId);
            entity.Property(e => e.ShotId);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Prompt).HasMaxLength(4000);
            entity.Property(e => e.Provider).HasMaxLength(100);
            entity.Property(e => e.Url).HasMaxLength(2000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.SceneId);
            entity.HasIndex(e => e.ShotId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
        });
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(320);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PasswordSalt).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.Email).IsUnique();
        });
    }

    private static void ConfigureProject(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Logline).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.RuntimeTargetSeconds).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasMany(e => e.Scenes)
                .WithOne()
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Characters)
                .WithOne()
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.StoryBible)
                .WithOne()
                .HasForeignKey<StoryBible>(sb => sb.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.StitchPlan)
                .WithOne()
                .HasForeignKey<StitchPlan>(sp => sp.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.DomainEvents);
        });
    }

    private static void ConfigureScene(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Scene>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.Number).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.NarrativeGoal).HasMaxLength(2000);
            entity.Property(e => e.EmotionalBeat).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.TimeOfDay).HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Script).HasColumnType("text");
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.ApprovedBy).HasMaxLength(200);

            // JSONB for CharacterNames list - use value converter for List<string>
            entity.Property(e => e.CharacterNames)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

            entity.HasMany(e => e.Shots)
                .WithOne()
                .HasForeignKey(s => s.SceneId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Proposals)
                .WithOne()
                .HasForeignKey(p => p.SceneId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint on ProjectId + Number
            entity.HasIndex(e => new { e.ProjectId, e.Number }).IsUnique();

            entity.Ignore(e => e.DomainEvents);
        });
    }

    private static void ConfigureShot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SceneId).IsRequired();
            entity.Property(e => e.Number).IsRequired();
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Duration).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Camera).HasMaxLength(500);

            entity.HasIndex(e => new { e.SceneId, e.Number });
        });
    }

    private static void ConfigureAgentProposal(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentProposal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SceneId).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Rationale).HasMaxLength(5000);
            entity.Property(e => e.Diff).HasColumnType("text");
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CostUsd).HasPrecision(18, 6);

            entity.HasOne(e => e.Scene)
                .WithMany(s => s.Proposals)
                .HasForeignKey(e => e.SceneId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.SceneId);
            entity.HasIndex(e => e.Status);
        });
    }

    private static void ConfigureCharacter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Role).HasMaxLength(200);
            entity.Property(e => e.Archetype).HasMaxLength(200);
            entity.Property(e => e.Age).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(5000);
            entity.Property(e => e.Backstory).HasColumnType("text");
            entity.Property(e => e.Version).IsRequired();

            // JSONB for Traits list - use value converter
            entity.Property(e => e.Traits)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

            // JSONB for Relationships list - use value converter
            entity.Property(e => e.Relationships)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<VidFlow.Api.Domain.ValueObjects.CharacterRelationship>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<VidFlow.Api.Domain.ValueObjects.CharacterRelationship>());

            // Unique constraint on ProjectId + Name
            entity.HasIndex(e => new { e.ProjectId, e.Name }).IsUnique();

            entity.Ignore(e => e.DomainEvents);
        });
    }

    private static void ConfigureStoryBible(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoryBible>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.Themes).HasColumnType("text");
            entity.Property(e => e.WorldRules).HasColumnType("text");
            entity.Property(e => e.Tone).HasMaxLength(1000);
            entity.Property(e => e.VisualStyle).HasMaxLength(2000);
            entity.Property(e => e.PacingRules).HasMaxLength(2000);
            entity.Property(e => e.Version).IsRequired();

            entity.HasIndex(e => e.ProjectId).IsUnique();

            entity.Ignore(e => e.DomainEvents);
        });
    }

    private static void ConfigureStitchPlan(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StitchPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.TotalRuntimeSeconds).IsRequired();

            // JSONB for Entries list - use value converter
            entity.Property(e => e.Entries)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<VidFlow.Api.Domain.ValueObjects.StitchPlanEntry>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<VidFlow.Api.Domain.ValueObjects.StitchPlanEntry>());

            entity.HasIndex(e => e.ProjectId).IsUnique();
        });
    }

    private static void ConfigureEventStore(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStoreEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Payload).IsRequired().HasColumnType("jsonb");
            entity.Property(e => e.EmittedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Timestamp).IsRequired();

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Timestamp);
        });
    }

    private static void ConfigureRenderJob(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RenderJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ProgressPercent).IsRequired();
            entity.Property(e => e.ArtifactPath).HasMaxLength(500);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.SceneId);
            entity.HasIndex(e => e.Status);
        });
    }

    private static void ConfigureLlmInteraction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LlmInteraction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.SceneId);
            entity.Property(e => e.AgentRole).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.JobId).HasMaxLength(100);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SystemPrompt).HasColumnType("text");
            entity.Property(e => e.Prompt).HasColumnType("text");
            entity.Property(e => e.Temperature).HasPrecision(4, 2);
            entity.Property(e => e.MaxTokens).IsRequired();
            entity.Property(e => e.ResponseContent).HasColumnType("text");
            entity.Property(e => e.InputTokens);
            entity.Property(e => e.OutputTokens);
            entity.Property(e => e.TotalTokens);
            entity.Property(e => e.CostUsd).HasPrecision(18, 8);
            entity.Property(e => e.Success).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.DurationMs);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CompletedAt);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.SceneId);
            entity.HasIndex(e => e.AgentRole);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Success);
        });
    }
}
