using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Diagnostics;

internal static class InterceptionWarmupMarkers
{
    internal static readonly FieldIdentifier FieldIdentifier = new(new object(), "WARMUP");
    internal static readonly ValidationMessageStore ValidationMessageStore = new(new EditContext(new object()));
}
