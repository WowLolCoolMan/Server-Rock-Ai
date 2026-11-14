import type { ReactNode } from "react";
import { Button } from "../ui/Primitives";
import { NavIcon } from "../ui/NavIcon";

export function ContextInspector({ title, subtitle, open, onClose, children, className = "" }: { title: string; subtitle?: string; open: boolean; onClose: () => void; children: ReactNode; className?: string }) {
  if (!open) return null;
  return <aside className={`context-inspector ${className}`} aria-label={`${title} inspector`}>
    <header className="context-inspector-header"><div><p className="section-kicker">CONTEXT</p><h3>{title}</h3>{subtitle && <p>{subtitle}</p>}</div><Button variant="ghost" type="button" onClick={onClose} aria-label={`Close ${title} inspector`} title="Close inspector"><NavIcon name="close" /></Button></header>
    <div className="context-inspector-body">{children}</div>
  </aside>;
}
