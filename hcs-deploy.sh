#!/bin/bash
# === Huawei Cloud Stack (HCS) Deployment ===
# Deploy to: ECS (Elastic Cloud Server), CCE (Kubernetes), or OBS (Object Storage)
#
# Usage:
#   ./hcs-deploy.sh ecs   <server-ip>   [user]
#   ./hcs-deploy.sh cce   <cluster-name>
#   ./hcs-deploy.sh obs   <bucket-name>

set -e

METHOD=${1:-help}
TARGET=${2}

case "$METHOD" in
  ecs)
    # === Deploy to ECS (Virtual Machine) ===
    SERVER_IP="$TARGET"
    USER="${3:-root}"

    if [ -z "$SERVER_IP" ]; then
      echo "ERROR: Provide server IP. Usage: $0 ecs <ip> [user]"
      exit 1
    fi

    echo "=== Deploying to ECS: $USER@$SERVER_IP ==="

    # Copy deploy script and run on server
    scp deploy.sh nginx.conf "$USER@$SERVER_IP:/tmp/"
    scp *.html *.pdf "$USER@$SERVER_IP:/tmp/"
    ssh "$USER@$SERVER_IP" "cd /tmp && chmod +x deploy.sh && sudo ./deploy.sh"
    echo "Done! Site: http://$SERVER_IP"
    ;;

  cce)
    # === Deploy to CCE (Kubernetes) ===
    CLUSTER="$TARGET"

    if [ -z "$CLUSTER" ]; then
      echo "ERROR: Provide cluster name. Usage: $0 cce <cluster-name>"
      exit 1
    fi

    echo "=== Deploying to CCE cluster: $CLUSTER ==="

    # Build & push Docker image to SWR
    SWR_REGISTRY="swr.$(echo $HWC_REGION | tr -d '\n').myhuaweicloud.com"
    IMAGE="$SWR_REGISTRY/whirl/whirl-site:latest"

    echo "1. Building Docker image..."
    docker build -t "$IMAGE" .

    echo "2. Pushing to SWR..."
    docker push "$IMAGE"

    echo "3. Applying Kubernetes config..."
    kubectl apply -f k8s-deployment.yaml
    kubectl rollout status deployment/whirl-site
    echo "Done!"
    ;;

  obs)
    # === Deploy to OBS (Static Website Hosting) ===
    BUCKET="$TARGET"

    if [ -z "$BUCKET" ]; then
      echo "ERROR: Provide bucket name. Usage: $0 obs <bucket-name>"
      exit 1
    fi

    echo "=== Deploying to OBS bucket: $BUCKET ==="

    # Upload all static files
    for f in *.html *.pdf; do
      echo "Uploading: $f"
      obsutil cp "$f" "obs://$BUCKET/$f" --acl public-read
    done

    # Enable static website hosting
    echo "Enabling static website hosting..."
    obsutil website obs://$BUCKET --method put --index index.html

    echo "Done! Site: https://$BUCKET.obs-website.$(echo $HWC_REGION | tr -d '\n').myhuaweicloud.com"
    echo ""
    echo "NOTE: Set HWC_REGION and HWC_ACCESS_KEY/HWC_SECRET_KEY env vars first!"
    ;;

  *)
    echo "Huawei Cloud Stack Deployment Script"
    echo ""
    echo "Usage: $0 {ecs|cce|obs} <target>"
    echo ""
    echo "  ecs  - Deploy to Elastic Cloud Server (VM)"
    echo "  cce  - Deploy to Cloud Container Engine (Kubernetes)"
    echo "  obs  - Deploy to Object Storage (static hosting)"
    echo ""
    echo "Prerequisites:"
    echo "  export HWC_REGION=ru-moscow-1"
    echo "  export HWC_ACCESS_KEY=your_access_key"
    echo "  export HWC_SECRET_KEY=your_secret_key"
    ;;
esac
