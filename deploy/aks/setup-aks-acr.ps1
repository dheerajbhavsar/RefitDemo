# ==============================================================================
# Automated Provisioning of Azure Container Registry (ACR) and AKS Cluster (PowerShell)
#
# Prerequisites:
#   - Azure CLI installed (az --version)
#   - Logged in to Azure (az login)
#   - Appropriate subscription selected (az account set --subscription <id>)
#
# Usage:
#   .\deploy\aks\setup-aks-acr.ps1
# ==============================================================================

[CmdletBinding()]
param (
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = "rg-refitdemo-prod",

    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus",

    [Parameter(Mandatory=$false)]
    [string]$ClusterName = "aks-refitdemo-prod",

    [Parameter(Mandatory=$false)]
    [string]$AcrName = "",

    [Parameter(Mandatory=$false)]
    [int]$NodeCount = 2,

    [Parameter(Mandatory=$false)]
    [string]$VmSize = "Standard_D2s_v5"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($AcrName)) {
    $randomSuffix = -join ((97..122) + (48..57) | Get-Random -Count 4 | ForEach-Object { [char]$_ })
    $AcrName = "acrrefitdemo$randomSuffix"
}

Write-Host "==============================================================================" -ForegroundColor Cyan
Write-Host "🚀 Provisioning Production-Ready AKS & ACR Infrastructure" -ForegroundColor Cyan
Write-Host "==============================================================================" -ForegroundColor Cyan
Write-Host "📍 Resource Group: $ResourceGroup"
Write-Host "📍 Region:         $Location"
Write-Host "📍 ACR Name:       $AcrName"
Write-Host "📍 AKS Cluster:    $ClusterName"
Write-Host "=============================================================================="

# 1. Create Azure Resource Group
Write-Host "`n1️⃣ Creating Resource Group '$ResourceGroup' in '$Location'..."
az group create --name $ResourceGroup --location $Location --output table

# 2. Create Azure Container Registry (ACR)
Write-Host "`n2️⃣ Creating Azure Container Registry '$AcrName' (Standard SKU)..."
az acr create --resource-group $ResourceGroup --name $AcrName --sku Standard --admin-enabled false --output table

# 3. Create AKS Cluster
Write-Host "`n3️⃣ Creating AKS cluster '$ClusterName' with OIDC & Workload Identity enabled..."
az aks create `
  --resource-group $ResourceGroup `
  --name $ClusterName `
  --node-count $NodeCount `
  --node-vm-size $VmSize `
  --enable-managed-identity `
  --enable-oidc-issuer `
  --enable-workload-identity `
  --network-plugin azure `
  --generate-ssh-keys `
  --output table

# 4. Attach ACR to AKS
Write-Host "`n4️⃣ Attaching ACR '$AcrName' to AKS '$ClusterName' for native AcrPull role..."
az aks update --resource-group $ResourceGroup --name $ClusterName --attach-acr $AcrName --output table

# 5. Fetch Kubernetes Credentials
Write-Host "`n5️⃣ Fetching kubectl credentials for cluster '$ClusterName'..."
az aks get-credentials --resource-group $ResourceGroup --name $ClusterName --overwrite-existing

$subscriptionId = az account show --query id -o tsv
$tenantId = az account show --query tenantId -o tsv

Write-Host "`n==============================================================================" -ForegroundColor Green
Write-Host "🎉 Infrastructure Setup Complete!" -ForegroundColor Green
Write-Host "==============================================================================" -ForegroundColor Green
Write-Host "`n🔐 Authentication Options for GitHub Actions & Kubernetes:" -ForegroundColor Cyan
Write-Host "`n👉 Option 1: Username & Password Authentication" -ForegroundColor Yellow
Write-Host "   Enable ACR Admin account:"
Write-Host "     az acr update --name $AcrName --admin-enabled true"
Write-Host "   Retrieve credentials:"
Write-Host "     az acr credential show --name $AcrName --query '[username, passwords[0].value]' -o tsv"
Write-Host "   Configure GitHub Secrets:"
Write-Host "     - ACR_NAME:     $AcrName"
Write-Host "     - ACR_USERNAME: (username from above command)"
Write-Host "     - ACR_PASSWORD: (password from above command)"
Write-Host "`n👉 Option 2: Azure OIDC Federated Credentials (Passwordless)" -ForegroundColor Yellow
Write-Host "   Configure GitHub Secrets:"
Write-Host "     - ACR_NAME:              $AcrName"
Write-Host "     - AZURE_SUBSCRIPTION_ID: $subscriptionId"
Write-Host "     - AZURE_TENANT_ID:       $tenantId"
Write-Host "     - AZURE_CLIENT_ID:       <YOUR_AZURE_APP_CLIENT_ID>"
Write-Host "`n👉 Next Step: Run the ArgoCD Bootstrap Script to install GitOps:"
Write-Host "   .\deploy\argocd\bootstrap-argocd.ps1 -GitRepoUrl <YOUR_GITHUB_REPO_URL>"
Write-Host "=============================================================================="
