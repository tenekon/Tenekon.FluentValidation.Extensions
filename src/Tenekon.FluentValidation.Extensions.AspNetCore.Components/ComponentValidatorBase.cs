using System.Diagnostics;
using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public abstract class ComponentValidatorBase : EditContextualComponentBase<ComponentValidatorBase>, IComponentValidator,
    IHandlingParametersTransition
{
    static ParametersTransitionHandlerRegistry IHandlingParametersTransition.ParametersTransitionHandlerRegistry { get; } = new();

    private readonly RenderFragment _renderComponentValidatorContent;
    private readonly RenderFragment<RenderFragment?> _renderEditContextualComponentFragment;
    private readonly RenderFragment<RenderFragment?> _renderComponentValidatorRoutesFragment;
    private readonly Action<ValidationStrategy<object>> _applyValidationStrategyAction;
    private bool _havingValidatorSetExplicitly;
    private IValidator? _validator;

    /* TODO: Make pluggable */
    // internal readonly LeafComponentValidatorContext _leafComponentValidatorContext = new();

    protected ComponentValidatorBase()
    {
        _renderComponentValidatorContent = RenderComponentValidatorContent;
        _renderEditContextualComponentFragment = childContent => builder => RenderEditContextualComponent(builder, childContent);
        _renderComponentValidatorRoutesFragment = childContent => builder => RenderComponentValidatorRoutes(builder, childContent);
        _applyValidationStrategyAction = ApplyValidationStrategy;
    }

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public IValidator? Validator {
#pragma warning restore BL0007 // Component parameters should be auto properties
        get => _validator;
        set {
            _validator = value;
            _havingValidatorSetExplicitly = value is not null;
        }
    }

    [Inject]
    private IServiceProvider? ServiceProvider { get; set; }

    [Inject]
    private ILogger<ComponentValidatorBase>? Logger { get; set; }

    [Parameter]
    public Type? ValidatorType { get; set; }

    [Parameter]
    public Action<ValidationStrategy<object>>? ConfigureValidationStrategy { get; set; }

    /// <summary>If true field identifiers with models not handable by validator won't throw.</summary>
    [Parameter]
    public bool SuppressInvalidatableFieldModels { get; set; }

    /// <summary>
    ///     Inclusive minimum severity to be treated as an validation error. The order of the severities is as follow:
    ///     <list type="bullet">
    ///         <item>
    ///             <see cref="Severity.Error" />
    ///         </item>
    ///         <item>
    ///             <see cref="Severity.Warning" />
    ///         </item>
    ///         <item>
    ///             <see cref="Severity.Info" />
    ///         </item>
    ///     </list>
    ///     For example, if severity is equal to <see cref="Severity.Warning" />, then any validation messages with severity
    ///     <see cref="Severity.Warning" /> and <see cref="Severity.Error" /> will pass, but validation messages with severity
    ///     <see cref="Severity.Info" /> not.
    /// </summary>
    [Parameter]
    public Severity MinimumSeverity { get; set; } = Severity.Info;

    [Parameter]
    public Expression<Func<object>>[]? Routes { get; set; }

    private void RenderComponentValidatorRoutes(RenderTreeBuilder builder, RenderFragment? childContent)
    {
        builder.OpenComponent<ComponentValidatorRoutes>(sequence: 0);
        builder.AddComponentParameter(sequence: 1, nameof(ComponentValidatorRoutes.Routes), Routes);
        builder.AddComponentParameter(sequence: 2, nameof(ChildContent), childContent);
        builder.CloseComponent();
    }

    private void RenderComponentValidatorContent(RenderTreeBuilder builder)
    {
        if (Routes is not null) {
            builder.AddContent(sequence: 0, _renderEditContextualComponentFragment, _renderComponentValidatorRoutesFragment(ChildContent));
        } else {
            builder.AddContent(sequence: 1, _renderEditContextualComponentFragment, ChildContent);
        }
    }

    private void RenderComponentValidator(RenderTreeBuilder builder, RenderFragment childContent)
    {
        builder.OpenComponent<CascadingValue<IComponentValidator>>(sequence: 0);
        builder.AddComponentParameter(sequence: 1, "IsFixed", value: true);
        builder.AddComponentParameter(sequence: 2, "Value", this);
        builder.AddComponentParameter(sequence: 3, nameof(CascadingValue<>.ChildContent), childContent);
        builder.CloseComponent();
    }

    void IComponentValidator.ValidateModel() => ValidateModel();

    void IComponentValidator.ValidateDirectField(FieldIdentifier fieldIdentifier) => ValidateDirectField(fieldIdentifier);

    void IComponentValidator.ValidateNestedField(FieldIdentifier directFieldIdentifier, FieldIdentifier nestedFieldIdentifier) =>
        ValidateNestedField(directFieldIdentifier, nestedFieldIdentifier);

    private void ApplyValidationStrategy(ValidationStrategy<object> validationStrategy) =>
        ConfigureValidationStrategy?.Invoke(validationStrategy);

    protected virtual void ConfigureValidationContext(ConfigueValidationContextArguments arguments)
    {
        /* TODO: Make pluggable */
        // arguments.ValidationContext.RootContextData[ComponentValidatorContextLookupKey.Standard] =
        //     _leafComponentValidatorContext;
    }

    private void ConfigureOnParametersSet()
    {
        // We do not allow to set the validator Explicitly and having specified the validator type as the same type 
        if (!(_havingValidatorSetExplicitly ^ ValidatorType is not null)) {
            throw new InvalidOperationException(
                $"{GetType()} requires either the parameter {nameof(Validator)} of type {nameof(IValidator)} or {nameof(ValidatorType)} of type {nameof(Type)}");
        }

        var validatorType = ValidatorType;
        if (validatorType is not null && _validator?.GetType() != validatorType) {
            Debug.Assert(ServiceProvider is not null);
            _validator = (IValidator)ServiceProvider.GetRequiredService(validatorType);
        }
    }

    protected override void OnParametersSet()
    {
        ConfigureOnParametersSet();
        base.OnParametersSet();
    }

    private void ClearValidationMessageStores()
    {
        Debug.Assert(_ownEditContextValidationMessageStore is not null);
        _ownEditContextValidationMessageStore.Clear();
        _rootEditContextValidationMessageStore?.Clear();
    }

    private void AddValidationMessageToStores(FieldIdentifier fieldIdentifier, string errorMessage)
    {
        Debug.Assert(_ownEditContextValidationMessageStore is not null);
        _ownEditContextValidationMessageStore.Add(fieldIdentifier, errorMessage);

        if (_areOwnEditContextAndSuperEditContextNotEqual) {
            Debug.Assert(_rootEditContextValidationMessageStore is not null);
            _rootEditContextValidationMessageStore.Add(fieldIdentifier, errorMessage);
        }
    }

    private void ValidateModel()
    {
        Debug.Assert(_validator is not null);
        Debug.Assert(_ownEditContext is not null);

        var validationContext = ValidationContext<object>.CreateWithOptions(_ownEditContext.Model, _applyValidationStrategyAction);
        ConfigureValidationContext(new ConfigueValidationContextArguments(validationContext));
        var validationResult = _validator.Validate(validationContext);

        ClearValidationMessageStores();
        foreach (var error in validationResult.Errors) {
            if (error.Severity > MinimumSeverity) {
                continue;
            }

            var fieldIdentifier = FieldIdentifierHelper.DeriveFieldIdentifier(_ownEditContext.Model, error.PropertyName);
            AddValidationMessageToStores(fieldIdentifier, error.ErrorMessage);
        }

        _ownEditContext.NotifyValidationStateChanged();
    }

    protected override void OnValidateModel(object? sender, ValidationRequestedEventArgs e) => ValidateModel();

    // TODO: Removable?
    private ValidationResult Validate(ValidationContext<object> validationContext)
    {
        Debug.Assert(_validator is not null);
        try {
            return _validator.Validate(validationContext);
        } catch (InvalidOperationException error) {
            throw new ComponentValidationException(
                $"{error.Message} Consider to make use of {nameof(ComponentValidatorSubpath)}, {nameof(ComponentValidatorRoutes)} or similiar.",
                error);
        }
    }

    private void ValidateDirectField(FieldIdentifier fieldIdentifier)
    {
        Debug.Assert(_ownEditContext is not null);
        Debug.Assert(_ownEditContextValidationMessageStore is not null);
        Debug.Assert(_validator is not null);

        if (SuppressInvalidatableFieldModels && !_validator.CanValidateInstancesOfType(fieldIdentifier.Model.GetType())) {
            Logger?.LogWarning(
                "Direct field identifier validation was supressed, because its model is invalidatable: {}",
                fieldIdentifier.Model.GetType());
            return;
        }

        var validationContext = ValidationContext<object>.CreateWithOptions(fieldIdentifier.Model, ApplyValidationStrategy2);
        ConfigureValidationContext(new ConfigueValidationContextArguments(validationContext));
        var validationResult = _validator.Validate(validationContext);

        ClearValidationMessageStores();
        foreach (var error in validationResult.Errors) {
            if (error.Severity > MinimumSeverity) {
                continue;
            }

            AddValidationMessageToStores(fieldIdentifier, error.ErrorMessage);
        }

        _ownEditContext.NotifyValidationStateChanged();
        return;

        void ApplyValidationStrategy2(ValidationStrategy<object> validationStrategy)
        {
            validationStrategy.IncludeProperties(fieldIdentifier.FieldName);
            _applyValidationStrategyAction.Invoke(validationStrategy);
        }
    }

    private void ValidateNestedField(FieldIdentifier directFieldIdentifier, FieldIdentifier nestedFieldIdentifier)
    {
        Debug.Assert(_validator is not null);
        Debug.Assert(_ownEditContext is not null);
        Debug.Assert(_ownEditContextValidationMessageStore is not null);

        if (SuppressInvalidatableFieldModels && !_validator.CanValidateInstancesOfType(nestedFieldIdentifier.Model.GetType())) {
            Logger?.LogWarning(
                "Nested field identifier validation was supressed, because its model is invalidatable: {}",
                nestedFieldIdentifier.Model.GetType());
            return;
        }

        var validationContext = ValidationContext<object>.CreateWithOptions(nestedFieldIdentifier.Model, ApplyValidationStrategy2);
        ConfigureValidationContext(new ConfigueValidationContextArguments(validationContext));
        var validationResult = _validator.Validate(validationContext);

        ClearValidationMessageStores();
        foreach (var error in validationResult.Errors) {
            if (error.Severity > MinimumSeverity) {
                continue;
            }
            AddValidationMessageToStores(directFieldIdentifier, error.ErrorMessage);
        }

        _ownEditContext.NotifyValidationStateChanged();
        return;

        void ApplyValidationStrategy2(ValidationStrategy<object> validationStrategy)
        {
            validationStrategy.IncludeProperties(nestedFieldIdentifier.FieldName);
            _applyValidationStrategyAction.Invoke(validationStrategy);
        }
    }

    protected override void OnValidateField(object? sender, FieldChangedEventArgs e) => ValidateDirectField(e.FieldIdentifier);

    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        RenderComponentValidator(builder, _renderComponentValidatorContent);
}
