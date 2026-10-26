using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Example.Rbac.Api;

public static class DependencyInjection
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISessionWrapper, SessionWrapper>();
        services.AddScoped<ITokenService, TokenService>();

        // Configure mediator
        services.AddMediator(opt => { 
            opt.Assemblies = [typeof(Program).Assembly];
            opt.ServiceLifetime = ServiceLifetime.Scoped;
        });

        // Configure database context
        services.AddDbContext<AppDbContext>(options =>
            options
                .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            );
        return services;
    }
}
