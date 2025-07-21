using System.Globalization;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class FieldIdentifierHelper
{
    private static readonly char[] s_seperators = ['.', '['];

    // Reference implementation: (https://blog.stevensanderson.com/2019/09/04/blazor-fluentvalidation/)
    /// <summary>
    ///     This method parses property paths like 'SomeProp.MyCollection[123].ChildProp' and returns a FieldIdentifier which is an(instance,
    ///     propName) pair.For example, it would return the pair(SomeProp.MyCollection[123], "ChildProp").It traverses as far into the propertyPath
    ///     as it can go until it finds any null instance.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="propertyPath"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static FieldIdentifier DeriveFieldIdentifier(object model, string propertyPath)
    {
        var currentModel = model;

        while (true) {
            var nextTokenEnd = propertyPath.IndexOfAny(s_seperators);
            if (nextTokenEnd <= 0) {
                return new FieldIdentifier(currentModel, propertyPath);
            }

            var nextToken = propertyPath.Substring(startIndex: 0, nextTokenEnd);
            propertyPath = propertyPath.Substring(nextTokenEnd + 1);

            object? newModel;
            if (nextToken.EndsWith("]")) {
                // It's an indexer
                // This code assumes C# conventions (one indexer named Item with one param)
                nextToken = nextToken.Substring(startIndex: 0, nextToken.Length - 1);

                var prop = currentModel.GetType()
                    .GetProperties()
                    .FirstOrDefault(e => e.Name == "Item" && e.GetIndexParameters().Length == 1) ?? currentModel.GetType()
                    .GetInterfaces()
                    .FirstOrDefault(e => (e.IsGenericType && e.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)) ||
                        e.GetGenericTypeDefinition() == typeof(IList<>))
                    ?.GetProperty("Item"); //e.g. arrays

                if (prop is not null) {
                    // we've got an Item property
                    var indexerType = prop.GetIndexParameters()[0].ParameterType;
                    var indexerValue = Convert.ChangeType(nextToken, indexerType, CultureInfo.InvariantCulture);
                    newModel = prop.GetValue(currentModel, [indexerValue]);
                } else if (currentModel is IEnumerable<object> objEnumerable &&
                           int.TryParse(nextToken, out var indexerValue)) //e.g. hashset
                {
                    newModel = objEnumerable.ElementAt(indexerValue);
                } else {
                    throw new InvalidOperationException($"Could not find indexer on object of type {currentModel.GetType().FullName}.");
                }
            } else {
                // It's a regular property
                var prop = currentModel.GetType().GetProperty(nextToken);
                if (prop == null) {
                    throw new InvalidOperationException(
                        $"Could not find property named {nextToken} on object of type {currentModel.GetType().FullName}.");
                }

                newModel = prop.GetValue(currentModel);
            }

            if (newModel == null) {
                // This is as far as we can go
                return new FieldIdentifier(currentModel, nextToken);
            }

            currentModel = newModel;
        }
    }
}
