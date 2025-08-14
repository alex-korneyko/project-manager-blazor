using Microsoft.AspNetCore.Components;

namespace ProjectManager.Components.Modals;

public interface IModal<in TModalData, TModalResult>
{
    public EventCallback<TModalResult> OnModalActionSucceeded { get; set; }
    public EventCallback<string> OnModalActionFailed { get; set; }

    void OpenModal(TModalData? modalData = default);
    void CloseModal();
}

public interface IModal<TModalResult>
{
    public EventCallback<TModalResult> OnModalActionSucceeded { get; set; }
    public EventCallback<string> OnModalActionFailed { get; set; }

    void OpenModal();
    void CloseModal();
}
