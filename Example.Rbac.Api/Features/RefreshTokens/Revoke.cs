namespace Example.Rbac.Api.Features.RefreshTokens;

public static class Revoke
{
    public record RevokeRequest : IRequest<Result<bool>>;

    public sealed class Handler(ISessionWrapper sessionWrapper, AppDbContext dbContext): IRequestHandler<RevokeRequest, Result<bool>>
    {
        private readonly ISessionWrapper _sessionWrapper = sessionWrapper;
        private readonly AppDbContext _dbContext = dbContext;

        public async ValueTask<Result<bool>> Handle(RevokeRequest request, CancellationToken cancellationToken)
        {
            await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == _sessionWrapper.UserId)
                .ExecuteDeleteAsync();

            return true;
        }
    }
}

public sealed class RevokeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("refresh-tokens/revoke", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new Revoke.RevokeRequest());
            return Results.Ok(result.Data);
        })
        .WithTags(EndpointTags.RefreshTokens)
        .WithSummary("Revoke current user tokens")
        .WithDescription("Revoke all current user refresh tokens")
        .Produces<bool>(statusCode: StatusCodes.Status200OK)
        .RequireAuthorization();
    }
}
