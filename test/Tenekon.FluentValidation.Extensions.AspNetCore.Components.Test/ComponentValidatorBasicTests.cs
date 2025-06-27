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
        ],
    ];
}

public class ComponentValidatorBasicTests : TestContext
{
    public ComponentValidatorBasicTests()
    {
        Services.AddValidatorsFromAssemblyContaining<AssemblyMarker>(includeInternalTypes: true);
    }

    [Theory]
    [MemberData(nameof(ComponentValidatorBasicTestCases.All), MemberType = typeof(ComponentValidatorBasicTestCases))]
    public void ValidModel_ValidationReturnsTrue<TComponentValidator>(ValidatorTestCase<TComponentValidator> testCase)
        where TComponentValidator : ComponentValidatorBase
    {
        var model = new Model("World");
        var context = new EditContext(model);

        using var cut = RenderComponent<TComponentValidator>(parameters => {
            parameters.AddCascadingValue(context);
            testCase.CustomizeParameters(parameters, typeof(Validator), model, context);
        });

        context.Validate().ShouldBeTrue();
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
        cut.Instance.OwnEditContext.NotifyFieldChanged(modelFieldIdentifier);
        var isValid = context.IsValid(modelFieldIdentifier);
        isValid.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(ComponentValidatorBasicTestCases.All), MemberType = typeof(ComponentValidatorBasicTestCases))]
    public void ValidModel_NestedFieldValidationReturnsFalse<TComponentValidator>(ValidatorTestCase<TComponentValidator> testCase)
        where TComponentValidator : ComponentValidatorBase
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
        routes.Instance.OwnEditContext.NotifyFieldChanged(modelFieldIdentifier);
        var isValid = context.IsValid(modelFieldIdentifier);
        isValid.ShouldBeFalse();
    }
}

file record Model(string? Hello = null)
{
    public string? Hello { get; set; } = Hello;

    [field: AllowNull]
    [field: MaybeNull]
    public ChildModel Child { get => field ??= new ChildModel(); }

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
