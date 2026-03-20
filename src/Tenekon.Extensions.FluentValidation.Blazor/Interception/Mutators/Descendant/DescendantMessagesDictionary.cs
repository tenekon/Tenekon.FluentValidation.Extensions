using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Mutators.Descendant;

internal sealed class DescendantMessagesDictionary(IEqualityComparer<FieldIdentifier> equalityComparer)
    : Dictionary<FieldIdentifier, List<string>>(equalityComparer);
