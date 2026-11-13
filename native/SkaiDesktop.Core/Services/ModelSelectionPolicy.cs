namespace SkaiDesktop.Core.Services;

public sealed record ModelSelectionStatus(string SelectedModel, bool IsAvailable);

public static class ModelSelectionPolicy
{
    public static ModelSelectionStatus Evaluate(string selectedModel, IEnumerable<string> availableModels)
    {
        var selected = selectedModel?.Trim() ?? string.Empty;
        var available = availableModels
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Select(model => model.Trim())
            .ToHashSet(StringComparer.Ordinal);
        return new ModelSelectionStatus(selected, selected.Length > 0 && available.Contains(selected));
    }
}
