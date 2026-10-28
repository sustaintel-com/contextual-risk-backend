
using CR.Model.Roles;

namespace CR.Model.Users
{
    public class User : BaseModel
    {
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool? Active { get; set; }

        // public bool IsActive { get; set; } = true;

        // Many-to-many relationship with Role
        public ICollection<UserRoleMap>? UserRoleMaps { get; set; } = default!;
        public ICollection<Role>? Roles { get; set; } = default!;
    }
}