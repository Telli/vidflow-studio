using VidFlow.Api.Features.Agents;
using VidFlow.Api.Features.Agents.Agents;

namespace VidFlow.Api.Features.Agents;

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        RunAgentPipeline.MapEndpoint(app);
    }
}
