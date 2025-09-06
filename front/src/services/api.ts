const BASE = import.meta.env?.VITE_API_BASE || "http://localhost:5275";

export async function login(email: string, password: string) {
  const res = await fetch(`${BASE}/auth/token`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "omit", 
    body: JSON.stringify({ email, password }),
  });

  if (!res.ok) {
    let body: any = null;
    try {
      body = await res.json();
    } catch {
      try {
        const text = await res.text();
        body = text ? { message: text } : null;
      } catch {}
    }
    const err: any = new Error(body?.message || `HTTP ${res.status}`);
    err.response = { status: res.status, data: body };
    throw err;
  }

  return res.json();
}
