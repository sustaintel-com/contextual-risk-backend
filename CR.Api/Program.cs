using System.Text;
using CR.Api.Audit;
using CR.Model.Data;
using CR.Model.Users;
using CR.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Password hasher
// Singleton allows DbContextFactory to resolve it from the root provider.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddPooledDbContextFactory<CRContext>(
    (sp, options) =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        );

        options.AddInterceptors(sp.GetRequiredService<AuditHistoryInterceptor>());
    }
);

builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<AuditHistoryInterceptor>();
builder.Services.AddScoped<IAuditContextProvider, HttpAuditContextProvider>();

// // Register CRContext for direct scoped resolution by Seeder
// builder.Services.AddScoped<CRContext>(serviceProvider =>
// {
//     var factory = serviceProvider.GetRequiredService<IDbContextFactory<CRContext>>();

//     return factory.CreateDbContext();
// });

// Database seeder
builder.Services.AddScoped<Seeder>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:5174")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// JWT Authentication setup
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT key is not configured.");
}

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.IncludeErrorDetails = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

var app = builder.Build();

// Apply migrations and seed the database
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = scope.ServiceProvider.GetRequiredService<CRContext>();

        var seeder = scope.ServiceProvider.GetRequiredService<Seeder>();

        // Uncomment when you want automatic migrations:
        // await context.Database.MigrateAsync();

        // Uncomment to create initial roles and admin user:
        // await seeder.SeedDatabaseAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();
app.Run();
