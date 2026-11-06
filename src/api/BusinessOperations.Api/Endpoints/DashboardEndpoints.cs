using BusinessOperations.Application.Dashboard;

namespace BusinessOperations.Api.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/", async (IDashboardService dashboard, CancellationToken cancellationToken) =>
        {
            var result = await dashboard.GetAsync(cancellationToken);
            return Results.Ok(result);
        });

        return group;
    }
}