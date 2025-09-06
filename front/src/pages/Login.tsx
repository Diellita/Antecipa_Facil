import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { login } from "../lib/api";

type Props = {
  onSuccess: (role: "CLIENTE" | "APROVADOR") => void;
};

export default function Login({ onSuccess }: Props) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  // 🔁 Reidrata erro se a tela remontar (evita "piscar e sumir")
  useEffect(() => {
    const saved = sessionStorage.getItem("loginError");
    if (saved) {
      setError(saved);
      sessionStorage.removeItem("loginError");
    }
  }, []);

  // impede que Enter gere dois submits
  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      e.stopPropagation();
      void handleLogin();
    }
  };

  const handleLogin = async () => {
    if (loading) return;

    // limpa qualquer credencial antiga ANTES de tentar
    localStorage.removeItem("accessToken");
    localStorage.removeItem("role");
    localStorage.removeItem("userId");

    setError(null);

    const emailTrim = email.trim();
    const passTrim  = password.trim();

    if (!emailTrim || !passTrim) {
      const msg = "Informe e-mail e senha.";
      setError(msg);
      sessionStorage.setItem("loginError", msg);
      return;
    }

    setLoading(true);
    try {
      const data = await login(emailTrim, passTrim); // deve lançar se 4xx/5xx

      localStorage.setItem("role", data.role);
      localStorage.setItem("accessToken", data.accessToken ?? "");
      localStorage.setItem("userId", data.userId ?? "");

      if (data.role === "APROVADOR") {
        onSuccess("APROVADOR");
        navigate("/admin");
      } else {
        onSuccess("CLIENTE");
        navigate("/lista");
      }
    } catch (e: any) {
      const status = e?.response?.status;
      const msg =
        status === 401
          ? "E-mail ou senha inválidos."
          : e?.response?.data?.message || e?.message || "Falha ao conectar.";

      // persiste para sobreviver a remontagens
      setError(msg);
      sessionStorage.setItem("loginError", msg);

      // garante ambiente “limpo”
      localStorage.removeItem("accessToken");
      localStorage.removeItem("role");
      localStorage.removeItem("userId");
    } finally {
      setLoading(false);
    }
  };

  const pageStyle: React.CSSProperties = {
    minHeight: "100vh",
    backgroundImage: "url('/src/assets/imgs/gestao-imobiliaria.jpg')",
    backgroundSize: "cover",
    backgroundPosition: "center",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    padding: "24px",
  };

  const cardStyle: React.CSSProperties = {
    width: 380,
    maxWidth: "100%",
    background: "rgba(255,255,255,0.9)",
    borderRadius: 12,
    boxShadow: "0 10px 30px rgba(0,0,0,0.15)",
    padding: 24,
    fontFamily: "Inter, system-ui, Arial, sans-serif",
  };

  const titleStyle: React.CSSProperties = {
    margin: 0,
    marginBottom: 16,
    fontSize: 24,
  };

  const inputStyle: React.CSSProperties = {
    width: "100%",
    padding: "10px 12px",
    marginBottom: 12,
    border: "1px solid #d0d7de",
    borderRadius: 8,
    fontSize: 14,
  };

  const btnStyle: React.CSSProperties = {
    width: "100%",
    padding: "10px 12px",
    background: "#111827",
    color: "#fff",
    border: 0,
    borderRadius: 8,
    fontSize: 14,
    cursor: "pointer",
    opacity: loading ? 0.7 : 1,
  };

  const errorStyle: React.CSSProperties = {
    background: "#fee2e2",
    color: "#b91c1c",
    padding: "10px 12px",
    borderRadius: 8,
    marginBottom: 12,
    fontSize: 13,
    border: "1px solid #fecaca",
  };

  return (
    <div style={pageStyle}>
      <div style={cardStyle}>
        <h2 style={titleStyle}>Login</h2>

        {error && <div style={errorStyle}>{error}</div>}

        <input
          type="email"
          placeholder="E-mail"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          onKeyDown={handleKeyDown}
          style={inputStyle}
        />
        <input
          type="password"
          placeholder="Senha"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          onKeyDown={handleKeyDown}
          style={inputStyle}
        />
        <button onClick={handleLogin} style={btnStyle} disabled={loading}>
          {loading ? "Entrando..." : "Acessar"}
        </button>
      </div>
    </div>
  );
}
