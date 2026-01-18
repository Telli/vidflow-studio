using VidFlow.Api.Features.Characters;

namespace VidFlow.Api.Features.Characters;

public static class CharacterEndpoints
{
    public static void MapCharacterEndpoints(this IEndpointRouteBuilder app)
    {
        CreateCharacter.MapEndpoint(app);
        GetCharacters.MapEndpoint(app);
        UpdateCharacter.MapEndpoint(app);
    }
}
