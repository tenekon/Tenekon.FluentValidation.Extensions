using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using FastExpressionCompiler;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal static class FieldIdentifierExtension
{
    /// <summary>
    ///     Because <see cref="FieldIdentifier.Create{TField}(Expression{Func{TField}})" /> only estimate one-level deep field names and its
    ///     corresponding model, we must extend it to support FluentValidation's one-or-more-level deep field names and its corresponding model.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="accessor"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static FieldIdentifier CreateFullAccessPath<T>(Expression<Func<T>> accessor)
    {
        var accessorBody = accessor.Body;

        // Unwrap casts to object
        if (accessorBody is UnaryExpression { NodeType: ExpressionType.Convert } unaryExpression &&
            unaryExpression.Type == typeof(object)) {
            accessorBody = unaryExpression.Operand;
        }

        if (accessorBody is MemberExpression { Expression: MemberExpression } memberExpression) {
            const string punctuationMark = ".";
            var nestedMemberExpression = memberExpression;
            var nestedMemberExpression2 = Unsafe.As<MemberExpression>(nestedMemberExpression.Expression);
            var fieldNameBuilder = new ReverseStringBuilder();

            while (true) {
                if (!fieldNameBuilder.Empty) {
                    fieldNameBuilder.InsertFront(punctuationMark);
                }

                fieldNameBuilder.InsertFront(nestedMemberExpression.Member.Name);

                if (nestedMemberExpression2.Expression is not MemberExpression) {
                    break;
                }

                nestedMemberExpression = nestedMemberExpression2;
                nestedMemberExpression2 = Unsafe.As<MemberExpression>(nestedMemberExpression2.Expression);
            }

            var fieldName = fieldNameBuilder.ToString();
            var modelLambda = Expression.Lambda(nestedMemberExpression2);
            var modelLambdaCompiled = (Func<object?>)modelLambda.CompileFast();
            var result = modelLambdaCompiled() ?? throw new ArgumentException("The provided expression must evaluate to a non-null value.");
            return new FieldIdentifier(result, fieldName);
        }

        return FieldIdentifier.Create(accessor);
    }
}
