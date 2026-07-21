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

// Membership is stored as a bare user id, but a list of GUIDs is unusable in a UI.
// The safe public summary (no e-mail, no roles) is joined in ProjectService, the
// same way comment authors and attachment uploaders are resolved. User is null
// only when the account behind a membership row no longer exists.
public sealed record ProjectMemberResponse(
    Guid UserId,
    UserSummaryResponse? User,
    DateTimeOffset JoinedAtUtc);
