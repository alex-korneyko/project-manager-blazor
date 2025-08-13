# Project Manager (Blazor) — Learning Plan & Progress

**Updated:** 2025-08-13 20:22 (EEST)  
**Stack:** ASP.NET Core **Blazor Server**, EF Core, SQL Server, ASP.NET Core Identity

---

## Goals
- Learn **Blazor** and modern ASP.NET Core patterns through a realistic training app.
- Build a **Project Manager** that supports: projects, tasks with statuses, rich descriptions with images, file attachments, threaded comments, authN/authZ, and role-based access.
- Emphasize clean domain model, resource-based authorization, and pragmatic UI (forms + Kanban).

---

## High‑Level Plan
1. **Iteration 0 — Setup & Domain skeleton**
2. **Iteration 1 — Comments model & migrations**
3. **Iteration 2 — Authentication, Authorization, Roles, Project membership**
4. **Iteration 3 — Projects CRUD & Access restrictions**
5. **Iteration 4 — Tasks CRUD, Statuses, Mini‑Kanban + modal create**
6. **Iteration 5 — Task attachments & image rendering (planned)**
7. **Iteration 6 — Threaded comments UI (planned)**
8. **Iteration 7 — Cleanups (validation, UX polish, toasts, resources), indexes (planned)**
9. **Iteration 8 — Real‑time updates via SignalR (planned)**

> We maintain per‑iteration summaries below with PR links and checklists.

---

## Iteration 0 — Setup & Domain skeleton
**What we did**
- Created solution and a Blazor Server app.
- Connected **SQL Server**; configured **EF Core** and first migrations.
- Bootstrapped **Domain**: Project, TaskItem, TaskStatus enum, TaskComment (self‑reference).

**Notes**
- Seeded initial infrastructure and `ApplicationDbContext`.
- Verified that the app runs and DB connection string works.

---

## Iteration 1 — Comments model & migrations
**What we did**
- Implemented **self‑referenced** `TaskComment` for threaded discussions.
- Fixed migration failure caused by **multiple cascade paths** on `TaskComments` → set **Restrict** on `ParentCommentId` to avoid cycles; handled deletion behavior in code.

**Why it matters**
- Enables hierarchical comment trees while keeping referential integrity predictable.

---

## Iteration 2 — Authentication, Authorization, Roles, Project membership
**What we did**
- Integrated **ASP.NET Core Identity** for registration & login.
- Defined **roles** and **policies**; added **resource‑based** checks for project access:
    - Only **project owner** can invite users.
    - Users can see **only** their own projects or those they are invited to.
- UI: login/register flows; project membership invite by email; basic guards for pages.

**Manual checks**
- Anonymous users can’t see Projects.
- Member can access project; non‑member → 403/redirect to login.

---

## Iteration 3 — Projects CRUD & Access restrictions
**What we did**
- Project list + details, create/delete; invite flow integrated in details page.
- Navigation/menu respects authentication (no “Projects” link for guests).
- Enforced **owner‑only** operations and **member‑only** visibility.

**Notes**
- Consistent use of `IAuthorizationService` in pages for server‑side guards.
- Clean separation between project owner vs members.

---

## Iteration 4 — Tasks CRUD, Statuses, Mini‑Kanban + modal create
**What we did**
- **Security**
    - Added **resource‑based** requirement **`TaskModifyRequirement`** + handler (**author or project owner** may edit/delete).
    - Status change allowed to **any project member** (policy `IsProjectMember`).

- **UI**
    - On **Project Details**: task list → enhanced with **mini‑Kanban** (columns: Backlog, InProgress, Blocked, Done).
    - Cards are standalone component **TaskKanbanCard**.
    - **Drag & Drop** (HTML5) changes `Status` with optimistic UI update + DB save.
    - **“+” button in each column** opens **modal** to create a task directly in that column.
    - Validation: Title required; buttons disabled while submitting.

- **Data flow**
    - On create: set `AuthorId`, `CreatedAtUtc`, `ProjectId`, initial `Status` from column.

**PRs**
- Iteration 4 Tasks CRUD & Statuses: **PR #3**
- Mini‑Kanban & modal create: **PR #4**

**Manual test checklist**
1. Owner creates tasks; list/kanban show newest first in each column.
2. Member can create tasks and **change statuses**; **cannot edit/delete** others’ tasks.
3. Owner can edit/delete any task in the project.
4. Non‑members cannot access project routes.
5. Modal create places the task in the selected column instantly.

**Follow‑ups**
- Keep UI language consistent (RU/EN) and remove duplicates.
- Consider DB index `Tasks(ProjectId, CreatedAtUtc)`.
- Later: markdown preview, toasts, drag animations.

---

## Next — Iteration 5 (Planned): Attachments & Images
**Scope**
- **File uploads** for tasks (per‑task storage + entity).
- **Image embedding** in markdown and inline gallery/preview.
- Access rules: only project members may view; delete by **task author** or **project owner**.
- Storage choices: local `wwwroot/uploads` vs cloud later; antivirus/size limits.
- Cleanup: delete files when task is removed (or mark orphaned).

**Milestones**
- DB: `TaskAttachment` (Id, TaskId FK, FileName, ContentType, Size, StoragePath/BlobKey, CreatedAtUtc, UploaderId).
- Services: `IFileStorage` abstraction; local provider.
- UI: upload control, list, download/delete actions; image preview lightbox.

---

## Later Iterations (planned)
- **Iteration 6 — Threaded comments UI**: nested display, reply, delete subtree by author, edit own comment.
- **Iteration 7 — UX polish & i18n**: validation messages, resource files, toasts, empty‑states; indexes & minor perf.
- **Iteration 8 — Real‑time**: SignalR updates for tasks/kanban/comments; optimistic concurrency notes.

---

## Running Locally (short)
1. Set SQL Server connection string in `appsettings.Development.json`.
2. `dotnet ef database update` (apply migrations).
3. Run Blazor Server app; register first user (owner).
4. Create project; invite a second user; test task flows & Kanban.

---

## Glossary
- **Owner**: user who created the project; may invite members, edit/delete any task and project.
- **Member**: user invited into a project; may create tasks and change statuses.
- **Task author**: user who created a task; may edit/delete *own* tasks.

---

## Changelog (highlights)
- **It‑0**: skeleton, EF Core setup (SQL Server).
- **It‑1**: threaded comments model + migration fix (Restrict self‑FK).
- **It‑2**: Identity, roles, resource‑based auth; project membership/invites.
- **It‑3**: Projects CRUD, protected navigation.
- **It‑4**: Tasks CRUD & statuses; **Kanban** with DnD + **modal** create.
