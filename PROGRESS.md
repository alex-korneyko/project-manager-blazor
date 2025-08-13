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

## Iteration 3 — Projects UI & Invitations
**Date:** 2025-08-12 (EEST)  
**PR:** #2 (iter-3-projects-ui → main)

### Goals
- Implement the basic UI for working with projects:
   - “My Projects” page (shows projects where the user is the owner or a member).
   - Project details page with a **Members** tab (visible to the owner only).
- Add server-side logic to invite users by email/username:
   - Enforce project owner privileges.
   - Validate user existence and prevent duplicates.
   - Add/remove project members.

### UI
- `Pages/Projects.razor`
   - Display the list of visible projects via `IProjectAccessService.GetVisibleProjectsAsync`.
   - Provide a form to create a new project (Name, Description).
   - Navigate to the project details view.
- `Pages/ProjectDetails.razor`
   - Load the project with Owner and Members.
   - Access checks: `IsProjectMember` to view; `IsProjectOwner` for the **Members** tab.
   - Invitation form by email/username.
   - Render members list; allow removing a member (owner only).
- Navigation
   - Add a “Projects” menu item → `/projects`.

### server logic
- Reuse resource-based policies from Iteration 2:
   - `IsProjectMember` to open a project.
   - `IsProjectOwner` to invite/remove members.
- Work with users through `UserManager<ApplicationUser>`:
   - Look up users by email/username.
- Validations & responses:
   - 404/Forbidden when the project is not visible to the current user.
   - Invitation errors: “user not found”, “already a member”, “not authorized”.
- Data access: use `ApplicationDbContext` directly in components for now; later we may extract to a dedicated `ProjectService`.

### Acceptance
- The user sees a list of projects they own or participate in.
- The owner can:
   - Invite an existing user.
   - Remove a member.
- Non-owners cannot see the **Members** tab or perform related operations.
- Unauthorized users cannot access `/projects/{id}` for projects they do not belong to.
- All checks use `IAuthorizationService` with resource instances.

### Manual test scenarios
1. User A creates a project and sees it under “My Projects”.
2. User B does not see A’s project until invited.
3. Owner (A) opens project details → **Members** tab is visible.
4. A invites B by email → B now sees the project.
5. A removes B → B loses access.
6. Any non-member visiting `/projects/{id}` gets “not found/forbidden”.

### Implementation notes
- In Blazor Server, it’s acceptable to perform operations directly in components at this stage (no separate API required).
- Add brief success/error messages (alerts/toasts) for UX polish.
- Later, move the logic to services and add integration tests.

### Next (after Iteration 3)
- **Iteration 4 — Tasks CRUD & Statuses**
   - CRUD for tasks within a project; statuses (`Backlog`, `InProgress`, `Blocked`, `Done`).
   - Access rules: members can view/create; editing/deleting by task author or project owner.
- Prepare for Iteration 5 — task attachments/images (upload + access checks).


## Iteration 4 — Tasks CRUD & Statuses
**Date:** 2025-08-13 (EEST)  
**PR:** #3 (`iter-4-tasks` → `main`)

### What’s done
- **Security**
    - Added `TaskModifyRequirement` + `TaskModifyHandler` (resource-based): a task can be **edited/deleted** by its **author** or the **project owner**.
    - Status changes allowed to **any project member** via existing `IsProjectMember` policy (resource = `TaskItem`).

- **UI (Project Details page)**
    - **Create Task** form (Title, optional Markdown description, initial Status).
    - Task list with author/email, created time, **status dropdown**, and conditional **Edit/Delete** actions.
    - Inline edit form (save/cancel) for title/description.
    - Basic button-disable UX to avoid accidental submits.

- **Data flow**
    - On create: set `AuthorId`, `CreatedAtUtc`, bind `ProjectId`.
    - Reload tasks after create/update/delete; order by `CreatedAtUtc` (desc).

### Access rules (recap)
- **Create / Change status** → `IsProjectMember` (any member).
- **Update / Delete** → `CanModifyTask` (task author **or** project owner).

### Manual test checklist
1. Owner (A) creates several tasks; list shows newest first.
2. Member (B) after invitation can create tasks & change statuses, but **cannot** edit/delete A’s tasks.
3. Owner (A) can edit/delete **any** project task.
4. Non-members cannot access `/projects/{id}` nor see tasks.
5. Validation: title is required (min length), action buttons disabled until valid.

### Notes / small follow-ups
- Ensure policy & DI are wired in `Program.cs`:
    - `options.AddPolicy("CanModifyTask", p => p.Requirements.Add(new TaskModifyRequirement()));`
    - `services.AddScoped<IAuthorizationHandler, TaskModifyHandler>();`
- Keep UI language consistent (EN or RU) and remove duplicated labels.
- Consider DB index `Tasks(ProjectId, CreatedAtUtc)` to speed up listings.
- Next UX steps: Kanban drag & drop; Markdown preview; toast notifications.

### Next
**Iteration 5 — Task Attachments & Images**
- File upload per task with access checks.
- Inline images in descriptions + simple preview/gallery.
- Deletion & cleanup rules (task author / project owner).
