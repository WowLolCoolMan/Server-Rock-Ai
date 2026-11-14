const loopbackHosts = new Set(["localhost", "127.0.0.1", "::1"]);

export const LOCAL_PROVIDERS = ["Ollama", "LM Studio", "AirLLM"] as const;
export type LocalProvider = typeof LOCAL_PROVIDERS[number];

export const LOCAL_ENDPOINT_ERROR = "Skai Desktop only connects to local services. Use localhost, 127.0.0.1, or ::1.";

export function isLocalProvider(value: string): value is LocalProvider {
  return LOCAL_PROVIDERS.includes(value as LocalProvider);
}

export function localProviderOrFallback(value: string | null | undefined, fallback: LocalProvider = "Ollama"): LocalProvider {
  return value && isLocalProvider(value) ? value : fallback;
}

export function isLocalEndpoint(value: string) {
  try {
    const url = new URL(value.trim());
    const host = url.hostname.toLowerCase().replace(/^\[|\]$/g, "");
    return (url.protocol === "http:" || url.protocol === "https:") && loopbackHosts.has(host);
  } catch {
    return false;
  }
}

export function localEndpointOrFallback(value: string | null | undefined, fallback: string) {
  return value && isLocalEndpoint(value) ? value : fallback;
}
