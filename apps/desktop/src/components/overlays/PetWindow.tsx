import { useEffect, useRef, useState, type PointerEvent } from "react";
import type { PetCompletionEvent, PetMotionSettings } from "../../../shared/contracts";
import { NavIcon } from "../ui/NavIcon";

const WALK_SHEET = new URL("../../../../../assets/pets/percy-walk-frames.png", import.meta.url).href;
const IDLE_SHEET = new URL("../../../../../assets/pets/percy-idle-frames.png", import.meta.url).href;
const SUCCESS_SHEET = new URL("../../../../../assets/pets/percy-success-frames.png", import.meta.url).href;
const ERROR_SHEET = new URL("../../../../../assets/pets/percy-error-frames.png", import.meta.url).href;

type PetPose = "idle" | "walk" | "success" | "error";

export function PetWindow() {
  const [pose, setPose] = useState<PetPose>("walk");
  const [bubble, setBubble] = useState<string | null>(null);
  const [bubbleKind, setBubbleKind] = useState<PetCompletionEvent["kind"]>("success");
  const [motion, setMotion] = useState<PetMotionSettings>({ wander: true, reduceMotion: false });
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
    const pet = window.serverRockAgent?.pet;
    if (!pet) return () => { delete document.documentElement.dataset.petWindow; delete document.body.dataset.petWindow; };
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
    };
  }, []);

  const activePose = pose === "success" || pose === "error" ? pose : motion.wander ? "walk" : "idle";
  const sheet = activePose === "success" ? SUCCESS_SHEET : activePose === "error" ? ERROR_SHEET : activePose === "idle" ? IDLE_SHEET : WALK_SHEET;
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
