using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shouldly;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal record SubpathTestCase(
    string Name,
    Action<ComponentParameterCollectionBuilder<EditModelScope>, EditContext, Model> CustomizeParameters);

internal static class EditModelScopeTestCases
{
    public static IEnumerable<object[]> All => [
        [
            new SubpathTestCase(
                "ChildContent",
                (p, ctx, model) => {
                    p.AddCascadingValue(ctx);
                    p.AddChildContent(static _ => { });
                })
        ],
        [
            new SubpathTestCase("WithoutChildContentWithoutRoutes", (p, ctx, _) => p.AddCascadingValue(ctx))
        ],
        [
            new SubpathTestCase("WithoutChildContent", (p, ctx, model) => { p.AddCascadingValue(ctx); })
        ]
    ];
}

public class EditModelScopeTests : TestContext
{
    [Theory]
    [MemberData(nameof(EditModelScopeTestCases.All), MemberType = typeof(EditModelScopeTestCases))]
    internal void ShouldCreateAncestorDerivedActorEditContext(SubpathTestCase testCase)
    {
        var model = new Model();
        var editContext = new EditContext(model);

        using var cut = RenderComponent<EditModelScope>(parameters => { testCase.CustomizeParameters(parameters, editContext, model); });

        cut.Instance.ActorEditContext.ShouldNotBeSameAs(editContext);
        cut.Instance.ActorEditContext.Model.ShouldBeSameAs(editContext.Model);
        EditContextAccessor.GetProperties(cut.Instance.ActorEditContext).ShouldNotBeSameAs(editContext.Properties);
        EditContextAccessor.EditContextFieldStatesMemberAccessor.GetValue(cut.Instance.ActorEditContext)
            .ShouldNotBeSameAs(EditContextAccessor.EditContextFieldStatesMemberAccessor.GetValue(editContext));
    }

    [Fact]
    public void RenderComponent_SetModelAndEditContext_Throws()
    {
        var model = new Model();
        var editContext = new EditContext(model);

        Should.Throw<InvalidOperationException>(() => {
                using var cut = RenderComponent<EditModelScope>(parameters => {
                    parameters.Add(x => x.Model, model);
                    parameters.Add(x => x.EditContext, editContext);
                });
            })
            .Message.ShouldContain("exactly one");
    }

    [Fact]
    public void RenderComponent_WithCascadingValueChange_ShouldRecreateActorEditContext()
    {
        var model = new Model();
        var editContext1 = new EditContext(model);
        var editContext2 = new EditContext(model);

        var cascadingEditForm = RenderComponent<CascadingValue<EditContext>>(p => {
            p.Add(x => x.Value, editContext1);
            p.Add(x => x.IsFixed, value: false);
            p.AddChildContent<EditModelScope>(p2 => { p2.AddChildContent(static _ => { }); });
        });

        var cut = cascadingEditForm.FindComponent<EditModelScope>();
        cut.Instance.AncestorEditContext.ShouldBeSameAs(editContext1);
        var actorEditContext1 = cut.Instance.ActorEditContext;
        actorEditContext1.ShouldNotBeSameAs(editContext1);

        cascadingEditForm.Instance.Value = editContext2;
        cut.Render();

        cut.Instance.AncestorEditContext.ShouldBeSameAs(editContext2);
        var actorEditContext2 = cut.Instance.ActorEditContext;
        actorEditContext2.ShouldNotBeSameAs(actorEditContext1);
    }

    [Fact]
    public void RenderComponent_WithRerendering_ActorEditContextRemainsSame()
    {
        var model = new Model();
        var editContext1 = new EditContext(model);

        var cascadingEditForm = RenderComponent<CascadingValue<EditContext>>(p => {
            p.Add(x => x.Value, editContext1);
            p.Add(x => x.IsFixed, value: false);
            p.AddChildContent<EditModelScope>(p2 => { p2.AddChildContent(static _ => { }); });
        });

        var cut = cascadingEditForm.FindComponent<EditModelScope>();
        cut.Instance.AncestorEditContext.ShouldBeSameAs(editContext1);
        var actorEditContext1 = cut.Instance.ActorEditContext;
        actorEditContext1.ShouldNotBeSameAs(editContext1);

        cut.Render();

        cut.Instance.AncestorEditContext.ShouldBeSameAs(editContext1);
        var actorEditContext2 = cut.Instance.ActorEditContext;
        actorEditContext2.ShouldBeSameAs(actorEditContext1);
    }
}
