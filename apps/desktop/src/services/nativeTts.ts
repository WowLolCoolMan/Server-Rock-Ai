export type VoiceProfile = {
  id: string;
  name: string;
  description: string;
  rate: number;
  pitch: number;
};

export const voiceProfiles: VoiceProfile[] = [
  { id: "skai-neutral", name: "Skai Neutral", description: "Balanced speed and clarity.", rate: 1, pitch: 1 },
  { id: "skai-warm", name: "Skai Warm", description: "Softer pacing for longer conversations.", rate: 0.95, pitch: 1.02 },
  { id: "skai-direct", name: "Skai Direct", description: "Crisp, quicker responses.", rate: 1.08, pitch: 1 },
  { id: "skai-calm", name: "Skai Calm", description: "Slower pacing for focused listening.", rate: 0.88, pitch: 0.98 },
];

export const voiceModels = [
  { id: "os-native", name: "OS-native speech", detail: "Windows and Linux system voices", size: "No model", installed: true, lowResource: true },
  { id: "kokoro-82m-onnx", name: "Kokoro 82M ONNX", detail: "Natural multi-voice local runtime", size: "~90 MB quantized", installed: false, lowResource: false },
  { id: "pocket-tts", name: "Pocket TTS", detail: "CPU-first streaming voice runtime", size: "Model-dependent", installed: false, lowResource: false },
  { id: "pocket-tts-low-resource", name: "Pocket TTS Low Resource", detail: "Small CPU-first English runtime", size: "Small model", installed: false, lowResource: true },
] as const;

const selectedProfileKey = "skai.voice.profile";
const primaryModelKey = "skai.voice.primaryModel";
const lowResourceModelKey = "skai.voice.lowResourceModel";
const lowResourceModeKey = "skai.voice.lowResourceMode";
const voiceOutputKey = "skai.voice.outputEnabled";
let activeCloudAudio: HTMLAudioElement | null = null;
let activeCloudAudioUrl: string | null = null;

export type CloudVoiceOptions = {
  enabled: boolean;
  voiceId: string;
  modelId: string;
};

export function selectedVoiceSettings() {
  return {
    profileId: localStorage.getItem(selectedProfileKey) ?? "skai-neutral",
    primaryModelId: localStorage.getItem(primaryModelKey) ?? "os-native",
    lowResourceModelId: localStorage.getItem(lowResourceModelKey) ?? "os-native",
    lowResourceMode: localStorage.getItem(lowResourceModeKey) === "true",
    voiceOutputEnabled: localStorage.getItem(voiceOutputKey) === "true",
  };
}

export function saveVoiceSetting(key: "profileId" | "primaryModelId" | "lowResourceModelId" | "lowResourceMode" | "voiceOutputEnabled", value: string | boolean) {
  const storageKey = { profileId: selectedProfileKey, primaryModelId: primaryModelKey, lowResourceModelId: lowResourceModelKey, lowResourceMode: lowResourceModeKey, voiceOutputEnabled: voiceOutputKey }[key];
  localStorage.setItem(storageKey, String(value));
}

export function stopNativeSpeech() {
  void window.serverRockAgent?.request("voice.stop");
  if ("speechSynthesis" in window) window.speechSynthesis.cancel();
  if (activeCloudAudio) {
    activeCloudAudio.pause();
    activeCloudAudio.currentTime = 0;
    activeCloudAudio = null;
  }
  if (activeCloudAudioUrl) {
    URL.revokeObjectURL(activeCloudAudioUrl);
    activeCloudAudioUrl = null;
  }
}

export async function speakConfiguredResponse(text: string, cloudVoice: CloudVoiceOptions, force = false) {
  if (!text.trim()) return false;
  const settings = selectedVoiceSettings();
  if (!force && !settings.voiceOutputEnabled) return false;
  if (cloudVoice.enabled) {
    const response = await window.serverRockAgent?.request("voice.speak.cloud", {
      text,
      voiceId: cloudVoice.voiceId,
      modelId: cloudVoice.modelId,
      cloudVoiceEnabled: true,
    });
    if (!response?.ok) throw new Error(response?.error?.message ?? "Cloud voice could not generate audio.");
    const result = response.result as { audioBase64?: string; mediaType?: string } | undefined;
    if (!result?.audioBase64) throw new Error("Cloud voice returned no audio.");
    stopNativeSpeech();
    const binary = atob(result.audioBase64);
    const bytes = new Uint8Array(binary.length);
    for (let index = 0; index < binary.length; index += 1) bytes[index] = binary.charCodeAt(index);
    activeCloudAudioUrl = URL.createObjectURL(new Blob([bytes], { type: result.mediaType ?? "audio/mpeg" }));
    activeCloudAudio = new Audio(activeCloudAudioUrl);
    activeCloudAudio.addEventListener("ended", () => {
      if (activeCloudAudioUrl) URL.revokeObjectURL(activeCloudAudioUrl);
      activeCloudAudio = null;
      activeCloudAudioUrl = null;
    }, { once: true });
    await activeCloudAudio.play();
    return true;
  }
  const profile = voiceProfiles.find((item) => item.id === settings.profileId) ?? voiceProfiles[0];
  try {
    const native = await window.serverRockAgent?.request("voice.speak", { text, profileId: profile.id, rate: profile.rate });
    if (native?.ok) return true;
  } catch { /* Browser preview fallback below. */ }
  if (!("speechSynthesis" in window)) return false;
  window.speechSynthesis.cancel();
  const utterance = new SpeechSynthesisUtterance(text.replace(/[`*_>#]/g, "").replace(/\s+/g, " ").trim().slice(0, 4000));
  utterance.rate = profile.rate;
  utterance.pitch = profile.pitch;
  window.speechSynthesis.speak(utterance);
  return true;
}

export async function speakNativeResponse(text: string, force = false) {
  return speakConfiguredResponse(text, { enabled: false, voiceId: "", modelId: "" }, force);
}
