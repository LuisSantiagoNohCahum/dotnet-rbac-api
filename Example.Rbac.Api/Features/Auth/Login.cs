namespace Example.Rbac.Api.Features.Auth;

using BCrypt.Net;
using System.Text.Json;

public static class Login
{
    public static class ErrorCodes
    {
        public const string Validation = "Login.Validation";
        public const string InvalidCredentials = "Login.InvalidCredentials";
        public const string UserInactive = "Login.UserInactive";
    }

    public record LoginRequest(string Email, string Password) : IRequest<Result<LoginResponse>>;
    public record LoginResponse(string AccessToken, string RefreshToken, bool IsTwoFactorEnabled);

    // Configure validator behavior.
    public class Validator : AbstractValidator<LoginRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ITokenService tokenService, IValidator<LoginRequest> validator)
        : IRequestHandler<LoginRequest, Result<LoginResponse>>
    {       
        private readonly ITokenService _tokenService = tokenService;
        private readonly IValidator<LoginRequest> _validator = validator;
        private readonly AppDbContext _dbContext = dbContext;

        public async ValueTask<Result<LoginResponse>> Handle(LoginRequest request, CancellationToken cancellationToken)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<LoginResponse>.Failure(new(ErrorCodes.Validation, validationResult.ToString()));

            var user = _dbContext.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
                .AsSplitQuery()
                .FirstOrDefault(u => u.Email == request.Email);

            if (user is null || !BCrypt.Verify(request.Password, user.HashPassword))
                return Result<LoginResponse>.Failure(new(ErrorCodes.InvalidCredentials, "Credenciales inválidas"));

            if (!user.IsActive)
                return Result<LoginResponse>.Failure(new(ErrorCodes.UserInactive, $"Tu cuenta esta inactiva. Contacta a soporte."));
            
            // Enviar a un aggregator o a una cola con outofbox pattern, nunca exponer datos del envio al usuario.
            if (user.EnableTwoFactorAuth)
                return new LoginResponse(string.Empty, string.Empty, true);
            
            var userPermissions = user.Roles
                .SelectMany(ur => ur.Permissions.Select(p => p.Key))
                .Distinct()
                .ToArray();

            var token = _tokenService.GenerateToken(
                new(user.Id,
                    user.Email,
                    user.Roles.Select(ur => ur.Key).ToArray(), 
                    userPermissions)
            );

            var refreshToken = _tokenService.GenerateRefreshToken();

            _dbContext.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresOnUtc = DateTime.UtcNow.AddHours(1)
            });

            await _dbContext.SaveChangesAsync();
            return new LoginResponse(token, refreshToken, false);
        }
    }
}

public class LoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (Login.LoginRequest request, ISender sender) =>
        {
            var result = await sender.Send(request);
            if (result.IsFailure && result.Error is not null)
            {
                return result.Error.ToProblemDetail("/auth/login", result.Error.Code switch
                {
                    Login.ErrorCodes.Validation => StatusCodes.Status400BadRequest,
                    Login.ErrorCodes.InvalidCredentials => StatusCodes.Status401Unauthorized,
                    Login.ErrorCodes.UserInactive => StatusCodes.Status403Forbidden,
                    _ => StatusCodes.Status500InternalServerError
                });
            }
            return Results.Ok(result.Data);
        })
        .WithTags(EndpointTags.Authentication)
        .WithSummary("Login to the application")
        .WithDescription("Login to the application")
        .Accepts<Login.LoginRequest>("application/json")
        .Produces<Login.LoginResponse>(StatusCodes.Status200OK)
        .RequireRateLimiting("per-ip");
    }
}
