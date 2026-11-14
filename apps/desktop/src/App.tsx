import { AppRouteView } from "./app/AppRouteView";
import { useDesktopWorkspace } from "./app/useDesktopWorkspace";
import { Sidebar } from "./components/layout/Sidebar";
import { Toolbar } from "./components/layout/Toolbar";
import { CommandPalette } from "./components/overlays/CommandPalette";

export default function App() {
  const workspace = useDesktopWorkspace();
  return <div className={`app-shell ${workspace.sidebarCollapsed ? "sidebar-collapsed" : ""}`}>
    <Sidebar route={workspace.route} onNavigate={workspace.onNavigate} conversations={workspace.conversations} projects={workspace.routeState.projects} selectedProject={workspace.routeState.selectedProject} tasks={workspace.routeState.tasks} taskBusy={workspace.routeState.taskBusy} query={workspace.conversationQuery} collapsed={workspace.sidebarCollapsed} onQueryChange={workspace.setConversationQuery} onToggleSidebar={workspace.toggleSidebar} onNewChat={workspace.startNewChat} onSelectConversation={workspace.selectConversation} onOpenProject={workspace.routeActions.onOpenProject} onConversationMetadata={workspace.updateConversationMetadata} onOpenCommandPalette={workspace.openCommandPalette} />
    <main className="app-main">
      <Toolbar route={workspace.route} connection={workspace.connection} onBack={() => window.history.back()} onForward={() => window.history.forward()} onSettings={() => workspace.onNavigate("settings")} />
      <div className="app-content"><AppRouteView state={workspace.routeState} actions={workspace.routeActions} />{workspace.error && <p className="global-error" role="alert">{workspace.error}</p>}{workspace.health && <span className="sr-only">{workspace.health.service} protocol {workspace.health.protocolVersion}</span>}</div>
    </main>
    <CommandPalette open={workspace.commandOpen} onClose={workspace.closeCommandPalette} onNavigate={workspace.onNavigate} />
  </div>;
}
