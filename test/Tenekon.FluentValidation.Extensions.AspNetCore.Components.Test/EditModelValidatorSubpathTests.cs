using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shouldly;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class EditModelValidatorSubpathTests : TestContext
{
    [Fact]
    public void RenderComponent_NotSetModelNorEditContext_Throws()
    {
        var error = Should.Throw<InvalidOperationException>(() => {
            using var cut = RenderComponent<EditModelValidatorSubpath>();
        });

        error.Message.ShouldContain("exactly one");
    }

    [Fact]
    public void RenderComponent_SetModelAndEditContext_Throws()
    {
        var model = new Model();
        var editContext = new EditContext(model);

        var error = Should.Throw<InvalidOperationException>(() => {
            using var cut = RenderComponent<EditModelValidatorSubpath>(parameters => {
                parameters.Add(x => x.Model, model);
                parameters.Add(x => x.EditContext, editContext);
            });
        });

        error.Message.ShouldContain("exactly one");
    }

    [Fact]
    public void RenderComponent_AfterCascadingValueChange_ShouldNotRecreateActorEditContext()
    {
        var model = new Model();
        var editContext1 = new EditContext(model);
        var editContext2 = new EditContext(model);

        var cascadingEditForm = RenderComponent<CascadingValue<EditContext>>(p => {
            p.Add(x => x.Value, editContext1);
            p.Add(x => x.IsFixed, false);
            p.AddChildContent<EditModelValidatorSubpath>(p2 => {
                p2.Add(x => x.Model, model);
                p2.Add(x => x.Validator, new ModelValidator());
                p2.AddChildContent(static _ => { });
            });
        });

        var cut = cascadingEditForm.FindComponent<EditModelValidatorSubpath>();
        var ancestorEditContext1 = cut.Instance.AncestorEditContext;
        var actorEditContext1 = cut.Instance.ActorEditContext;

        cut.Instance.AncestorEditContext.ShouldBeSameAs(editContext1);
        ancestorEditContext1.ShouldBeSameAs(editContext1);

        cascadingEditForm.Instance.Value = editContext2;
        cut.Render();

        var ancestorEditContext2 = cut.Instance.AncestorEditContext;
        var actorEditContext2 = cut.Instance.ActorEditContext;

        cut.Instance.AncestorEditContext.ShouldBeSameAs(editContext2);
        ancestorEditContext2.ShouldBeSameAs(editContext2);
        actorEditContext2.ShouldBeSameAs(actorEditContext1);
    }

    [Fact]
    public void RenderComponent_AfterRerendering_ActorEditContextRemainsSame()
    {
        var model = new Model();
        var editContext1 = new EditContext(model);

        var cascadingEditForm = RenderComponent<CascadingValue<EditContext>>(p => {
            p.Add(x => x.Value, editContext1);
            p.Add(x => x.IsFixed, false);
            p.AddChildContent<EditModelValidatorSubpath>(p2 => {
                p2.Add(x => x.Model, model);
                p2.Add(x => x.Validator, new ModelValidator());
                p2.AddChildContent(static _ => { });
            });
        });

        var cut = cascadingEditForm.FindComponent<EditModelValidatorSubpath>();
        cut.Instance.AncestorEditContext.ShouldBeSameAs(editContext1);
        var actorEditContext1 = cut.Instance.ActorEditContext;
        actorEditContext1.ShouldNotBeSameAs(editContext1);

        cut.Render();

        cut.Instance.AncestorEditContext.ShouldBeSameAs(editContext1);
        var actorEditContext2 = cut.Instance.ActorEditContext;
        actorEditContext2.ShouldBeSameAs(actorEditContext1);
    }

    [Fact]
    public void RenderComponent_WithRoutes_ActorEditContextMatchesSubpath()
    {
        var model = new Model();
        var editContext = new EditContext(model);

        var cut = RenderComponent<EditModelValidatorSubpath>(parameters => {
            parameters.AddCascadingValue(editContext);
            parameters.Add(x => x.Model, model);
            parameters.Add(x => x.Validator, new ModelValidator());
            parameters.Add<Expression<Func<object>>[]?>(x => x.Routes, [() => model.Child]);
        });

        var subpath = cut.FindComponent<EditModelSubpath>();
        cut.Instance.ActorEditContext.ShouldBeSameAs(subpath.Instance.ActorEditContext);
    }

    [Fact]
    public void RenderComponent_SetThenClearedRoutes_ActorEditContextRevertsToOwn()
    {
        var model = new Model();
        var editContext = new EditContext(model);

        var cut = RenderComponent<EditModelValidatorSubpath>(parameters => {
            parameters.AddCascadingValue(editContext);
            parameters.Add(x => x.Model, model);
            parameters.Add(x => x.Validator, new ModelValidator());
            parameters.Add<Expression<Func<object>>[]?>(x => x.Routes, [() => model.Child]);
        });

        {
            var subpath = cut.FindComponent<EditModelSubpath>();
            cut.Instance.ActorEditContext.ShouldBeSameAs(subpath.Instance.ActorEditContext);
        }

        cut.Instance.Routes = null;
        cut.Render();

        cut.Instance.ActorEditContext.ShouldBeSameAs(cut.Instance.LastParameterSetTransition.ActorEditContext.New);
    }
}
