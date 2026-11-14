import { useEffect, useId, useMemo, useRef, useState, type KeyboardEvent } from "react";
import type { Model } from "../../app/types";
import { NavIcon } from "./NavIcon";

type ModelPickerProps = {
  model: string;
  provider: string;
  models: Model[];
  onChange: (model: string) => void;
  onOpenModels?: () => void;
};

function modelProvider(model: Model, fallback: string) {
  if (model.provider) return model.provider;
  const separator = model.model.indexOf("/");
  return separator > 0 ? model.model.slice(0, separator) : fallback || "Local runtime";
}

export function ModelPicker({ model, provider, models, onChange, onOpenModels }: ModelPickerProps) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const rootRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const optionRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const listId = useId();
  const options = useMemo(() => {
    const current = models.some((item) => item.model === model) ? [] : [{ model, vision: "unknown", capabilities: [], provider }];
    return [...current, ...models];
  }, [model, models, provider]);
  const filtered = useMemo(() => options.filter((item) => `${item.model} ${item.provider ?? ""} ${item.capabilities.join(" ")}`.toLowerCase().includes(query.trim().toLowerCase())), [options, query]);

  const close = (restoreFocus = true) => {
    setOpen(false);
    setQuery("");
    if (restoreFocus) window.requestAnimationFrame(() => triggerRef.current?.focus());
  };

  useEffect(() => {
    if (!open) return;
    setActiveIndex(0);
    const focusTimer = window.setTimeout(() => searchRef.current?.focus(), 0);
    const closeOnOutsidePointer = (event: PointerEvent) => {
      if (event.target instanceof Element && rootRef.current?.contains(event.target)) return;
      close(false);
    };
    document.addEventListener("pointerdown", closeOnOutsidePointer);
    return () => { window.clearTimeout(focusTimer); document.removeEventListener("pointerdown", closeOnOutsidePointer); };
  }, [open]);

  useEffect(() => {
    if (!open || !filtered.length) return;
    setActiveIndex((index) => Math.min(index, filtered.length - 1));
  }, [filtered.length, open]);

  const choose = (value: string) => {
    onChange(value);
    close(false);
    window.requestAnimationFrame(() => triggerRef.current?.focus());
  };

  const move = (index: number) => {
    if (!filtered.length) return;
    const next = Math.max(0, Math.min(index, filtered.length - 1));
    setActiveIndex(next);
    window.requestAnimationFrame(() => optionRefs.current[next]?.scrollIntoView({ block: "nearest" }));
  };

  const onSearchKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Escape") { event.preventDefault(); close(); return; }
    if (event.key === "ArrowDown") { event.preventDefault(); move(activeIndex + 1); optionRefs.current[Math.min(activeIndex + 1, filtered.length - 1)]?.focus(); }
    if (event.key === "ArrowUp") { event.preventDefault(); move(activeIndex - 1); optionRefs.current[Math.max(activeIndex - 1, 0)]?.focus(); }
    if (event.key === "Enter" && filtered[activeIndex]) { event.preventDefault(); choose(filtered[activeIndex].model); }
  };

  const onOptionKeyDown = (event: KeyboardEvent<HTMLButtonElement>, index: number) => {
    if (event.key === "Escape") { event.preventDefault(); close(); return; }
    if (event.key === "ArrowDown") { event.preventDefault(); move(index + 1); optionRefs.current[Math.min(index + 1, filtered.length - 1)]?.focus(); }
    if (event.key === "ArrowUp") { event.preventDefault(); move(index - 1); optionRefs.current[Math.max(index - 1, 0)]?.focus(); }
    if (event.key === "Home") { event.preventDefault(); move(0); optionRefs.current[0]?.focus(); }
    if (event.key === "End") { event.preventDefault(); move(filtered.length - 1); optionRefs.current[filtered.length - 1]?.focus(); }
  };

  return <div className="model-picker" ref={rootRef}>
    <button ref={triggerRef} className="model-picker-trigger" type="button" aria-haspopup="listbox" aria-expanded={open} aria-controls={listId} onClick={() => setOpen((value) => !value)}>
      <span className="model-picker-trigger-copy"><span className="model-picker-label">MODEL</span><strong>{model || "Choose a local model"}</strong></span>
      <NavIcon name="chevron-right" />
    </button>
    {open && <div className="model-picker-popover" id={listId} role="dialog" aria-label="Choose local model">
      <div className="model-picker-heading"><div><span className="section-kicker">LOCAL MODELS</span><strong>Choose a model</strong></div><span>{filtered.length} available</span></div>
      <input ref={searchRef} className="model-picker-search" value={query} onChange={(event) => setQuery(event.target.value)} onKeyDown={onSearchKeyDown} placeholder="Search models…" aria-label="Search local models" aria-controls={`${listId}-options`} />
      {filtered.length ? <div className="model-picker-options" id={`${listId}-options`} role="listbox" aria-label="Available local models">{filtered.map((item, index) => <button ref={(node) => { optionRefs.current[index] = node; }} className={`model-picker-option ${item.model === model ? "is-selected" : ""} ${index === activeIndex ? "is-active" : ""}`} id={`${listId}-option-${index}`} key={item.model} type="button" role="option" aria-selected={item.model === model} onMouseEnter={() => setActiveIndex(index)} onKeyDown={(event) => onOptionKeyDown(event, index)} onClick={() => choose(item.model)}><span className="model-picker-option-copy"><strong>{item.model}</strong><small>{modelProvider(item, provider)} · {item.vision.toLowerCase() === "supported" ? "Vision" : "Text"} · {item.capabilities.join(" · ") || "Text generation"}</small></span>{item.model === model && <span className="model-picker-check" aria-label="Selected">Selected</span>}</button>)}</div> : <div className="model-picker-empty"><strong>No local models found</strong><span>Start a supported local runtime, then discover its models.</span>{onOpenModels && <button type="button" onClick={() => { close(false); onOpenModels(); }}>Open Models</button>}</div>}
      <p className="model-picker-hint">↑ ↓ browse · Enter select · Esc close</p>
    </div>}
  </div>;
}
