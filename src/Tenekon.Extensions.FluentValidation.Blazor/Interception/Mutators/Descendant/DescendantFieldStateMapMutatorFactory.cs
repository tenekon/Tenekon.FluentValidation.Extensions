using Microsoft.AspNetCore.Components.Forms;
using Tenekon.Extensions.FluentValidation.Blazor.Interception.Maps;

namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Mutators.Descendant;

internal static class DescendantFieldStateMapMutatorFactory
{
    public static IFieldStateMapMutator Create(EditContext editContext)
        => new DescendantFieldStateMapMutator(editContext);
}
