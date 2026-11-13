using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class AirLlmProviderTests
{
    public static void Run()
    {
        Check(AirLlmProvider.IsAirLlmProvider("AirLLM") && AirLlmProvider.IsHuggingFaceModel("Qwen/Qwen3-32B"), "AirLLM recognizes built-in provider and Hugging Face model IDs");
        Check(!AirLlmProvider.IsHuggingFaceModel("llama3.2") && !AirLlmProvider.IsHuggingFaceModel("https://example.test/model"), "AirLLM rejects endpoint and tag values as model IDs");
        Check(new AirLlmProvider().CanHandle(new AppSettings { Model = "Qwen/Qwen3-32B", AirLlmAutoFallbackEnabled = true }), "AirLLM auto fallback accepts a Hugging Face model");
        Check(!new AirLlmProvider().CanHandle(new AppSettings { Model = "llama3.2", AirLlmAutoFallbackEnabled = true }), "AirLLM auto fallback does not guess a mapping for an Ollama tag");
        Check(AirLlmProvider.IsCapacityFailure(new InvalidOperationException("CUDA out of memory while loading model")), "AirLLM identifies local capacity failures");
        Check(!AirLlmProvider.IsCapacityFailure(new InvalidOperationException("invalid API key")), "AirLLM does not classify unrelated provider failures as capacity failures");
    }
}
