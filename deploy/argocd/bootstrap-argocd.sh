#!/usr/bin/env bash
# ==============================================================================
# Bootstrap ArgoCD on Azure Kubernetes Service (AKS) from Scratch
#
# Usage:
#   chmod +x deploy/argocd/bootstrap-argocd.sh
#   ./deploy/argocd/bootstrap-argocd.sh [GIT_REPO_URL]
# ==============================================================================

set -euo pipefail

GIT_REPO_URL="${1:-https://github.com/dheeraj-89/RefitDemo.git}"
ARGOCD_NAMESPACE="argocd"
APP_NAMESPACE="refitdemo"
ARGOCD_VERSION="v2.14.0" # Official stable version

echo "=============================================================================="
echo "🚀 Bootstrapping ArgoCD GitOps on AKS Cluster from Scratch"
echo "=============================================================================="

# 1. Verify Kubernetes connectivity
echo "🔍 Checking cluster connectivity..."
if ! kubectl cluster-info > /dev/null 2>&1; then
  echo "❌ Error: Cannot connect to Kubernetes cluster. Ensure 'az aks get-credentials' was executed."
  exit 1
fi
echo "✅ Connected to cluster: $(kubectl config current-context)"

# 2. Create the ArgoCD namespace
echo ""
echo "📁 Creating namespace '${ARGOCD_NAMESPACE}'..."
kubectl create namespace "${ARGOCD_NAMESPACE}" --dry-run=client -o yaml | kubectl apply -f -

# 3. Install official ArgoCD manifests
echo ""
echo "📦 Installing ArgoCD (${ARGOCD_VERSION}) into '${ARGOCD_NAMESPACE}'..."
kubectl apply -n "${ARGOCD_NAMESPACE}" \
  -f "https://raw.githubusercontent.com/argoproj/argo-cd/${ARGOCD_VERSION}/manifests/install.yaml"

# 4. Wait for all ArgoCD components to become healthy
echo ""
echo "⏳ Waiting for ArgoCD control plane deployments to be ready (timeout 300s)..."
kubectl wait --for=condition=available --timeout=300s deployment/argocd-server -n "${ARGOCD_NAMESPACE}"
kubectl wait --for=condition=available --timeout=300s deployment/argocd-repo-server -n "${ARGOCD_NAMESPACE}"
kubectl wait --for=condition=available --timeout=300s deployment/argocd-dex-server -n "${ARGOCD_NAMESPACE}" || true
kubectl wait --for=condition=available --timeout=300s deployment/argocd-redis -n "${ARGOCD_NAMESPACE}" || true
kubectl wait --for=condition=available --timeout=300s deployment/argocd-applicationset-controller -n "${ARGOCD_NAMESPACE}"

echo "✅ All ArgoCD services are running and healthy!"

# 5. Retrieve Initial Admin Password
echo ""
echo "🔑 Retrieving initial ArgoCD admin password..."
ARGOCD_PASSWORD=$(kubectl -n "${ARGOCD_NAMESPACE}" get secret argocd-initial-admin-secret -o jsonpath="{.data.password}" | base64 -d)

# 6. Create application target namespace
echo ""
echo "📁 Pre-creating application namespace '${APP_NAMESPACE}'..."
kubectl create namespace "${APP_NAMESPACE}" --dry-run=client -o yaml | kubectl apply -f -

# 7. Update Application Manifest with Git URL and Deploy
echo ""
echo "📄 Updating ArgoCD Application manifest with Git Repo: ${GIT_REPO_URL}..."
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_FILE="${SCRIPT_DIR}/application.yaml"

# Replace repoURL in application.yaml
if [[ "$OSTYPE" == "darwin"* ]]; then
  sed -i '' "s|repoURL: .*|repoURL: ${GIT_REPO_URL}|" "${APP_FILE}"
else
  sed -i "s|repoURL: .*|repoURL: ${GIT_REPO_URL}|" "${APP_FILE}"
fi

echo "🚀 Applying ArgoCD Application resource..."
kubectl apply -f "${APP_FILE}"

echo ""
echo "=============================================================================="
echo "🎉 ArgoCD Installation & Application Registration Complete!"
echo "=============================================================================="
echo ""
echo "👤 Username: admin"
echo "🔐 Password: ${ARGOCD_PASSWORD}"
echo ""
echo "🌐 To access the ArgoCD Web UI locally, run:"
echo "   kubectl port-forward svc/argocd-server -n argocd 8080:443"
echo "   Then open: https://localhost:8080 (ignore self-signed SSL warning)"
echo ""
echo "📊 To check application synchronization status:"
echo "   kubectl get application -n argocd"
echo "   kubectl get pods -n ${APP_NAMESPACE}"
echo "=============================================================================="
