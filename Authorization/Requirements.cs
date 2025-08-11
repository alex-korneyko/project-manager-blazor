using Microsoft.AspNetCore.Authorization;

namespace ProjectManager.Authorization;

public sealed record ProjectMemberRequirement : IAuthorizationRequirement;
public sealed record ProjectOwnerRequirement : IAuthorizationRequirement;
public sealed record CommentAuthorRequirement : IAuthorizationRequirement;
