export type Perfil = "CLIENTE" | "APROVADOR";

export interface AuthState {
  isLoggedIn: boolean;
  perfil: Perfil;
  usuarioId: string | null; 
}

export interface AuthContextValue extends AuthState {
  setAuth: (next: Partial<AuthState>) => void;
}
