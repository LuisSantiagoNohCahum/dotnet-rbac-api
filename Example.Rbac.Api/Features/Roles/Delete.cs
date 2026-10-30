using Microsoft.AspNetCore.Mvc;

namespace Example.Rbac.Api.Features.Roles;

public class Delete
{
    public static class ErrorCodes
    {
        public const string DeleteRoleValidation = "DeleteRole.Validation";
        public const string DeleteRoleNotFound = "DeleteRole.NotFound";
        public const string DeleteRoleIsDefault = "DeleteRole.DefaultRole";

    }
    public record DeleteRoleCommand(int Id): IRequest<Result<bool>>;
    public class Validator : AbstractValidator<DeleteRoleCommand>
    {
        public Validator() => RuleFor(r => r.Id).GreaterThan(0);
    }

    public sealed class Handler(AppDbContext dbContext, IValidator<DeleteRoleCommand> validator): IRequestHandler<DeleteRoleCommand, Result<bool>>
    {
        private readonly AppDbContext _dbContext = dbContext;
        private readonly IValidator<DeleteRoleCommand> _validator = validator;

        public async ValueTask<Result<bool>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<bool>.Failure(
                    new (ErrorCodes.DeleteRoleValidation, validationResult.ToString()));
            }

            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
            if (role is null)
            {
                return Result<bool>.Failure(
                    new (ErrorCodes.DeleteRoleNotFound, "No se encontro el rol especificado."));
            }

            if (role.IsDefault)
            {
                return Result<bool>.Failure(
                    new(ErrorCodes.DeleteRoleIsDefault, "El rol no se puede eliminar debido a que es un rol por defecto."));
            }

            _dbContext.Roles.Remove(role);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}

public class DeleteRoleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/roles/{id:int}", async ([FromRoute] int id, ISender sender, CancellationToken ct) =>
        {
            var command = new Delete.DeleteRoleCommand(id);
            var result = await sender.Send(command, ct);
            var error = result.Error;
            if (result.IsFailure && error is not null)
            {
                return error.ToProblemDetail($"/roles/{id}", error.Code switch
                {
                    Delete.ErrorCodes.DeleteRoleValidation => StatusCodes.Status400BadRequest,
                    Delete.ErrorCodes.DeleteRoleNotFound => StatusCodes.Status404NotFound,
                    Delete.ErrorCodes.DeleteRoleIsDefault => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status500InternalServerError
                });
            }

            return Results.Ok(result.Data);
        })
        .WithTags("Roles")
        .WithSummary("Delete a role")
        .WithDescription("Delete a single role and permision relations.")
        .Produces<bool>(statusCode: StatusCodes.Status200OK);
    }
}
