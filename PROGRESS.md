Project Manager (Blazor) — Progress Summary
Date: 2025-08-11 12:24 EEST

Repository: https://github.com/alex-korneyko/project-manager-blazor
Key commits:
- 3495d1d25bb2b9e8fdd163ac8c32e246baec832b — Bootstrap: Blazor Web App (.NET 8, Interactive Server) + ASP.NET Core Identity (Individual Accounts), SQL Server provider, roles seeding (Admin/User), auth cookies, default token providers. Template-required identity endpoints wired.
- 5e78da8898515c277cc5fd4d23e8cfbfbb179913 — Domain model & EF Core configuration added; SQL Server “multiple cascade paths” fixed by setting Restrict/NoAction on self-referential FK (TaskComment.ParentCommentId). Migration applied successfully.

Stack:
- .NET 8, Blazor Web App (Interactive Server)
- ASP.NET Core Identity + Roles (Admin, User)
- EF Core 8 + SQL Server
- Razor Components with Additional Identity Endpoints

What’s implemented:
1) Bootstrap (Iteration 0)
    - SQL Server configured via UseSqlServer and connection string (user-secrets recommended).
    - IdentityCore with roles, cookies, default token providers.
    - Role seeding and admin user (admin@pm.local) created on startup.
    - InitialIdentitySqlServer migration applied.

2) Domain (Iteration 1)
    - Entities: Project, ProjectMember, TaskItem, TaskAttachment, TaskComment, enum TaskStatus.
    - ApplicationDbContext DbSets added.
    - Relationships & indexes:
      • Project.Owner → ApplicationUser: DeleteBehavior.Restrict
      • ProjectMember unique index on (ProjectId, UserId)
      • TaskItem → Project: Cascade
      • TaskAttachment → TaskItem: Cascade
      • TaskComment → TaskItem: Cascade
      • TaskComment self-reference (ParentCommentId): Restrict/NoAction (to avoid multiple cascade paths in SQL Server)
      • Author/Uploader FKs to ApplicationUser: Restrict
    - DomainInit migration created and applied.

Design decisions:
- Keep user-related FKs as Restrict so deleting a user doesn’t cascade content.
- Allow cascades for Project/Task/Attachment/Comment to clean up owned data.
- For deleting a comment that has replies: do not use DB cascade on the self-FK; instead delete the whole branch programmatically (e.g., via a recursive CTE), ensuring the entire thread is removed atomically.

Suggested service (planned):
- CommentService.DeleteBranchAsync(rootCommentId): executes a recursive CTE to delete the root comment and all descendants in one statement.

Next steps:
Iteration 2 — Authorization (resource-based)
- Policies/requirements: IsProjectMember, IsProjectOwner, IsCommentAuthor.
- Authorization handlers querying the DbContext to validate access.
- A query/service method to list projects visible to the current user (owner or member).

Iteration 3 — Projects UI & invitations
- CRUD for Projects.
- Owner-only invitation of existing users as ProjectMember.
- Project list shows only owned/joined projects.

Iteration 4 — Tasks
- CRUD within a project, status changes, visibility restricted to project members.
- Author/owner rules for updates/deletes.

Notes:
- Consider adding PROGRESS.md to the repository and copying this summary there; keep a running log of iterations, decisions, and links to commits.
- Ensure any leftover SQLite artifacts are removed and ignored (if present) now that SQL Server is the target.

## Iteration 2 — Authorization (resource-based)
**Date:** 2025-08-11 19:10 EEST
**PR:** #1 (iter-2-auth → main)

### What was added
- Policies (requirements): `IsProjectMember`, `IsProjectOwner`, `IsCommentAuthor`.
- Authorization handlers (resource-based) for `Project`, `TaskItem`, `TaskComment`; registered in DI as *Scoped*.
- `IProjectAccessService` + `ProjectAccessService`:
   - `IsProjectMemberAsync`, `IsProjectOwnerAsync`, `IsCommentAuthorAsync`
   - `GetProjectIfVisibleAsync`, `GetVisibleProjectsAsync`
- Program wiring: policies registered, handlers and access service added to DI, minimal API example for `/api/projects/{projectId}/invite` protected by `IsProjectOwner`.

### Notes / decisions
- Member/owner checks are always evaluated **per project** (no cross-project leakage).
- Comment editing/deletion is limited to the **author**; this will be used in UI and endpoints.
- Handlers are resource-based so they can be used both in Minimal API and Blazor components via `IAuthorizationService`.

### Small follow-ups (nice-to-have)
- In `Program.cs` there are two consecutive `AddAuthorization(...)` calls; one can be removed (keep the overload with options).
- In `ProjectAccessService` prefer comparing `OwnerId`/`AuthorId` fields (simple columns) instead of joining via navigation properties — a micro-optimization for generated SQL.
- Remove unused `using` in `CommentAuthorHandler` if present.

### Self-checks performed
- Verified that member check for tasks filters by **current** project ID.
- Confirmed handlers are registered as Scoped and invoked via `IAuthorizationService`.

### Next
**Iteration 3 — Projects UI & invitations**
- Projects list (only owned/joined).
- Project details with **Members** tab (owner-only) and invite form.
- Implement `/api/projects/{id}/invite`: validate user existence, prevent duplicates (409), add member.
