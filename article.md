## A Blazor Tab Control

The Tab control provides an excellent example to demonstrate some modern component coding techniques.

The classic Tab solution is documented in the Microsoft Documentation [here](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/cascading-values-and-parameters?view=aspnetcore-7.0#pass-data-across-a-component-hierarchy) and regurgitated *ad finitum* by many authors as their code.

This article uses a very different approach to the problem, using what I call the *Component Registration Pattern*.

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

The core problem we need to solve is that although each `BlazrTab` is a child component of `BlazrTabControl`, `BlazrTabControl` has no knowledge of the `BlazrTab` instances defined in it's content and created as part of the render process.  All component instances are created and managed by the Renderer, and are structured into a render tree.  Individual component instances have no access to that render tree.

The classic way to deal with this is to cascade the instance of `BlazorTabControl`, and for each `BlazorTab` to register itself with the `BlazorTabControl` instance.  The tab displayed is controlled by an `ActiveTab` public property each Tab can see.

What's wrong with this approach?

The key problem is that we are passing around component objects which:

1. We don't own: they are owned and managed by the Renderer.
2. Expose functionality that isn't applicable outside the context of the renderer: calling say `SetParamnetersAsync` will probably be disasterous.
3. Expose referenced objects such as services that may not be in a *live* state.
4. May implement `I{Async}Disposable` and be disposed: the Renderer will dispose them when it's finished with them (but you aren't).

For these reasons I consider passing components around to be bad practice and avoid.




