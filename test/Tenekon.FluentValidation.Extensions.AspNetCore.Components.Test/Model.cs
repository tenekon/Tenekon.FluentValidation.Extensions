using System.Diagnostics.CodeAnalysis;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public record Model(string? Hello = null)
{
    public string? Hello { get; set; } = Hello;

    [field: AllowNull]
    [field: MaybeNull]
    public ChildModel Child => field ??= new ChildModel();

    public record ChildModel(string? Hello = null)
    {
        public string? Hello { get; set; } = Hello;
    }
}
