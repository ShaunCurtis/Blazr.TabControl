using Microsoft.AspNetCore.Components;

namespace Blazr.TabControl.Components;

public class BlazrTab : ComponentBase
{
    [CascadingParameter] private Action<BlazrTab>? Register { get; set; }
    [Parameter, EditorRequired] public string Label { get; set; } = "No Label Set";
    [Parameter] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Parameter] public RenderFragment? ChildContent { get; set; }

    // Registers the component on initialization
    protected override void OnInitialized()
        => this.Register?.Invoke(this);
}
