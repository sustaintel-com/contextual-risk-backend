using CR.Model.Users;
using CR.Service.Dtos.User;
using Microsoft.AspNetCore.Mvc;

namespace CR.Service.Users;

public interface IUserService
{
    Task<object> AddOrUpdateUserAsync(
        CreateUserDTO userDto,
        string? createdBy,
        IdType? createdByID
    );
    Task<User?> GetUserDetails(IdType id);
    Task<List<UserDto>> GetAllUsersAsync();

    // Task<IActionResult> UpdatePasswordAsync(
    //     PasswordResetDto dto,
    //     string? updatedBy,
    //     IdType? updatedByID,
    //     string? Email
    // );

    // Task<IActionResult> UpdatePasswordAdminAsync(
    //     PasswordResetDto dto,
    //     string? updatedBy,
    //     IdType? updatedByID,
    //     string? Email
    // );

    // Task<List<object>> GetFacilityUserListAsync(IdType? FacilityId);
    //Task<List<object>> GetEntityUserListAsync(IdType? EntityId);
    // Task<List<EntityUserListDto>> GetEntityUserListAsync(IdType? entityId);
    // Task<List<User>> GetAllUserAdminAsync();
    Task<User> ActiveOrInactiveUserAsync(IdType id, bool? Active);

    // Task<string?> ValidateUserAsync(UserCreateOrUpdateDto userDto);
    Task<bool> SoftDeleteUserAsync(IdType id, IdType userId);
}
