import { Button } from "../ui/button";
import { 
  LayoutDashboard, 
  Clapperboard, 
  BookOpen, 
  Users, 
  Scissors, 
  FileVideo, 
  Activity,
  Settings,
  LogOut
} from "lucide-react";
import { cn } from "../ui/utils";

interface NavItemProps {
  label: string;
  icon: React.ReactNode;
  isActive?: boolean;
  onClick?: () => void;
}

function NavItem({ label, icon, isActive, onClick }: NavItemProps) {
  return (
    <Button
      variant="ghost"
      className={cn(
        "w-full justify-start gap-3 text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800/50",
        isActive && "bg-zinc-800 text-zinc-100 border-r-2 border-amber-500 rounded-r-none"
      )}
      onClick={onClick}
    >
      {icon}
      <span>{label}</span>
    </Button>
  );
}

interface SidebarProps {
  currentRoute: string;
  onNavigate: (route: string) => void;
}

export function Sidebar({ currentRoute, onNavigate }: SidebarProps) {
  return (
    <div className="w-64 border-r border-zinc-800 bg-zinc-950 flex flex-col flex-shrink-0">
      <div className="h-14 flex items-center px-6 border-b border-zinc-800">
        <div className="flex items-center gap-2 text-zinc-100 font-bold tracking-tight">
          <div className="w-6 h-6 bg-amber-600 rounded-sm flex items-center justify-center text-xs text-black font-black">
            VF
          </div>
          VidFlow Studio
        </div>
      </div>

      <div className="flex-1 py-6 space-y-1">
        <div className="px-6 text-xs font-semibold text-zinc-500 uppercase tracking-wider mb-2">
          Project
        </div>
        <NavItem 
          label="Dashboard" 
          icon={<LayoutDashboard className="w-4 h-4" />} 
          isActive={currentRoute === 'projects' || currentRoute === 'dashboard'}
          onClick={() => onNavigate('projects')}
        />
        <NavItem 
          label="Scene Planner" 
          icon={<Clapperboard className="w-4 h-4" />} 
          isActive={currentRoute === 'planner'}
          onClick={() => onNavigate('planner')}
        />
        <NavItem 
          label="Story Bible" 
          icon={<BookOpen className="w-4 h-4" />} 
          isActive={currentRoute === 'bible'}
          onClick={() => onNavigate('bible')}
        />
        <NavItem 
          label="Characters" 
          icon={<Users className="w-4 h-4" />} 
          isActive={currentRoute === 'characters'}
          onClick={() => onNavigate('characters')}
        />

        <div className="px-6 text-xs font-semibold text-zinc-500 uppercase tracking-wider mb-2 mt-8">
          Production
        </div>
        <NavItem 
          label="Stitch Plan" 
          icon={<Scissors className="w-4 h-4" />} 
          isActive={currentRoute === 'stitch'}
          onClick={() => onNavigate('stitch')}
        />
        <NavItem 
          label="Renders" 
          icon={<FileVideo className="w-4 h-4" />} 
          isActive={currentRoute === 'renders'}
          onClick={() => onNavigate('renders')}
        />
        <NavItem 
          label="Agent Activity" 
          icon={<Activity className="w-4 h-4" />} 
          isActive={currentRoute === 'activity'}
          onClick={() => onNavigate('activity')}
        />
      </div>

      <div className="p-4 border-t border-zinc-800">
        <NavItem 
          label="Settings" 
          icon={<Settings className="w-4 h-4" />} 
        />
        <div className="mt-4 flex items-center gap-3 px-4 py-2">
           <div className="w-8 h-8 rounded-full bg-zinc-800 border border-zinc-700"></div>
           <div className="flex-1 min-w-0">
              <div className="text-sm font-medium text-zinc-200 truncate">Alex K.</div>
              <div className="text-xs text-zinc-500 truncate">Pro Plan</div>
           </div>
        </div>
      </div>
    </div>
  );
}
