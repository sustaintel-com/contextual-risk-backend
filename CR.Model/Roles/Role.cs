using CR.Model.Users;

namespace CR.Model.Roles
{
    public class Role
    {
        public IdType RoleId { get; set; }
        public string? Name { get; set; }
        // Many-to-many relationship with Permission
        public ICollection<UserRoleMap>? UserRoleMaps { get; set; } = default!;
        public ICollection<User>? Users { get; set; } = default!;
    }
}