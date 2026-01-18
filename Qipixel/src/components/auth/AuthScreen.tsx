import { useState } from "react";
import { LoginForm } from "./LoginForm";
import { RegisterForm } from "./RegisterForm";

export function AuthScreen() {
  const [mode, setMode] = useState<"login" | "register">("login");

  if (mode === "register") {
    return <RegisterForm onSwitchToLogin={() => setMode("login")} />;
  }

  return <LoginForm onSwitchToRegister={() => setMode("register")} />;
}
