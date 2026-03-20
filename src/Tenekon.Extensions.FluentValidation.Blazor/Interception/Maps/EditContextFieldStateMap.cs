using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Maps;

internal class EditContextFieldStateMap<TFieldState>(IEqualityComparer<FieldIdentifier> equalityComparer)
    : Dictionary<FieldIdentifier, TFieldState>(equalityComparer) where TFieldState : class;
