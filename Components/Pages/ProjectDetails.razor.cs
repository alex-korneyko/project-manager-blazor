using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Common.Security;
using ProjectManager.Components.Common;
using ProjectManager.Components.UiModels;
using ProjectManager.Data;
using ProjectManager.Data.Models;
using ProjectManager.Domain.Entities;
using static ProjectManager.Authorization.AuthorizationPoliciesNames;
using TaskStatus = ProjectManager.Domain.Entities.TaskStatus;

namespace ProjectManager.Components.Pages;

public partial class ProjectDetails : ComponentBase
{
    private Project? _project;
    private bool _loading = true;
    private bool _isOwner;
    private string? _ownerEmail;
    private List<ProjectMember> _members = new();
    private string _inviteEmail = "";
    private string? _inviteError;
    private string? _inviteOk;
    private string? _currentUserId = "";
    private List<TaskItem> _tasks = new();
    private Dictionary<string,string> _usersById = new(); // для отображения email автора

    private TaskEditModel _newTask = new();
    private TaskEditModel _editTask = new();
    private Guid? _editId;
    private bool _creating;
    private string? _taskError;
    private bool _createTaskButtonDisabled = true;
    private bool _inviteButtonDisabled = true;

    private TaskKanban? _kanban;

    [Parameter] public Guid ProjectId { get; set; }

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = null!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = null!;
    [Inject] public IDbContextFactory<ApplicationDbContext> DbContextFactory { get; set; } = null!;
    [Inject] private ILogger<ProjectDetails> Logger { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;

        var dbContext = await DbContextFactory.CreateDbContextAsync();

        var project = await dbContext.Projects
            .Include(prj => prj.Members)
            .ThenInclude(member => member.User)
            .Include(prj => prj.Owner)
            .FirstOrDefaultAsync(prj => prj.Id == ProjectId);

        if (project is null)
        {
            _project = null;
            _loading = false;
            return;
        }

        var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;

        _currentUserId = user.GetUserId();

        var memberResult = await AuthorizationService.AuthorizeAsync(user, project, IsProjectMember);
        if (!memberResult.Succeeded)
        {
            _project = null;
            _loading = false;
            return;
        }

        _project = project;
        _ownerEmail = project.Owner.Email;
        _members = project.Members.OrderBy(member => member.User.Email).ToList();

        var ownerResult = await AuthorizationService.AuthorizeAsync(user, project, IsProjectOwner);
        _isOwner = ownerResult.Succeeded;

        await LoadTasksAsync();

        _loading = false;
    }

    private async Task Invite()
    {
        _inviteError = _inviteOk = null;

        try
        {
            if (_project is null) return;

            var dbContext = await DbContextFactory.CreateDbContextAsync();

            // Ещё раз проверим право владельца
            var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
            var ownerCheck = await AuthorizationService.AuthorizeAsync(user, _project, IsProjectOwner);
            if (!ownerCheck.Succeeded) { _inviteError = "You do not have rights"; return; }

            var email = _inviteEmail.Trim();
            if (string.IsNullOrWhiteSpace(email)) { _inviteError = "Enter Email"; return; }

            // Найдём пользователя
            var target = await UserManager.FindByEmailAsync(email);
            if (target is null) { _inviteError = "User with entered Email not found."; return; }

            // Проверим дубликаты
            var exists = await dbContext.ProjectMembers.AnyAsync(m => m.ProjectId == _project.Id && m.UserId == target.Id);
            if (exists) { _inviteError = "User already added"; return; }

            dbContext.ProjectMembers.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = _project.Id,
                UserId = target.Id,
                Role = "Member"
            });
            await dbContext.SaveChangesAsync();

            // Refresh list
            await OnParametersSetAsync();
            _inviteEmail = "";
            _inviteOk = "Member invited.";
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to invite user");
            _inviteError = "Failed to invite user";
        }
    }

    private async Task RemoveMember(Guid projectMemberId)
    {
        _inviteError = _inviteOk = null;

        try
        {
            if (_project is null) return;

            var dbContext = await DbContextFactory.CreateDbContextAsync();

            var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
            var ownerCheck = await AuthorizationService.AuthorizeAsync(user, _project, IsProjectOwner);
            if (!ownerCheck.Succeeded) { _inviteError = "No rights to delete."; return; }

            var pm = await dbContext.ProjectMembers.FirstOrDefaultAsync(x => x.Id == projectMemberId && x.ProjectId == _project.Id);
            if (pm is null) { _inviteError = "Member not found."; return; }

            dbContext.ProjectMembers.Remove(pm);
            await dbContext.SaveChangesAsync();

            await OnParametersSetAsync();
            _inviteOk = "Member removed.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Remove member failed");
            _inviteError = "Remove member failed.";
        }
    }

    private async Task LoadTasksAsync()
    {
        var dbContext = await DbContextFactory.CreateDbContextAsync();

        _tasks = await dbContext.Tasks
            .Where(t => t.ProjectId == ProjectId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync();

        var authorIds = _tasks.Select(t => t.AuthorId).Distinct().ToList();
        var users = await dbContext.Users
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();
        _usersById = users.ToDictionary(x => x.Id, x => x.Email ?? x.Id);
    }

    private async Task CreateTaskAsync()
    {
        _taskError = null;
        _creating = true;
        try
        {
            if (_project is null) return;

            var dbContext = await DbContextFactory.CreateDbContextAsync();
            var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
            var memberResult = await AuthorizationService.AuthorizeAsync(user, _project, IsProjectMember);
            if (!memberResult.Succeeded) { _taskError = "No rights to task create."; return; }

            if (_currentUserId == null)
            {
                Logger.LogWarning("CreateTask failed. CurrentUserId is null.");
                _taskError = "Failed to create task. CurrentUserId is null.";
                return;
            }

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = _project.Id,
                Title = _newTask.Title.Trim(),
                DescriptionMarkdown = string.IsNullOrWhiteSpace(_newTask.DescriptionMarkdown) ? null : _newTask.DescriptionMarkdown!.Trim(),
                Status = _newTask.Status,
                AuthorId = _currentUserId,
                CreatedAtUtc = DateTime.UtcNow
            };
            dbContext.Tasks.Add(task);
            await dbContext.SaveChangesAsync();

            _newTask = new(); // reset
            await LoadTasksAsync();

            if (_kanban is not null)
            {
                await _kanban.ReloadAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CreateTask failed");
            _taskError = "Failed to create task.";
        }
        finally { _creating = false; }
    }

    private void BeginEdit(TaskItem t)
    {
        _editId = t.Id;
        _editTask = new TaskEditModel
        {
            Title = t.Title,
            DescriptionMarkdown = t.DescriptionMarkdown,
            Status = t.Status
        };
    }

    private void CancelEdit()
    {
        _editId = null;
        _editTask = new TaskEditModel();
    }

    private async Task UpdateTaskAsync(TaskItem t)
    {
        try
        {
            var dbContext = await DbContextFactory.CreateDbContextAsync();
            var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
            var canModify = await AuthorizationService.AuthorizeAsync(user, t, CanTaskModify);
            if (!canModify.Succeeded) { _taskError = "No rights to task update."; return; }

            t.Title = _editTask.Title.Trim();
            t.DescriptionMarkdown = string.IsNullOrWhiteSpace(_editTask.DescriptionMarkdown) ? null : _editTask.DescriptionMarkdown!.Trim();
            await dbContext.SaveChangesAsync();

            _editId = null;
            _taskError = null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UpdateTask failed");
            _taskError = "Failed to update task.";
        }
    }

    private async Task DeleteTaskAsync(TaskItem t)
    {
        try
        {
            var dbContext = await DbContextFactory.CreateDbContextAsync();
            var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
            var canModify = await AuthorizationService.AuthorizeAsync(user, t, CanTaskModify);
            if (!canModify.Succeeded) { _taskError = "No rights to task delete."; return; }

            dbContext.Tasks.Remove(t);
            await dbContext.SaveChangesAsync();

            _tasks.RemoveAll(x => x.Id == t.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DeleteTask failed");
            _taskError = "Failed to delete task.";
        }
    }

    private async Task ChangeStatusAsync(TaskItem t, TaskStatus newStatus)
    {
        try
        {
            var dbContext = await DbContextFactory.CreateDbContextAsync();
            var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
            var isMember = await AuthorizationService.AuthorizeAsync(user, t, IsProjectMember);
            if (!isMember.Succeeded) { _taskError = "No rights to change status."; return; }

            t.Status = newStatus;
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ChangeStatus failed");
            _taskError = "ChangeStatus failed.";
        }
    }

    private void InviteEmailChanged(ChangeEventArgs args)
    {
        _inviteEmail = args.Value?.ToString() ?? "";

        var emailAddressAttribute = new EmailAddressAttribute();
        var result = emailAddressAttribute.IsValid(_inviteEmail);
        _inviteButtonDisabled = !result;
    }

    private sealed class TaskEditModel
    {
        [Required, MinLength(2)]
        public string Title { get; set; } = string.Empty;
        public string? DescriptionMarkdown { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Backlog;
    }

    private void TaskTitleChanged(ChangeEventArgs args)
    {
        _createTaskButtonDisabled = args.Value?.ToString() is null || ((string)args.Value).Length < 2;
    }
}
