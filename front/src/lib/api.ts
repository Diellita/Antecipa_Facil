import axios from "axios";

type LoginResponse = {
  accessToken: string;
  role: "CLIENTE" | "APROVADOR";
  userId: string;
};

export const api = axios.create({
  baseURL: "http://localhost:5275",
  headers: {
    "Content-Type": "application/json",
    Accept: "application/json",
  },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token && config.headers) {
    (config.headers as any).set
      ? (config.headers as any).set("Authorization", `Bearer ${token}`)
      : ((config.headers as any)["Authorization"] = `Bearer ${token}`);
  }
  return config;
});

api.interceptors.response.use(
  (resp) => resp,
  (error) => {
    const status = error?.response?.status;
    if (status === 401) {
      try {
        localStorage.removeItem("token");
        localStorage.removeItem("role");
        localStorage.removeItem("userId");
      } finally {
        if (typeof window !== "undefined") {
          window.location.href = "/login";
        }
      }
    }
    return Promise.reject(error);
  }
);

export type AdvanceRequestStatus = "PENDENTE" | "APROVADO" | "REPROVADO";

export async function createAdvanceRequest(payload: any) {
  const { data } = await api.post("/advance-request", payload);
  return data;
}

export async function getMyAdvanceRequests() {
  const { data } = await api.get("/advance-request");
  return data;
}

export async function getAdminAdvanceRequests() {
  const { data } = await api.get("/advance-request/admin");
  return data;
}

export async function getPendingAdvanceRequests() {
  const { data } = await api.get("/advance-request/admin");
  return data;
}

export async function approveAdvanceRequests(ids: number[] | string[]) {
  const body = { ids };
  const { data } = await api.put("/advance-request/approve", body);
  return data;
}


export async function rejectAdvanceRequests(ids: number[] | string[]) {
  const body = { ids };
  const { data } = await api.put("/advance-request/reject", body);
  return data;
}


export async function login(email: string, password: string): Promise<LoginResponse> {
  const resp = await api.post<LoginResponse>("/auth/token", { email, password });
  const data = resp.data;
  if (data?.accessToken) localStorage.setItem("token", data.accessToken);
  if (data?.role) localStorage.setItem("role", data.role);
  if (data?.userId) localStorage.setItem("userId", data.userId);
  return data;
}

export const BASE_URL = "http://localhost:5275";

type HeaderMap = Record<string, string>;

function authHeaders(): HeaderMap {
  const tok =
    localStorage.getItem("token") ||
    localStorage.getItem("authToken") ||
    "";
  return tok ? { Authorization: `Bearer ${tok}` } : {};
}


export async function getContracts() {
  const r = await fetch(`${BASE_URL}/contracts`, { headers: { ...authHeaders() }, credentials: "include" });
  if (!r.ok) throw new Error(await r.text());
  return await r.json();
}

export async function getAllContracts() {
  const r = await fetch(`${BASE_URL}/contracts/all`, { headers: { ...authHeaders() }, credentials: "include" });
  if (!r.ok) throw new Error(await r.text());
  return await r.json();
}

export async function getAllClients() {
  const r = await fetch(`${BASE_URL}/clients`, {
    headers: { ...authHeaders() },
    credentials: "include",
  });
  return await r.json();
}


