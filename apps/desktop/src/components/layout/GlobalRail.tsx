import type { Route } from "../../app/types";
import { NavIcon, type NavIconName } from "../ui/NavIcon";

const destinations: Array<{ route: Route; icon: NavIconName; label: string }> = [
  { route: "chat", icon: "chat", label: "Home" },
  { route: "images", icon: "image", label: "Image Gen" },
  { route: "usage", icon: "activity", label: "Usage" },
  { route: "settings", icon: "settings", label: "Settings" },
];

export function GlobalRail({ route, onNavigate, onOpenLauncher }: { route: Route; onNavigate: (route: Route) => void; onOpenLauncher: () => void }) {
  return <aside className="global-rail" aria-label="Global navigation">
    <div className="global-rail-mark"><span>SK</span></div>
    <nav className="global-rail-destinations" aria-label="Primary destinations">
      {destinations.map((item) => <button key={item.route} className={route === item.route ? "is-active" : ""} type="button" aria-current={route === item.route ? "page" : undefined} aria-label={item.label} title={item.label} onClick={() => onNavigate(item.route)}><NavIcon name={item.icon} /></button>)}
    </nav>
    <div className="global-rail-more"><button type="button" aria-label="Open tools launcher" title="More tools" onClick={onOpenLauncher}><NavIcon name="more" /></button></div>
  </aside>;
}
