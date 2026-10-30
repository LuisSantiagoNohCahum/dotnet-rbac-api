using Microsoft.AspNetCore.Mvc;

namespace Example.Rbac.Api.Features.Roles;

public static class Create
{
    public record CreateRoleCommand(string Key, string Name, string Description, bool IsFullAccess, int[] Permissions): IRequest<Result<CreateRoleResponse>>;
    public record CreateRoleResponse(string Code);

    public class Validator :AbstractValidator<CreateRoleCommand>
    {
        public Validator()
        {
            RuleFor(cr => cr.Key)
                .NotEmpty()
                .MinimumLength(4)
                .WithMessage("Ingrese una clave valida.");

            RuleFor(cr => cr.Name)
                .NotEmpty();

            RuleFor(cr => cr.Description)
                .NotEmpty();

            RuleFor(cr => cr.Permissions)
                .NotEmpty()
                .When(cr => !cr.IsFullAccess)
                .WithMessage("Debe especificar los permisos del rol.");
        }
    }

    public sealed class Handler(AppDbContext dbContext, IValidator<CreateRoleCommand> validator): IRequestHandler<CreateRoleCommand, Result<CreateRoleResponse>>
    {
        private readonly AppDbContext _dbContext = dbContext;
        private readonly IValidator<CreateRoleCommand> _validator = validator;
        public async ValueTask<Result<CreateRoleResponse>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                return Result<CreateRoleResponse>.Failure(
                    new ("CreateRole.Validation", validationResult.ToString()));
            }

            var role = new Role()
            {
                Key = request.Key,
                Name = request.Name,
                Description = request.Name,
                IsActive = true,
                IsDefault = false,
                IsFullAccess = request.IsFullAccess
            };

            if (!request.IsFullAccess)
            {
                var permissions = await _dbContext.Permissions
                    .Where(p => request.Permissions.Contains(p.Id)).ToListAsync(cancellationToken);
                role.Permissions.AddRange(permissions);
            }

            _dbContext.Roles.Add(role);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreateRoleResponse(role.Key);
        }
    }
}

public sealed class CreateRoleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/roles", async ([FromBody] Create.CreateRoleCommand request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(request, ct);
            return result.IsFailure
                ? result?.Error?.ToProblemDetail("/roles", StatusCodes.Status400BadRequest)
                : Results.Created("/roles", result.Data);
        })
        .WithTags("Roles")
        .WithSummary("Create system role")
        .WithDescription("Allow created a new system auth role.")
        .Accepts<Create.CreateRoleCommand>("application/json")
        .Produces<Create.CreateRoleResponse>(contentType: "application/json", statusCode: StatusCodes.Status200OK);
        //.RequireAuthorization(); 
    }
}
