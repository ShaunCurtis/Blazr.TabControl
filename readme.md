This repo demonstrates how to use the Blazor Component Registration Pattern.  It demonstrates how to use the pattern in building one of the standard BootStrap Tab controls.

Let's look at what we want to achieve.  We have a `BlazrTabControl` component with serveral `BlazrTab` coponents, each with their own content.

```csharp
<BlazrTabControl Class="p-2 bg-light">

    <BlazrTab Label="First">
        <div class="alert alert-primary m-4">
            First
        </div>
    </BlazrTab>

    <BlazrTab Label="Second">
        <div class="alert alert-success m-4">
            Second
        </div>
    </BlazrTab>

    <BlazrTab Label="Third">
        <div class="alert alert-danger m-4">
            Third
        </div>
    </BlazrTab>

</BlazrTabControl>
``` 

The problem we face is that although `BlazrTab` is a child component of `BlazrTabControl`, `BlazrTabControl` doesn't know intrinsically know about the existence of the `BlazrTab` instances when the are created as part of the render process.  All the component instances are created and managed by the Renderer, and are structured into a render tree that the individual component instance have no reference to.

What we can do is cascade a reference from `BlazrTabControl` that the `BlazrTab` instances can capture and provide a registration mechanism.


Here's the code for the `BlazrTab`.

1. It's a class, not a Razor component.  There's no content that we want to render. `BlazrTabControl` will render the `ChildContent` of the selected tab,  
2. The reference to `BlazrTabControl` is not the component itself but a callback method to register the component.  Cascading component references is not good practice.
3. Thgere's a Label for the Tab and a Id reference for external reference.
4. `ChildContent` holds the `RenderFragment` reference to the content in the parent.
5. The component registers itself by calling the cascaded Register callback.


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

`TabData` is a simple record to hold the Tab data.  I use this instead of passing around component references. 

```csharp
public record TabData(string Id, string Label, RenderFragment? Content);
```

### BlazrTabControl

The html that the component will output should look like this:

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

We have a set of parameters for setting the Css and other data:

```csharp
    [Parameter] public string Class { get; set; } = string.Empty;
    [Parameter] public string UlCss { get; set; } = "nav nav-tabs";
    [Parameter] public string LiCss { get; set; } = "nav-item";
    [Parameter] public string ACss { get; set; } = "nav-link";
    [Parameter] public string ACssActive { get; set; } = "nav-link bg-light active";
    [Parameter] public RenderFragment? ChildContent { get; set; }
```

Local variables to hold the state including the tab collection:

```csharp
    private List<TabData> _tabs = new();
    private TabData? _activeTab;
    private bool _firstRender = true;
```

The Register method which checks to see if the component is already registered and sets the active tab to the first entry.

```csharp
private void Register(TabData tab)
{
    if (!_tabs.Any(item => item == tab))
    {
        _tabs.Add(tab);
        if (_tabs.Count() == 1)
            _activeTab = tab;
    }
}
```

Two methods to set the active tab.  The external method is for situations where you want to switch between tabs with a tab's content.

```csharp
// Internal method for setting the current Tab
private void ChangeToTab(TabData tab)
    => _activeTab = tab;

// External mwthod to set the current tab using the Id
public void SetTab(string id)
{
    var tab = _tabs.FirstOrDefault(item => item.Id.Equals(id));
    if (tab is not null)
        _activeTab = tab;
}
``` 

A method to set the tab's Css.

```csharp
private string TabCss(TabData tab)
    => tab == _activeTab ? this.ACssActive : this.ACss;
``` 

And finally the `OnInitializedAsync`.  The method yields the task and then sets `_firstRender` to false.  What this means is on the first render of the component (on the yield) {firstRender} is true, whilw on subsequent renders (when OnInitializedAsync and OnParametersSetAsync completes and thereafter) `_firstRender` is false. 

```csharp
protected override async Task OnInitializedAsync()
{
    await Task.Yield();
    _firstRender = false;
}
```

Now to the markup.

Cascade the Register method.  

When the component first renders (on the yield in `OnInitializedAsync`):

`_firstRender` is true, so the component renders it's child content.  The `BlazrTab` component instances are all rendered and register a `TabData` object.  I'll cover why we use a `TabData` object rather than just registering the component itself shortly.

On subsequent renders:

`_firstRender` is false, so the tabs are rendered and the render fragment that holds the active tab's contents is rendered. 

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