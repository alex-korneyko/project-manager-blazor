using Microsoft.AspNetCore.Components;

namespace ProjectManager.Components.Modals;

public interface IModal<T>
{
    public EventCallback<T> OnModalActionSucceeded { get; set; }
    public EventCallback<string> OnModalActionFailed { get; set; }

    void OpenModal();
    void CloseModal();
}
