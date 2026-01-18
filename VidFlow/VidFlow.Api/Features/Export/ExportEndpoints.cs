namespace VidFlow.Api.Features.Export;

public static class ExportEndpoints
{
    public static void MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        ExportFountain.MapEndpoint(app);
        ExportJson.MapEndpoint(app);
        ExportStoryboardPdf.MapEndpoint(app);
    }
}
