using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ProjectManager.Common.Security;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;
using ProjectManager.Services.Security;

namespace ProjectManager.Components.Pages;

public partial class Projects : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] private IProjectAccessService ProjectAccessService { get; set; } = null!;
    [Inject] private ApplicationDbContext DbContext { get; set; } = null!;
    [Inject] private ILogger<Projects> Logger { get; set; } = null!;

    private bool _loading = true;
    private List<Project>? _projects = new();
    private string _newProjectName = "";
    private string? _newProjectDesc = "";
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
        var userId = user.GetUserId();
        _projects = (await ProjectAccessService.GetVisibleProjectsAsync(userId!)).ToList();
        _loading = false;

        await base.OnInitializedAsync();
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
                Name = _newProjectName.Trim(),
                Description = string.IsNullOrWhiteSpace(_newProjectDesc) ? null : _newProjectDesc.Trim(),
                OwnerId = userId!
            };

            DbContext.Projects.Add(project);
            await DbContext.SaveChangesAsync();
            _projects!.Add(project);
            _newProjectName = "";
            _newProjectDesc = null;

            // StateHasChanged();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to create project");
            _error = "Failed to create project";
        }
    }
}
