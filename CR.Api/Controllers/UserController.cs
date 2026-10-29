using CR.Model.Data;
using CR.Model.Users;
using CR.Service.Dtos.User;
using CR.Service.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CR.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(IUserService UserService, IDbContextFactory<CRContext> contextFactory)
    : BaseController(contextFactory)
{
    private readonly IUserService _userService = UserService;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserDetails(IdType id)
    {
        var c = await _userService.GetUserDetails(id);
        if (c == null)
        {
            return BadRequest(new { Message = "User not found" });
        }
        return Ok(c);
    }

    [Authorize]
    [HttpPost("signup")]
    public async Task<IActionResult> CreateOrUpdate(CreateUserDTO userDto)
    {
        if (string.IsNullOrWhiteSpace(userDto.FullName))
            return BadRequest(new { Message = "User full name required" });

        if (string.IsNullOrWhiteSpace(userDto.Username))
            return BadRequest(new { Message = "User username required" });

        if (string.IsNullOrWhiteSpace(userDto.Email))
            return BadRequest(new { Message = "User email required" });

        if (string.IsNullOrWhiteSpace(userDto.Password))
            return BadRequest(new { Message = "User password required" });

        var currentUser = await GetCurrentUserAsync();

        var result = await _userService.AddOrUpdateUserAsync(
            userDto,
            currentUser.Username,
            currentUser.Id
        );

        return Ok(result);
    }

    // [Authorize]
    // [HttpPost("saveNewPassword")]
    // public async Task<IActionResult> SaveResetPassword([FromBody] PasswordResetDto resetDto)
    // {
    //     var currentUser = await GetCurrentUserAsync();

    //     var resetpassword = await _userService.UpdatePasswordAsync(
    //         resetDto,
    //         currentUser.Username,
    //         currentUser.Id,
    //         currentUser.Email
    //     );
    //     return Ok(resetpassword);
    // }

    // [Authorize]
    // [HttpPost("saveNewPasswordAdmin")]
    // public async Task<IActionResult> SaveResetPasswordAdmin([FromBody] PasswordResetDto resetDto)
    // {
    //     var currentUser = await GetCurrentUserAsync();
    //     var resetpasswordAdmin = await _userService.UpdatePasswordAdminAsync(
    //         resetDto,
    //         currentUser.Username,
    //         currentUser.Id,
    //         currentUser.Email
    //     );
    //     return Ok(resetpasswordAdmin);
    // }

    [HttpPut("{Id}/status")]
    public async Task<IActionResult> ActiveOrInactiveUser(
        IdType Id,
        [FromBody] ActiveOrInactiveDto dto
    )
    {
        var result = await _userService.ActiveOrInactiveUserAsync(Id, dto.Active);
        if (result == null)
            return NotFound(new { Message = "User not found" });

        result.Active = dto.Active;

        return Ok(new { result.Id, Message = "User active or inactive successfully" });
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDeleteUser(Guid id)
    {
        var user = await GetCurrentUserAsync();
        var result = await _userService.SoftDeleteUserAsync(id, user.Id);

        if (!result)
        {
            return NotFound(new { Message = "User not found" });
        }

        return Ok(new { Message = "User deleted successfully" });
    }
}
