using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VidFlow.Api.Data;
using VidFlow.Api.Features.Projects;
using VidFlow.Api.Features.Scenes;
using VidFlow.Api.Features.Characters;
using VidFlow.Api.Features.StoryBible;
using VidFlow.Api.Features.Shots;
using VidFlow.Api.Features.Proposals;
using VidFlow.Api.Features.StitchPlan;
using VidFlow.Api.Features.Agents;
using VidFlow.Api.Features.Agents.Agents;
using VidFlow.Api.Features.Render;
using VidFlow.Api.Features.Budget;
using VidFlow.Api.Features.Events;
using VidFlow.Api.Features.Export;
using VidFlow.Api.Features.Notifications;
using VidFlow.Api.Features.Auth;
using VidFlow.Api.Features.Assets;
using VidFlow.Api.Features.LLM;
using VidFlow.Api.Features.Jobs;
using VidFlow.Api.Hubs;
using VidFlow.Api.Shared;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "VidFlow Studio API",
        Version = "v1",
        Description = "Backend API for VidFlow Studio - an event-driven, agent-orchestrated creative system for producing short films."
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

// Register DbContext with PostgreSQL (scoped lifetime by default)
builder.Services.AddDbContext<VidFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("VidFlowDb")));

// Configure Hangfire with PostgreSQL storage
builder.Services.AddHangfire(configuration =>
{
    var connectionString = builder.Configuration.GetConnectionString("VidFlowDb");
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions
        {
            PrepareSchemaIfNecessary = true,
            QueuePollInterval = TimeSpan.FromSeconds(2)
        });
});

builder.Services.AddHangfireServer();

// Configure SignalR for real-time updates
builder.Services.AddSignalR();

// Auth (feature flagged)
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Auth:Jwt"));

var jwtOptions = builder.Configuration.GetSection("Auth:Jwt").Get<JwtOptions>()
    ?? new JwtOptions(false, "", "", "", 60);

if (jwtOptions.Enabled)
{
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"].ToString();
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/agent-activity"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });
}
else
{
    builder.Services.AddAuthorization();
}

// Configure global exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// JSON options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Register agents and agent runner
builder.Services.AddScoped<AgentRunner>();
builder.Services.AddScoped<ICreativeAgent, WriterAgent>();
builder.Services.AddScoped<ICreativeAgent, DirectorAgent>();
builder.Services.AddScoped<ICreativeAgent, CinematographerAgent>();
builder.Services.AddScoped<ICreativeAgent, EditorAgent>();
builder.Services.AddScoped<ICreativeAgent, ProducerAgent>();
builder.Services.AddScoped<ICreativeAgent, ShowrunnerAgent>();

// Register Hangfire job handlers
builder.Services.AddTransient<VidFlow.Api.Features.Jobs.AgentPipelineJob>();
builder.Services.AddTransient<VidFlow.Api.Features.Jobs.RenderJobProcessor>();

// Register LLM providers
builder.Services.AddLlmProviders(builder.Configuration);

// Register Render service
builder.Services.AddScoped<RenderService>();

// Register Webhook service
builder.Services.Configure<WebhookOptions>(builder.Configuration.GetSection("Webhooks"));
builder.Services.AddHttpClient<IWebhookService, WebhookService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AllowAutoRedirect = false
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (origins.Length > 0)
            {
                policy.WithOrigins(origins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var rendersPath = Path.Combine(app.Environment.ContentRootPath, "renders");
Directory.CreateDirectory(rendersPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(rendersPath),
    RequestPath = "/renders"
});

app.UseCors();
app.UseExceptionHandler();

app.UseHangfireDashboard("/hangfire");

if (jwtOptions.Enabled)
{
    app.UseAuthentication();
}
app.UseAuthorization();

// Map SignalR hub
var hubEndpoint = app.MapHub<AgentActivityHub>("/hubs/agent-activity");
if (jwtOptions.Enabled)
{
    hubEndpoint.RequireAuthorization();
}

// Map feature endpoints
app.MapAuthEndpoints();
app.MapProjectEndpoints();
app.MapSceneEndpoints();
app.MapCharacterEndpoints();
app.MapStoryBibleEndpoints();
app.MapShotEndpoints();
app.MapProposalEndpoints();
app.MapStitchPlanEndpoints();
app.MapAgentEndpoints();
app.MapRenderEndpoints();
app.MapBudgetEndpoints();
app.MapEventEndpoints();
app.MapExportEndpoints();
app.MapNotificationEndpoints();
app.MapAssetEndpoints();
app.MapJobEndpoints();

app.Run();
