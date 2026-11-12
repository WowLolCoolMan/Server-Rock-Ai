// Plain-language messages for chat/image request failures shown in the conversation and
// status banner (server error vs unreachable server vs timeout, and image-rejected).
// Depends on: nothing. Used by: MainPage chat sending and Image Studio.

using System.Net;

namespace ServerRock.Core.Infrastructure;

public enum ProviderFailureKind
{
    Unknown,
    EndpointUnavailable,
    ServerRejected,
    ModelCompatibility,
    ImageRejected,
    Timeout
}

public static class ProviderErrorText
{
    public static ProviderFailureKind Classify(Exception ex, bool hasImageAttachments = false)
    {
        if (ex is TaskCanceledException) return ProviderFailureKind.Timeout;
        if (hasImageAttachments && ex is HttpRequestException { StatusCode: HttpStatusCode.BadRequest or HttpStatusCode.UnsupportedMediaType or HttpStatusCode.UnprocessableEntity })
            return ProviderFailureKind.ImageRejected;
        if (ex is HttpRequestException { StatusCode: not null }) return ProviderFailureKind.ServerRejected;
        if (ex is HttpRequestException) return ProviderFailureKind.EndpointUnavailable;
        if (ex is InvalidOperationException && ex.Message.Contains("closed the stream", StringComparison.OrdinalIgnoreCase))
            return ProviderFailureKind.ModelCompatibility;
        return ProviderFailureKind.Unknown;
    }

    public static string Describe(Exception ex)
    {
        if (ex is HttpRequestException { StatusCode: not null }) return $"Model server error: {ex.Message}";
        if (ex is HttpRequestException) return $"Could not reach the model server. Start it, then check its address in Settings.\n\nDetails: {ex.Message}";
        if (ex is TaskCanceledException) return "The request timed out or was interrupted.";
        return ex.Message;
    }

    /// <summary>Like <see cref="Describe(Exception)"/>, but explains a 400/415/422 as "this endpoint can't take images".</summary>
    public static string Describe(Exception ex, bool hasImageAttachments)
    {
        if (hasImageAttachments && ex is HttpRequestException { StatusCode: HttpStatusCode.BadRequest or HttpStatusCode.UnsupportedMediaType or HttpStatusCode.UnprocessableEntity })
            return "This model endpoint rejected the image. Choose a vision-capable model and an endpoint that accepts OpenAI-compatible image_url content. The image remains in this saved conversation.";
        return Describe(ex);
    }
}
