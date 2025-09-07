import axios from "axios";

export type LoginResponse = {
  accessToken: string;
  role: "CLIENTE" | "APROVADOR";
  userId: string;
};

export type AdvanceRequestStatus = "PENDENTE" | "APROVADO" | "REPROVADO";


const rawBase =
  (typeof import.meta !== "undefined" && (import.meta as any).env?.VITE_API_URL) ||
  "http://localhost:5275";

const BASE_URL = String(rawBase).replace(/\/+$/, "");
export { BASE_URL };


export const api = axios.create({
  baseURL: BASE_URL,
  headers: {
    "Content-Type": "application/json",
    Accept: "application/json",
  },

});


api.interceptors.request.use((config) => {
  const token = typeof window !== "undefined" ? localStorage.getItem("token") : null;
  if (token) {
    config.headers = config.headers ?? {};
    (config.headers as Record<string, string>)["Authorization"] = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (resp) => resp,
  (error) => {
    const status = error?.response?.status;
    if (status === 401) {
      try {
        if (typeof window !== "undefined") {
          localStorage.removeItem("token");
          localStorage.removeItem("role");
          localStorage.removeItem("userId");
        }
      } finally {
        if (typeof window !== "undefined" && window.location.pathname !== "/login") {
          window.location.href = "/login";
        }
      }
    }
    return Promise.reject(error);
  }
);


export async function login(email: string, password: string): Promise<LoginResponse> {
  const { data } = await api.post<LoginResponse>("/auth/token", { email, password });
  if (typeof window !== "undefined") {
    if (data?.accessToken) localStorage.setItem("token", data.accessToken);
    if (data?.role) localStorage.setItem("role", data.role);
    if (data?.userId) localStorage.setItem("userId", data.userId);
  }
  return data;
}

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
  const { data } = await api.put("/advance-request/approve", { ids });
  return data;
}

export async function rejectAdvanceRequests(ids: number[] | string[]) {
  const { data } = await api.put("/advance-request/reject", { ids });
  return data;
}


export async function getContracts() {
  const { data } = await api.get("/contracts");
  return data;
}

export async function getAllContracts() {
  const { data } = await api.get("/contracts/all");
  return data;
}

export async function getAllClients() {
  const { data } = await api.get("/clients");
  return data;
}
