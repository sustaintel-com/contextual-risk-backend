using CR.Service.Dtos.User;

namespace CR.Service.Roles;

public interface IRoleService
{
    Task<List<RoleDTO>> GetAllRolesAsync();
}
