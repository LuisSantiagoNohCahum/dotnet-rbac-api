using Carter;
using Example.Rbac.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.ConfigureServices(builder.Configuration);

// Configure problem details and exeption handler midleware.
// builder.Services.AddProblemDetails();

// TODO. Use cookies for store the JWT.
builder.Services.AddAuthentication(s =>
{
    s.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    s.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opt =>
{
    var issuer = builder.Configuration.GetValue<string>(ConfigurationConstants.JwtIssuer);
    var audience = builder.Configuration.GetValue<string>(ConfigurationConstants.JwtAudience);
    var key = builder.Configuration.GetValue<string>(ConfigurationConstants.JwtKey) ?? "";

    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = !string.IsNullOrEmpty(issuer),
        ValidateAudience = !string.IsNullOrEmpty(audience),
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("per-ip", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10, // Maximum number of requests allowed
                Window = TimeSpan.FromMinutes(1), // Time window for the limit
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // No queuing, reject requests immediately when limit is reached
            });
    });

    options.AddPolicy("per-user", context =>
    {
        var userEmail = context.User?.FindFirst(CustomClaims.Email)?.Value ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userEmail,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5, // Maximum number of requests allowed
                Window = TimeSpan.FromMinutes(1), // Time window for the limit
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // No queuing, reject requests immediately when limit is reached
            });
    });
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly, includeInternalTypes: true);
builder.Services.AddCarter();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Run database migrations on startup
    var scope = app.Services.CreateScope();
    using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DbInitializer.Initialize(context);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapCarter();

// TODO. Add validattion behavior.
// TODO. Add global exception hanlder.

app.Run();
