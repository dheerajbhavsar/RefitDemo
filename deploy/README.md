# RefitDemo Enterprise DevOps & GitOps Guide

Complete guide to building, packaging, deploying, and managing **RefitDemo (.NET 10)** on **Azure Kubernetes Service (AKS)** using **GitHub Actions**, **Azure Container Registry (ACR)**, **Helm**, and **ArgoCD**.

---

## 🏗️ Architecture Overview

```mermaid
flowchart TD
    subgraph Development
        Dev[Developer] -->|git push main| GitHub[GitHub Repository]
    end

    subgraph "GitHub Actions CI/CD"
        Test[1. dotnet test .NET 10] --> DockerBuild[2. Build Docker Image]
        DockerBuild --> ACRPush[3. Push Image to ACR]
        ACRPush --> UpdateTag[4. Commit new tag to Helm values.yaml]
        UpdateTag -->|git commit [skip ci]| GitHub
    end

    subgraph "Azure Kubernetes Service (AKS)"
        subgraph "argocd Namespace"
            ArgoController[ArgoCD Application Controller]
            ArgoServer[ArgoCD API & Web UI]
        end

        subgraph "refitdemo Namespace"
            HPA[Horizontal Pod Autoscaler]
            Service[ClusterIP Service: 80 -> 8080]
            Pod1[Pod 1: refitdemo:tag]
            Pod2[Pod 2: refitdemo:tag]
            Health[/health Endpoint Probe]
        end

        ArgoController -->|Reconciles desired state| Service
        ArgoController -->|Deploys pods| Pod1
        ArgoController -->|Deploys pods| Pod2
        HPA -->|Autoscales 2-10 replicas| Pod1
    end

    subgraph "Azure Container Registry (ACR)"
        ACR[(Azure Container Registry)]
    end

    GitHub -->|"Trigger CI"| Test
    ArgoController -.->|"Watches deploy/helm/refitdemo"| GitHub
    ACR -.->|"Passwordless Pull via Managed Identity"| Pod1
    ACR -.->|"Passwordless Pull via Managed Identity"| Pod2
```

---

## 📁 DevOps Directory Structure

```
RefitDemo/
├── .github/
│   └── workflows/
│       └── ci-cd.yml                   # GitHub Actions pipeline (Test, Build, ACR Push, GitOps tag update)
├── Dockerfile                          # Hardened non-root multi-stage Dockerfile
├── deploy/
│   ├── README.md                       # This documentation guide
│   ├── aks/
│   │   ├── setup-aks-acr.sh            # Automated Bash script to create RG, AKS, ACR & attach role
│   │   └── setup-aks-acr.ps1           # Automated PowerShell script for AKS & ACR
│   ├── argocd/
│   │   ├── application.yaml            # ArgoCD Application CRD resource
│   │   ├── bootstrap-argocd.sh         # Installs ArgoCD on AKS from scratch (Bash)
│   │   └── bootstrap-argocd.ps1        # Installs ArgoCD on AKS from scratch (PowerShell)
│   └── helm/
│       └── refitdemo/                  # Production Helm Chart
│           ├── Chart.yaml
│           ├── values.yaml
│           └── templates/
│               ├── _helpers.tpl
│               ├── deployment.yaml     # Non-root, probes, resource limits, secrets
│               ├── service.yaml        # ClusterIP routing
│               ├── serviceaccount.yaml # Pod identity
│               ├── secret.yaml         # TMDB API Read Access Token
│               ├── ingress.yaml        # Optional HTTP/HTTPS routing
│               └── hpa.yaml            # Horizontal Pod Autoscaling (2-10 replicas)
```

---

## 🚀 Step-by-Step Deployment Walkthrough

### Step 1: Provision Infrastructure (AKS & ACR)

Run the included automated provisioning script using either Bash or PowerShell:

**Using Bash (Linux / macOS / Azure Cloud Shell):**
```bash
chmod +x deploy/aks/setup-aks-acr.sh
./deploy/aks/setup-aks-acr.sh
```

**Using PowerShell (Windows / Cloud Shell):**
```powershell
.\deploy\aks\setup-aks-acr.ps1
```

The script automatically:
1. Creates the Resource Group (`rg-refitdemo-prod`).
2. Creates the Azure Container Registry (`acrrefitdemoXXXX`).
3. Creates the AKS cluster with Managed Identity & OIDC Issuer enabled.
4. **Attaches ACR to AKS** (`az aks update --attach-acr`) so Kubernetes nodes pull images passwordlessly without storing registry secrets.
5. Downloads the cluster `kubeconfig` to your local environment.

---

### Step 2: Configure GitHub Actions Secrets & Azure OIDC

In your GitHub repository, navigate to **Settings** > **Secrets and variables** > **Actions** and add the following repository secrets:

| Secret Name | Description | Example / Source |
|---|---|---|
| `ACR_NAME` | The name of your Azure Container Registry | e.g. `acrrefitdemo1001` (without `.azurecr.io`) |
| `AZURE_CLIENT_ID` | App Registration Client ID for GitHub OIDC | From Azure App Registration |
| `AZURE_TENANT_ID` | Azure Active Directory Tenant ID | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | Azure Subscription ID | `az account show --query id -o tsv` |

#### (Recommended) Configuring Azure OIDC Federated Credential
To allow GitHub Actions to authenticate passwordlessly without long-lived secret keys:
```bash
# 1. Create Azure AD Application & Service Principal
APP_ID=$(az ad app create --display-name "github-actions-refitdemo" --query appId -o tsv)
az ad sp create --id "$APP_ID"

# 2. Grant Contributor role on the Resource Group
az role assignment create \
  --role "Contributor" \
  --assignee "$APP_ID" \
  --scope "/subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/rg-refitdemo-prod"

# 3. Add Federated Credential for GitHub repository
az ad app federated-credential create \
  --id "$APP_ID" \
  --parameters "{
    \"name\": \"github-actions-main\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:<YOUR_GITHUB_USER_OR_ORG>/RefitDemo:ref:refs/heads/main\",
    \"description\": \"GitHub Actions OIDC\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"
```

---

### Step 3: Install ArgoCD on AKS from Scratch

Run the bootstrap script to deploy ArgoCD into the AKS cluster:

**Using Bash:**
```bash
chmod +x deploy/argocd/bootstrap-argocd.sh
./deploy/argocd/bootstrap-argocd.sh "https://github.com/<YOUR_USER_OR_ORG>/RefitDemo.git"
```

**Using PowerShell:**
```powershell
.\deploy\argocd\bootstrap-argocd.ps1 -GitRepoUrl "https://github.com/<YOUR_USER_OR_ORG>/RefitDemo.git"
```

The script will:
1. Create the `argocd` namespace.
2. Install the official ArgoCD control plane manifests.
3. Wait until all ArgoCD pods are running and healthy.
4. Extract the initial `admin` password.
5. Create the application namespace (`refitdemo`).
6. Register the ArgoCD `Application` resource linking to your Git repository.

#### Accessing the ArgoCD Web Dashboard
Run port-forwarding in a separate terminal:
```bash
kubectl port-forward svc/argocd-server -n argocd 8080:443
```
- Open browser at: **`https://localhost:8080`**
- Username: **`admin`**
- Password: The password output by the bootstrap script (or run `kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}" | base64 -d`)

---

### Step 4: The End-to-End GitOps Flow in Action

Once set up, your continuous deployment pipeline runs completely automatically:

```
Developer commits to main
       ↓
GitHub Actions runs 'build-and-test' (.NET 10 compilation & tests)
       ↓
GitHub Actions builds Docker image & pushes to ACR (tags: <sha>, latest)
       ↓
GitHub Actions updates 'tag:' in deploy/helm/refitdemo/values.yaml and pushes commit
       ↓
ArgoCD detects Git commit, compares with live AKS state, and automatically syncs!
       ↓
AKS performs rolling update with zero downtime
```

---

### Step 5: Verify the Deployment on AKS

#### Check ArgoCD Application Status
```bash
kubectl get application -n argocd
# NAME        SYNC STATUS   HEALTH STATUS
# refitdemo   Synced        Healthy
```

#### Check Pods and Scaling in the Application Namespace
```bash
kubectl get pods -n refitdemo -o wide
kubectl get hpa -n refitdemo
kubectl get svc -n refitdemo
```

#### Test Application Health Inside Cluster
```bash
kubectl run curl-test --rm -i --tty --image=curlimages/curl --restart=Never -- \
  curl -i http://refitdemo.refitdemo.svc.cluster.local/health
# Returns: HTTP/1.1 200 OK - Healthy
```

---

## 🔒 Security Best Practices Implemented

| Security Dimension | Implementation |
|---|---|
| **Non-Root Execution** | Runs as non-root user `app` (`UID 1654`), enforced both in Dockerfile and Kubernetes `podSecurityContext`. |
| **Zero-Trust Hardening** | `allowPrivilegeEscalation: false`, all Linux capabilities dropped (`capabilities.drop: [ALL]`). |
| **Passwordless Registry Access** | AKS nodes pull images using Azure Managed Identity (`AcrPull` role) — zero static Docker credentials stored in Kubernetes secrets. |
| **Passwordless CI/CD** | GitHub Actions connects to Azure via OpenID Connect (OIDC) Workload Identity Federation — no client secrets to expire or leak. |
| **Automated Drift Correction** | ArgoCD enforces `selfHeal: true`. Any manual `kubectl edit` or accidental cluster changes are instantly reverted to match the Git repository. |
| **Fail-Fast Validation** | The application verifies the TMDB API Read Access Token against JWT format at startup, preventing bad deployments from going live. |
