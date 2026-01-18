using VidFlow.Api.Features.StoryBible;

namespace VidFlow.Api.Features.StoryBible;

public static class StoryBibleEndpoints
{
    public static void MapStoryBibleEndpoints(this IEndpointRouteBuilder app)
    {
        CreateStoryBible.MapEndpoint(app);
        GetStoryBible.MapEndpoint(app);
        UpdateStoryBible.MapEndpoint(app);
        GenerateStoryBible.MapEndpoint(app);
    }
}
