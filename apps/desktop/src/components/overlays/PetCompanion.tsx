import { useEffect, useRef, useState, type KeyboardEvent } from "react";

const WALK_FRAME = new URL("../../../../../assets/pets/byte-walk-1.png", import.meta.url).href;
const POSITION_KEY = "skai.pet.position";
type Position = { x: number; y: number };

function clampPosition(position: Position): Position {
  const width = typeof window === "undefined" ? 1200 : window.innerWidth;
  const height = typeof window === "undefined" ? 800 : window.innerHeight;
  return { x: Math.max(12, Math.min(position.x, width - 96)), y: Math.max(12, Math.min(position.y, height - 110)) };
}

function initialPosition(): Position {
  try {
    const stored = JSON.parse(localStorage.getItem(POSITION_KEY) ?? "null") as Position | null;
    if (stored && Number.isFinite(stored.x) && Number.isFinite(stored.y)) return clampPosition(stored);
  } catch { /* Fall back to the lower-right corner. */ }
  return clampPosition({ x: (typeof window === "undefined" ? 1100 : window.innerWidth) - 118, y: (typeof window === "undefined" ? 700 : window.innerHeight) - 132 });
}

export function PetCompanion({ visible, wander, reduceMotion, onHide }: { visible: boolean; wander: boolean; reduceMotion: boolean; onHide: () => void }) {
  const [position, setPosition] = useState(initialPosition);
  const [dragging, setDragging] = useState(false);
  const dragOffset = useRef({ x: 0, y: 0 });
  const target = useRef<Position | null>(null);

  useEffect(() => { localStorage.setItem(POSITION_KEY, JSON.stringify(position)); }, [position]);
  useEffect(() => {
    const onResize = () => setPosition((current) => clampPosition(current));
    window.addEventListener("resize", onResize);
    return () => window.removeEventListener("resize", onResize);
  }, []);
  useEffect(() => {
    if (!visible || !wander || reduceMotion || dragging) return;
    const timer = window.setInterval(() => {
      if (!target.current || Math.hypot(target.current.x - position.x, target.current.y - position.y) < 14) {
        target.current = { x: 30 + Math.random() * Math.max(30, window.innerWidth - 150), y: 70 + Math.random() * Math.max(30, window.innerHeight - 190) };
      }
      setPosition((current) => {
        const nextTarget = target.current;
        if (!nextTarget) return current;
        const distance = Math.max(1, Math.hypot(nextTarget.x - current.x, nextTarget.y - current.y));
        return clampPosition({ x: current.x + ((nextTarget.x - current.x) / distance) * 2.2, y: current.y + ((nextTarget.y - current.y) / distance) * 1.3 });
      });
    }, 120);
    return () => window.clearInterval(timer);
  }, [visible, wander, reduceMotion, dragging, position.x, position.y]);

  if (!visible) return null;
  const moveWithKeyboard = (event: KeyboardEvent<HTMLButtonElement>) => {
    const distance = event.shiftKey ? 28 : 10;
    const delta = event.key === "ArrowLeft" ? { x: -distance, y: 0 } : event.key === "ArrowRight" ? { x: distance, y: 0 } : event.key === "ArrowUp" ? { x: 0, y: -distance } : event.key === "ArrowDown" ? { x: 0, y: distance } : null;
    if (!delta) return;
    event.preventDefault();
    setPosition((current) => clampPosition({ x: current.x + delta.x, y: current.y + delta.y }));
  };
  return <div className={`pet-companion${dragging ? " is-dragging" : ""}`} style={{ left: position.x, top: position.y }}>
    <button className="pet-companion-dismiss" type="button" onClick={onHide} aria-label="Hide Skai pet">×</button>
    <button className="pet-companion-button" type="button" aria-label="Skai pet. Drag to move, or use arrow keys." onKeyDown={moveWithKeyboard} onPointerDown={(event) => { const bounds = event.currentTarget.getBoundingClientRect(); dragOffset.current = { x: event.clientX - bounds.left, y: event.clientY - bounds.top }; event.currentTarget.setPointerCapture(event.pointerId); setDragging(true); target.current = null; }} onPointerMove={(event) => { if (dragging) setPosition(clampPosition({ x: event.clientX - dragOffset.current.x, y: event.clientY - dragOffset.current.y })); }} onPointerUp={(event) => { if (event.currentTarget.hasPointerCapture(event.pointerId)) event.currentTarget.releasePointerCapture(event.pointerId); setDragging(false); }} onPointerCancel={() => setDragging(false)}>
      <img className="pet-companion-image" src={WALK_FRAME} alt="" draggable="false" />
    </button>
    <span className="pet-companion-hint">{dragging ? "Drop me anywhere" : wander && !reduceMotion ? "Walking around" : "Drag me"}</span>
  </div>;
}
