# Antecipa Fácil — Sistema de Antecipação

Projeto **fullstack** com **React + TypeScript + Vite** no frontend e **.NET 8 (ASP.NET Core Web API)** no backend.  
O sistema simula a **antecipação de parcelas**, permitindo que clientes solicitem adiantamentos e que aprovadores façam a gestão.

---

## Tecnologias

**Frontend**
- React + TypeScript (Vite)
- TailwindCSS
- Axios
- SweetAlert2

**Backend**
- .NET 8 (ASP.NET Core Web API)
- Entity Framework Core
- PostgreSQL

**Infra/Dev**
- Docker / Docker Compose
- Swagger (UI)

---

## 🗂 Estrutura (resumo)

```text
AntecipaFacil/
├─ docker-compose.yml
├─ front/                  # React + Vite (TypeScript)
│  ├─ package.json
│  ├─ vite.config.ts
│  ├─ index.html
│  ├─ public/
│  └─ src/
└─ backend/
   └─ WebApi/              # ASP.NET Core API (.NET 8)
      ├─ appsettings.Development.json
      └─ ...
```

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

**Checagem rápida:**
```bash
dotnet --version
node -v
npm -v
docker --version
```

---

## ▶️ Como rodar (Local + Docker)

### 1) Subir o banco PostgreSQL via Docker
O repositório inclui um `docker-compose.yml` com o serviço **db** (container `antecipafacil-db`).

```bash
docker compose up -d
docker ps  
```

**Conflito de container?**
```bash
docker rm -f antecipafacil-db
docker compose up -d
```

> Observação: se o Docker avisar que o atributo `version` no `docker-compose.yml` está obsoleto, é apenas um aviso — pode ser removido futuramente sem afetar a execução.

### 2) Aplicar migrações do Entity Framework

**CMD (Windows):**
```cmd
cd backend\WebApi
dotnet tool update -g dotnet-ef
set ASPNETCORE_ENVIRONMENT=Development
dotnet ef database update
```

**PowerShell (Windows):**
```powershell
cd backend/WebApi
dotnet tool update -g dotnet-ef
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet ef database update
```

**Connection string padrão** (ajuste se necessário em `backend/WebApi/appsettings.Development.json`):
```
Host=localhost;Port=5432;Database=antecipafacil;Username=postgres;Password=postgres
```

### 3) Rodar o backend (API)
```cmd
cd backend\WebApi
dotnet run
```
Swagger: **http://localhost:5275/swagger** (ou a porta exibida no console).

### 4) Rodar o frontend (Vite/React)
Em outro terminal:
```cmd
cd front
npm install
```
Se aparecer erro de import para **axios** ou **sweetalert2**, instale explicitamente (em alguns ambientes o `package.json` pode não conter as entradas):
```cmd
npm i axios sweetalert2
```
Inicie o dev server:
```cmd
npm run dev   # http://localhost:5173
```

---

## Perfis de acesso (seed)

**Aprovador**
- Email: `aprovador.demo@antecipafacil.com`
- Senha: `123456`

**Clientes (seed)**
- Ana Sousa — `ana.sousa@antecipafacil.com` / `as123456`
- João Ribeiro — `joao.ribeiro@antecipafacil.com` / `jr123456`
- Regina Falange — `regina.falange@antecipafacil.com` / `rf123456`
- Gabriel Alves — `gabriel.alves@antecipafacil.com` / `ga123456`
- Lucas Machado — `lucas.machado@antecipafacil.com` / `lm123456`
- Pedro Rocha — `pedro.rocha@antecipafacil.com` / `pr123456`
- Renato Santos — `renato.santos@antecipafacil.com` / `rs123456`
- Fátima Mohamad — `fatima.mohamad@antecipafacil.com` / `fm123456`
- Ibrahim Mustafa — `ibrahim.mustafa@antecipafacil.com` / `im123456`
- Hideki Suzuki — `hideki.suzuki@antecipafacil.com` / `hs123456`

> Dados fictícios usados somente para testes locais.

---

## Testes sugeridos

1. Logar como **Aprovador** e ver pendências.  
2. Logar como **Cliente** e criar nova solicitação de antecipação.  
3. Voltar ao Aprovador e **aprovar/reprovar em lote**; validar filtros/status.  
4. Confirmar que **clientes veem apenas seus próprios contratos/solicitações**.

---

## Solução de problemas

- **`Failed to connect to 127.0.0.1:5432`** → Postgres não está rodando. Rode `docker compose up -d` e confira `docker ps`.
- **Conflito de nome do container `antecipafacil-db`** → `docker rm -f antecipafacil-db && docker compose up -d`.
- **`dotnet-ef` não encontrado** → `dotnet tool update -g dotnet-ef` e **reabra o terminal**.
- **Erro de import no front (`axios`/`sweetalert2`)** → `cd front && npm i axios sweetalert2 && npm run dev`.
- **Porta 5432 em uso** → pare serviços locais do PostgreSQL ou mapeie outra porta no `docker-compose.yml` (ex.: `"5433:5432"`) e ajuste a connection string.

---

## Licença

Projeto para **portfólio/aprendizado** — adicione um arquivo `LICENSE` (sugestão: MIT).
