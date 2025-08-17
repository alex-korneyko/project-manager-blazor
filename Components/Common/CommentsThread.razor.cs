using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ProjectManager.Domain;
using ProjectManager.Services;

namespace ProjectManager.Components.Common;

public partial class CommentsThread : ComponentBase
{
    private List<CommentNode>? _nodes;

    [Inject] public CommentsService CommentsService { get; set; } = null!;
    [Inject] public AuthenticationStateProvider Auth { get; set; } = null!;

    [Parameter] public Guid TaskId { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await Reload();
    }

    private async Task Reload()
    {
        var user = (await Auth.GetAuthenticationStateAsync()).User;
        _nodes = await CommentsService.GetTreeAsync(TaskId, user);
        StateHasChanged();
    }

    private async Task PostTopLevel(string body)
    {
        var c = await CommentsService.AddAsync(TaskId, parentId: null, body, (await Auth.GetAuthenticationStateAsync()).User);
        if (c is not null) await Reload();
    }
}
