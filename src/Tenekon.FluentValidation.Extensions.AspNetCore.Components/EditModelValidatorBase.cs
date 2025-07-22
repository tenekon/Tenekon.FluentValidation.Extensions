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

public abstract class EditModelValidatorBase : EditContextualComponentBase<EditModelValidatorBase>, IEditModelValidator,
    IEditModelValidationNotifier, IHandlingParametersTransition
{
    static ParametersTransitionHandlerRegistry IHandlingParametersTransition.ParametersTransitionHandlerRegistry { get; } = new();

    private readonly RenderFragment _renderEditModelValidatorContent;
    private readonly RenderFragment<RenderFragment?> _renderEditContextualComponentFragment;
    private readonly RenderFragment<RenderFragment?> _renderEditModelSubpathFragment;
    private readonly Action<ValidationStrategy<object>> _applyValidationStrategyAction;
    private bool _havingValidatorSetExplicitly;
    private IValidator? _validator;
    private ServiceScopeSource _serviceScopeSource;

    /* TODO: Make pluggable */
    // internal readonly LeafEditModelValidatorContext _leafEditModelValidatorContext = new();

    protected EditModelValidatorBase()
    {
        _renderEditModelValidatorContent = RenderEditModelValidatorContent;
        _renderEditContextualComponentFragment = childContent => builder => RenderEditContextualComponent(builder, childContent);
        _renderEditModelSubpathFragment = childContent => builder => RenderEditModelSubpath(builder, childContent);
        _applyValidationStrategyAction = ApplyValidationStrategy;
    }

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public IValidator? Validator {
#pragma warning restore BL0007 // Component parameters should be auto properties
        get;
        set {
            field = value;
            _havingValidatorSetExplicitly = value is not null;
        }
    }

    [Inject]
    private IServiceProvider? ServiceProvider { get; set; }

    [Inject]
    private ILogger<EditModelValidatorBase>? Logger { get; set; }

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

    private void RenderEditModelSubpath(RenderTreeBuilder builder, RenderFragment? childContent)
    {
        builder.OpenComponent<EditModelSubpath>(sequence: 0);
        builder.AddComponentParameter(sequence: 1, nameof(EditModelSubpath.Routes), Routes);
        builder.AddComponentParameter(sequence: 2, nameof(ChildContent), childContent);
        builder.CloseComponent();
    }

    private void RenderEditModelValidatorContent(RenderTreeBuilder builder)
    {
        if (Routes is not null) {
            builder.AddContent(sequence: 0, _renderEditContextualComponentFragment, _renderEditModelSubpathFragment(ChildContent));
        } else {
            builder.AddContent(sequence: 1, _renderEditContextualComponentFragment, ChildContent);
        }
    }

    private void RenderEditModelValidator(RenderTreeBuilder builder, RenderFragment childContent)
    {
        builder.OpenComponent<CascadingValue<IEditModelValidationNotifier>>(sequence: 0);
        builder.AddComponentParameter(sequence: 1, "IsFixed", value: true);
        builder.AddComponentParameter(sequence: 2, "Value", this);
        builder.AddComponentParameter(sequence: 3, nameof(CascadingValue<>.ChildContent), childContent);
        builder.CloseComponent();
    }

    private void ApplyValidationStrategy(ValidationStrategy<object> validationStrategy) =>
        ConfigureValidationStrategy?.Invoke(validationStrategy);

    protected virtual void ConfigureValidationContext(ConfigueValidationContextArguments arguments)
    {
        /* TODO: Make pluggable */
        // arguments.ValidationContext.RootContextData[EditModelValidatorContextLookupKey.Standard] =
        //     _leafEditModelValidatorContext;
    }

    protected override async Task OnParametersSetAsync()
    {
        var serviceScopeSource = new ServiceScopeSource(ServiceProvider);

        // We do not allow to set the validator Explicitly and having specified the validator type as the same type 
        if (!(_havingValidatorSetExplicitly ^ ValidatorType is not null)) {
            throw new InvalidOperationException(
                $"{GetType()} requires either the parameter {nameof(Validator)} of type {nameof(IValidator)} or {nameof(ValidatorType)} of type {nameof(Type)}, but not both.");
        }

        var validatorType = ValidatorType;
        // Whenever validator type is not null AND the validator type of the not yet or already materialized validator differs from that
        // validator type, then recreate.
        if (validatorType is not null && _validator?.GetType() != validatorType) {
            if (ServiceScopeSource.TryAcquireInitialization(ref serviceScopeSource)) {
                await DeinitalizeServiceScopeSourceAsync();
                // ReSharper disable once MethodHasAsyncOverload
                DeinitalizeServiceScopeSource();
                ServiceScopeSource.Initialize(ref serviceScopeSource, ref _serviceScopeSource, this);
            }

            _validator = (IValidator)serviceScopeSource.Value.ServiceProvider.GetRequiredService(validatorType);
        } else {
            Debug.Assert(Validator is not null);
            _validator = Validator;
        }

        await base.OnParametersSetAsync();
    }

    internal override async Task OnParametersTransitioningAsync() => await base.OnParametersTransitioningAsync();

    bool IEditModelValidationNotifier.IsInScope(EditContext candidate) =>
        (LastParametersTransition.RootEditContextTransition.TryGetNew(out var rootEditContext) &&
            ReferenceEquals(rootEditContext, candidate)) ||
        (RootEditContextPropertyAccessorHolder.s_accessor.TryGetPropertyValue(candidate, out var candidateRootEditContext) &&
            ReferenceEquals(rootEditContext, candidateRootEditContext));

    private void ClearValidationMessageStores()
    {
        Debug.Assert(_rootEditContextValidationMessageStore is not null);
        _rootEditContextValidationMessageStore.Clear();
        _actorEditContextValidationMessageStore?.Clear();
    }

    private void AddValidationMessageToStores(FieldIdentifier fieldIdentifier, string errorMessage)
    {
        Debug.Assert(_rootEditContextValidationMessageStore is not null);
        _rootEditContextValidationMessageStore.Add(fieldIdentifier, errorMessage);

        // REMINDER: actor edit context validation store can be null,
        // if actor edit context == ancestor edit context == root edit context 
        _actorEditContextValidationMessageStore?.Add(fieldIdentifier, errorMessage);
    }

    void IEditModelValidator.ValidateFullModel() => LastParametersTransition.RootEditContextTransition.New.Validate();

    private void ValidateModel()
    {
        Debug.Assert(_validator is not null);
        var actorEditContext = LastParametersTransition.ActorEditContextTransition.New;

        var validationContext = ValidationContext<object>.CreateWithOptions(actorEditContext.Model, _applyValidationStrategyAction);
        ConfigureValidationContext(new ConfigueValidationContextArguments(validationContext));
        var validationResult = _validator.Validate(validationContext);

        ClearValidationMessageStores();
        foreach (var error in validationResult.Errors) {
            if (error.Severity > MinimumSeverity) {
                continue;
            }

            var fieldIdentifier = FieldIdentifierHelper.DeriveFieldIdentifier(actorEditContext.Model, error.PropertyName);
            AddValidationMessageToStores(fieldIdentifier, error.ErrorMessage);
        }

        actorEditContext.NotifyValidationStateChanged();
    }

    protected override void OnValidateModel(object? sender, ValidationRequestedEventArgs args) => ValidateModel();

    protected virtual void NotifyModelValidationRequested(EditModelModelValidationRequestedArgs args)
    {
        // A. Whenever an actor edit context of a direct descendant of EditModelSubpath fires OnValidationRequested,
        //    it bubbles up to the root edit context, triggering OnValidateModel(object? sender, ValidationRequestedEventArgs args)
        //    and implicitly ValidateModel() of any validator component associated with the root edit context,
        //    except EditModelSubpath. This is because they do not subscribe to OnValidationRequested of the root edit context
        //    to avoid a second invocation of ValidateModel().
        // An additional safety net: To prevent a potential second invocation of ValidateModel(), we return early if the original source of
        // the event is reference-equal to the root edit context, since that instance already handles OnValidationRequested for
        // the root edit context.
        if (ReferenceEquals(LastParametersTransition.RootEditContextTransition.NewOrNull, args.OriginalSource)) {
            return;
        }

        ValidateModel();
    }

    void IEditModelValidationNotifier.NotifyModelValidationRequested(EditModelModelValidationRequestedArgs args) =>
        NotifyModelValidationRequested(args);

    void IEditModelValidator.Validate() => ValidateModel();

    // TODO: Removable?
    private ValidationResult Validate(ValidationContext<object> validationContext)
    {
        Debug.Assert(_validator is not null);
        try {
            return _validator.Validate(validationContext);
        } catch (InvalidOperationException error) {
            throw new EditModelValidationException(
                $"{error.Message} Consider to make use of {nameof(EditModelValidatorSubpath)}, {nameof(EditModelSubpath)} or similiar.",
                error);
        }
    }

    private void ValidateDirectField(FieldIdentifier fieldIdentifier)
    {
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

        LastParametersTransition.ActorEditContextTransition.New.NotifyValidationStateChanged();
        return;

        void ApplyValidationStrategy2(ValidationStrategy<object> validationStrategy)
        {
            validationStrategy.IncludeProperties(fieldIdentifier.FieldName);
            _applyValidationStrategyAction.Invoke(validationStrategy);
        }
    }

    void IEditModelValidationNotifier.NotifyDirectFieldValidationRequested(EditModelDirectFieldValidationRequestedArgs args) =>
        ValidateDirectField(args.FieldIdentifier);

    void IEditModelValidator.ValidateDirectField(FieldIdentifier fieldIdentifier) => ValidateDirectField(fieldIdentifier);

    private void ValidateNestedField(FieldIdentifier fullFieldPath, FieldIdentifier subFieldIdentifier)
    {
        Debug.Assert(_validator is not null);

        if (SuppressInvalidatableFieldModels && !_validator.CanValidateInstancesOfType(fullFieldPath.Model.GetType())) {
            Logger?.LogWarning(
                "Nested field identifier validation was supressed, because its model is invalidatable: {}",
                fullFieldPath.Model.GetType());
            return;
        }

        var validationContext = ValidationContext<object>.CreateWithOptions(fullFieldPath.Model, ApplyValidationStrategy2);
        ConfigureValidationContext(new ConfigueValidationContextArguments(validationContext));
        var validationResult = _validator.Validate(validationContext);

        ClearValidationMessageStores();
        foreach (var error in validationResult.Errors) {
            if (error.Severity > MinimumSeverity) {
                continue;
            }
            AddValidationMessageToStores(subFieldIdentifier, error.ErrorMessage);
        }

        LastParametersTransition.ActorEditContextTransition.New.NotifyValidationStateChanged();
        return;

        void ApplyValidationStrategy2(ValidationStrategy<object> validationStrategy)
        {
            validationStrategy.IncludeProperties(fullFieldPath.FieldName);
            _applyValidationStrategyAction.Invoke(validationStrategy);
        }
    }

    protected override void OnValidateField(object? sender, FieldChangedEventArgs e) => ValidateDirectField(e.FieldIdentifier);

    void IEditModelValidationNotifier.NotifyNestedFieldValidationRequested(EditModelNestedFieldValidationRequestedArgs args) =>
        ValidateNestedField(args.FullFieldPath, args.SubFieldIdentifier);

    void IEditModelValidator.ValidateNestedField(FieldIdentifier fullFieldPath, FieldIdentifier subFieldIdentifier) =>
        ValidateNestedField(fullFieldPath, subFieldIdentifier);

    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        RenderEditModelValidator(builder, _renderEditModelValidatorContent);

    private void DeinitalizeServiceScopeSource()
    {
        if (ServiceScopeSource.TryAcquireSyncDisposal(ref _serviceScopeSource)) {
            _serviceScopeSource.Dispose();
        }
    }

    private async Task DeinitalizeServiceScopeSourceAsync()
    {
        if (ServiceScopeSource.TryAcquireAsyncDisposal(ref _serviceScopeSource)) {
            await _serviceScopeSource.DisposeAsync();
        }
    }

    public override async ValueTask DisposeAsyncCore()
    {
        await DeinitalizeServiceScopeSourceAsync();
        await base.DisposeAsyncCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) {
            DeinitalizeServiceScopeSource();
        }

        base.Dispose(disposing);
    }

    private struct ServiceScopeSource(IServiceProvider? serviceProvider) : IDisposable, IAsyncDisposable
    {
        internal static ServiceScopeSource None { get; } = default;

        private readonly IServiceProvider? _serviceProvider = serviceProvider;
        private AsyncServiceScope? _asyncServiceScope;
        private int _state;

        public readonly AsyncServiceScope Value => _asyncServiceScope ?? throw new InvalidOperationException();

        public static bool TryAcquireInitialization(ref ServiceScopeSource source)
        {
            if ((Interlocked.Or(ref source._state, (int)States.Initialized) & (int)States.Initialized) != 0) {
                return false;
            }

            return true;
        }

        public static void Initialize(ref ServiceScopeSource source, ref ServiceScopeSource target, object caller)
        {
            if (source._serviceProvider is null) {
                throw new InvalidOperationException(
                    $"{caller.GetType()} requires a dependency injection available value of type {nameof(IValidator)}.");
            }

            source._asyncServiceScope ??= source._serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
            target = source;
        }

        public static bool TryAcquireSyncDisposal(ref ServiceScopeSource source) =>
            (Interlocked.Or(ref source._state, (int)States.SyncDisposed) & (int)(States.SyncDisposed | States.Initialized)) ==
            (int)States.Initialized;

        public static bool TryAcquireAsyncDisposal(ref ServiceScopeSource source) =>
            (Interlocked.Or(ref source._state, (int)States.AsyncDisposed) & (int)(States.AsyncDisposed | States.Initialized)) ==
            (int)States.Initialized;

        public readonly void Dispose()
        {
            if (!_asyncServiceScope.HasValue) {
                return;
            }

            Value.Dispose();
        }

        public readonly ValueTask DisposeAsync()
        {
            if (!_asyncServiceScope.HasValue) {
                return ValueTask.CompletedTask;
            }

            return Value.DisposeAsync();
        }

        [Flags]
        private enum States
        {
            Initialized = 1 << 0,
            SyncDisposed = 1 << 1,
            AsyncDisposed = 1 << 2
        }
    }
}
