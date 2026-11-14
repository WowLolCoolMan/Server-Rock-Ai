import type { Route } from "../../app/types";
import { NavIcon, type NavIconName } from "../ui/NavIcon";

const destinations: Array<{ route: Route; icon: NavIconName; label: string }> = [
  { route: "chat", icon: "chat", label: "Home" },
  { route: "images", icon: "image", label: "Image Gen" },
  { route: "usage", icon: "activity", label: "Usage" },
  { route: "settings", icon: "settings", label: "Settings" },
];

const moreDestinations: Array<{ route: Route; icon: NavIconName; label: string }> = [
  { route: "coding", icon: "code", label: "Coding" },
  { route: "workbench", icon: "activity", label: "Agents" },
  { route: "models", icon: "model", label: "Models" },
  { route: "marketplace", icon: "sparkle", label: "Marketplace" },
  { route: "terminal", icon: "terminal", label: "Terminal" },
  { route: "memory", icon: "memory", label: "Memory" },
];

export function GlobalRail({ route, onNavigate }: { route: Route; onNavigate: (route: Route) => void }) {
  return <aside className="global-rail" aria-label="Global navigation">
    <div className="global-rail-mark"><span>SK</span></div>
    <nav className="global-rail-destinations" aria-label="Primary destinations">
      {destinations.map((item) => <button key={item.route} className={route === item.route ? "is-active" : ""} type="button" aria-current={route === item.route ? "page" : undefined} aria-label={item.label} title={item.label} onClick={() => onNavigate(item.route)}><NavIcon name={item.icon} /></button>)}
    </nav>
    <nav className="global-rail-more" aria-label="Additional destinations">
      {moreDestinations.map((item) => <button key={item.route} className={route === item.route ? "is-active" : ""} type="button" aria-current={route === item.route ? "page" : undefined} aria-label={item.label} title={item.label} onClick={() => onNavigate(item.route)}><NavIcon name={item.icon} /></button>)}
    </nav>
  </aside>;
}
