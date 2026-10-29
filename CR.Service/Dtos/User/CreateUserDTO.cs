namespace CR.Service.Dtos.User;

public record CreateUserDTO(
    IdType? Id,
    string Username,
    string? FullName,
    string Email,
    string? Password,
    bool? Active,
    List<IdType>? RoleId,
    List<RoleDTO>? Roles,
    DateTimeOffset? CreatedAt,
    IdType? CreatedBy,
    IdType? UpdatedBy,
    DateTimeOffset? UpdatedAt
);
