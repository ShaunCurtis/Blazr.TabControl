# A Blazor Tab Control

The Tab control provides an excellent example to demonstrate some modern component coding techniques.

The classic Tab solution is documented in the Microsoft Documentation [here](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/cascading-values-and-parameters?view=aspnetcore-7.0#pass-data-across-a-component-hierarchy) and regurgitated *ad finitum* by many authors as their code.

This article applies a very different approach to the problem, using what I call the *Component Registration Pattern*.

This is what we want to achieve [which looks no different to the many other implementations].

```html
<BlazrTabControl>

    <BlazrTab>
        ... content
    </BlazrTab>

    <BlazrTab>
        ... content
    </BlazrTab>

    <BlazrTab>
        ... content
    </BlazrTab>

</BlazrTabControl>
``` 

## Repository

The repository for this article is [here - Blazr.TabControl](https://github.com/ShaunCurtis/Blazr.TabControl).

## Where are those Tabs?

The core problem with the component -> sub-component architecture above is that although you think `BlazrTabControl` should know about the existence of the `BlazrTab` components, it doesn't.  The instances of `BlazrTab` are created and owned by the Renderer as part of the render process, and are structured into a Render Tree.  Individual component instances have no access to that render tree.

The classic way to handle this is to cascade the instance of `BlazorTabControl`.  Each `BlazorTab` instance can register itself with the `BlazorTabControl` instance.  `BlazorTabControl` has a public property `ActiveTab` which controls which tab displays it's content.

### What's wrong with this approach?

The solution passes around component instances which:

1. We don't own: they are owned and managed by the Renderer.
2. Exposes functionality in the Tab that isn't applicable outside the context of the renderer: calling say `SetParamnetersAsync` will probably be disasterous.
3. Exposes referenced objects such as services that may not be in a *live* state.
4. May implement `I{Async}Disposable` and be disposed: the Renderer will dispose them when it's finished with them (but you aren't).

For these reasons I consider passing components around to be bad practice and avoid.

## Solution

Let's look at the code to see how we solve the problem.

### TabData

We don't register the `BlazrTab` component, just a `TabData` record that captures the component state.

```csharp
public record TabData(string Id, string Label, RenderFragment? Content);
```

### BlazrTab

1. It's a class, not a Razor component: there's no content to render.  The `ChildContent` is passed to `BlazrTabControl` in `TabData`.  
2. `Register` is an `Action` callback to register the component data.
3. There's a `Label` for the Tab and a `Id` reference for external reference.
4. `ChildContent` holds the `RenderFragment` reference to the content in the parent.
5. When the component initialises it registers a `TabData` object with `BlazrTabControl`.

```csharp
public class BlazrTab : ComponentBase
{
    [CascadingParameter] private Action<TabData>? Register { get; set; }
    [Parameter, EditorRequired] public string Label { get; set; } = "No Label Set";
    [Parameter] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Parameter] public RenderFragment? ChildContent { get; set; }

    // Registers the component on initialization
    protected override void OnInitialized()
        => this.Register?.Invoke(new(this.Id, this.Label, this.ChildContent));
}
```


### BlazrTabControl

The Tab html that the component will output should look like this:

```html
<ul class="nav nav-tabs">
  <li class="nav-item">
    <a class="nav-link active" aria-current="page" href="#">Active</a>
  </li>
  <li class="nav-item">
    <a class="nav-link" href="#">Link</a>
  </li>
  <li class="nav-item">
    <a class="nav-link" href="#">Link</a>
  </li>
  <li class="nav-item">
    <a class="nav-link disabled" href="#" tabindex="-1" aria-disabled="true">Disabled</a>
  </li>
</ul>
```

1. A set of parameters for setting the Css and other data:

```csharp
    [Parameter] public string Class { get; set; } = string.Empty;
    [Parameter] public string UlCss { get; set; } = "nav nav-tabs";
    [Parameter] public string LiCss { get; set; } = "nav-item";
    [Parameter] public string ACss { get; set; } = "nav-link";
    [Parameter] public string ACssActive { get; set; } = "nav-link bg-light active";
    [Parameter] public RenderFragment? ChildContent { get; set; }
```

2. Local state variables including the `TabData` collection:

```csharp
    private List<TabData> _tabs = new();
    private TabData? _activeTab;
    private bool _firstRender = true;
```

3. The Register method. It checks to see if the component is already registered and sets the active tab to the first entry.

```csharp
private void Register(TabData tab)
{
    if (!_tabs.Any(item => item == tab))
        _tabs.Add(tab);

    if (_tabs.Count() == 1)
        _activeTab = tab;
}
```

4. Two methods to set the active tab.  The external method is for where you want to switch between tabs with a tab's content.  The final code section shows how to do this.

```csharp
private void ChangeToTab(TabData tab)
    => _activeTab = tab;

public void SetTab(string id)
{
    var tab = _tabs.FirstOrDefault(item => item.Id.Equals(id));
    if (tab is not null)
        _activeTab = tab;
}
``` 

5. A method to set the tab's Css.

```csharp
private string TabCss(TabData tab)
    => tab == _activeTab ? this.ACssActive : this.ACss;
``` 

6. `OnInitializedAsync`.  The method yields the task and then sets `_firstRender` to false.  On first render (on the yield) `_firstRender` is true, while on subsequent renders (when OnInitializedAsync and OnParametersSetAsync completes and thereafter) `_firstRender` is false. 

```csharp
protected override async Task OnInitializedAsync()
{
    await Task.Yield();
    _firstRender = false;
}
```

The Razor markup.

Cascade the Register method.  

When the component first renders (on the yield in `OnInitializedAsync`):

- `_firstRender` is true, so the component renders it's child content.  The `BlazrTab` component instances are all rendered and register a `TabData` object.  I'll cover why we use a `TabData` object rather than just registering the component itself shortly.

On subsequent renders:

- `_firstRender` is false, so the tabs are rendered and the render fragment that holds the active tab's contents is rendered. 

```html
<CascadingValue Value=this.Register>

    @if (_firstRender)
    {
        @*Renders the child content on the first render event*@
        @ChildContent
    }
    else
    {
        @*Builds out the top tab*@
        <ul class="nav nav-tabs">
            @foreach (var tab in _tabs)
            {
                <li class="@this.LiCss">
                    <a class="@this.TabCss(tab)" aria-current="page" @onclick="() => this.ChangeToTab(tab)">@tab.Label</a>
                </li>
            }
        </ul>
        @*Renders the active Tab's content*@
        <div class="@this.Class">
            @_activeTab?.Content
        </div>
    }
```

### Demo Page

It's self explanatory.

I've added the inter-tab navigation to show how that's done using the Tab `Id`.

```html
@page "/"

<PageTitle>Index</PageTitle>

<BlazrTabControl Class="p-2 bg-light" @ref=_blazrTabControl>

    <BlazrTab Label="First" Id="1">
        <div class="alert alert-primary m-4">
            First
        </div>
    </BlazrTab>

    <BlazrTab Label="Second" Id="2">
        <div class="alert alert-success m-4">
            Second
        </div>
    </BlazrTab>

    <BlazrTab Label="Third" Id="3">
        <div class="alert alert-danger m-4">
            Third
        </div>
        <button class="btn btn-primary" @onclick="@(() => this.ChangeTab("1"))">Back to First</button>
    </BlazrTab>

</BlazrTabControl>
```
```html
@code {
    private BlazrTabControl? _blazrTabControl;

    private void ChangeTab(string id)
        => _blazrTabControl?.SetTab(id);
}
```
## Summary

It's important to understand that the `BlazrTab` components are rendered on the first render of the `BlazorTabControl` and then discarded.  The Renderer will de-reference them and leave the GC to remove them.  The `BlazrTab`'s content is a `RenderFragment` owned by `BlazorTabControl`.  This gets passed back in the `TabData` object.  `BlazorTabControl` adds the active tab's `RenderFragment` to it's rendered content.  





