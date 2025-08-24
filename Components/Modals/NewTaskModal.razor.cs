using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Components.UiModels;
using ProjectManager.Data;
using ProjectManager.Data.Models;
using ProjectManager.Domain.Entities;
using static ProjectManager.Authorization.AuthorizationPoliciesNames;
using TaskStatus = ProjectManager.Domain.Entities.TaskStatus;

namespace ProjectManager.Components.Modals;

public partial class NewTaskModal : ComponentBase, IModal<TaskStatus, TaskItem>
{
    private readonly TaskEditModel _modalModel = new();
    private bool _showModal;
    private TaskStatus _modalStatus;
    private string? _modalError;
    private bool _submitting;

    [Inject] public IDbContextFactory<ApplicationDbContext> DbContextFactory { get; set; } = null!;
    [Inject] private IAuthorizationService Authz { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = null!;
    [Inject] private ILogger<NewTaskModal> Log { get; set; } = null!;

    [Parameter] public EventCallback<TaskItem> OnModalActionSucceeded { get; set; }
    [Parameter] public EventCallback<string> OnModalActionFailed { get; set; }
    [Parameter] public Project? Project { get; set; }

    public async Task OpenModalAsync(TaskStatus taskStatus)
    {
        _showModal = true;
        _modalStatus = taskStatus;
    }

    public void CloseModal()
    {
        _showModal = false;
    }

    private async Task CreateTaskInModalAsync()
    {
        if (string.IsNullOrWhiteSpace(_modalModel.Title)) return;

        try
        {
            _modalError = null;
            if (Project is null)
            {
                _modalError = "Project not found";
                Log.LogError("Project not found");
                await OnModalActionFailed.InvokeAsync("Project not found");
                return;
            }

            _submitting = true;

            var user = (await AuthState.GetAuthenticationStateAsync()).User;

            var memberResult = await Authz.AuthorizeAsync(user, Project, IsProjectMember);
            if (!memberResult.Succeeded)
            {
                _modalError = "No rights for task creation.";
                return;
            }

            var applicationUser = await UserManager.GetUserAsync(user);
            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = Project.Id,
                Title = _modalModel.Title.Trim(),
                DescriptionMarkdown = string.IsNullOrWhiteSpace(_modalModel.DescriptionMarkdown)
                    ? null
                    : _modalModel.DescriptionMarkdown!.Trim(),
                Status = _modalStatus,
                AuthorId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            var dbContext = await DbContextFactory.CreateDbContextAsync();
            dbContext.Tasks.Add(task);
            await dbContext.SaveChangesAsync();

            task.Author = applicationUser;

            await OnModalActionSucceeded.InvokeAsync(task);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Create task modal failed");
            _modalError = "Create task modal failed";
            await OnModalActionFailed.InvokeAsync("Create task modal failed");
        }
        finally
        {
            _submitting = false;
        }
    }
}
