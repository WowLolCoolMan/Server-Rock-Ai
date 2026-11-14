import type { SVGProps } from "react";

export type NavIconName = "chat" | "code" | "activity" | "model" | "image" | "sparkle" | "terminal" | "memory" | "settings" | "project" | "plus" | "arrow-right" | "arrow-up-right";

export function NavIcon({ name, ...props }: { name: NavIconName } & SVGProps<SVGSVGElement>) {
  const common = { fill: "none", stroke: "currentColor", strokeLinecap: "round" as const, strokeLinejoin: "round" as const, strokeWidth: 1.65 };
  let content: React.ReactNode;
  switch (name) {
    case "chat": content = <><path {...common} d="M4 4.5h16v10H9l-4.5 4v-4H4z" /><path {...common} d="M8 8.5h8M8 11.5h5" /></> ; break;
    case "code": content = <><path {...common} d="m8 6-5 6 5 6M16 6l5 6-5 6M14 3l-4 18" /></> ; break;
    case "activity": content = <path {...common} d="M3 12h3l2.3-5.5L12 18l2.7-7 1.8 3H21" />; break;
    case "model": content = <><circle {...common} cx="12" cy="12" r="7.5" /><path {...common} d="M12 4.5v15M4.5 12h15" /><circle {...common} cx="12" cy="12" r="2" /></> ; break;
    case "image": content = <><rect {...common} x="3" y="4" width="18" height="16" rx="2" /><circle {...common} cx="8" cy="9" r="1.5" /><path {...common} d="m4 17 5-5 3 3 2-2 6 5" /></> ; break;
    case "sparkle": content = <><path {...common} d="m12 3 1.4 5.6L19 10l-5.6 1.4L12 17l-1.4-5.6L5 10l5.6-1.4z" /><path {...common} d="m19 16 .6 2.4L22 19l-2.4.6L19 22l-.6-2.4L16 19l2.4-.6z" /></> ; break;
    case "terminal": content = <><path {...common} d="m4 6 6 6-6 6M13 18h7" /></> ; break;
    case "memory": content = <><ellipse {...common} cx="12" cy="6" rx="7" ry="3" /><path {...common} d="M5 6v6c0 1.7 3.1 3 7 3s7-1.3 7-3V6M5 12v6c0 1.7 3.1 3 7 3s7-1.3 7-3v-6" /></> ; break;
    case "settings": content = <><circle {...common} cx="12" cy="12" r="3" /><path {...common} d="M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1-1.7 1.7-.1-.1a1.7 1.7 0 0 0-1.9-.3 1.7 1.7 0 0 0-1 1.6v.1h-2.4v-.1a1.7 1.7 0 0 0-1-1.6 1.7 1.7 0 0 0-1.9.3l-.1.1L8 17l.1-.1a1.7 1.7 0 0 0 .3-1.9 1.7 1.7 0 0 0-1.6-1H6.7v-2.4h.1a1.7 1.7 0 0 0 1.6-1 1.7 1.7 0 0 0-.3-1.9L8 8.6l1.7-1.7.1.1a1.7 1.7 0 0 0 1.9.3 1.7 1.7 0 0 0 1-1.6v-.1h2.4v.1a1.7 1.7 0 0 0 1 1.6 1.7 1.7 0 0 0 1.9-.3l.1-.1 1.7 1.7-.1.1a1.7 1.7 0 0 0-.3 1.9 1.7 1.7 0 0 0 1.6 1h.1V14h-.1a1.7 1.7 0 0 0-1.6 1Z" /></> ; break;
    case "project": content = <><path {...common} d="M3.5 7.5h6l1.8 2h9.2v9.2a1.8 1.8 0 0 1-1.8 1.8H5.3a1.8 1.8 0 0 1-1.8-1.8z" /><path {...common} d="M3.5 7.5V5.8A1.8 1.8 0 0 1 5.3 4h4l1.8 2h7.6a1.8 1.8 0 0 1 1.8 1.8v1.7" /></> ; break;
    case "plus": content = <path {...common} d="M12 5v14M5 12h14" />; break;
    case "arrow-right": content = <><path {...common} d="M4 12h15" /><path {...common} d="m13 6 6 6-6 6" /></> ; break;
    case "arrow-up-right": content = <><path {...common} d="M6 18 18 6" /><path {...common} d="M9 6h9v9" /></> ; break;
  }
  return <svg className="nav-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false" {...props}>{content}</svg>;
}
