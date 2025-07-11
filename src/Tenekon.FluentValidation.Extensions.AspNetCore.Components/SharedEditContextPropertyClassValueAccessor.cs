using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

/// <summary>
/// 
/// </summary>
/// <param name="key">Must be unique across</param>
/// <typeparam name="T"></typeparam>
internal readonly struct SharedEditContextPropertyClassValueAccessor<T>(object key) where T : class
{
    private static PropertyValue GetPropertyValue(object originalPropertyValue)
    {
        if (originalPropertyValue is not PropertyValue propertyValue) {
            throw new InvalidOperationException(
                $"A property with the same key exists, but its value is not of type {typeof(PropertyValue)}.");
        }

        if (propertyValue.Counter <= 0) {
            throw new InvalidOperationException(
                "A property with the same key and type exists, but its counter is zero or smaller and it should no longer exist.");
        }

        return propertyValue;
    }

    public bool TryGetPropertyValue(EditContext owner, [NotNullWhen(true)] out T? value)
    {
        if (!owner.Properties.TryGetValue(key, out var originalPropertyValue)) {
            goto @false;
        }

        var propertyValue = GetPropertyValue(originalPropertyValue);
        value = propertyValue.Value;
        return true;

        @false:
        value = null;
        return false;
    }

    public void OccupyProperty(EditContext owner, T value)
    {
        if (owner.Properties.TryGetValue(key, out var originalPropertyValue)) {
            var propertyValue = GetPropertyValue(originalPropertyValue);

            if (!ReferenceEquals(value, propertyValue.Value)) {
                throw new InvalidOperationException(
                    "A property with the same key and type exists, but its inner value is a different reference than the given value.");
            }

            propertyValue.Counter++;
        }

        owner.Properties[key] = new PropertyValue(value, counter: 1);
    }

    public void DisoccupyProperty(EditContext owner)
    {
        if (!owner.Properties.TryGetValue(key, out var originalPropertyValue)) {
            throw new InvalidOperationException($"A property with the key {key} does not exist");
        }

        var propertyValue = GetPropertyValue(originalPropertyValue);

        if (--propertyValue.Counter > 0) {
            return;
        }

        owner.Properties.Remove(key);
    }
    
    internal class PropertyValue(T value, int counter)
    {
        public T Value { get; set; } = value;
        internal int Counter { get; set; } = counter;
    }
}
