using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Common.Security;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Components.Modals;

public partial class NewProjectModal : ComponentBase, IModal<Project>
{
    private bool _showModal;
    private ProjectEditModel _modalModel = new();
    private bool _submitting;
    private string? _modalError;
    private string? _error;
    // private string _newProjectName = string.Empty;
    // private string? _newProjectDesc;

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] private ApplicationDbContext DbContext { get; set; } = null!;
    [Inject] private ILogger<NewProjectModal> Logger { get; set; } = null!;

    [Parameter] public EventCallback<Project> OnModalActionSucceeded { get; set; }
    [Parameter] public EventCallback<string> OnModalActionFailed { get; set; }


    public async Task OpenModalAsync()
    {
        _showModal = true;
        StateHasChanged();
    }

    public async void CloseModal()
    {
        _showModal = false;
    }

    private async Task CreateProject()
    {
        _error = null;
        try
        {
            var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
            var userId = user.GetUserId();

            var project = new Project()
            {
                Id = Guid.NewGuid(),
                Name = _modalModel.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(_modalModel.DescriptionMarkdown) ? null : _modalModel.DescriptionMarkdown.Trim(),
                OwnerId = userId!
            };

            DbContext.Projects.Add(project);
            await DbContext.SaveChangesAsync();
            _modalModel.Title = "";
            _modalModel.DescriptionMarkdown = null;

            await OnModalActionSucceeded.InvokeAsync(project);
            CloseModal();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to create project");
            _error = "Failed to create project";
        }
    }

    private sealed class ProjectEditModel
    {
        [Required, MinLength(2)] public string Title { get; set; } = "";
        public string? DescriptionMarkdown { get; set; }
    }
}
