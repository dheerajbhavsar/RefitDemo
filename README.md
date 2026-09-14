# RefitDemo (.NET 10 & Refit v15.2)

A modern implementation of the tutorial **"Using Refit in .NET"** by Sena Kılıçarslan, upgraded to **.NET 10** and the latest **Refit v15.2.0** library.

[![CI/CD Pipeline - Build, Test, ACR Push & GitOps Deploy](https://github.com/dheerajbhavsar/RefitDemo/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/dheerajbhavsar/RefitDemo/actions/workflows/ci-cd.yml)
---

## 📖 Overview

[Refit](https://github.com/reactiveui/refit) is an automatic type-safe REST library for .NET inspired by Square's Retrofit. It turns your REST API into a live interface where network calls, URL parameter replacements, request bodies, and headers are defined declaratively via attributes.

This project implements an ASP.NET Core Web API that proxies requests to **The Movie Database (TMDB) API v3**, demonstrating:

1. **Declarative Interface Definition** (`ITmdbApi`): Mapping REST endpoints to C# methods.
2. **Header Management**: Interface-level headers (`accept`, `Authorization: Bearer`) and method-level headers (`Content-Type`).
3. **Dynamic / Static Bearer Authentication**: Using `RefitSettings.AuthorizationHeaderValueGetter` with `ValueTask<string>`.
4. **Options Pattern & Fail-Fast Validation**: Using `IOptions<TmdbOptions>` with `ValidateDataAnnotations()`, `ValidateOnStart()`, and JWT Regex validation (`^eyJ...`) to prevent the application from booting without a valid token format.
5. **Modern .NET 10 HybridCache**: Integrating `Microsoft.Extensions.Caching.Hybrid` for high-performance L1 in-process caching with automatic cache stampede protection and tag-based invalidation (`RemoveByTagAsync`).
6. **Dependency Injection**: Registering Refit with `HttpClientFactory` (`services.AddRefitClient<ITmdbApi>()`).
7. **Request & Response Body Serialization**: Automatic JSON serialization for request bodies (`[Body] Rating rating`) and response models.
8. **Metadata & Error Inspection with `ApiResponse<T>`**: Using Refit's generic response wrapper to inspect HTTP status codes, error payloads, and response headers without throwing unhandled exceptions.
9. **Interactive API UI**: Powered by `Microsoft.AspNetCore.OpenApi` and `Scalar.AspNetCore` in .NET 10.

---

## 🚀 What's New in This .NET 10 Implementation

Compared to the original .NET 8 article:
- **Target Framework**: .NET 10 (`net10.0`) with C# modern syntax (primary constructors, collection expressions).
- **Refit Version**: Upgraded to **Refit 15.2.0** and **Refit.HttpClientFactory 15.2.0**.
- **ValueTask Authorization**: Refit 15+ uses `ValueTask<string>` for `AuthorizationHeaderValueGetter` to optimize allocation overhead.
- **OpenAPI / Scalar UI**: Integrated .NET 10's native OpenAPI support (`Microsoft.AspNetCore.OpenApi`) paired with modern `Scalar.AspNetCore` interactive documentation (`/scalar/v1`).
- **Safety & Error Handling**: Robust handling of `ApiResponse<T>` with `ApiException` cast and status code preservation.

---

## 📁 Project Structure

```
RefitDemo/
├── global.json                         # Pinned to .NET 10 SDK (10.0.400)
├── RefitDemo.sln                       # Solution file
├── README.md                           # This documentation
└── RefitDemo/
    ├── RefitDemo.csproj                # Project file (net10.0, Refit 15.2.0, Scalar)
    ├── Program.cs                      # App startup, Refit DI registration, OpenAPI/Scalar
    ├── appsettings.json                # Configuration (TMDB BaseUrl and Token)
    ├── appsettings.Development.json    # Development configuration
    ├── RefitDemo.http                  # Ready-to-run HTTP test requests
    ├── Models/
    │   ├── Actor.cs                    # Actor data model
    │   ├── ActorList.cs                # Actor list wrapper (results array)
    │   ├── Movie.cs                    # Movie details data model
    │   ├── MovieList.cs                # Movie credits list wrapper (cast array)
    │   ├── Rating.cs                   # Rating request payload (value)
    │   └── ResponseBody.cs             # TMDB API status code & status message
    ├── Services/
    │   └── ITmdbApi.cs                 # Refit declarative interface for TMDB
    └── Controllers/
        └── MovieDbController.cs        # ASP.NET Core API controller exposing endpoints
```

---

## 🔑 TMDB API Token Configuration

To call live TMDB endpoints, obtain a free API Read Access Token (v4 auth token) from [The Movie Database](https://www.themoviedb.org/settings/api):

1. Open `RefitDemo/appsettings.json` (or use .NET User Secrets):
   ```json
   {
     "Tmdb": {
       "BaseUrl": "https://api.themoviedb.org/3",
       "ApiReadAccessToken": "PASTE_YOUR_TMDB_API_READ_ACCESS_TOKEN_HERE"
     }
   }
   ```
2. Or via User Secrets:
   ```bash
   dotnet user-secrets set "Tmdb:ApiReadAccessToken" "<YOUR_ACCESS_TOKEN>" --project RefitDemo
   ```

---

## 🛠️ Running the Application

### 1. Build the Solution
```bash
dotnet build
```

### 2. Run the API
```bash
dotnet run --project RefitDemo/RefitDemo.csproj
```

### 3. Explore via Scalar UI
Open your browser and navigate to:
```
http://localhost:5104/scalar/v1
```
(or the HTTPS port shown in the console)

---

## 🧪 Available Endpoints & Testing

You can execute tests via **Scalar UI** or the included [`RefitDemo.http`](file:///c:/antigravity/RefitDemo/RefitDemo/RefitDemo.http) file:

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/MovieDb/actors?name={name}` | Search for an actor/actress by name (direct deserialization) |
| `GET` | `/api/MovieDb/actors/with-response?name={name}` | Search actor with `ApiResponse<T>` (metadata & error inspection) |
| `GET` | `/api/MovieDb/actors/{actorId}/movies` | Get movie credits of an actor/actress |
| `POST` | `/api/MovieDb/movies/{movieId}/rating` | Submit a movie rating (`{"value": 8.5}`) |
| `DELETE` | `/api/MovieDb/movies/{movieId}/rating` | Delete a rating from a movie |

---

## 🐳 Docker Deployment (Production-Grade & Secure)

The project includes an enterprise-grade multi-stage [`Dockerfile`](file:///c:/antigravity/RefitDemo/Dockerfile) hardened for production:

- **Non-Root Execution**: Runs as the built-in `app` user (`UID 1654`) to prevent privilege escalation.
- **Layer Caching**: Copies `.sln` and `.csproj` dependencies first, restoring NuGet packages in an isolated cached layer.
- **In-Container Test Gate**: Runs `dotnet test` during image build, guaranteeing that images with failing tests are never built.
- **Diagnostics Disabled**: `DOTNET_EnableDiagnostics=0` to close internal diagnostic ports in production.
- **Health Checks**: Uses `/health` with Docker `HEALTHCHECK` probe.
- **Configuration Defaults**: Bundles `ASPNETCORE_*` and `Tmdb__*` environment variables.

### Build the Docker Image
```bash
docker build -t refitdemo:latest .
```

### Run the Container
Pass your TMDB Read Access Token via environment variable `-e`:
```bash
docker run -d \
  --name refitdemo \
  -p 8080:8080 \
  -e Tmdb__ApiReadAccessToken="YOUR_TMDB_JWT_TOKEN" \
  refitdemo:latest
```

### Test Container Health
```bash
curl http://localhost:8080/health
# Returns: Healthy
```

### Run with Docker Compose
1. Copy template and configure `.env`:
   ```bash
   cp .env.example .env
   ```
2. Start the service:
   ```bash
   docker compose up -d
   ```

---

## 🚀 CI/CD & GitOps: GitHub Actions Secrets & Variables Guide

The repository includes a production CI/CD workflow ([`.github/workflows/ci-cd.yml`](file:///c:/antigravity/RefitDemo/.github/workflows/ci-cd.yml)) that builds, tests, pushes container images to Azure Container Registry (ACR), and triggers ArgoCD deployments.

### 📍 Where to Configure in GitHub

Navigate to your GitHub repository in your browser:
```
Your GitHub Repository
  └── ⚙️ Settings (top tab)
        └── 🔐 Secrets and variables (left sidebar)
              └── ⚡ Actions
```
On this page, you will see two tabs: **Secrets** and **Variables**.

```
[ Secrets ]    [ Variables ]
    ├── "New repository secret"   <-- For passwords, tokens, private keys (masked in logs)
    └── "New repository variable" <-- For non-sensitive configs (visible in logs)
```

---

### 🔑 Configuration Options (Choose Option A or Option B)

The pipeline automatically inspects your repository. You can choose either authentication method:

#### 👉 Option A: Username & Password Authentication (Quickest Setup)

| Configuration Item | Type | Where to Add | Description / Value |
|---|---|---|---|
| `ACR_NAME` | **Variable** *(or Secret)* | **Variables** tab > *New repository variable* | Name of your registry, e.g. `acrrefitdemo1001` (without `.azurecr.io`) |
| `ACR_USERNAME` | **Secret** *(or Variable)* | **Secrets** tab > *New repository secret* | ACR Admin username (from `az acr credential show`) |
| `ACR_PASSWORD` | **Secret** | **Secrets** tab > *New repository secret* | ACR Admin password (masked) |

> [!TIP]
> **How to get ACR Username & Password via Azure CLI:**
> ```bash
> # 1. Enable admin user
> az acr update --name <ACR_NAME> --admin-enabled true
> 
> # 2. View credentials
> az acr credential show --name <ACR_NAME> --query "[username, passwords[0].value]" -o tsv
> ```

---

#### 👉 Option B: Azure OIDC Federated Credentials (Passwordless / Recommended)

| Configuration Item | Type | Where to Add | Description / Value |
|---|---|---|---|
| `ACR_NAME` | **Variable** *(or Secret)* | **Variables** tab > *New repository variable* | Name of your registry, e.g. `acrrefitdemo1001` |
| `AZURE_SUBSCRIPTION_ID` | **Variable** *(or Secret)* | **Variables** tab > *New repository variable* | Your Azure Subscription ID (`az account show --query id -o tsv`) |
| `AZURE_TENANT_ID` | **Variable** *(or Secret)* | **Variables** tab > *New repository variable* | Your Azure Tenant ID (`az account show --query tenantId -o tsv`) |
| `AZURE_CLIENT_ID` | **Secret** | **Secrets** tab > *New repository secret* | App Registration Application (Client) ID |

---

### 🌍 Repository Scope vs. Environment Scope

GitHub allows configuring settings at two scopes:

1. **Repository Secrets & Variables** *(Default & Simplest)*:
   - Available to all workflow runs across branches.
   - Configured under: `Settings` > `Secrets and variables` > `Actions`.
2. **Environment Secrets & Variables** *(Advanced / Multi-Environment)*:
   - Configured under: `Settings` > `Environments` > Create environment (e.g. `Production` or `Staging`).
   - Allows setting **deployment protection rules** (e.g. required reviewers / manual approval before deploying to Production).
   - If configured in an environment, reference `environment: Production` in your workflow job.

> [!NOTE]
> The workflow in this project supports both `${{ vars.ACR_NAME }}` and `${{ secrets.ACR_NAME }}` interchangeably. If you place all values into **Secrets**, it will work immediately!

---

## 📝 Article References

- Original Medium Article: [Using Refit in .NET](https://medium.com/net-core/using-refit-in-net-0843bb199987) by Sena Kılıçarslan
- Original Repository: [AspNetCoreRefitDemo](https://github.com/kilicars/AspNetCoreRefitDemo)
- Official Refit Documentation: [ReactiveUI Refit](https://github.com/reactiveui/refit)

