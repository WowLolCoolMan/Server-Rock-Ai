import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { Button } from "./Primitives";

export type OverflowMenuItem = {
  label: string;
  onSelect: () => void;
  disabled?: boolean;
};

export function OverflowMenu({ items, label = "More" }: { items: OverflowMenuItem[]; label?: string }) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const firstItemRef = useRef<HTMLButtonElement>(null);

  const close = (restoreFocus = true) => {
    setOpen(false);
    if (restoreFocus) window.requestAnimationFrame(() => triggerRef.current?.focus());
  };

  useEffect(() => {
    if (!open) return;
    firstItemRef.current?.focus();
    const closeOnOutsidePointer = (event: PointerEvent) => {
      const target = event.target;
      if (target instanceof Element && rootRef.current?.contains(target)) return;
      close(false);
    };
    document.addEventListener("pointerdown", closeOnOutsidePointer);
    return () => document.removeEventListener("pointerdown", closeOnOutsidePointer);
  }, [open]);

  const onMenuKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const actions = Array.from(event.currentTarget.querySelectorAll<HTMLButtonElement>("[role=menuitem]:not([disabled])"));
    const current = document.activeElement as HTMLButtonElement | null;
    const index = current ? actions.indexOf(current) : -1;
    if (event.key === "Escape") { event.preventDefault(); close(); return; }
    const nextIndex = event.key === "ArrowDown" ? Math.min(index + 1, actions.length - 1)
      : event.key === "ArrowUp" ? Math.max(index - 1, 0)
      : event.key === "Home" ? 0
      : event.key === "End" ? actions.length - 1
      : -1;
    if (nextIndex < 0 || nextIndex === index) return;
    event.preventDefault();
    actions[nextIndex]?.focus();
  };

  return <div className="overflow-menu" ref={rootRef}>
    <button ref={triggerRef} className="ui-button ui-button-ghost overflow-menu-trigger" type="button" aria-haspopup="menu" aria-expanded={open} onClick={() => setOpen((value) => !value)}>{label}</button>
    {open && <div className="overflow-menu-panel" role="menu" aria-label={label} onKeyDown={onMenuKeyDown}>{items.map((item, index) => <button ref={index === 0 ? firstItemRef : undefined} className="overflow-menu-item" key={item.label} type="button" role="menuitem" disabled={item.disabled} onClick={() => { if (item.disabled) return; item.onSelect(); close(false); }}>{item.label}</button>)}</div>}
  </div>;
}
