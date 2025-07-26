using System.Linq.Expressions;
using Bunit;
using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;
using Shouldly;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

#pragma warning disable xUnit1042
public record ValidatorTestCase<TEditModelValidator>(
    // ReSharper disable once NotAccessedPositionalProperty.Global
    string Name,
    Action<ComponentParameterCollectionBuilder<TEditModelValidator>, Type, object, EditContext> CustomizeParameters)
    where TEditModelValidator : EditModelValidatorBase<TEditModelValidator>, IParameterSetTransitionHandlerRegistryProvider;

public static class EditModelValidatorBasicTestCases
{
    private static IValidator CreateValidator(Type validatorType) =>
        (IValidator)(Activator.CreateInstance(validatorType) ?? throw new InvalidOperationException());

    public static IEnumerable<object[]> All => [
        [
            new ValidatorTestCase<EditModelValidatorRootpath>(
                "RootpathWithValidatorType",
                (p, validatorType, model, ctx) => { p.Add(x => x.ValidatorType, validatorType); })
        ],
        [
            new ValidatorTestCase<EditModelValidatorRootpath>(
                "RootpathWithValidatorInstance",
                (p, validatorType, model, ctx) => { p.Add(x => x.Validator, CreateValidator(validatorType)); })
        ],
        [
            new ValidatorTestCase<EditModelValidatorSubpath>(
                "SubpathWithModelAndValidatorType",
                (p, validatorType, model, ctx) => {
                    p.Add(x => x.ValidatorType, validatorType);
                    p.Add(x => x.Model, model);
                })
        ],
        [
            new ValidatorTestCase<EditModelValidatorSubpath>(
                "SubpathWithEditContextAndValidatorType",
                (p, validatorType, model, ctx) => {
                    p.Add(x => x.ValidatorType, validatorType);
                    p.Add(x => x.EditContext, ctx);
                })
        ],
        [
            new ValidatorTestCase<EditModelValidatorSubpath>(
                "SubpathWithModelAndValidatorInstance",
                (p, validatorType, model, ctx) => {
                    p.Add(x => x.Validator, CreateValidator(validatorType));
                    p.Add(x => x.Model, model);
                })
        ],
        [
            new ValidatorTestCase<EditModelValidatorSubpath>(
                "SubpathWithEditContextAndValidatorInstance",
                (p, validatorType, model, ctx) => {
                    p.Add(x => x.Validator, CreateValidator(validatorType));
                    p.Add(x => x.EditContext, ctx);
                })
        ]
    ];
}

public class EditModelValidatorBasicTests : TestContext
{
    public EditModelValidatorBasicTests() => Services.AddValidatorsFromAssemblyContaining<AssemblyMarker>(includeInternalTypes: true);

    [Theory]
    [MemberData(nameof(EditModelValidatorBasicTestCases.All), MemberType = typeof(EditModelValidatorBasicTestCases))]
    public void ValidModel_ValidationReturnsTrue<TEditModelValidator>(ValidatorTestCase<TEditModelValidator> testCase)
        where TEditModelValidator : EditModelValidatorBase<TEditModelValidator>, IParameterSetTransitionHandlerRegistryProvider
    {
        var model = new Model("World");
        var context = new EditContext(model);

        using var cut = RenderComponent<TEditModelValidator>(parameters => {
            parameters.AddCascadingValue(context);
            testCase.CustomizeParameters(parameters, typeof(ModelValidator), model, context);
        });

        context.Validate().ShouldBeTrue();


        var cutActorEditContex = cut.Instance.ActorEditContext;
        EditContextPropertyAccessor.s_rootEditContext.TryGetPropertyValue(cutActorEditContex, out _, out var counter).ShouldBeTrue();
        counter.ShouldBe(expected: 1);

        DisposeComponents();
        EditContextPropertyAccessor.s_rootEditContext.TryGetPropertyValue(context, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(EditModelValidatorBasicTestCases.All), MemberType = typeof(EditModelValidatorBasicTestCases))]
    public void InvalidModel_ValidationReturnsFalse<TEditModelValidator>(ValidatorTestCase<TEditModelValidator> testCase)
        where TEditModelValidator : EditModelValidatorBase<TEditModelValidator>, IParameterSetTransitionHandlerRegistryProvider
    {
        var model = new Model("WRONG");
        var context = new EditContext(model);

        using var cut = RenderComponent<TEditModelValidator>(parameters => {
            parameters.AddCascadingValue(context);
            testCase.CustomizeParameters(parameters, typeof(ModelValidator), model, context);
        });

        context.Validate().ShouldBeFalse();

        var cutActorEditContex = cut.Instance.ActorEditContext;
        EditContextPropertyAccessor.s_rootEditContext.TryGetPropertyValue(cutActorEditContex, out _, out var counter).ShouldBeTrue();
        counter.ShouldBe(expected: 1);

        DisposeComponents();
        EditContextPropertyAccessor.s_rootEditContext.TryGetPropertyValue(cutActorEditContex, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(EditModelValidatorBasicTestCases.All), MemberType = typeof(EditModelValidatorBasicTestCases))]
    public void ValidModel_DirectFieldValidationReturnsFalse<TEditModelValidator>(ValidatorTestCase<TEditModelValidator> testCase)
        where TEditModelValidator : EditModelValidatorBase<TEditModelValidator>, IParameterSetTransitionHandlerRegistryProvider
    {
        var model = new Model("WRONG");
        var context = new EditContext(model);

        using var cut = RenderComponent<TEditModelValidator>(parameters => {
            parameters.AddCascadingValue(context);
            testCase.CustomizeParameters(parameters, typeof(ModelValidator), model, context);
        });

        var modelFieldIdentifier = FieldIdentifier.Create(() => model.Hello);
        var cutActorEditContex = cut.Instance.ActorEditContext;
        cutActorEditContex.NotifyFieldChanged(modelFieldIdentifier);
        context.IsValid(modelFieldIdentifier).ShouldBeFalse();

        EditContextPropertyAccessor.s_rootEditContext.TryGetPropertyValue(cutActorEditContex, out _, out var counter).ShouldBeTrue();
        counter.ShouldBe(expected: 1);

        DisposeComponents();
        EditContextPropertyAccessor.s_rootEditContext.TryGetPropertyValue(cutActorEditContex, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(EditModelValidatorBasicTestCases.All), MemberType = typeof(EditModelValidatorBasicTestCases))]
    public void ValidModel_NestedFieldValidationReturnsFalse<TEditModelValidator>(ValidatorTestCase<TEditModelValidator> testCase)
        where TEditModelValidator : EditModelValidatorBase<TEditModelValidator>, IParameterSetTransitionHandlerRegistryProvider
    {
        var model = new Model { Child = { Hello = "WRONG" } };
        var context = new EditContext(model);

        using var cut = RenderComponent<TEditModelValidator>(parameters => {
            parameters.AddCascadingValue(context);
            testCase.CustomizeParameters(parameters, typeof(ModelValidator), model, context);
            parameters.Add<Expression<Func<object>>[]?>(x => x.Routes, [() => model.Child]);
        });
        var subPath = cut.FindComponent<EditModelSubpath>();
        var modelFieldIdentifier = FieldIdentifier.Create(() => model.Child.Hello);
        var subPathActorEditContext = subPath.Instance.ActorEditContext;
        subPathActorEditContext.NotifyFieldChanged(modelFieldIdentifier);
        context.IsValid(modelFieldIdentifier).ShouldBeFalse();

        var cutActorEditContext = cut.Instance.ActorEditContext;
        EditContextPropertyAccessor.s_rootEditContext.TryGetPropertyValue(cutActorEditContext, out _, out var cutActorEditContextCounter)
            .ShouldBeTrue();
        cutActorEditContextCounter.ShouldBe(expected: 2);

        EditContextPropertyAccessor.s_rootEditContext
            .TryGetPropertyValue(subPathActorEditContext, out _, out var subPathActorEditContextCounter)
            .ShouldBeTrue();
        subPathActorEditContextCounter.ShouldBe(expected: 2);

        DisposeComponents();
        EditContextPropertyAccessor.s_rootEditContext.TryGetPropertyValue(cutActorEditContext, out _).ShouldBeFalse();
        EditContextPropertyAccessor.s_rootEditContext.TryGetPropertyValue(subPathActorEditContext, out _).ShouldBeFalse();
    }
}
