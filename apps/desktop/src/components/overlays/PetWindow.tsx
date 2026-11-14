import { useEffect, useRef, useState, type PointerEvent } from "react";
import type { PetCompletionEvent, PetMotionSettings } from "../../../shared/contracts";
import { NavIcon } from "../ui/NavIcon";

const WALK_SHEET = new URL("../../../../../assets/pets/percy-walk-frames.png", import.meta.url).href;
const IDLE_SHEET = new URL("../../../../../assets/pets/percy-idle-frames.png", import.meta.url).href;
const SUCCESS_SHEET = new URL("../../../../../assets/pets/percy-success-frames.png", import.meta.url).href;
const ERROR_SHEET = new URL("../../../../../assets/pets/percy-error-frames.png", import.meta.url).href;
const BUILTIN_SHEETS: Record<string, Record<PetPose, string>> = {
  percy: { idle: IDLE_SHEET, walk: WALK_SHEET, success: SUCCESS_SHEET, error: ERROR_SHEET },
  byte: { idle: new URL("../../../../../assets/pets/byte-idle-frames.png", import.meta.url).href, walk: new URL("../../../../../assets/pets/byte-walk-frames.png", import.meta.url).href, success: new URL("../../../../../assets/pets/byte-success-frames.png", import.meta.url).href, error: new URL("../../../../../assets/pets/byte-error-frames.png", import.meta.url).href },
  ember: { idle: new URL("../../../../../assets/pets/ember-idle-frames.png", import.meta.url).href, walk: new URL("../../../../../assets/pets/ember-walk-frames.png", import.meta.url).href, success: new URL("../../../../../assets/pets/ember-success-frames.png", import.meta.url).href, error: new URL("../../../../../assets/pets/ember-error-frames.png", import.meta.url).href },
  nova: { idle: new URL("../../../../../assets/pets/nova-idle-frames.png", import.meta.url).href, walk: new URL("../../../../../assets/pets/nova-walk-frames.png", import.meta.url).href, success: new URL("../../../../../assets/pets/nova-success-frames.png", import.meta.url).href, error: new URL("../../../../../assets/pets/nova-error-frames.png", import.meta.url).href },
  rocky: { idle: new URL("../../../../../assets/pets/rocky-idle-frames.png", import.meta.url).href, walk: new URL("../../../../../assets/pets/rocky-walk-frames.png", import.meta.url).href, success: new URL("../../../../../assets/pets/rocky-success-frames.png", import.meta.url).href, error: new URL("../../../../../assets/pets/rocky-error-frames.png", import.meta.url).href },
  sprout: { idle: new URL("../../../../../assets/pets/sprout-idle-frames.png", import.meta.url).href, walk: new URL("../../../../../assets/pets/sprout-walk-frames.png", import.meta.url).href, success: new URL("../../../../../assets/pets/sprout-success-frames.png", import.meta.url).href, error: new URL("../../../../../assets/pets/sprout-error-frames.png", import.meta.url).href },
};

type PetPose = "idle" | "walk" | "success" | "error";

export function PetWindow() {
  const [pose, setPose] = useState<PetPose>("walk");
  const [bubble, setBubble] = useState<string | null>(null);
  const [bubbleKind, setBubbleKind] = useState<PetCompletionEvent["kind"]>("success");
  const [motion, setMotion] = useState<PetMotionSettings>({ wander: true, reduceMotion: false });
  const [character, setCharacter] = useState(() => localStorage.getItem("skai.pet.character") ?? "percy");
  const [customSprite, setCustomSprite] = useState(() => localStorage.getItem("skai.pet.sprite") ?? "");
  const dragging = useRef(false);
  const motionRef = useRef(motion);
  const bubbleTimer = useRef<number | undefined>(undefined);
  const poseTimer = useRef<number | undefined>(undefined);

  useEffect(() => {
    motionRef.current = motion;
  }, [motion]);

  useEffect(() => {
    document.title = "Percy · Skai companion";
    document.documentElement.dataset.petWindow = "true";
    document.body.dataset.petWindow = "true";
    const syncSettings = () => { setCharacter(localStorage.getItem("skai.pet.character") ?? "percy"); setCustomSprite(localStorage.getItem("skai.pet.sprite") ?? ""); };
    window.addEventListener("storage", syncSettings);
    const pet = window.serverRockAgent?.pet;
    if (!pet) return () => { window.removeEventListener("storage", syncSettings); delete document.documentElement.dataset.petWindow; delete document.body.dataset.petWindow; };
    const showCompletion = (event: PetCompletionEvent) => {
      setBubble(event.text);
      setBubbleKind(event.kind);
      setPose(event.kind);
      window.clearTimeout(bubbleTimer.current);
      window.clearTimeout(poseTimer.current);
      bubbleTimer.current = window.setTimeout(() => setBubble(null), 6500);
      poseTimer.current = window.setTimeout(() => setPose(motionRef.current.wander ? "walk" : "idle"), 1500);
    };
    const removeMessage = pet.onMessage(showCompletion);
    const removeMotion = pet.onMotionChange(setMotion);
    const cancelDrag = () => {
      if (!dragging.current) return;
      dragging.current = false;
      pet.endDrag();
    };
    window.addEventListener("blur", cancelDrag);
    return () => {
      removeMessage();
      removeMotion();
      window.removeEventListener("blur", cancelDrag);
      cancelDrag();
      window.clearTimeout(bubbleTimer.current);
      window.clearTimeout(poseTimer.current);
      delete document.documentElement.dataset.petWindow;
      delete document.body.dataset.petWindow;
      window.removeEventListener("storage", syncSettings);
    };
  }, []);

  const activePose = pose === "success" || pose === "error" ? pose : motion.wander ? "walk" : "idle";
  const sheet = customSprite || (BUILTIN_SHEETS[character] ?? BUILTIN_SHEETS.percy)[activePose];
  const animationPaused = motion.reduceMotion;
  const beginDrag = (event: PointerEvent<HTMLElement>) => {
    if (event.button !== 0 || event.target instanceof Element && event.target.closest("button")) return;
    event.preventDefault();
    dragging.current = true;
    event.currentTarget.setPointerCapture?.(event.pointerId);
    window.serverRockAgent?.pet?.beginDrag({ x: event.screenX, y: event.screenY });
  };
  const moveDrag = (event: PointerEvent<HTMLElement>) => {
    if (!dragging.current) return;
    window.serverRockAgent?.pet?.moveDrag({ x: event.screenX, y: event.screenY });
  };
  const endDrag = (event: PointerEvent<HTMLElement>) => {
    if (!dragging.current) return;
    dragging.current = false;
    if (event.currentTarget.hasPointerCapture?.(event.pointerId)) event.currentTarget.releasePointerCapture(event.pointerId);
    window.serverRockAgent?.pet?.endDrag();
  };
  return <main className="pet-window-root" aria-label="Percy desktop companion. Drag to move." data-pose={activePose} onPointerDown={beginDrag} onPointerMove={moveDrag} onPointerUp={endDrag} onPointerCancel={endDrag}>
    {bubble && <div className={`pet-window-bubble pet-window-bubble-${bubbleKind}`} role="status" aria-live="polite">{bubble}</div>}
    <button className="pet-window-hide" type="button" onClick={() => window.serverRockAgent?.pet?.hide()} aria-label="Hide Percy companion"><NavIcon name="close" /></button>
    <div className={`pet-window-sprite pet-window-sprite-${activePose}${animationPaused ? " is-paused" : ""}`} style={{ backgroundImage: `url(${sheet})` }} role="img" aria-label="Animated Percy companion" />
  </main>;
}
