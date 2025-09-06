
# Antecipa Fácil — Sistema de Antecipação

Projeto **fullstack** com **React + TypeScript + Vite** no frontend e **.NET 8 (ASP.NET Core Web API)** no backend.  
O sistema simula a **antecipação de parcelas**, permitindo que clientes solicitem adiantamentos e que aprovadores façam a gestão.

## 🧰 Tecnologias

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

```
AntecipaFacil/
├─ docker-compose.yml
├─ front/                # React + Vite (TS)
│  └─ ...
└─ backend/
   └─ WebApi/            # ASP.NET Core API (.NET 8)
      ├─ appsettings.Development.json
      └─ ...
```


## ✅ Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

Checagem rápida:
```bash
dotnet --version
node -v
npm -v
docker --version
```


## ▶️ Passo a passo (Local + Docker)

### 1) Subir o banco PostgreSQL via Docker
O repositório inclui um `docker-compose.yml` funcional com o serviço **db** (container `antecipafacil-db`).  
Na **raiz do projeto**, rode:

```bash
docker compose up -d
docker ps
```

Padrões expostos: `localhost:5432` (POSTGRES_DB=antecipafacil / POSTGRES_USER=postgres / POSTGRES_PASSWORD=postgres).  
Os dados persistem no volume `postgres_data`.

> ⚠️ Windows/WSL travado?  
> ```powershell
> wsl --shutdown
> docker pull postgres:15
> docker compose up -d
> ```

### 2) Aplicar migrações do Entity Framework
Com o banco de pé, aplique as migrações da API:

```powershell
cd backend/WebApi
dotnet tool update -g dotnet-ef
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update
```

Connection string padrão (ajuste se necessário em `backend/WebApi/appsettings.Development.json`):
```
Host=localhost;Port=5432;Database=antecipafacil;Username=postgres;Password=postgres
```

### 3) Rodar o backend (API)
```powershell
cd backend/WebApi
dotnet run
```
Swagger: **http://localhost:5275/swagger**

### 4) Rodar o frontend (React/Vite)
Em outro terminal:
```powershell
cd front
npm install
npm run dev
```
App: **http://localhost:5173**

## 👥 Perfis de acesso (seed)

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

> ℹ️ Dados fictícios usados somente para testes locais.


## 🧪 Teste rápido (roteiro sugerido)

1. Logar como **Aprovador** e ver pendências.  
2. Logar como **Cliente** e criar nova solicitação de antecipação.  
3. Voltar ao Aprovador e **aprovar/reprovar em lote**; validar filtros/status.  
4. Confirmar que **clientes veem apenas seus próprios contratos/solicitações**.

---

## 🛠 Solução de problemas

- **`Failed to connect to 127.0.0.1:5432`**: o Postgres não está rodando. Execute `docker compose up -d` e confira `docker ps`.
- **EF Tools 7 vs runtime 8**: `dotnet tool update -g dotnet-ef`.
- **Porta 5432 em uso**: pare serviços locais do PostgreSQL ou altere a porta (ex.: `"5433:5432"`) e ajuste a connection string.
- **Variáveis por ambiente** (opcional, .NET):
  ```powershell
  $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=antecipafacil;Username=postgres;Password=postgres"
  ```

## 📜 Observações de Arquitetura

- O **Aprovador** enxerga e gerencia todas as solicitações.  
- Cada **Cliente** acessa somente seus contratos e solicitações.  
- O fluxo de **aprovação/reprovação** atualiza automaticamente os status de parcelas/contratos.


## 📄 Licença

Projeto para **portfólio/aprendizado** — adicione `LICENSE` (sugestão: MIT).
