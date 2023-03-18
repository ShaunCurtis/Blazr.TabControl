This repo demonstrates how to use the Blazor Component Registration Pattern.  It uses the pattern in building a Tab component based on one of the standard BootStrap Tab controls.

Let's look at what we want to achieve. A `BlazrTabControl` component with serveral `BlazrTab` components, each with their own content.

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

The problem we need to solve is that although `BlazrTab` is a child component of `BlazrTabControl`, `BlazrTabControl` has no knowledge of the `BlazrTab` instances created as part of the render process.  All component instances are created and managed by the Renderer, and are structured into a render tree.  Individual component instances have no access to that render tree.

We need to create a registration process, that provides that information to `BlazrTabControl`.

As passing references to component instances around in not good practice we need a data object to hold the `BlazrTab` state:

`TabData` is a simple record to hold the Tab data.

```csharp
public record TabData(string Id, string Label, RenderFragment? Content);
```

We can cascade a reference from `BlazrTabControl` that `BlazrTab` instances capture and use to register.

### BlazrTab

Here's the code for `BlazrTab`.  It's simple.

1. It's a class, not a Razor component.  There's no content to render. `BlazrTabControl` will render the `ChildContent` of the selected tab.  
2. `register` is an `Action` callback to register the component data.
3. There's a Label for the Tab and a Id reference for external reference.
4. `ChildContent` holds the `RenderFragment` reference to the content in the parent.
5. When the component initialises it registers a `TabData` object containing the relevant data `BlazrTabControl` needs to manage and render the component content.


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

## Why shouldn't you cascade a Component

1. It's not your object.  It's owned and managed by the Renderer.
2. It exposes a lot of functionality that isn't applicable outside the context of the render, and calling say `SetParamnetersAsync` can be disasterous.
3. Referenced objects such as services may not be in the state you expect them if you hold a reference to the component, but the Renderer has finished with it.
4. If the component implements `I{Async}Disposable` then it will be disposed when the Renderer is finished with it (but you may not be).  