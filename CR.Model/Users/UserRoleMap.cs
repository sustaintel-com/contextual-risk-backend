using CR.Model.Roles;

namespace CR.Model.Users;

public class UserRoleMap
{
    public IdType UserId { get; set; }
    public IdType RoleId { get; set; }
    public User User { get; set; } = default!;
    public Role Role { get; set; } = default!;
}
