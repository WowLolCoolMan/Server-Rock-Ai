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
}

export async function speakNativeResponse(text: string, force = false) {
  if (!text.trim()) return false;
  const settings = selectedVoiceSettings();
  if (!force && !settings.voiceOutputEnabled) return false;
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
