namespace Example.Rbac.Api.Features.Auth;

using BCrypt.Net;

public static class Login
{
    public static class ErrorCodes
    {
        public const string Validation = "Login.Validation";
        public const string UserNotFound = "Login.UserNotFound";
        public const string UserInactive = "Login.UserInactive";
        public const string InvalidPassword = "Login.InvalidPassword";
    }
    public record Request(string Email, string Password) : IRequest<Result<Response>>;
    public record Response(string Token, string RefreshToken);

    // Configure validator behavior.
    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ITokenService tokenService, IValidator<Request> validator)
        : IRequestHandler<Request, Result<Response>>
    {       
        private readonly ITokenService _tokenService = tokenService;
        private readonly IValidator<Request> _validator = validator;
        private readonly AppDbContext _dbContext = dbContext;

        public async ValueTask<Result<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<Response>.Failure(new (ErrorCodes.Validation, validationResult.ToString()));
            
            var user = _dbContext.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user is null)
                return Result<Response>.Failure(new (ErrorCodes.UserNotFound, $"User {request.Email} not found"));
            
            if (!BCrypt.Verify(request.Password, user.HashPassword))
                return Result<Response>.Failure(new (ErrorCodes.InvalidPassword, "Credenciales inválidas"));

            if (!user.IsActive)
                return Result<Response>.Failure(new(ErrorCodes.UserInactive, $"Tu cuenta esta inactiva. Contacta a soporte."));

            var userPermissions = user.Roles
                .SelectMany(ur => ur.Permissions.Select(p => p.Name))
                .Distinct()
                .ToArray();

            var token = _tokenService.GenerateToken(
                new(user.Id,
                    user.Email,
                    user.Roles.Select(ur => ur.Name).ToArray(), 
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

            return new Response(token, refreshToken);
        }
    }
}

public class LoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (Login.Request request, ISender sender) =>
        {
            var result = await sender.Send(request);
            if (result.IsFailure && result.Error is not null)
            {
                return result.Error.Code switch
                {
                    Login.ErrorCodes.Validation => Results.BadRequest(result.Error),
                    Login.ErrorCodes.UserNotFound => Results.NotFound(result.Error),
                    Login.ErrorCodes.UserInactive => Results.Forbid(),
                    Login.ErrorCodes.InvalidPassword => Results.Unauthorized(),
                    _ => Results.Problem(result.Error.Message)
                };
            }
            return Results.Ok(result.Data);
        })
        .WithTags(EndpointTags.Authentication)
        .WithSummary("Login to the application")
        .WithDescription("Login to the application")
        .Accepts<Login.Request>("application/json")
        .Produces<Login.Response>(200)
        .RequireRateLimiting("per-ip");
    }
}
