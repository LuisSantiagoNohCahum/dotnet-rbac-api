using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace Example.Rbac.Api.Features.Roles;

public static class GetAll
{
    public record GetAllQuery(int PageNumber = 0, int Size = 10, string? OrderBy = null, string? OrderDir = null, string? SearchTerm = null) : IRequest<PaginatedResponse<GetAllRoleDto>>;
    public record GetAllRoleDto(int Id, string Key, string Name, bool IsActive, bool IsDefault, bool IsFullAccess, int NumberOfPermissions);

    public sealed class Handler(AppDbContext dbContext): IRequestHandler<GetAllQuery, PaginatedResponse<GetAllRoleDto>>
    {
        private readonly AppDbContext _dbContext = dbContext;
        public async ValueTask<PaginatedResponse<GetAllRoleDto>> Handle(GetAllQuery request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Roles
                .Include(r => r.Permissions)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(r => r.Name.Contains(request.SearchTerm) 
                    || r.Description.Contains(request.SearchTerm)
                    || r.Key.Contains(request.SearchTerm));
            }

            Expression<Func<Role, object>> orderPredicate = request.OrderBy switch
            {
                "key" => (Role r) => r.Key,
                "name" => (Role r) => r.Name,
                _ => (Role r) => r.Key
            };

            query = request.OrderDir?.ToLower() == "desc"
                ? query.OrderByDescending(orderPredicate) : query.OrderBy(orderPredicate);

            var totalCount = await query.CountAsync(cancellationToken);

            var roles = await query.Select(r => new GetAllRoleDto(
                    r.Id,
                    r.Key,
                    r.Name,
                    r.IsActive,
                    r.IsDefault,
                    r.IsFullAccess,
                    r.Permissions.Count()
                    ))
                .Skip(request.PageNumber * request.Size)
                .ToListAsync(cancellationToken);

            return new PaginatedResponse<GetAllRoleDto>()
            {
                Items = roles?.AsReadOnly() ?? [],
                TotalCount = totalCount,
                HasPrevious = request.PageNumber > 0,
                HasNext = totalCount > (request.PageNumber + 1 * request.Size)
            };
        }
    }
}

public sealed class GetAllRolesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/roles", async ([AsParameters] GetAll.GetAllQuery query, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
        .WithTags("Roles")
        .WithSummary("Gets all roles.")
        .WithDescription("Gets a paginated collection of roles.")
        .Produces<IEnumerable<GetAll.GetAllRoleDto>>(statusCode: StatusCodes.Status200OK);
    }
}
