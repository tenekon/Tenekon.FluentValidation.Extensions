using System.Diagnostics.CodeAnalysis;

using Tenekon.Extensions.FluentValidation.Blazor.Reflection;

namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Maps;

internal interface IFieldStateMapMutator
{
    IAccessLog AccessLog { get; }
    
    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicFields,
        "Microsoft.AspNetCore.Components.Forms.FieldState",
        "Microsoft.AspNetCore.Components.Forms")]
    void DoMutation();
}
