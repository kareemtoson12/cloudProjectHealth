#!/bin/bash

# Check if environment argument is provided
if [ -z "$1" ]; then
    echo "Usage: ./deploy.sh <environment>"
    echo "Environments: dev, staging, prod"
    exit 1
fi

ENV=$1
NAMESPACE="healthcare-$ENV"

# Validate environment
if [[ ! "$ENV" =~ ^(dev|staging|prod)$ ]]; then
    echo "Invalid environment. Use: dev, staging, or prod"
    exit 1
fi

# Create namespace if it doesn't exist
kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

# Apply base configurations
echo "Applying base configurations..."
kubectl apply -f k8s/base/configmap.yaml -n $NAMESPACE

# Apply environment-specific configurations
echo "Applying $ENV environment configurations..."
kubectl apply -f k8s/overlays/$ENV/ -n $NAMESPACE

# Apply deployments
echo "Applying deployments..."
kubectl apply -f k8s/base/deployment.yaml -n $NAMESPACE

# Apply services
echo "Applying services..."
kubectl apply -f k8s/base/service.yaml -n $NAMESPACE

# Apply HPA
echo "Applying Horizontal Pod Autoscaler..."
kubectl apply -f k8s/base/hpa.yaml -n $NAMESPACE

# Apply Ingress (only for staging and prod)
if [[ "$ENV" =~ ^(staging|prod)$ ]]; then
    echo "Applying Ingress..."
    kubectl apply -f k8s/base/ingress.yaml -n $NAMESPACE
fi

# Wait for deployments to be ready
echo "Waiting for deployments to be ready..."
kubectl rollout status deployment/healthcare-app -n $NAMESPACE
kubectl rollout status deployment/healthcare-db -n $NAMESPACE

echo "Deployment to $ENV environment completed successfully!"
echo "You can check the status using:"
echo "kubectl get pods -n $NAMESPACE"
echo "kubectl get services -n $NAMESPACE"
if [[ "$ENV" =~ ^(staging|prod)$ ]]; then
    echo "kubectl get ingress -n $NAMESPACE"
fi 