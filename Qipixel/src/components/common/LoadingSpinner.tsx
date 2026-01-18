import { Loader2 } from "lucide-react";
import { cn } from "../ui/utils";

interface LoadingSpinnerProps {
  size?: "sm" | "md" | "lg";
  message?: string;
  className?: string;
}

const sizeClasses = {
  sm: "w-4 h-4",
  md: "w-5 h-5",
  lg: "w-8 h-8",
};

export function LoadingSpinner({ size = "md", message, className }: LoadingSpinnerProps) {
  return (
    <div className={cn("flex items-center justify-center text-zinc-500", className)}>
      <Loader2 className={cn(sizeClasses[size], "animate-spin", message && "mr-2")} />
      {message && <span className="text-sm">{message}</span>}
    </div>
  );
}

interface LoadingOverlayProps {
  message?: string;
  subMessage?: string;
}

export function LoadingOverlay({ message = "Loading...", subMessage }: LoadingOverlayProps) {
  return (
    <div className="flex flex-col items-center justify-center py-8 text-zinc-500 animate-in fade-in duration-300">
      <Loader2 className="w-8 h-8 text-amber-500 animate-spin mb-3" />
      <h4 className="text-zinc-300 font-medium text-sm">{message}</h4>
      {subMessage && <p className="text-xs text-zinc-500 mt-1">{subMessage}</p>}
    </div>
  );
}
