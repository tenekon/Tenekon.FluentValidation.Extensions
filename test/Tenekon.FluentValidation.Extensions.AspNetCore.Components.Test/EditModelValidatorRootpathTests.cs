using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shouldly;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class EditModelValidatorRootpathTests : TestContext
{
    [Fact]
    public void RenderComponent_WithChildContentWithoutRoutes_ShouldCreateIsolatedEditContext()
    {
        var model = new Model();
        var editContext = new EditContext(model);

        var cut = RenderComponent<EditModelValidatorRootpath>(parameters => {
            parameters.AddCascadingValue(editContext);
            parameters.Add(x => x.Validator, new ModelValidator());
            parameters.AddChildContent(static _ => { });
        });

        cut.Instance.LastParameterSetTransition.ActorEditContext.New.ShouldNotBeSameAs(editContext);
    }

    [Fact]
    public void RenderComponent_WithChildContentWithRoutes_ShouldTakeAncestorEditContext()
    {
        var model = new Model();
        var editContext = new EditContext(model);

        var cut = RenderComponent<EditModelValidatorRootpath>(parameters => {
            parameters.AddCascadingValue(editContext);
            parameters.Add<Expression<Func<object>>[]?>(x => x.Routes, [() => model.Child]);
            parameters.Add(x => x.Validator, new ModelValidator());
            parameters.AddChildContent(static _ => { });
        });

        cut.Instance.LastParameterSetTransition.ActorEditContext.New.ShouldBeSameAs(editContext);
    }

    [Fact]
    public void RenderComponent_WithoutChildContentWithoutRoutes_ShouldCreateIsolatedEditContext()
    {
        var model = new Model();
        var editContext = new EditContext(model);

        var cut = RenderComponent<EditModelValidatorRootpath>(parameters => {
            parameters.AddCascadingValue(editContext);
            parameters.Add(x => x.Validator, new ModelValidator());
        });

        cut.Instance.LastParameterSetTransition.ActorEditContext.New.ShouldBeSameAs(editContext);
    }

    [Fact]
    public void RenderComponent_WithoutChildContentWithRoutes_ShouldTakeAncestorEditContext()
    {
        var model = new Model();
        var editContext = new EditContext(model);

        var cut = RenderComponent<EditModelValidatorRootpath>(parameters => {
            parameters.AddCascadingValue(editContext);
            parameters.Add<Expression<Func<object>>[]?>(x => x.Routes, [() => model.Child]);
            parameters.Add(x => x.Validator, new ModelValidator());
        });

        cut.Instance.LastParameterSetTransition.ActorEditContext.New.ShouldBeSameAs(editContext);
    }

    [Fact]
    public void RenderComponent_WithCascadingValueChange_ShouldRecreateActorEditContext()
    {
        var model = new Model();
        var editContext1 = new EditContext(model);
        var editContext2 = new EditContext(model);

        var cascadingEditForm = RenderComponent<CascadingValue<EditContext>>(p => {
            p.Add(x => x.Value, editContext1);
            p.Add(x => x.IsFixed, false);
            p.AddChildContent<EditModelValidatorRootpath>(p2 => {
                p2.Add(x => x.Validator, new ModelValidator());
                p2.AddChildContent(static _ => { });
            });
        });

        var cut = cascadingEditForm.FindComponent<EditModelValidatorRootpath>();
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
            p.Add(x => x.IsFixed, false);
            p.AddChildContent<EditModelValidatorRootpath>(p2 => {
                p2.Add(x => x.Validator, new ModelValidator());
                p2.AddChildContent(static _ => { });
            });
        });

        var cut = cascadingEditForm.FindComponent<EditModelValidatorRootpath>();
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

        var cut = RenderComponent<EditModelValidatorRootpath>(parameters => {
            parameters.AddCascadingValue(editContext);
            parameters.Add(x => x.Validator, new ModelValidator());
            parameters.Add<Expression<Func<object>>[]?>(x => x.Routes, [() => model.Child]);
        });

        var subpath = cut.FindComponent<EditModelSubpath>();
        cut.Instance.ActorEditContext.ShouldBeSameAs(subpath.Instance.ActorEditContext);
    }

    [Fact]
    public void RenderComponent_SetThenClearedRoutes_ActorEditContextRevertsToAncestor()
    {
        var model = new Model();
        var editContext = new EditContext(model);

        var cut = RenderComponent<EditModelValidatorRootpath>(parameters => {
            parameters.AddCascadingValue(editContext);
            parameters.Add(x => x.Validator, new ModelValidator());
            parameters.Add<Expression<Func<object>>[]?>(x => x.Routes, [() => model.Child]);
        });

        {
            var subpath = cut.FindComponent<EditModelSubpath>();
            cut.Instance.ActorEditContext.ShouldBeSameAs(subpath.Instance.ActorEditContext);
        }

        cut.Instance.Routes = null;
        cut.Render();

        cut.Instance.ActorEditContext.ShouldBeSameAs(cut.Instance.AncestorEditContext);
    }
}
