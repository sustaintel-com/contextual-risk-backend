namespace CR.Service.Dtos.User;

public record CreateUserDTO(
    IdType? Id,
    string Username,
    string? FullName,
    string Email,
    string? Password,
    bool? Active,
    List<IdType>? RoleId,
    List<UserRoleSelectionDto>? Roles,
    DateTimeOffset? CreatedAt,
    IdType? CreatedBy,
    IdType? UpdatedBy,
    DateTimeOffset? UpdatedAt
);

public record UserRoleSelectionDto(IdType? RoleId, string? Name);
