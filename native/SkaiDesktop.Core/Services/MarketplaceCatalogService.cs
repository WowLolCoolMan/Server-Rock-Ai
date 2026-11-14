using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>
/// Describes capabilities that are already understood by the local runtime.
/// Remote model discovery is handled separately by ModelLibraryService so the
/// catalog never presents an install action without a matching adapter.
/// </summary>
public static class MarketplaceCatalogService
{
    public static object Snapshot() => new
    {
        providers = new[]
        {
            new { id = "huggingface", name = "Hugging Face", kinds = new[] { "llm", "voice", "image" }, remote = true },
            new { id = "modelscope", name = "ModelScope", kinds = new[] { "llm" }, remote = true },
            new { id = "ollama", name = "Ollama", kinds = new[] { "llm" }, remote = false },
            new { id = "lm-studio", name = "LM Studio", kinds = new[] { "llm" }, remote = false },
            new { id = "comfyui", name = "ComfyUI", kinds = new[] { "image" }, remote = false },
            new { id = "github", name = "GitHub", kinds = new[] { "skill", "tool" }, remote = true },
        },
        voices = VoiceModelCatalog.All.Select(voice => new
        {
            id = voice.Id,
            name = voice.Name,
            description = voice.Description,
            runtime = voice.Runtime,
            license = voice.License,
            size = voice.Size,
            installed = voice.IsInstalled,
            lowResource = voice.IsLowResource,
            sourceUrl = voice.SourceUrl,
            installable = !string.IsNullOrWhiteSpace(voice.SourceUrl) && !voice.IsInstalled,
        }).ToArray(),
        skills = SkillCatalogService.BuiltIns.Select(skill => new
        {
            id = skill.Id,
            name = skill.Name,
            description = skill.Description,
            category = skill.Category,
            permissions = skill.RequiredPermissions,
            source = "Skai built-in",
            installed = true,
            enabled = true,
            installable = false,
        }).ToArray(),
        tools = PluginCatalogService.Search().Select(tool => new
        {
            id = tool.Id,
            name = tool.Name,
            description = tool.Description,
            category = tool.Category,
            access = tool.Access,
            builtIn = tool.IsBuiltIn,
            status = tool.StatusLabel,
            source = tool.WhereToFind,
            installable = false,
        }).ToArray(),
    };
}
