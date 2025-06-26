using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal interface IComponentValidatorSubpathTrait
{
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
    void SetOwnEditContextExplictly(EditContext? editContext)
    {
        OwnEditContext = editContext;
        HasOwnEditContextBeenSetExplicitly = editContext is not null;
    }

    void OnSubpathParametersSet()
    {
        if (HasOwnEditContextBeenSetExplicitly && Model is not null) {
            // We have OwnEditContext and Model
            throw new InvalidOperationException(
                $"{GetType().Name} requires a non-null {nameof(Model)} parameter or a non-null {nameof(OwnEditContext)} parameter, but not both.");
        }

        if (!HasOwnEditContextBeenSetExplicitly && Model is null) {
            // We have no OwnEditContext and no Model
            throw new InvalidOperationException(
                $"{GetType().Name} requires either a non-null {nameof(Model)} parameter or a non-null {nameof(OwnEditContext)} parameter.");
        }

        // Re-assign only if Model is not null and either OwnEditContext is null or Model differs from OwnEditContext.Model,
        // or in other words: do not re-assign if Model is null or equal to the current OwnEditContext.Model.
        if (Model is not null && !ReferenceEquals(Model, OwnEditContext?.Model)) {
            OwnEditContext = new EditContext(Model!);
        }
    }
}
