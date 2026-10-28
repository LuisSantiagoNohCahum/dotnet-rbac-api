using Example.Rbac.Api.Features.Auth;

namespace Example.Rbac.Api.Features.RefreshTokens;

public static class Renew
{
    public static class ErrorCodes
    {
        public const string RefreshTokenValidation = "RefreshToken.Validation";
        public const string RefreshTokenInvalid = "RefreshToken.Invalid";
        public const string RefreshTokenUserInactive = "RefreshToken.UserInactive";
    }

    public record RenewTokenRequest(string RefreshToken): IRequest<Result<RenewTokenResponse>>;
    public record RenewTokenResponse(string AccessToken, string RefreshToken);

    public class Validator : AbstractValidator<RenewTokenRequest>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .WithMessage("El token de renovación es requerido.");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ITokenService tokenService, IValidator<RenewTokenRequest> validator)
        : IRequestHandler<RenewTokenRequest, Result<RenewTokenResponse>>
    {
        private readonly ITokenService _tokenService = tokenService;
        private readonly IValidator<RenewTokenRequest> _validator = validator;
        private readonly AppDbContext _dbContext = dbContext;
        public async ValueTask<Result<RenewTokenResponse>> Handle(RenewTokenRequest request, CancellationToken cancellationToken)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<RenewTokenResponse>.Failure(new(
                    ErrorCodes.RefreshTokenValidation, validationResult.ToString()));

            var refreshToken = _dbContext.RefreshTokens
                .FirstOrDefault(rt => rt.Token == request.RefreshToken);

            if (refreshToken is null || refreshToken.IsRevoked || refreshToken.ExpiresOnUtc < DateTime.UtcNow)
                return Result<RenewTokenResponse>.Failure(new(ErrorCodes.RefreshTokenInvalid, 
                    "El token de renovación es inválido o ha expirado."));

            var user = await _dbContext.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.Id == refreshToken.UserId, cancellationToken);

            if (user is null || !user.IsActive)
                return Result<RenewTokenResponse>.Failure(new(ErrorCodes.RefreshTokenUserInactive, 
                    "El usuario asociado al token de renovación está inactivo."));
            
            var userPermissions = user.Roles
                .SelectMany(ur => ur.Permissions.Select(p => p.Key))
                .Distinct()
                .ToArray();

            var newAccessToken = _tokenService.GenerateToken(
                new(user.Id,user.Email,user.Roles.Select(ur => ur.Key).ToArray(),userPermissions));
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            refreshToken.IsRevoked = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<RenewTokenResponse>.Success(new(newAccessToken, newRefreshToken));
        }
    }
}

public sealed class RenewEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("refresh-tokens/renew", async (Renew.RenewTokenRequest request, ISender mediator) =>
        {
            var result = await mediator.Send(request);
            if (result.IsFailure && result.Error is not null)
            {
                return result.Error.ToProblemDetail("refresh-tokens/renew", result.Error.Code switch
                {
                    Renew.ErrorCodes.RefreshTokenValidation => StatusCodes.Status400BadRequest,
                    Renew.ErrorCodes.RefreshTokenInvalid => StatusCodes.Status401Unauthorized,
                    Renew.ErrorCodes.RefreshTokenUserInactive => StatusCodes.Status403Forbidden,
                    _ => StatusCodes.Status500InternalServerError
                });
            }
            return Results.Ok(result.Data);
        })
        .WithTags(EndpointTags.RefreshTokens)
        .WithSummary("Renew tokens.")
        .WithDescription("Allow renew the access and refresh tokens.")
        .Accepts<Renew.RenewTokenRequest>("application/json")
        .Produces<Renew.RenewTokenResponse>(StatusCodes.Status200OK)
        .RequireRateLimiting("per-ip");
    }
}
