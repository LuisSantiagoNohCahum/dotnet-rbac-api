using Microsoft.AspNetCore.Mvc;

namespace Example.Rbac.Api.Features.Roles;

public static class Get
{
    public static class ErrorCodes
    {
        public const string GetRoleValidation = "GetRole.Validation";
        public const string GetRoleNotFound = "GetRole.NotFound";

    }
    public record GetRoleQuery(int Id) : IRequest<Result<GetRoleDto>>;
    public record GetRoleDto(int Id, string Key, string Description, bool IsDefault, bool IsActive, bool IsFullAccess, GetRolePermissionDto[] Permissions);
    public record GetRolePermissionDto(int Id, string Key);
    public class Validator : AbstractValidator<GetRoleQuery>
    {
        public Validator() => RuleFor(r => r.Id).GreaterThan(0);
    }

    public sealed class Handler(AppDbContext dbContext, IValidator<GetRoleQuery> validator): IRequestHandler<GetRoleQuery, Result<GetRoleDto>>
    {
        private readonly AppDbContext _dbContext = dbContext;
        private readonly IValidator<GetRoleQuery> _validator = validator;
        public async ValueTask<Result<GetRoleDto>> Handle(GetRoleQuery request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<GetRoleDto>.Failure(
                    new(ErrorCodes.GetRoleValidation, validationResult.ToString()));
            }

            var role = await _dbContext.Roles
                .Include(r => r.Permissions)
                .Where(r => r.Id == request.Id)
                .Select(r => new GetRoleDto(
                    r.Id,
                    r.Key,
                    r.Description,
                    r.IsDefault,
                    r.IsActive,
                    r.IsFullAccess,
                    r.Permissions.Select(p => 
                        new GetRolePermissionDto(p.Id, p.Key)).ToArray()
                    ))
                .FirstOrDefaultAsync(cancellationToken);

            if (role is null)
            {
                return Result<GetRoleDto>.Failure(
                    new(ErrorCodes.GetRoleNotFound, "No se encontro el recurso solicitado."));
            }

            return role;
        }
    }
}

public sealed class GetRoleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/roles/{id:int}", async ([FromRoute] int id, ISender sender, CancellationToken ct) => 
        {
            var query = new Get.GetRoleQuery(id);
            var result = await sender.Send(query, ct);
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
        .WithSummary("Gets a role info.")
        .WithDescription("Gets a single role info with related permision.")
        .Produces<Get.GetRoleDto>(statusCode: StatusCodes.Status200OK);
    }
}
