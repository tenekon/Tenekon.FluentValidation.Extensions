using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public interface IComponentValidatorSubpathTrait
{
    public static readonly Func<ErrorContext, Exception> DefaultExceptionFactory = DefaultExceptionFactoryImpl;

    private static Exception DefaultExceptionFactoryImpl(ErrorContext errorContext) =>
        errorContext.Identifier switch {
            ErrorIdentifier.OwnEditContextAndModel => new InvalidOperationException(
                $"{errorContext.Provocateur?.GetType().Name} requires a non-null {nameof(Model)} property or a non-null {nameof(OwnEditContext)} property, but not both."),
            ErrorIdentifier.NoOwnEditContextAndNoModel => new InvalidOperationException(
                $"{errorContext.Provocateur?.GetType().Name} requires either a non-null {nameof(Model)} property or a non-null {nameof(OwnEditContext)} property."),
            _ => new Exception()
        };

    Func<ErrorContext, Exception> ExceptionFactory => DefaultExceptionFactory;

    /// <summary>
    /// Indicates having non-null <see cref="OwnEditContext"/> parameter.
    /// </summary>
    [MemberNotNullWhen(true, nameof(OwnEditContext))]
    bool HasOwnEditContextBeenSetExplicitly { get; set; }

    EditContext? OwnEditContext { get; set; }
    object? Model { get; set; }

    /// <summary>
    /// Sets <see cref="OwnEditContext"/> to <paramref name="editContext"/>.
    /// <see cref="HasOwnEditContextBeenSetExplicitly"/> is set to <c>true</c> if <paramref name="editContext"/> is not <c>null</c>, otherwise <c>false</c>.
    /// </summary>
    /// <param name="editContext">The edit context to set.</param>
    void SetOwnEditContextExplicitly(EditContext? editContext)
    {
        OwnEditContext = editContext;
        HasOwnEditContextBeenSetExplicitly = editContext is not null;
    }

    void OnSubpathParametersSet()
    {
        if (HasOwnEditContextBeenSetExplicitly && Model is not null) {
            // We have OwnEditContext and Model
            throw ExceptionFactory(new ErrorContext(this, ErrorIdentifier.OwnEditContextAndModel));
        }

        if (!HasOwnEditContextBeenSetExplicitly && Model is null) {
            // We have no OwnEditContext and no Model
            throw ExceptionFactory(new ErrorContext(this, ErrorIdentifier.NoOwnEditContextAndNoModel));
        }

        // Re-assign only if Model is not null and either OwnEditContext is null or Model differs from OwnEditContext.Model,
        // or in other words: do not re-assign if Model is null or equal to the current OwnEditContext.Model.
        if (Model is not null && !ReferenceEquals(Model, OwnEditContext?.Model)) {
            OwnEditContext = new EditContext(Model!);
        }
    }

    public sealed class ErrorContext(object? provocateur, ErrorIdentifier identifier)
    {
        public object? Provocateur { get; } = provocateur;
        public ErrorIdentifier Identifier { get; } = identifier;
    }

    public enum ErrorIdentifier
    {
        OwnEditContextAndModel,
        NoOwnEditContextAndNoModel
    }
}
