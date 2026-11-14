import { useEffect, useRef, useState, type PointerEvent } from "react";
import type { PetCompletionEvent, PetMotionSettings } from "../../../shared/contracts";

const WALK_SHEET = new URL("../../../../../assets/pets/percy-walk-frames.png", import.meta.url).href;
const SUCCESS_SHEET = new URL("../../../../../assets/pets/percy-success-frames.png", import.meta.url).href;

type PetPose = "walk" | "success";

export function PetWindow() {
  const [pose, setPose] = useState<PetPose>("walk");
  const [bubble, setBubble] = useState<string | null>(null);
  const [motion, setMotion] = useState<PetMotionSettings>({ wander: true, reduceMotion: false });
  const dragging = useRef(false);
  const bubbleTimer = useRef<number | undefined>(undefined);
  const poseTimer = useRef<number | undefined>(undefined);

  useEffect(() => {
    document.title = "Skai companion";
    document.documentElement.dataset.petWindow = "true";
    document.body.dataset.petWindow = "true";
    const pet = window.serverRockAgent?.pet;
    if (!pet) return () => { delete document.documentElement.dataset.petWindow; delete document.body.dataset.petWindow; };
    const showCompletion = (event: PetCompletionEvent) => {
      setBubble(event.text);
      setPose("success");
      window.clearTimeout(bubbleTimer.current);
      window.clearTimeout(poseTimer.current);
      bubbleTimer.current = window.setTimeout(() => setBubble(null), 6500);
      poseTimer.current = window.setTimeout(() => setPose("walk"), 1500);
    };
    const removeMessage = pet.onMessage(showCompletion);
    const removeMotion = pet.onMotionChange(setMotion);
    return () => {
      removeMessage();
      removeMotion();
      window.clearTimeout(bubbleTimer.current);
      window.clearTimeout(poseTimer.current);
      delete document.documentElement.dataset.petWindow;
      delete document.body.dataset.petWindow;
    };
  }, []);

  const sheet = pose === "success" ? SUCCESS_SHEET : WALK_SHEET;
  const animationPaused = motion.reduceMotion || !motion.wander || pose === "success";
  const beginDrag = (event: PointerEvent<HTMLElement>) => {
    if (event.target instanceof HTMLElement && event.target.closest("button")) return;
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
  return <main className="pet-window-root" aria-label="Skai companion" onPointerDown={beginDrag} onPointerMove={moveDrag} onPointerUp={endDrag} onPointerCancel={endDrag}>
    {bubble && <div className="pet-window-bubble" role="status">{bubble}</div>}
    <button className="pet-window-hide" type="button" onClick={() => window.serverRockAgent?.pet?.hide()} aria-label="Hide Skai companion">×</button>
    <div className={`pet-window-sprite pet-window-sprite-${pose}${animationPaused ? " is-paused" : ""}`} style={{ backgroundImage: `url(${sheet})` }} role="img" aria-label="Animated Skai companion" />
    <span className="pet-window-caption">{pose === "success" ? "Finished" : motion.wander && !motion.reduceMotion ? "Walking around" : "Drag me"}</span>
  </main>;
}
