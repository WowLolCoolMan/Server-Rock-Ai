namespace SkaiDesktop.Core.Services;

/// <summary>Evaluates the only currently executable workflow node: a pure,
/// bounded condition over caller-provided local values.</summary>
public static class ReadOnlyConditionExecutor
{
    public static bool Evaluate(IReadOnlyDictionary<string, string> config, IReadOnlyDictionary<string, string> inputs)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(inputs);
        if (!config.TryGetValue("field", out var field) || string.IsNullOrWhiteSpace(field) || field.Length > 128 || field.Any(char.IsControl))
            throw new InvalidDataException("A condition needs a bounded field name.");
        if (!config.TryGetValue("equals", out var expected) || expected.Length > 4_000 || expected.Any(char.IsControl))
            throw new InvalidDataException("A condition needs a bounded expected value.");
        if (inputs.Count > 64 || inputs.Any(item => item.Key.Length > 128 || item.Value.Length > 4_000 || item.Key.Any(char.IsControl) || item.Value.Any(char.IsControl)))
            throw new InvalidDataException("Condition inputs exceed the safety limit.");
        return inputs.TryGetValue(field, out var actual) && string.Equals(actual, expected, StringComparison.Ordinal);
    }
}
