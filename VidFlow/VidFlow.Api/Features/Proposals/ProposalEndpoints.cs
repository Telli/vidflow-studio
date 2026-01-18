using VidFlow.Api.Features.Proposals;

namespace VidFlow.Api.Features.Proposals;

public static class ProposalEndpoints
{
    public static void MapProposalEndpoints(this IEndpointRouteBuilder app)
    {
        GetProposals.MapEndpoint(app);
        ApplyProposal.MapEndpoint(app);
        DismissProposal.MapEndpoint(app);
    }
}
