# ==============================================================================
# Bootstrap ArgoCD on Azure Kubernetes Service (AKS) from Scratch (PowerShell)
#
# Usage:
#   .\deploy\argocd\bootstrap-argocd.ps1 -GitRepoUrl "https://github.com/your-org/RefitDemo.git"
# ==============================================================================

[CmdletBinding()]
param (
    [Parameter(Mandatory=$false)]
    [string]$GitRepoUrl = "https://github.com/dheeraj-89/RefitDemo.git",

    [Parameter(Mandatory=$false)]
    [string]$ArgoCdNamespace = "argocd",

    [Parameter(Mandatory=$false)]
    [string]$AppNamespace = "refitdemo",

    [Parameter(Mandatory=$false)]
    [string]$ArgoCdVersion = "v2.14.0"
)

$ErrorActionPreference = "Stop"

Write-Host "==============================================================================" -ForegroundColor Cyan
Write-Host "🚀 Bootstrapping ArgoCD GitOps on AKS Cluster from Scratch" -ForegroundColor Cyan
Write-Host "==============================================================================" -ForegroundColor Cyan

# 1. Verify Kubernetes connectivity
Write-Host "🔍 Checking cluster connectivity..."
try {
    $context = kubectl config current-context
    Write-Host "✅ Connected to cluster: $context" -ForegroundColor Green
} catch {
    Write-Error "❌ Error: Cannot connect to Kubernetes cluster. Ensure 'az aks get-credentials' was executed."
}

# 2. Create ArgoCD namespace
Write-Host "`n📁 Creating namespace '$ArgoCdNamespace'..."
kubectl create namespace $ArgoCdNamespace --dry-run=client -o yaml | kubectl apply -f -

# 3. Install official ArgoCD manifests
$manifestUrl = "https://raw.githubusercontent.com/argoproj/argo-cd/$ArgoCdVersion/manifests/install.yaml"
Write-Host "`n📦 Installing ArgoCD ($ArgoCdVersion) into '$ArgoCdNamespace'..."
kubectl apply -n $ArgoCdNamespace -f $manifestUrl

# 4. Wait for ArgoCD deployments
Write-Host "`n⏳ Waiting for ArgoCD control plane deployments to be ready (timeout 300s)..."
kubectl wait --for=condition=available --timeout=300s deployment/argocd-server -n $ArgoCdNamespace
kubectl wait --for=condition=available --timeout=300s deployment/argocd-repo-server -n $ArgoCdNamespace
kubectl wait --for=condition=available --timeout=300s deployment/argocd-applicationset-controller -n $ArgoCdNamespace
Write-Host "✅ ArgoCD control plane is healthy!" -ForegroundColor Green

# 5. Retrieve admin password
Write-Host "`n🔑 Retrieving initial ArgoCD admin password..."
$encodedPassword = kubectl -n $ArgoCdNamespace get secret argocd-initial-admin-secret -o jsonpath="{.data.password}"
$adminPassword = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($encodedPassword))

# 6. Create application namespace
Write-Host "`n📁 Pre-creating application namespace '$AppNamespace'..."
kubectl create namespace $AppNamespace --dry-run=client -o yaml | kubectl apply -f -

# 7. Apply Application manifest
$appFile = Join-Path $PSScriptRoot "application.yaml"
Write-Host "`n📄 Updating ArgoCD Application manifest with Git URL: $GitRepoUrl..."
(Get-Content $appFile) -replace 'repoURL: .*', "repoURL: $GitRepoUrl" | Set-Content $appFile

Write-Host "🚀 Applying ArgoCD Application resource..."
kubectl apply -f $appFile

Write-Host "`n==============================================================================" -ForegroundColor Green
Write-Host "🎉 ArgoCD Installation & Application Registration Complete!" -ForegroundColor Green
Write-Host "==============================================================================" -ForegroundColor Green
Write-Host "👤 Username: admin"
Write-Host "🔐 Password: $adminPassword" -ForegroundColor Yellow
Write-Host "`n🌐 To access the ArgoCD Web UI locally, run:"
Write-Host "   kubectl port-forward svc/argocd-server -n $ArgoCdNamespace 8080:443"
Write-Host "   Then navigate to: https://localhost:8080 (ignore self-signed TLS warning)"
Write-Host "`n📊 To check application synchronization status:"
Write-Host "   kubectl get application -n $ArgoCdNamespace"
Write-Host "   kubectl get pods -n $AppNamespace"
Write-Host "=============================================================================="
