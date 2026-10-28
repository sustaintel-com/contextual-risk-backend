namespace CR.Service.Dtos.User
{
    public class UserDto
    {
        public IdType Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string Email { get; set; } = string.Empty;

        public bool? Active { get; set; }

        public List<IdType> RoleIds { get; set; } = new();

        public List<string> RoleNames { get; set; } = new();

        public IdType? CreatedBy { get; set; }

        public IdType? UpdatedBy { get; set; }
    }
}
