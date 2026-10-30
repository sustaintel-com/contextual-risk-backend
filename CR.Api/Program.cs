using System.Text;
using CR.Api.Audit;
using CR.Model.Data;
using CR.Model.Users;
using CR.Service;
using CR.Service.Roles;
using CR.Service.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
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

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();

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
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Authentication setup. Cookie authentication is used by default, while the
// policy scheme keeps existing Bearer-token clients working as well.
// var jwtKey = builder.Configuration["Jwt:Key"];
// if (string.IsNullOrEmpty(jwtKey))
// {
//     throw new InvalidOperationException("JWT key is not configured.");
// }

// builder
//     .Services.AddAuthentication(options =>
//     {
//         options.DefaultAuthenticateScheme = "SmartAuthentication";
//         options.DefaultChallengeScheme = "SmartAuthentication";
//     })
//     .AddPolicyScheme("SmartAuthentication", "Cookie or JWT", options =>
//     {
//         options.ForwardDefaultSelector = context =>
//         {
//             var authorization = context.Request.Headers.Authorization.ToString();
//             return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
//                 ? JwtBearerDefaults.AuthenticationScheme
//                 : CookieAuthenticationDefaults.AuthenticationScheme;
//         };
//     })
//     .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
//     {
//         options.Cookie.Name = "contextual-risk-auth";
//         options.Cookie.HttpOnly = true;
//         options.Cookie.SameSite = SameSiteMode.Lax;
//         options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
//         options.ExpireTimeSpan = TimeSpan.FromHours(8);
//         options.SlidingExpiration = true;
//         options.Events.OnRedirectToLogin = context =>
//         {
//             context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//             return Task.CompletedTask;
//         };
//         options.Events.OnRedirectToAccessDenied = context =>
//         {
//             context.Response.StatusCode = StatusCodes.Status403Forbidden;
//             return Task.CompletedTask;
//         };
//     })
//     .AddJwtBearer(options =>
//     {
//         options.IncludeErrorDetails = true;
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = false,
//             ValidateAudience = false,
//             ValidateLifetime = false,
//             ValidateIssuerSigningKey = false,
//             ValidIssuer = builder.Configuration["Jwt:Issuer"],
//             ValidAudience = builder.Configuration["Jwt:Audience"],
//             IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
//         };
//     });

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "contextual-risk-auth";

        options.Cookie.HttpOnly = true;

        options.Cookie.IsEssential = true;

        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // Cookie expires after 8 hours.
        //options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.ExpireTimeSpan = TimeSpan.FromMinutes(1);

        // Refresh expiration during active usage.
        // options.SlidingExpiration = true;
        options.SlidingExpiration = false;

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                return Task.CompletedTask;
            },

            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;

                return Task.CompletedTask;
            },
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
