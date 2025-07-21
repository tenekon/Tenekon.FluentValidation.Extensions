using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Bunit;
using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;
using Shouldly;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

#pragma warning disable xUnit1042
public record ValidatorTestCase<TComponentValidator>(
    string Name,
    Action<ComponentParameterCollectionBuilder<TComponentValidator>, Type, object, EditContext> CustomizeParameters)
    where TComponentValidator : ComponentValidatorBase;

public static class ComponentValidatorBasicTestCases
{
    private static IValidator CreateValidator(Type validatorType) =>
        (IValidator)(Activator.CreateInstance(validatorType) ?? throw new InvalidOperationException());

    public static IEnumerable<object[]> All => [
        [
            new ValidatorTestCase<ComponentValidatorRootpath>(
                "RootpathWithValidatorType",
                (p, validatorType, model, ctx) => { p.Add(x => x.ValidatorType, validatorType); })
        ],
        [
            new ValidatorTestCase<ComponentValidatorRootpath>(
                "RootpathWithValidatorInstance",
                (p, validatorType, model, ctx) => { p.Add(x => x.Validator, CreateValidator(validatorType)); })
        ],
        [
            new ValidatorTestCase<ComponentValidatorSubpath>(
                "SubpathWithModelAndValidatorType",
                (p, validatorType, model, ctx) => {
                    p.Add(x => x.ValidatorType, validatorType);
                    p.Add(x => x.Model, model);
                })
        ],
        [
            new ValidatorTestCase<ComponentValidatorSubpath>(
                "SubpathWithEditContextAndValidatorType",
                (p, validatorType, model, ctx) => {
                    p.Add(x => x.ValidatorType, validatorType);
                    p.Add(x => x.EditContext, ctx);
                })
        ],
        [
            new ValidatorTestCase<ComponentValidatorSubpath>(
                "SubpathWithModelAndValidatorInstance",
                (p, validatorType, model, ctx) => {
                    p.Add(x => x.Validator, CreateValidator(validatorType));
                    p.Add(x => x.Model, model);
                })
        ],
        [
            new ValidatorTestCase<ComponentValidatorSubpath>(
                "SubpathWithEditContextAndValidatorInstance",
                (p, validatorType, model, ctx) => {
                    p.Add(x => x.Validator, CreateValidator(validatorType));
                    p.Add(x => x.EditContext, ctx);
                })
        ]
    ];
}

public class ComponentValidatorBasicTests : TestContext
{
    public ComponentValidatorBasicTests() => Services.AddValidatorsFromAssemblyContaining<AssemblyMarker>(includeInternalTypes: true);

    [Theory]
    [MemberData(nameof(ComponentValidatorBasicTestCases.All), MemberType = typeof(ComponentValidatorBasicTestCases))]
    public void ValidModel_ValidationReturnsTrue<TComponentValidator>(ValidatorTestCase<TComponentValidator> testCase)
        where TComponentValidator : ComponentValidatorBase
    {
        var modelA = new { A = new List<string>() };
        var field = FieldIdentifier.Create(() => modelA.A[0]);

        var model = new Model("World");
        var context = new EditContext(model);

        using var cut = RenderComponent<TComponentValidator>(parameters => {
            parameters.AddCascadingValue(context);
            testCase.CustomizeParameters(parameters, typeof(Validator), model, context);
        });

        context.Validate().ShouldBeTrue();


        var cutActorEditContex = cut.Instance.ActorEditContext;
        RootEditContextPropertyAccessorHolder.s_accessor.TryGetPropertyValue(cutActorEditContex, out _, out var counter).ShouldBeTrue();
        counter.ShouldBe(expected: 1);

        DisposeComponents();
        RootEditContextPropertyAccessorHolder.s_accessor.TryGetPropertyValue(context, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(ComponentValidatorBasicTestCases.All), MemberType = typeof(ComponentValidatorBasicTestCases))]
    public void InvalidModel_ValidationReturnsFalse<TComponentValidator>(ValidatorTestCase<TComponentValidator> testCase)
        where TComponentValidator : ComponentValidatorBase
    {
        var model = new Model("WRONG");
        var context = new EditContext(model);

        using var cut = RenderComponent<TComponentValidator>(parameters => {
            parameters.AddCascadingValue(context);
            testCase.CustomizeParameters(parameters, typeof(Validator), model, context);
        });

        context.Validate().ShouldBeFalse();

        var cutActorEditContex = cut.Instance.ActorEditContext;
        RootEditContextPropertyAccessorHolder.s_accessor.TryGetPropertyValue(cutActorEditContex, out _, out var counter).ShouldBeTrue();
        counter.ShouldBe(expected: 1);

        DisposeComponents();
        RootEditContextPropertyAccessorHolder.s_accessor.TryGetPropertyValue(cutActorEditContex, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(ComponentValidatorBasicTestCases.All), MemberType = typeof(ComponentValidatorBasicTestCases))]
    public void ValidModel_DirectFieldValidationReturnsFalse<TComponentValidator>(ValidatorTestCase<TComponentValidator> testCase)
        where TComponentValidator : ComponentValidatorBase
    {
        var model = new Model("WRONG");
        var context = new EditContext(model);

        using var cut = RenderComponent<TComponentValidator>(parameters => {
            parameters.AddCascadingValue(context);
            testCase.CustomizeParameters(parameters, typeof(Validator), model, context);
        });

        var modelFieldIdentifier = FieldIdentifier.Create(() => model.Hello);
        var cutActorEditContex = cut.Instance.ActorEditContext;
        cutActorEditContex.NotifyFieldChanged(modelFieldIdentifier);
        var isValid = context.IsValid(modelFieldIdentifier);
        isValid.ShouldBeFalse();

        RootEditContextPropertyAccessorHolder.s_accessor.TryGetPropertyValue(cutActorEditContex, out _, out var counter).ShouldBeTrue();
        counter.ShouldBe(expected: 1);

        DisposeComponents();
        RootEditContextPropertyAccessorHolder.s_accessor.TryGetPropertyValue(cutActorEditContex, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(ComponentValidatorBasicTestCases.All), MemberType = typeof(ComponentValidatorBasicTestCases))]
    public void ValidModel_NestedFieldValidationReturnsFalse<TComponentValidator>(ValidatorTestCase<TComponentValidator> testCase)
        where TComponentValidator : ComponentValidatorBase, IDisposable
    {
        var model = new Model { Child = { Hello = "WRONG" } };
        var context = new EditContext(model);

        using var cut = RenderComponent<TComponentValidator>(parameters => {
            parameters.AddCascadingValue(context);
            testCase.CustomizeParameters(parameters, typeof(Validator), model, context);
            parameters.Add<Expression<Func<object>>[]?>(x => x.Routes, [() => model.Child]);
        });
        var routes = cut.FindComponent<ComponentValidatorRoutes>();
        var modelFieldIdentifier = FieldIdentifier.Create(() => model.Child.Hello);
        var routesActorEditContext = routes.Instance.ActorEditContext;
        routesActorEditContext.NotifyFieldChanged(modelFieldIdentifier);
        var isValid = context.IsValid(modelFieldIdentifier);
        isValid.ShouldBeFalse();

        var cutActorEditContext = cut.Instance.ActorEditContext;
        RootEditContextPropertyAccessorHolder.s_accessor.TryGetPropertyValue(cutActorEditContext, out _, out var cutActorEditContextCounter)
            .ShouldBeTrue();
        cutActorEditContextCounter.ShouldBe(expected: 2);

        RootEditContextPropertyAccessorHolder.s_accessor
            .TryGetPropertyValue(routesActorEditContext, out _, out var routesActorEditContextCounter)
            .ShouldBeTrue();
        routesActorEditContextCounter.ShouldBe(expected: 2);

        DisposeComponents();
        RootEditContextPropertyAccessorHolder.s_accessor.TryGetPropertyValue(cutActorEditContext, out _).ShouldBeFalse();
        RootEditContextPropertyAccessorHolder.s_accessor.TryGetPropertyValue(routesActorEditContext, out _).ShouldBeFalse();
    }
}

file record Model(string? Hello = null)
{
    public string? Hello { get; set; } = Hello;

    [field: AllowNull]
    [field: MaybeNull]
    public ChildModel Child => field ??= new ChildModel();

    public record ChildModel(string? Hello = null)
    {
        public string? Hello { get; set; } = Hello;
    }
}

file class Validator : AbstractValidator<Model>
{
    public Validator()
    {
        When(x => x.Hello is not null, () => RuleFor(x => x.Hello).Equal("World"));
        When(x => x.Child.Hello is not null, () => RuleFor(x => x.Child.Hello).Equal("World"));
    }
}
