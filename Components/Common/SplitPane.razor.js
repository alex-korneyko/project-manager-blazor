export function init(container, gutter, left, dotnet, minLeft, minRight, initialRatio) {
    const state = { dragging: false };
    
    // initial width of the left pane
    requestAnimationFrame(() => {
        const rect = container.getBoundingClientRect();
        const initialLeft = clamp(rect.width * (initialRatio ?? 0.5), minLeft, rect.width - minRight);
        left.style.width = initialLeft + "px";
        dotnet.invokeMethodAsync("OnSizeChanged", initialLeft);
    });
    
    const onMove = (e) => {
        if (!state.dragging) return;
        const rect = container.getBoundingClientRect();
        const clientX = e.touches ? e.touches[0].clientX : e.clientX;
        const x = clamp(clientX - rect.left, minLeft, rect.width - minRight);
        left.style.width = x + "px";
        dotnet.invokeMethodAsync("OnSizeChanged", x);
        e.preventDefault();
    };
    
    const stop = () => {
        if (!state.dragging) return;
        state.dragging = false;
        window.removeEventListener("pointermove", onMove, true);
        window.removeEventListener("pointerup", stop, true);
        window.removeEventListener("touchmove", onMove, { passive:false, capture:true });
        window.removeEventListener("touchend", stop, true);
        document.body.style.cursor = "";
    };
    
    const start = (e) => {
        state.dragging = true;
        window.addEventListener("pointermove", onMove, true);
        window.addEventListener("pointerup", stop, true);
        window.addEventListener("touchmove", onMove, { passive:false, capture:true });
        window.addEventListener("touchend", stop, true);
        document.body.style.cursor = "col-resize";
        e.preventDefault();
    };
    
    gutter.addEventListener("pointerdown", start);
    gutter.addEventListener("touchstart", start, { passive:false });
}

function clamp(v, min, max) { return Math.max(min, Math.min(max, v)); }
