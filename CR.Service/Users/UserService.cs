using CR.Model.Data;
using CR.Model.Users;
using CR.Service.Dtos.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CR.Service.Users;

public class UserService(
    IPasswordHasher<User> passwordHasher,
    IDbContextFactory<CRContext> contextFactory
) : IUserService
{
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
    private readonly IDbContextFactory<CRContext> _contextFactory = contextFactory;

    // public async Task<object> AddOrUpdateUserAsync(
    //     UserDto userDto,
    //     string? createdBy,
    //     IdType? createdByID
    // )
    // {
    //     await using var context = await _contextFactory.CreateDbContextAsync();

    //     User? user;

    //     if (userDto.Id == null)
    //     {
    //         user = new User { CreatedBy = createdByID, CreatedAt = DateTimeOffset.UtcNow };

    //         context.Users.Add(user);
    //     }
    //     else
    //     {
    //         user = await context
    //             .Users.Include(u => u.Roles)
    //             .Include(u => u.Corporates)
    //             .Include(u => u.Facilities)
    //             .FirstOrDefaultAsync(u => u.Id == userDto.Id);

    //         if (user == null)
    //             throw new Exception("User not found");

    //         user.UpdatedBy = createdByID;
    //         user.UpdatedAt = DateTimeOffset.UtcNow;
    //     }

    //      user.Id = userDto.Username;
    //     user.Name = userDto.Name;
    //     user.Email = userDto.Email;
    //     user.Password = _passwordHasher.HashPassword(user, userDto.Password);
    //     user.Active = userDto.Active;

    //     if (userDto.RoleId != null)
    //     {
    //         user.Roles = await context
    //             .Roles.Where(x => userDto.RoleId.Contains(x.RoleId))
    //             .ToListAsync();
    //     }

    //     if (userDto.CorporateId != null)
    //     {
    //         user.Corporates = await context
    //             .Corporates.Where(x => userDto.CorporateId.Contains(x.Id))
    //             .ToListAsync();
    //     }

    //     if (userDto.FacilityId != null)
    //     {
    //         user.Facilities = await context
    //             .Facilities.Where(x => userDto.FacilityId.Contains(x.Id))
    //             .ToListAsync();
    //     }

    //     await context.SaveChangesAsync();

    //     return new
    //     {
    //         UserId = user.Id,
    //         Message = userDto.Id == null
    //             ? "User created successfully!"
    //             : "User updated successfully!",
    //     };
    // }
    // public async Task<string?> ValidateUserAsync(UserCreateOrUpdateDto userDto)
    // {
    //     await using var context = await _contextFactory.CreateDbContextAsync();

    //     var existingUsername = await context.Users.FirstOrDefaultAsync(u =>
    //         u.Username.ToLower() == userDto.Username.ToLower()
    //     // && u.Id != (userDto.Id ?? 0)
    //     );

    //     var existingEmail = await context.Users.FirstOrDefaultAsync(u =>
    //         u.Email.ToLower() == userDto.Email.ToLower()
    //     // && u.Id != (userDto.Id ?? 0)
    //     );

    //     if (existingUsername != null && existingEmail != null)
    //     {
    //         return "Username and Email already exist. Please use a different username and email.";
    //     }

    //     if (existingUsername != null)
    //     {
    //         return "Username already exists. Please use a different username.";
    //     }

    //     if (existingEmail != null)
    //     {
    //         return "Email already exists. Please use a different email.";
    //     }

    //     return null;
    // }

    public async Task<object> AddOrUpdateUserAsync(
        CreateUserDTO userDto,
        string? createdBy,
        IdType? createdByID
    )
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        User? user;

        if (userDto.Id == null)
        {
            user = new User
            {
                CreatedBy = createdByID,
                CreatedAt = DateTimeOffset.UtcNow,
                Active = true,
            };

            context.Users.Add(user);
        }
        else
        {
            user = await context
                .Users.Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userDto.Id);

            if (user == null)
                throw new Exception("User not found");

            user.UpdatedBy = createdByID;
            user.UpdatedAt = DateTimeOffset.UtcNow;
        }

        user.Id = userDto.Id ?? user.Id;
        user.Username = userDto.Username;
        user.FullName = userDto.FullName;
        user.Email = userDto.Email;

        if (!string.IsNullOrWhiteSpace(userDto.Password))
        {
            user.Password = _passwordHasher.HashPassword(user, userDto.Password);
        }

        if (userDto.Id != null && userDto.Active.HasValue)
        {
            user.Active = userDto.Active.Value;
        }

        var roleIds = ResolveRoleIds(userDto);

        if (roleIds.Count > 0)
        {
            user.Roles = await context.Roles.Where(r => roleIds.Contains(r.RoleId)).ToListAsync();
        }

        await context.SaveChangesAsync();

        return new
        {
            UserId = user.Id,
            Message = userDto.Id == null
                ? "User created successfully!"
                : "User updated successfully!",
        };
    }

    private static List<IdType> ResolveRoleIds(CreateUserDTO userDto)
    {
        if (userDto.RoleId is { Count: > 0 })
            return userDto.RoleId.Distinct().ToList();

        if (userDto.Roles is not { Count: > 0 })
            return [];

        return userDto
            .Roles.Select(r => r.RoleId)
            .Where(roleId => roleId.HasValue)
            .Select(roleId => roleId!.Value)
            .Distinct()
            .ToList();
    }

    public async Task<User?> GetUserDetails(IdType id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var result = await context
            .Users.Where(y => y.Id == id)
            .Include(csm => csm.Roles)
            .FirstOrDefaultAsync();
        return result;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context
            .Users.Where(user => user.DeletedAt == null)
            .OrderBy(user => user.Username)
            .Select(user => new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Active = user.Active,
                RoleIds = user.Roles!.Select(role => role.RoleId).ToList(),
                RoleNames = user.Roles!.Where(role => role.Name != null)
                    .Select(role => role.Name!)
                    .ToList(),
                CreatedBy = user.CreatedBy,
                UpdatedBy = user.UpdatedBy,
            })
            .ToListAsync();
    }

    // public async Task<IActionResult> UpdatePasswordAsync(
    //     PasswordResetDto dto,
    //     string? updatedBy,
    //     IdType? updatedByID,
    //     string? Email
    // )
    // {
    //     await using var context = await _contextFactory.CreateDbContextAsync();
    //     var otpValue = new Random().Next(100000, 999999).ToString();

    //     if (Email == null)
    //         return new BadRequestObjectResult("Email cannot be null");

    //     var otpValid = await _otpService.ValidateOtpDuringPasswordResetAsync(
    //         dto.UserName,
    //         otpValue,
    //         Email
    //     );
    //     if (!otpValid)
    //         return new UnauthorizedObjectResult("Unauthorized");

    //     var MailToEmail = Email;

    //     try
    //     {
    //         var user = await context.Users.FirstOrDefaultAsync(u => u.Username == dto.UserName);

    //         if (user == null)
    //             return new NotFoundObjectResult("User not found");

    //         user.Password = _passwordHasher.HashPassword(user, dto.Password);
    //         user.UpdatedBy = updatedByID;
    //         user.UpdatedAt = DateTime.UtcNow;

    //         context.Users.Update(user);
    //         await context.SaveChangesAsync();

    //         string toEmail = MailToEmail;
    //         string subject = $"OTP For Password Reset";

    //         string body =
    //             $@"
    //             <html>
    //              <head>
    //                 <style>
    //                     body {{ font-family: Arial, sans-serif; }}
    //                     h2 {{ color: #2c3e50; }}
    //                     p {{ font-size: 14px; }}
    //                 </style>
    //             </head>
    //             <body>
    //                 <p>OTP: <b>{otpValue}</b> This OTP is valid for 10 minutes only.</p>
    //                 <p>This is an auto-generated message. Do not reply.</p>
    //             </body>
    //             </html>";

    //         // _backgroundJobClient.Enqueue(() =>
    //         //     _emailJobService.SendEmailJob(toEmail, subject, body)
    //         // );

    //         return new OkObjectResult("Password changed successfully");
    //     }
    //     catch (Exception ex)
    //     {
    //         return new ObjectResult(ex.Message) { StatusCode = 500 };
    //     }
    // }

    // public async Task<IActionResult> UpdatePasswordAdminAsync(
    //     PasswordResetDto dto,
    //     string? updatedBy,
    //     IdType? updatedByID,
    //     string? Email
    // )
    // {
    //     await using var context = await _contextFactory.CreateDbContextAsync();
    //     var otpValue = new Random().Next(100000, 999999).ToString();
    //     if (Email == null)
    //         return new BadRequestObjectResult("Email cannot be null");
    //     // Validate OTP
    //     var otpValid = await _otpService.ValidateOtpDuringPasswordResetAsync(
    //         dto.UserName,
    //         otpValue,
    //         Email
    //     );
    //     if (!otpValid)
    //         return new UnauthorizedObjectResult("Unauthorized");

    //     try
    //     {
    //         var user = await context.Users.FirstOrDefaultAsync(u => u.Username == dto.UserName);

    //         if (user == null)
    //             return new NotFoundObjectResult("User not found");

    //         user.Password = _passwordHasher.HashPassword(user, dto.Password);
    //         user.UpdatedBy = updatedByID;
    //         user.UpdatedAt = DateTime.UtcNow;

    //         context.Users.Update(user);
    //         await context.SaveChangesAsync();

    //         // string toEmail = "esgadmin@mailinator.com";
    //         string subject = $"Admin Password Reset – OTP Verification";

    //         string body =
    //             $@"
    //             <html>
    //              <head>
    //                 <style>
    //                     body {{ font-family: Arial, sans-serif; }}
    //                     h2 {{ color: #2c3e50; }}
    //                     p {{ font-size: 14px; }}
    //                 </style>
    //             </head>
    //             <body>
    //                 <p>OTP: <b>{otpValue}</b> This OTP is valid for 10 minutes only.</p>
    //                 <p>This is an auto-generated message. Do not reply.</p>
    //             </body>
    //             </html>";

    //         // _backgroundJobClient.Enqueue(() =>
    //         //     _emailJobService.SendEmailJob(toEmail, subject, body)
    //         // );

    //         return new OkObjectResult("Password changed successfully");
    //     }
    //     catch (Exception ex)
    //     {
    //         return new ObjectResult(ex.Message) { StatusCode = 500 };
    //     }
    // }

    // public async Task<List<EntityUserListDto>> GetEntityUserListAsync(IdType? entityId)
    // {
    //     await using var context = await _contextFactory.CreateDbContextAsync();

    //     if (entityId == null)
    //         throw new Exception("EntityId is required");

    //     var entityExists = await context.Entities.AnyAsync(e => e.Id == entityId.Value);

    //     if (!entityExists)
    //         throw new Exception("Entity not found");

    //     return await context
    //         .UserEntityMaps.Where(uem => uem.EntityId == entityId.Value)
    //         .Select(uem => uem.User)
    //         .Select(u => new EntityUserListDto
    //         {
    //             Id = u.Id,
    //             Username = u.Username,
    //             Name = u.Name,
    //             Email = u.Email,
    //             Active = u.Active,
    //             CreatedAt = u.CreatedAt,

    //             Roles = u.UserRoleMaps!.Select(urm => new EntityUserRoleDto
    //                 {
    //                     RoleId = urm.Role!.RoleId,

    //                     Name = urm.Role.Name == "Level 4 User" ? "Level 1 User" : urm.Role.Name,

    //                     EntityRole = urm.Role.EntityRole,
    //                 })
    //                 .ToList(),
    //         })
    //         .ToListAsync();
    // }

    // public async Task<IActionResult> GetUserListAsync(IdType? CorporateId, IdType? FacilityId)
    // {
    //     await using var context = await _contextFactory.CreateDbContextAsync();
    //     try
    //     {
    //         List<List<object>> userList;

    //         var corporate = await context.Corporates.FirstOrDefaultAsync(c => c.Id == CorporateId);

    //         if (corporate == null)
    //             return new NotFoundObjectResult("Corporate not found");

    //         if (FacilityId == null)
    //         {
    //             userList = await context
    //                 .Users.Where(u => u.Corporates!.Any(c => c.Id == corporate.Id))
    //                 .Select(u => new List<object> { u.Id, u.Username, u.Email })
    //                 .ToListAsync();
    //         }
    //         else
    //         {
    //             var facility = await context.Facilities.FirstOrDefaultAsync(f =>
    //                 f.Id == FacilityId
    //             );

    //             if (facility == null)
    //                 return new NotFoundObjectResult("Facility not found");

    //             userList = await context
    //                 .Users.Where(u =>
    //                     u.Corporates!.Any(c => c.Id == corporate.Id)
    //                     && u.Facilities!.Any(f => f.Id == facility.Id)
    //                 )
    //                 .Select(u => new List<object> { u.Id, u.Username, u.Email })
    //                 .ToListAsync();
    //         }

    //         return new OkObjectResult(ConvertToKeyValue(userList));
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine(ex.Message);
    //         return new NotFoundObjectResult(new List<object>());
    //     }
    // }

    // public async Task<List<object>> GetEntityUserListAsync(IdType? EntityId)
    // {
    //     await using var context = await _contextFactory.CreateDbContextAsync();

    //     if (EntityId == null)
    //         throw new Exception("EntityId is required");

    //     var entityExists = await context.Entities.AnyAsync(e => e.Id == EntityId.Value);

    //     if (!entityExists)
    //         throw new Exception("Entity not found");

    //     IQueryable<User> query = context
    //         .UserEntityMaps.Where(u => u.EntityId == EntityId.Value)
    //         .Select(uem => uem.User);

    //     return await query
    //         .Select(u => new
    //         {
    //             u.Id,
    //             u.Username,
    //             u.Name,
    //             u.Email,
    //             u.Active,
    //             u.CreatedAt,
    //             Roles = u.UserRoleMaps!.Select(urm => new
    //                 {
    //                     urm.Role!.RoleId,
    //                     Name = urm.Role.Name == "Level 4 User" ? "Level 1 User" : urm.Role.Name,
    //                     urm.Role.EntityRole,
    //                 })
    //                 .ToList(),
    //         })
    //         .Cast<object>()
    //         .ToListAsync();
    // }

    // public async Task<List<object>> GetFacilityUserListAsync(IdType? FacilityId)
    // {
    //     if (FacilityId == null)
    //         throw new Exception("FacilityId is required");

    //     await using var context = await _contextFactory.CreateDbContextAsync();

    //     var facilityExists = await context.Facilities.AnyAsync(f => f.Id == FacilityId);
    //     if (!facilityExists)
    //         throw new Exception("Facility not found");

    //     IQueryable<User> query = context
    //         .UserFacilityMaps.Where(u => u.FacilityId == FacilityId.Value)
    //         .Select(ufm => ufm.User);

    //     return await query
    //         .Select(u => new
    //         {
    //             u.Id,
    //             u.Username,
    //             u.Name,
    //             u.Email,
    //             u.Active,

    //             Roles = u.UserRoleMaps!.Select(urm => new
    //                 {
    //                     urm.Role!.RoleId,
    //                     urm.Role.Name,
    //                     urm.Role.RoleType,
    //                 })
    //                 .ToList(),
    //             u.CreatedAt,
    //         })
    //         .Cast<object>()
    //         .ToListAsync();
    // }

    private List<Dictionary<string, object>> ConvertToKeyValue(List<List<object>> rows)
    {
        var list = new List<Dictionary<string, object>>();

        foreach (var row in rows)
        {
            var dict = new Dictionary<string, object>
            {
                { "Id", row[0] },
                { "Username", row[1] },
                { "Email", row[2] },
            };

            list.Add(dict);
        }

        return list;
    }

    // public async Task<List<User>> GetAllUserAdminAsync()
    // {
    //     await using var context = await _contextFactory.CreateDbContextAsync();
    //     var users = await context
    //         .Users.Include(c => c.Roles)
    //         .Include(c => c.Entities)
    //         .ToListAsync();
    //     return users;
    // }

    public async Task<User> ActiveOrInactiveUserAsync(IdType id, bool? Active)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            throw new Exception("User not found");

        user.Active = Active;
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<bool> SoftDeleteUserAsync(IdType id, IdType userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context
            .Set<User>()
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

        if (user == null)
            return false;

        var now = DateTimeOffset.UtcNow;

        user.DeletedAt = now;
        user.DeletedBy = userId;
        user.UpdatedAt = now;
        user.UpdatedBy = userId;
        user.Active = false;
        await context.SaveChangesAsync();

        return true;
    }
}
