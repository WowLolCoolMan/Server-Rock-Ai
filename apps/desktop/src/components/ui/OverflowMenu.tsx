import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
import { Button } from "./Primitives";

export type OverflowMenuItem = {
  label: string;
  onSelect: () => void;
  disabled?: boolean;
};

export function OverflowMenu({ items, label = "More" }: { items: OverflowMenuItem[]; label?: string }) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const menuId = useId();

  const close = (restoreFocus = true) => {
    setOpen(false);
    if (restoreFocus) window.requestAnimationFrame(() => triggerRef.current?.focus());
  };

  useEffect(() => {
    if (!open) return;
    const focusTimer = window.setTimeout(() => {
      const firstEnabledItem = menuRef.current?.querySelector<HTMLButtonElement>("[role=menuitem]:not([disabled])");
      (firstEnabledItem ?? triggerRef.current)?.focus();
    }, 0);
    const closeOnOutsidePointer = (event: PointerEvent) => {
      const target = event.target;
      if (target instanceof Element && rootRef.current?.contains(target)) return;
      close(false);
    };
    document.addEventListener("pointerdown", closeOnOutsidePointer);
    return () => { window.clearTimeout(focusTimer); document.removeEventListener("pointerdown", closeOnOutsidePointer); };
  }, [open]);

  const onMenuKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const actions = Array.from(event.currentTarget.querySelectorAll<HTMLButtonElement>("[role=menuitem]:not([disabled])"));
    const current = document.activeElement as HTMLButtonElement | null;
    const index = current ? actions.indexOf(current) : -1;
    if (event.key === "Escape") { event.preventDefault(); close(); return; }
    const nextIndex = event.key === "ArrowDown" ? (index + 1) % actions.length
      : event.key === "ArrowUp" ? (index - 1 + actions.length) % actions.length
      : event.key === "Home" ? 0
      : event.key === "End" ? actions.length - 1
      : -1;
    if (nextIndex < 0 || !actions.length) return;
    event.preventDefault();
    actions[nextIndex]?.focus();
  };

  return <div className="overflow-menu" ref={rootRef}>
    <button ref={triggerRef} className="ui-button ui-button-ghost overflow-menu-trigger" type="button" aria-haspopup="menu" aria-expanded={open} aria-controls={menuId} onMouseDown={(event) => { if (!open) event.preventDefault(); }} onClick={() => open ? close(false) : setOpen(true)}>{label}</button>
    {open && <div ref={menuRef} id={menuId} className="overflow-menu-panel" role="menu" aria-label={label} onKeyDown={onMenuKeyDown}>{items.map((item) => <button className="overflow-menu-item" key={item.label} type="button" role="menuitem" disabled={item.disabled} onClick={() => { if (item.disabled) return; item.onSelect(); close(false); }}>{item.label}</button>)}</div>}
  </div>;
}
