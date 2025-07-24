namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public sealed class Ancestor
{
    internal static Ancestor DirectAncestor { get; } = new() { IsDirectAncestor = true };

    internal bool IsDirectAncestor { get; set; }

    internal Ancestor() { }
}
