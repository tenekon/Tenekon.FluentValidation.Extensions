using Microsoft.AspNetCore.Components.Forms;
using Tenekon.Extensions.FluentValidation.Blazor.Interception.Maps;

namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Mutators.Root;

internal static class RootFieldStateMapMutatorFactory
{
    internal static readonly Func<EditContext, IFieldStateMapMutator> Create =
        static editContext => new RootFieldStateMapMutator(editContext);
}
