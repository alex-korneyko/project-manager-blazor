using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ProjectManager.Components.UiModels;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;
using TaskStatus = ProjectManager.Domain.Entities.TaskStatus;

namespace ProjectManager.Components.Modals;

public partial class NewTaskModal : ComponentBase, IModal<TaskItem>
{
    private readonly TaskEditModel _modalModel = new();
    private bool _showModal;
    private TaskStatus _modalStatus;
    private string? _modalError;
    private bool _submitting;

    [Inject] private ApplicationDbContext Db { get; set; } = null!;
    [Inject] private IAuthorizationService Authz { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;
    [Inject] private ILogger<NewTaskModal> Log { get; set; } = null!;

    [Parameter] public EventCallback<TaskItem> OnModalActionSucceeded { get; set; }
    [Parameter] public EventCallback<string> OnModalActionFailed { get; set; }
    [Parameter] public Project? Project { get; set; }

    public void OpenModal(TaskStatus taskStatus)
    {
        _showModal = true;
        _modalStatus = taskStatus;
    }

    public void OpenModal()
    {
        _showModal = true;
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

            var memberResult = await Authz.AuthorizeAsync(user, Project, "IsProjectMember");
            if (!memberResult.Succeeded)
            {
                _modalError = "No rights for task creation.";
                return;
            }

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

            Db.Tasks.Add(task);
            await Db.SaveChangesAsync();

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
