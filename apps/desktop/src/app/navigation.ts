import type { Route } from "./types";

const STORAGE_KEY = "skai.navigation.route";
const HISTORY_KEY = "skaiNavigation";

export type NavigationState = {
  route: Route;
  index: number;
  length: number;
};

export function routeFromLocation(): Route {
  const hashRoute = window.location.hash.replace(/^#\/?/, "").split("/")[0];
  if (isRoute(hashRoute)) return hashRoute;
  const pathSegments = window.location.pathname.split("/").filter(Boolean);
  const route = decodeURIComponent(pathSegments[pathSegments.length - 1] ?? "") as Route;
  return isRoute(route) ? route : ((localStorage.getItem(STORAGE_KEY) as Route) || "chat");
}

export function isRoute(value: string): value is Route {
  return ["chat", "coding", "settings", "models", "images", "marketplace", "workbench", "terminal", "memory"].includes(value);
}

function readHistoryState(): NavigationState | null {
  const state = window.history.state?.[HISTORY_KEY];
  if (!state || typeof state !== "object" || !isRoute(String(state.route))) return null;
  const index = Number(state.index);
  const length = Number(state.length);
  if (!Number.isInteger(index) || !Number.isInteger(length) || index < 0 || length < 1 || index >= length) return null;
  return { route: state.route, index, length };
}

export function readNavigationState(fallbackRoute: Route): NavigationState {
  return readHistoryState() ?? { route: fallbackRoute, index: 0, length: 1 };
}

export function initializeNavigation(route: Route): NavigationState {
  const current = readHistoryState();
  const state = current?.route === route ? current : { route, index: current?.index ?? 0, length: Math.max(current?.length ?? 1, (current?.index ?? 0) + 1) };
  window.history.replaceState({ ...(window.history.state ?? {}), [HISTORY_KEY]: state }, "", window.location.protocol === "file:" ? `#/${route}` : `/${route}`);
  localStorage.setItem(STORAGE_KEY, route);
  return state;
}

export function navigate(route: Route, replace = false) {
  const currentRoute = routeFromLocation();
  const current = readNavigationState(currentRoute);
  if (!replace && current.route === route) return current;
  const nextLength = Math.max(current.length, current.index + 2);
  const state = replace
    ? { route, index: current.index, length: current.length }
    : { route, index: current.index + 1, length: nextLength };
  localStorage.setItem(STORAGE_KEY, route);
  if (!replace) {
    window.history.replaceState({ ...(window.history.state ?? {}), [HISTORY_KEY]: { ...current, length: nextLength } }, "", window.location.href);
  }
  window.history[replace ? "replaceState" : "pushState"]({ ...(window.history.state ?? {}), [HISTORY_KEY]: state }, "", window.location.protocol === "file:" ? `#/${route}` : `/${route}`);
  window.dispatchEvent(new PopStateEvent("popstate"));
  return state;
}
