using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

public sealed record ModelComparisonPlan(string Prompt, IReadOnlyList<PinnedModelConnection> Models);

/// <summary>Validates a bounded comparison request before any model request is sent.</summary>
public static class ModelComparisonPolicy
{
    public const int MinModels = 2;
    public const int MaxModels = 4;
    public const int MaxPromptCharacters = 8_000;

    public static ModelComparisonPlan Create(string prompt, IEnumerable<PinnedModelConnection> models, bool requireLocalEndpoints)
    {
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > MaxPromptCharacters || prompt.Any(char.IsControl))
            throw new ArgumentException("The comparison prompt is invalid.", nameof(prompt));
        var selected = models
            .Where(model => model is not null)
            .DistinctBy(model => (model.Model?.Trim() ?? string.Empty) + "\n" + (model.Endpoint?.Trim() ?? string.Empty), StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (selected.Count is < MinModels or > MaxModels)
            throw new ArgumentException($"Choose between {MinModels} and {MaxModels} distinct pinned models.", nameof(models));
        foreach (var model in selected)
        {
            var validation = ConnectionSettingsValidator.Validate(model.Endpoint, model.Model, requireLocalEndpoints);
            if (!validation.IsValid) throw new ArgumentException($"{model.Model}: {validation.Error}", nameof(models));
        }
        return new ModelComparisonPlan(prompt.Trim(), selected);
    }
}
