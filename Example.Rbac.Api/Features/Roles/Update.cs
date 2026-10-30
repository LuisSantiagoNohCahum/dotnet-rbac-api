using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace Example.Rbac.Api.Features.Roles;

public static class Update
{
    public static class ErrorCodes
    {
        public const string UpdateRoleValidation = "UpdateRole.Validation";
        public const string UpdateRoleNotFound = "UpdateRole.NotFound";
        public const string UpdateRoleIsDefault = "UpdateRole.DefaultRole";

    }

    // If you want use attrinutes as JsonIgnore or IgnoreDataMember in records, use the [property: JsonIgnore] declaration.
    // See: https://github.com/dotnet/runtime/issues/60690
    // See: https://github.com/dotnet/runtime/issues/62961
    public record UpdateRoleRequest(string Name, string Description, bool IsActive, bool IsFullAccess, int[] Permissions);

    public class UpdateRoleCommand : IRequest<Result<int>>
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description {get; set;}
        public bool IsActive {get; set;}
        public bool IsFullAccess {get; set;}
        public int[] Permissions { get; set; } = [];
    }

    public class Validator : AbstractValidator<UpdateRoleCommand>
    {
        public Validator()
        {
            RuleFor(urc => urc.Id)
                .GreaterThan(0);

            RuleFor(urc => urc.Name)
                .NotEmpty();

            RuleFor(urc => urc.Description)
                .NotEmpty();

            RuleFor(urc => urc.Permissions)
                .NotEmpty()
                .When(urc => !urc.IsFullAccess)
                .WithMessage("Debe especificar los permisos del rol.");
        }
    }

    public sealed class Handler(AppDbContext dbContext, IValidator<UpdateRoleCommand> validator): IRequestHandler<UpdateRoleCommand, Result<int>>
    {
        private readonly AppDbContext _dbContext = dbContext;
        private readonly IValidator<UpdateRoleCommand> _validator = validator;

        public async ValueTask<Result<int>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<int>.Failure(
                    new (ErrorCodes.UpdateRoleValidation, validationResult.ToString()));
            }

            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == request.Id);

            if (role is null)
            {
                return Result<int>.Failure(
                    new(ErrorCodes.UpdateRoleNotFound, "No se encontro el rol especificado."));
            }

            if (role.IsDefault)
            {
                return Result<int>.Failure(
                    new(ErrorCodes.UpdateRoleIsDefault, "No se puede modificar el rol debido a que es un rol por defecto."));
            }

            role.Name = request.Name;
            role.Description = request.Description;
            role.IsActive = request.IsActive;
            role.IsFullAccess = request.IsFullAccess;

            role.Permissions.Clear();
            if (!role.IsFullAccess)
            {
                var permissions = await _dbContext.Permissions
                    .Where(p => request.Permissions.Contains(p.Id))
                    .ToListAsync();

                role.Permissions.AddRange(permissions);
            }

            await _dbContext.SaveChangesAsync();
            return role.Id;
        }
    }
}

public sealed class UpdateRoleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/roles/{id:int}", async ([FromRoute] int id, [FromBody] Update.UpdateRoleRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = request.Adapt<Update.UpdateRoleCommand>();
            command.Id = id;

            var result = await sender.Send(command, ct);
            var error = result.Error;
            if (result.IsFailure && error is not null)
            {
                return error.ToProblemDetail($"/roles/{id}", error.Code switch
                {
                    Get.ErrorCodes.GetRoleValidation => StatusCodes.Status400BadRequest,
                    Get.ErrorCodes.GetRoleNotFound => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status500InternalServerError
                });
            }

            return Results.Ok(result.Data);
        })
        .WithTags("Roles")
        .WithSummary("Updates a role info.")
        .WithDescription("Updates a single role info with related permision.")
        .Produces<int>(statusCode: StatusCodes.Status200OK);
    }
}
