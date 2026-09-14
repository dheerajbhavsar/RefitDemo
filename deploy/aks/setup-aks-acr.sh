#!/usr/bin/env bash
# ==============================================================================
# Automated Provisioning of Azure Container Registry (ACR) and AKS Cluster
#
# Prerequisites:
#   - Azure CLI installed (az --version)
#   - Logged in to Azure (az login)
#   - Appropriate subscription selected (az account set --subscription <id>)
#
# Usage:
#   chmod +x deploy/aks/setup-aks-acr.sh
#   ./deploy/aks/setup-aks-acr.sh
# ==============================================================================

set -euo pipefail

# Configuration defaults (change as desired)
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-refitdemo-prod}"
LOCATION="${LOCATION:-eastus}"
CLUSTER_NAME="${CLUSTER_NAME:-aks-refitdemo-prod}"
# ACR names must be alphanumeric only and globally unique (max 50 chars)
RANDOM_SUFFIX=$(head /dev/urandom | tr -dc 'a-z0-9' | head -c 4 || echo "1001")
ACR_NAME="${ACR_NAME:-acrrefitdemo${RANDOM_SUFFIX}}"
NODE_COUNT=2
VM_SIZE="Standard_D2s_v5"

echo "=============================================================================="
echo "🚀 Provisioning Production-Ready AKS & ACR Infrastructure"
echo "=============================================================================="
echo "📍 Resource Group: $RESOURCE_GROUP"
echo "📍 Region:         $LOCATION"
echo "📍 ACR Name:       $ACR_NAME"
echo "📍 AKS Cluster:    $CLUSTER_NAME"
echo "=============================================================================="

# 1. Create Azure Resource Group
echo ""
echo "1️⃣ Creating Resource Group '$RESOURCE_GROUP' in '$LOCATION'..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output table

# 2. Create Azure Container Registry (ACR)
echo ""
echo "2️⃣ Creating Azure Container Registry '$ACR_NAME' (Standard SKU)..."
az acr create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$ACR_NAME" \
  --sku Standard \
  --admin-enabled false \
  --output table

# 3. Create Azure Kubernetes Service (AKS) Cluster
echo ""
echo "3️⃣ Creating AKS cluster '$CLUSTER_NAME' with OIDC & Workload Identity enabled..."
az aks create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$CLUSTER_NAME" \
  --node-count "$NODE_COUNT" \
  --node-vm-size "$VM_SIZE" \
  --enable-managed-identity \
  --enable-oidc-issuer \
  --enable-workload-identity \
  --network-plugin azure \
  --generate-ssh-keys \
  --output table

# 4. Attach ACR to AKS (Passwordless AcrPull Role Assignment)
echo ""
echo "4️⃣ Attaching ACR '$ACR_NAME' to AKS '$CLUSTER_NAME' for native AcrPull role..."
az aks update \
  --resource-group "$RESOURCE_GROUP" \
  --name "$CLUSTER_NAME" \
  --attach-acr "$ACR_NAME" \
  --output table

# 5. Fetch Kubernetes Cluster Credentials
echo ""
echo "5️⃣ Fetching kubectl credentials for cluster '$CLUSTER_NAME'..."
az aks get-credentials \
  --resource-group "$RESOURCE_GROUP" \
  --name "$CLUSTER_NAME" \
  --overwrite-existing

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)

echo ""
echo "=============================================================================="
echo "🎉 Infrastructure Setup Complete!"
echo "=============================================================================="
echo ""
echo "🔐 Authentication Options for GitHub Actions & Kubernetes:"
echo ""
echo "👉 Option 1: Username & Password Authentication"
echo "   Enable ACR Admin account:"
echo "     az acr update --name $ACR_NAME --admin-enabled true"
echo "   Retrieve credentials:"
echo "     az acr credential show --name $ACR_NAME --query \"[username, passwords[0].value]\" -o tsv"
echo "   Configure GitHub Secrets:"
echo "     - ACR_NAME:     $ACR_NAME"
echo "     - ACR_USERNAME: (username from above command)"
echo "     - ACR_PASSWORD: (password from above command)"
echo ""
echo "👉 Option 2: Azure OIDC Federated Credentials (Passwordless)"
echo "   Configure GitHub Secrets:"
echo "     - ACR_NAME:              $ACR_NAME"
echo "     - AZURE_SUBSCRIPTION_ID: $SUBSCRIPTION_ID"
echo "     - AZURE_TENANT_ID:       $TENANT_ID"
echo "     - AZURE_CLIENT_ID:       <YOUR_AZURE_APP_CLIENT_ID>"
echo ""
echo "👉 Next Step: Run the ArgoCD Bootstrap Script to install GitOps:"
echo "   ./deploy/argocd/bootstrap-argocd.sh <YOUR_GITHUB_REPO_URL>"
echo "=============================================================================="
