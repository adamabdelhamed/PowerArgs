namespace PowerArgs;

/// <summary>Converts a generated collection argument without reflected collection construction.</summary>
public delegate object GeneratedCollectionReviver(string name, string firstValue, ReadOnlySpan<string> additionalValues);

/// <summary>Runtime support for collection converters emitted by PowerArgs.Generator.</summary>
public static class GeneratedCollectionParser
{
    public static object ReviveArray<T>(string name, string firstValue, ReadOnlySpan<string> additionalValues)
    {
        var elementName = name + "_element";
        if (additionalValues.Length > 0)
        {
            var result = new T[additionalValues.Length + 1];
            result[0] = (T)ArgRevivers.Revive(typeof(T), elementName, firstValue);
            for (var i = 0; i < additionalValues.Length; i++) result[i + 1] = (T)ArgRevivers.Revive(typeof(T), elementName, additionalValues[i]);
            return result;
        }

        if (string.IsNullOrWhiteSpace(firstValue)) return Array.Empty<T>();
        var elements = firstValue.Split(',');
        var values = new T[elements.Length];
        for (var i = 0; i < elements.Length; i++) values[i] = (T)ArgRevivers.Revive(typeof(T), elementName, elements[i]);
        return values;
    }

    public static object ReviveList<T>(string name, string firstValue, ReadOnlySpan<string> additionalValues)
    {
        var elementName = name + "_element";
        if (additionalValues.Length > 0)
        {
            var result = new List<T>(additionalValues.Length + 1) { (T)ArgRevivers.Revive(typeof(T), elementName, firstValue) };
            foreach (var element in additionalValues) result.Add((T)ArgRevivers.Revive(typeof(T), elementName, element));
            return result;
        }

        if (string.IsNullOrWhiteSpace(firstValue)) return new List<T>();
        var elements = firstValue.Split(',');
        var values = new List<T>(elements.Length);
        foreach (var element in elements) values.Add((T)ArgRevivers.Revive(typeof(T), elementName, element));
        return values;
    }
}
