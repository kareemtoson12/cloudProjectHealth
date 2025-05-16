# Kubernetes Deployment Guide

## Prerequisites

- Kubernetes cluster (Minikube, Kind, or cloud provider)
- kubectl configured
- Docker images built and pushed to registry

## Directory Structure

```
k8s/
├── base/
│   ├── deployment.yaml
│   ├── service.yaml
│   ├── configmap.yaml
│   ├── hpa.yaml
│   └── ingress.yaml
├── overlays/
│   ├── dev/
│   ├── staging/
│   └── prod/
├── deploy.sh
└── README.md
```

## Deployment

### 1. Development Environment

```bash
# Deploy to development
./deploy.sh dev

# Check deployment status
kubectl get pods -n healthcare-dev
kubectl get services -n healthcare-dev
```

### 2. Staging Environment

```bash
# Deploy to staging
./deploy.sh staging

# Check deployment status
kubectl get pods -n healthcare-staging
kubectl get services -n healthcare-staging
```

### 3. Production Environment

```bash
# Deploy to production
./deploy.sh prod

# Check deployment status
kubectl get pods -n healthcare-prod
kubectl get services -n healthcare-prod
kubectl get ingress -n healthcare-prod
```

## Monitoring and Maintenance

### Check Pod Status
```bash
kubectl get pods -n <namespace>
kubectl describe pod <pod-name> -n <namespace>
```

### Check Logs
```bash
kubectl logs <pod-name> -n <namespace>
kubectl logs -f <pod-name> -n <namespace>  # Follow logs
```

### Scale Deployments
```bash
kubectl scale deployment <deployment-name> --replicas=5 -n <namespace>
```

### Rolling Updates
```bash
# Update image
kubectl set image deployment/<deployment-name> <container-name>=<new-image> -n <namespace>

# Check rollout status
kubectl rollout status deployment/<deployment-name> -n <namespace>

# Rollback if needed
kubectl rollout undo deployment/<deployment-name> -n <namespace>
```

## Troubleshooting

### Common Issues

1. **Pods in CrashLoopBackOff**
   ```bash
   kubectl describe pod <pod-name> -n <namespace>
   kubectl logs <pod-name> -n <namespace>
   ```

2. **Services Not Accessible**
   ```bash
   kubectl get endpoints -n <namespace>
   kubectl describe service <service-name> -n <namespace>
   ```

3. **Ingress Issues**
   ```bash
   kubectl describe ingress -n <namespace>
   kubectl get events -n <namespace>
   ```

### Health Checks

All services include health checks:
- Liveness probe: `/health`
- Readiness probe: `/health`
- Initial delay: 30s
- Period: 10s

## Security

- All sensitive data is stored in Kubernetes Secrets
- TLS is enabled for all external traffic
- Network policies restrict pod-to-pod communication
- Resource limits prevent resource exhaustion

## Backup and Restore

### Database Backups
```bash
# Backup
kubectl exec <pod-name> -n <namespace> -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "BACKUP DATABASE [DatabaseName] TO DISK = N'/var/opt/mssql/backup/backup.bak'"

# Restore
kubectl exec <pod-name> -n <namespace> -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "RESTORE DATABASE [DatabaseName] FROM DISK = N'/var/opt/mssql/backup/backup.bak'"
```

## Cleanup

```bash
# Delete all resources in namespace
kubectl delete namespace <namespace>

# Delete specific resources
kubectl delete deployment <deployment-name> -n <namespace>
kubectl delete service <service-name> -n <namespace>
kubectl delete ingress <ingress-name> -n <namespace>
``` 