using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Mutators.Root;

internal sealed class RootMessagesDictionary(IEqualityComparer<FieldIdentifier> equalityComparer)
    : Dictionary<FieldIdentifier, List<string>>(equalityComparer);
