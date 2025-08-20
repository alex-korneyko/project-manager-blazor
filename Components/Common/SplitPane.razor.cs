using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ProjectManager.Components.Common;

public partial class SplitPane : SplitPaneBase, IAsyncDisposable
{
    IJSObjectReference? _mod;

    // Pane content
    [Parameter] public RenderFragment? Left { get; set; }
    [Parameter] public RenderFragment? Right { get; set; }

    // Container size
    [Parameter] public string Width { get; set; } = "100%";
    [Parameter] public string Height { get; set; } = "400px";

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

    [Inject] protected IJSRuntime JsRuntime { get; set; } = null!;

    private DotNetObjectReference<SplitPane>? _dotnetRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _mod = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Common/SplitPane.razor.js");
        _dotnetRef = DotNetObjectReference.Create(this);
        await _mod.InvokeVoidAsync("init", _containerRef, _gutterRef, _leftRef, _dotnetRef, MinLeft, MinRight, InitialLeftRatio);
    }

    [JSInvokable]
    public Task OnSizeChanged(double leftPx)  // Invokes from js
    {
        LeftWidth = leftPx;
        StateHasChanged();
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _dotnetRef?.Dispose();
        base.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_mod is not null) await _mod.DisposeAsync();
    }
}
