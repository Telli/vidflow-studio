import React, { createContext, useContext, useEffect, useState, useCallback } from "react";
import { vidflowApi, setAuthToken, clearAuthToken, getAuthToken, type MeResponse } from "../../api/vidflow";

interface AuthContextType {
  user: MeResponse | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const hot = (import.meta as any).hot;
const AuthContext = (hot?.data?.AuthContext as React.Context<AuthContextType | undefined> | undefined)
  ?? createContext<AuthContextType | undefined>(undefined);

if (hot) {
  hot.data.AuthContext = AuthContext;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<MeResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const fetchUser = useCallback(async () => {
    const token = getAuthToken();
    if (!token) {
      setIsLoading(false);
      return;
    }

    try {
      const me = await vidflowApi.me();
      setUser(me);
    } catch {
      clearAuthToken();
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchUser();
  }, [fetchUser]);

  const login = useCallback(async (email: string, password: string) => {
    try {
      const response = await vidflowApi.login({ email, password });
      setAuthToken(response.token);
      const me = await vidflowApi.me();
      setUser(me);
    } catch (err: any) {
      // Re-throw with better error messages
      if (err?.status === 401) {
        throw new Error("Invalid email or password");
      } else if (err?.status === 400) {
        throw new Error("Invalid email or password");
      } else {
        throw err;
      }
    }
  }, []);

  const register = useCallback(async (email: string, password: string) => {
    try {
      const response = await vidflowApi.register({ email, password });
      setAuthToken(response.token);
      const me = await vidflowApi.me();
      setUser(me);
    } catch (err: any) {
      // Re-throw with better error messages
      if (err?.status === 409) {
        throw new Error("An account with this email already exists");
      } else if (err?.status === 400) {
        throw new Error("Invalid email or password");
      } else {
        throw err;
      }
    }
  }, []);

  const logout = useCallback(() => {
    clearAuthToken();
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: !!user,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
