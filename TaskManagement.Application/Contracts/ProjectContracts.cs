namespace TaskManagement.Application.Contracts;

public sealed record CreateProjectRequest(string Name, string? Description);

public sealed record UpdateProjectRequest(string Name, string? Description);

public sealed record ProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record AddProjectMemberRequest(Guid UserId);

public sealed record ProjectMemberResponse(
    Guid UserId,
    DateTimeOffset JoinedAtUtc);
