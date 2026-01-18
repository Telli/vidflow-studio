import { LayoutGrid, List, Film, Scissors, Download, Settings, FileVideo, LogOut } from "lucide-react";
import { cn } from "../ui/utils";
import { Button } from "../ui/button";
import { useAuth } from "../context/AuthContext";

interface AppSidebarProps {
  currentScreen: string;
  onNavigate: (screen: string) => void;
  onToggleActivity: () => void;
}

export function AppSidebar({ currentScreen, onNavigate, onToggleActivity }: AppSidebarProps) {
  const { user, logout } = useAuth();
  const navItems = [
    { id: "dashboard", label: "Project Overview", icon: LayoutGrid },
    { id: "planner", label: "Scene Planner", icon: List },
    { id: "workspace", label: "Studio Workspace", icon: Film },
    { id: "stitch", label: "Stitch Plan", icon: Scissors },
    { id: "export", label: "Render & Export", icon: Download },
  ];

  return (
    <div className="w-64 h-screen bg-zinc-950 border-r border-zinc-800 flex flex-col flex-shrink-0">
      <div className="p-6">
        <div className="flex items-center gap-2 mb-8">
          <div className="w-8 h-8 bg-amber-600 rounded-lg flex items-center justify-center">
            <FileVideo className="w-5 h-5 text-zinc-950 fill-current" />
          </div>
          <span className="font-bold text-lg tracking-tight text-zinc-100">VidFlow</span>
        </div>

        <nav className="space-y-1">
          {navItems.map((item) => (
            <Button
              key={item.id}
              variant="ghost"
              className={cn(
                "w-full justify-start gap-3 h-10 font-medium",
                currentScreen === item.id 
                  ? "bg-zinc-800 text-amber-500 hover:bg-zinc-800 hover:text-amber-500" 
                  : "text-zinc-400 hover:bg-zinc-900 hover:text-zinc-200"
              )}
              onClick={() => onNavigate(item.id)}
            >
              <item.icon className="w-4 h-4" />
              {item.label}
            </Button>
          ))}
        </nav>
      </div>

      <div className="mt-auto p-6 border-t border-zinc-900">
        <Button 
          variant="ghost" 
          onClick={onToggleActivity}
          className="w-full justify-start gap-3 text-zinc-500 hover:text-zinc-300 mb-1"
        >
          <div className="w-4 h-4 rounded-full border border-zinc-600 flex items-center justify-center text-[10px] font-mono">A</div>
          Agent Activity
        </Button>
        <Button 
          variant="ghost" 
          className="w-full justify-start gap-3 text-zinc-500 hover:text-zinc-300"
        >
          <Settings className="w-4 h-4" />
          Settings
        </Button>
        <div className="mt-4 flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-zinc-800 border border-zinc-700 flex items-center justify-center text-xs font-medium text-zinc-400">
            {user?.email?.charAt(0).toUpperCase() ?? "?"}
          </div>
          <div className="flex flex-col flex-1 min-w-0">
            <span className="text-xs font-medium text-zinc-300 truncate">{user?.email ?? "Guest"}</span>
            <button
              onClick={logout}
              className="text-[10px] text-zinc-500 hover:text-red-400 text-left flex items-center gap-1"
            >
              <LogOut className="w-3 h-3" />
              Sign out
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
