using Microsoft.AspNetCore.SignalR;

namespace VidFlow.Api.Hubs;

/// <summary>
/// SignalR hub for real-time agent activity updates.
/// Clients can subscribe to project or scene groups to receive updates.
/// </summary>
public class AgentActivityHub : Hub
{
    /// <summary>
    /// Join a project group to receive project-level updates.
    /// </summary>
    public async Task JoinProjectGroup(Guid projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }

    /// <summary>
    /// Leave a project group.
    /// </summary>
    public async Task LeaveProjectGroup(Guid projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }

    /// <summary>
    /// Join a scene group to receive scene-level updates (agent proposals, status changes).
    /// </summary>
    public async Task JoinSceneGroup(Guid sceneId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"scene-{sceneId}");
    }

    /// <summary>
    /// Leave a scene group.
    /// </summary>
    public async Task LeaveSceneGroup(Guid sceneId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"scene-{sceneId}");
    }
}
