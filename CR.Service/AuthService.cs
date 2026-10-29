using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CR.Model.Data;
using CR.Model.Users;
using CR.Service.Dtos.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CR.Service;

public class AuthService(
    Microsoft.Extensions.Configuration.IConfiguration config,
    IPasswordHasher<User> passwordHasher,
    IDbContextFactory<CRContext> contextFactory
)
{
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config = config;
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
    private readonly IDbContextFactory<CRContext> _contextFactory = contextFactory;

    public async Task<User?> AuthenticateUserAsync(string username, string password)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context
            .Users.Where(u => u.Username == username || u.Email == username)
            .FirstOrDefaultAsync();
        if (user == null)
        {
            return null;
        }
        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }
        return user;
    }

    public async Task<UserDto> AllUserAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Get all role IDs and role names assigned to this user
        var roles = await context
            .Set<UserRoleMap>()
            .Where(mapping => mapping.UserId == user.Id)
            .Join(
                context.Roles,
                mapping => mapping.RoleId,
                role => role.RoleId,
                (mapping, role) => new { role.RoleId, role.Name }
            )
            .ToListAsync();

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Active = user.Active,
            RoleIds = roles.Select(role => role.RoleId).ToList(),
            RoleNames = roles.Where(role => role.Name != null).Select(role => role.Name!).ToList(),
            CreatedBy = user.CreatedBy,
            UpdatedBy = user.UpdatedBy,
        };
    }

    // public async Task<string> GenerateJSONWebToken(User userInfo)
    // {
    //     await using var context = await _contextFactory.CreateDbContextAsync();
    //     var role = await context
    //         .Roles.Where(r => r.RoleId == userInfo.UserRoleMaps.FirstOrDefault().RoleId)
    //         .FirstOrDefaultAsync();
    //     if (role == null)
    //     {
    //         throw new Exception("Role not found for the given RoleId.");
    //     }
    //     var key =
    //         _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
    //     var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    //     var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    //     var claims = new[]
    //     {
    //         new Claim(ClaimTypes.Name, userInfo.Username),
    //         new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString()),
    //         new Claim(ClaimTypes.Role, role.Name ?? ""),
    //         new Claim(ClaimTypes.Email, userInfo.Email),
    //         new Claim(ClaimTypes.Expired, userInfo.Active?.ToString() ?? ""),
    //         new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    //         new Claim(
    //             JwtRegisteredClaimNames.Iat,
    //             DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
    //         ),
    //     };

    //     var token = new JwtSecurityToken(
    //         _config["Jwt:Issuer"],
    //         _config["Jwt:Issuer"],
    //         claims,
    //         expires: DateTime.Now.AddHours(8),
    //         signingCredentials: credentials
    //     );
    //     return new JwtSecurityTokenHandler().WriteToken(token);
    // }

    public async Task<string> GenerateJSONWebToken(User userInfo)
    {
        ArgumentNullException.ThrowIfNull(userInfo);

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Retrieve a role assigned to this user.
        var roleName = await context
            .Set<UserRoleMap>()
            .Where(mapping => mapping.UserId == userInfo.Id)
            .Join(
                context.Roles,
                mapping => mapping.RoleId,
                role => role.RoleId,
                (mapping, role) => role.Name
            )
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new InvalidOperationException(
                $"No valid role is assigned to user '{userInfo.Username}'."
            );
        }

        var key =
            _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, userInfo.Username ?? ""),
            new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString()),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(ClaimTypes.Email, userInfo.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
        };

        var issuer = _config["Jwt:Issuer"];

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<User?> findByUsernameAsynce(string Username)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existinguser = await context
            .Users.Where(u => u.Username == Username)
            .FirstOrDefaultAsync();
        if (existinguser == null)
        {
            return null;
        }
        return existinguser;
    }
}
