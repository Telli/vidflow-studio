import { ReactNode } from "react";
import { Sidebar } from "./Sidebar";

interface AppShellProps {
  children: ReactNode;
  currentRoute: string;
  onNavigate: (route: string) => void;
}

export function AppShell({ children, currentRoute, onNavigate }: AppShellProps) {
  return (
    <div className="flex h-screen bg-zinc-950 text-zinc-100 font-sans antialiased overflow-hidden selection:bg-amber-500/30">
      <Sidebar currentRoute={currentRoute} onNavigate={onNavigate} />
      <main className="flex-1 flex flex-col min-w-0 bg-zinc-950 relative">
        {children}
      </main>
    </div>
  );
}
