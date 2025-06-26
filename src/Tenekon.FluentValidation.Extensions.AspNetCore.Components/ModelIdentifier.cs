using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using FastExpressionCompiler;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

/// <summary>
///     Uniquely identifies a single field that can be edited. This may correspond to a property on a model object, or can be any other
///     named value.
/// </summary>
public readonly struct ModelIdentifier : IEquatable<ModelIdentifier>
{
    /// <summary>Initializes a new instance of the <see cref="ModelIdentifier" /> structure.</summary>
    /// <param name="accessor">An expression that identifies an object member.</param>
    /// <typeparam name="TField">The field <see cref="Type" />.</typeparam>
    public static ModelIdentifier Create<TField>(Expression<Func<TField>> accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        ParseAccessor(accessor, out var model);
        return new ModelIdentifier(model);
    }

    /// <summary>Initializes a new instance of the <see cref="ModelIdentifier" /> structure.</summary>
    /// <param name="model">The object that owns the field.</param>
    /// <param name="fieldName">The name of the editable field.</param>
    public ModelIdentifier(object model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.GetType().IsValueType) {
            throw new ArgumentException("The model must be a reference-typed object.", nameof(model));
        }

        Model = model;
    }

    /// <summary>Gets the object that owns the editable field.</summary>
    public object Model { get; }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // We want to compare Model instances by reference. RuntimeHelpers.GetHashCode returns identical hashes for equal object references (ignoring any `Equals`/`GetHashCode` overrides) which is what we want.
        var modelHash = RuntimeHelpers.GetHashCode(Model);
        return modelHash;
    }

    /// <inheritdoc />
    public bool Equals(ModelIdentifier otherIdentifier) => ReferenceEquals(otherIdentifier.Model, Model);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ModelIdentifier otherIdentifier && Equals(otherIdentifier);

    private static void ParseAccessor<T>(Expression<Func<T>> accessor, out object model)
    {
        var accessorBody = accessor.Body;

        // Unwrap casts to object
        if (accessorBody is UnaryExpression { NodeType: ExpressionType.Convert } unaryExpression &&
            unaryExpression.Type == typeof(object)) {
            accessorBody = unaryExpression.Operand;
        }

        switch (accessorBody) {
            case MemberExpression memberExpression:
                switch (memberExpression.Expression) {
                    case ConstantExpression { Value: null }:
                        throw new ArgumentException("The provided expression must evaluate to a non-null value.");
                    case ConstantExpression { Value: not null } constant:
                        model = constant.Value;
                        break;
                    case not null:
                        // It would be great to cache this somehow, but it's unclear there's a reasonable way to do
                        // so, given that it embeds captured values such as "this". We could consider special-casing
                        // for "() => something.Member" and building a cache keyed by "something.GetType()" with values
                        // of type Func<object, object> so we can cheaply map from "something" to "something.Member".
                        var modelLambda = Expression.Lambda(memberExpression);
                        var modelLambdaCompiled = (Func<object?>)modelLambda.CompileFast();
                        var result = modelLambdaCompiled() ??
                            throw new ArgumentException("The provided expression must evaluate to a non-null value.");
                        model = result;
                        break;
                    default:
                        throw new ArgumentException(
                            $"The provided expression contains a {accessorBody.GetType().Name} which is not supported. {nameof(ModelIdentifier)} only supports simple member accessors (fields, properties) of an object.");
                }

                break;
            default:
                throw new ArgumentException(
                    $"The provided expression contains a {accessorBody.GetType().Name} which is not supported. {nameof(ModelIdentifier)} only supports simple member accessors (fields, properties) of an object.");
        }
    }
}
