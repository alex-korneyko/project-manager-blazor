using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ProjectManager.Components.Common;

public partial class SplitPane : SplitPaneBase, IAsyncDisposable
{
    IJSObjectReference? _mod;

    [Parameter] [EditorRequired] public string Id { get; set; }
    // Pane content
    [Parameter] public RenderFragment? Left { get; set; }
    [Parameter] public RenderFragment? Right { get; set; }

    // Container size
    [Parameter] public string Width { get; set; } = "100%";

    // Minimal width of each panel (px)
    [Parameter] public int MinLeft { get; set; } = 150;
    [Parameter] public int MinRight { get; set; } = 150;

    // Initial ratio between panels (0..1)
    [Parameter] public double InitialLeftRatio { get; set; } = 0.5;

    // Current width of the left panel in px (controlled by JS when dragging)
    private double LeftWidth { get; set; } = double.NaN;

    private string LeftWidthPx => double.IsNaN(LeftWidth) ? "auto" : $"{LeftWidth}px";
    private string MinLeftPx => $"{MinLeft}px";
    private string MinRightPx => $"{MinRight}px";

    private ElementReference _containerRef;
    private ElementReference _leftRef;
    private ElementReference _gutterRef;
    private string _localStorageRatioFieldName;

    [Inject] protected IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] protected ProtectedLocalStorage ProtectedLocalStorage { get; set; } = null!;
    [Inject] public ILogger<SplitPane> Logger { get; set; } = null!;

    private DotNetObjectReference<SplitPane>? _dotnetRef;

    protected override void OnParametersSet()
    {
        _localStorageRatioFieldName = $"splitPaneRatio_{Id}";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _mod = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Common/SplitPane.razor.js");
        _dotnetRef = DotNetObjectReference.Create(this);

        var storageResult = await ProtectedLocalStorage.GetAsync<double>(_localStorageRatioFieldName);
        if (storageResult.Success)
        {
            InitialLeftRatio = storageResult.Value;
        }

        await _mod.InvokeVoidAsync("init", _containerRef, _gutterRef, _leftRef, _dotnetRef, MinLeft, MinRight, InitialLeftRatio);
        StateHasChanged();
    }

    [JSInvokable]
    public Task OnSizeChanged(double leftPx)  // Invokes from js
    {
        LeftWidth = leftPx;
        StateHasChanged();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnMouseUp(double ratio)
    {
        await ProtectedLocalStorage.SetAsync(_localStorageRatioFieldName, ratio);
    }

    public override void Dispose()
    {
        // _dotnetRef?.Dispose();
        // base.Dispose();
        //
        // _mod?.DisposeAsync().AsTask();
    }

    public async ValueTask DisposeAsync()
    {
        _dotnetRef?.Dispose();

        try
        {
            if (_mod is not null)
                await _mod.DisposeAsync();
        }
        catch (JSDisconnectedException e)
        {
            Logger.LogWarning("JS runtime disconnected");
        }
    }
}
