import type { ButtonHTMLAttributes, HTMLAttributes, ReactNode } from "react";

export function Card({ children, className = "", ...props }: HTMLAttributes<HTMLElement> & { children: ReactNode }) {
  return <section className={`ui-card ${className}`} {...props}>{children}</section>;
}

export function Button({ children, variant = "secondary", className = "", ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "secondary" | "ghost" | "danger" }) {
  return <button className={`ui-button ui-button-${variant} ${className}`} {...props}>{children}</button>;
}

export function PageHeader({ eyebrow, title, description, actions }: { eyebrow: string; title: string; description: string; actions?: ReactNode }) {
  return <header className="page-header"><div><p className="eyebrow">{eyebrow}</p><h2>{title}</h2><p className="page-description">{description}</p></div>{actions && <div className="page-actions">{actions}</div>}</header>;
}

export function StatusBadge({ children, tone = "neutral" }: { children: ReactNode; tone?: "neutral" | "success" | "warning" | "danger" }) {
  return <span className={`status-badge status-badge-${tone}`}><span className="status-badge-dot" />{children}</span>;
}

export function EmptyState({ title, description, action, visual }: { title: string; description: string; action?: ReactNode; visual?: ReactNode }) {
  return <div className="ui-empty">{visual ? <div className="ui-empty-visual" aria-hidden="true">{visual}</div> : <div className="ui-empty-icon">⌁</div>}<strong>{title}</strong><p>{description}</p>{action}</div>;
}
