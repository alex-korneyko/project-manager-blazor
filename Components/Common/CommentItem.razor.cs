using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ProjectManager.Domain;
using ProjectManager.Services;
using static ProjectManager.Authorization.AuthorizationPoliciesNames;

namespace ProjectManager.Components.Common;

public partial class CommentItem : ComponentBase
{
    private bool _editing;
    private bool _replying;
    private bool CanModify;
    private bool _confirmDelete;

    [Inject] public CommentsService CommentsService { get; set; } = null!;
    [Inject] public AuthenticationStateProvider Auth { get; set; } = null!;
    [Inject] public IAuthorizationService Authz { get; set; } = null!;

    [Parameter] public CommentNode Node { get; set; } = null!;
    [Parameter] public EventCallback OnChanged { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        var user = (await Auth.GetAuthenticationStateAsync()).User;

        var isAuthor = await Authz.AuthorizeAsync(user, Node.Comment, CommentAuthor);
        CanModify = isAuthor.Succeeded;
    }

    private void ToggleEdit() => _editing = !_editing;
    private void ToggleReply() => _replying = !_replying;
    private void ToggleDelete() => _confirmDelete = !_confirmDelete;

    private async Task SaveEdit(string newBody)
    {
        var ok = await CommentsService.EditAsync(Node.Comment.Id, newBody, (await Auth.GetAuthenticationStateAsync()).User);
        if (ok) { _editing = false; await OnChanged.InvokeAsync(); }
    }

    private async Task SendReply(string body)
    {
        var c = await CommentsService.AddAsync(Node.Comment.TaskItemId, Node.Comment.Id, body, (await Auth.GetAuthenticationStateAsync()).User);
        if (c is not null) { _replying = false; await OnChanged.InvokeAsync(); }
    }

    private async Task DeleteThread()
    {
        var ok = await CommentsService.DeleteThreadAsync(Node.Comment.Id, (await Auth.GetAuthenticationStateAsync()).User);
        if (ok)
        {
            _confirmDelete = false;
            await OnChanged.InvokeAsync();
        }
    }
}
