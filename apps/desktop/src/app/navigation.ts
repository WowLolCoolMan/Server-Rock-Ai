import type { Route } from "./types";

const STORAGE_KEY = "skai.navigation.route";

export function routeFromLocation(): Route {
  const route = window.location.pathname.replace(/^\//, "") as Route;
  return isRoute(route) ? route : ((localStorage.getItem(STORAGE_KEY) as Route) || "chat");
}

export function isRoute(value: string): value is Route {
  return ["chat", "coding", "settings", "models", "images", "workbench", "terminal", "memory"].includes(value);
}

export function navigate(route: Route, replace = false) {
  localStorage.setItem(STORAGE_KEY, route);
  window.history[replace ? "replaceState" : "pushState"]({ route }, "", `/${route}`);
  window.dispatchEvent(new PopStateEvent("popstate"));
}
