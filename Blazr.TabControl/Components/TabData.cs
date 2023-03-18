using Microsoft.AspNetCore.Components;

namespace Blazr.TabControl.Components;

public record TabData(string Id, string Label, RenderFragment? Content);
