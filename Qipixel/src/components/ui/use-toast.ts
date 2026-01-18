import { toast as sonnerToast } from "sonner@2.0.3";

type ToastOptions = {
  title?: string;
  description?: string;
  variant?: "default" | "destructive";
};

function toast(options: ToastOptions) {
  const { title, description, variant } = options;

  if (variant === "destructive") {
    sonnerToast.error(title, { description });
  } else {
    sonnerToast(title, { description });
  }
}

export function useToast() {
  return { toast };
}
