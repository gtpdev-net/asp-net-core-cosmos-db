# Azure Resource Requirements - Production Environment

## Document Information

**Date**: 2026-01-30  
**Status**: Active  
**Deployment Model**: Hybrid (Consolidated + Independent)  
**Related Documentation**: 
- [Deployment Strategy](deployment-strategy.md) - Overall strategic approach
- [ADR 0004: Progressive Deployment Strategy](adr/0004-progressive-deployment-strategy.md) - Evidence-based approach
- [ADR 0005: Hybrid Production Architecture](adr/0005-hybrid-production-architecture.md) - Hybrid deployment model
- [Consolidated Deployment Architecture](consolidated-deployment-architecture.md) - Consolidated deployment details
- [App Service Private Endpoint Architecture](app-service-private-endpoint-architecture.md) - Network architecture

## Overview

This document outlines the Azure resources required for the **Production environment** using a **hybrid deployment** model. Based on performance data collected from non-production, APIs are deployed either to a consolidated App Service (lazy APIs) or independent App Services (greedy APIs), optimizing for both cost and performance.

**Production Characteristics**:
- **Architecture**: Hybrid (consolidated deployment for lazy APIs + independent App Services for greedy APIs)
- **Cost**: ~$571-1,431/month (28-62% savings vs fully independent deployment)
- **Data-Driven**: API placement based on actual non-production performance metrics
- **Optimized**: Balance between cost efficiency and performance isolation
- **IP Address Space**: 172.18.121.0/24 (shared with non-production)

**Example Hybrid Configuration** (for 10 APIs):
- **Consolidated**: 8 lazy APIs in single App Service
- **Independent**: 2 greedy APIs in separate App Services
- **Flexibility**: Can adjust based on actual workload patterns

## Prerequisites

- **Non-production data**: Minimum 3 months of performance metrics from consolidated deployment
- **API classification**: Clear identification of "greedy" vs "lazy" APIs
- Azure subscription with appropriate permissions (Contributor or Owner role)
- Access to Azure Portal: https://portal.azure.com
- Azure CLI installed (for CLI-based steps): https://aka.ms/azure-cli
- **Existing infrastructure**: VPN Gateway, Private DNS zones, Private Endpoint subnet

## Resource Overview (Example: 8 Lazy + 2 Greedy)

```
Resource Group (rg-apis-prod)
│
├── Networking
│   ├── Virtual Network (vnet-apis-prod) - 172.18.121.0/24 [SHARED]
│   │   ├── snet-privateendpoint (172.18.121.160/27) [EXISTING - SHARED]
│   │   ├── snet-integration (172.18.121.192/26) [SHARED]
│   │   └── Business Layer (172.18.121.0/25) [AVAILABLE]
│   │
│   └── Network Security Group (nsg-integration-prod)
│
├── Compute - Consolidated Group (Lazy APIs)
│   └── App Service Plan (asp-consolidated-api-prod) - P1V3
│       └── App Service (1 consolidated host)
│           └── app-consolidated-api-prod
│               ├── Examples API (/api/examples) - LAZY
│               ├── Products API (/api/products) - LAZY
│               ├── Categories API (/api/categories) - LAZY
│               ├── Suppliers API (/api/suppliers) - LAZY
│               ├── Reports API (/api/reports) - LAZY
│               ├── Analytics API (/api/analytics) - LAZY
│               ├── Notifications API (/api/notifications) - LAZY
│               └── Settings API (/api/settings) - LAZY
│
├── Compute - Independent (Greedy APIs)
│   ├── App Service Plan (asp-orders-api-prod) - P1V3
│   │   └── App Service (app-orders-api-prod)
│   │       └── Orders API - GREEDY (CPU 75%, 5000 req/min)
│   │
│   └── App Service Plan (asp-customers-api-prod) - P1V3
│       └── App Service (app-customers-api-prod)
│           └── Customers API - GREEDY (CPU 68%, 3500 req/min)
│
├── Private Connectivity
│   └── Private Endpoints (3 total)
│       ├── pe-app-consolidated-api (→ 172.18.121.160)
│       ├── pe-app-orders-api (→ 172.18.121.161)
│       └── pe-app-customers-api (→ 172.18.121.162)
│
├── Security & Secrets
│   └── Key Vault (kv-apis-prod-unique)
│
└── Monitoring & Logging
    ├── Log Analytics Workspace (log-apis-prod)
    └── Application Insights (appi-apis-prod)
        └── Adaptive sampling enabled (production-ready)
```

## Decision Framework: API Placement

### Classification Criteria

Based on non-production monitoring data:

| Classification | CPU Usage | Memory Usage | Request Rate | Deployment Target |
|----------------|-----------|--------------|--------------|-------------------|
| **Lazy** | < 30% | < 40% | < 200 req/min | Consolidated App Service |
| **Moderate** | 30-60% | 40-70% | 200-1000 req/min | Consolidated (monitor closely) |
| **Greedy** | > 60% | > 70% | > 1000 req/min | Independent App Service |
| **Unpredictable** | Variable | Variable | Spiky | Independent App Service |

### Example API Classifications

From non-production monitoring:

```
Lazy APIs (Consolidated):
├── Examples API: CPU 15%, Memory 25%, 100 req/min
├── Products API: CPU 20%, Memory 30%, 150 req/min
├── Categories API: CPU 10%, Memory 20%, 50 req/min
├── Suppliers API: CPU 18%, Memory 28%, 80 req/min
├── Reports API: CPU 25%, Memory 35%, 200 req/min
├── Analytics API: CPU 22%, Memory 32%, 120 req/min
├── Notifications API: CPU 12%, Memory 22%, 60 req/min
└── Settings API: CPU 8%, Memory 18%, 30 req/min

Greedy APIs (Independent):
├── Orders API: CPU 75%, Memory 80%, 5000 req/min
└── Customers API: CPU 68%, Memory 72%, 3500 req/min
```

## Network Requirements

### IP Address Planning

**Shared Address Space**: Production shares VNet with non-production.

| Subnet Name | Address Range | Usable IPs | Purpose | Consumption (8+2) |
|-------------|---------------|------------|---------|-------------------|
| `snet-privateendpoint` | `172.18.121.160/27` | 27 | Private Endpoints | 3 IPs (consolidated + 2 independent) |
| `snet-integration` | `172.18.121.192/26` | 59 | VNet Integration | ~10-15 IPs (3 App Services × 2-3 instances each) |

**Production IP Consumption** (example):
- **Private Endpoints**: 3 IPs (1 consolidated + 2 greedy APIs)
- **VNet Integration**: ~10-15 IPs (for 3 App Services with multiple instances)
- **Total**: ~13-18 IPs (well within capacity)

### Scaling Capacity

| Scenario | Private Endpoints | VNet Integration IPs | Status |
|----------|-------------------|---------------------|---------|
| Current (8+2) | 3 | ~10-15 | ✅ Supported |
| Growth (6+4) | 5 | ~15-20 | ✅ Supported |
| Maximum (0+20) | 20 | ~40-60 | ✅ Supported |

### DNS Requirements

**Private DNS Zones** (existing, shared):
- `privatelink.azurewebsites.net` - All App Services
- `privatelink.documents.azure.com` - Cosmos DB
- `privatelink.database.windows.net` - Azure SQL
- `privatelink.vaultcore.azure.net` - Key Vault

**Production Hostnames**:
- Consolidated: `app-consolidated-api-prod.azurewebsites.net` → 172.18.121.160
- Orders: `app-orders-api-prod.azurewebsites.net` → 172.18.121.161
- Customers: `app-customers-api-prod.azurewebsites.net` → 172.18.121.162

**Client DLL Routing**:
```csharp
private readonly Dictionary<string, string> _apiHosts = new()
{
    // Lazy APIs - Consolidated
    ["examples"] = "app-consolidated-api-prod.azurewebsites.net",
    ["products"] = "app-consolidated-api-prod.azurewebsites.net",
    ["categories"] = "app-consolidated-api-prod.azurewebsites.net",
    // ... other lazy APIs
    
    // Greedy APIs - Independent
    ["orders"] = "app-orders-api-prod.azurewebsites.net",
    ["customers"] = "app-customers-api-prod.azurewebsites.net"
};
```

## Deployment Sequence

### Phase 1: Shared Infrastructure

1. Resource Group (`rg-apis-prod`)
2. Log Analytics Workspace (`log-apis-prod`)
3. Network Security Group (`nsg-integration-prod`)
4. Key Vault (`kv-apis-prod-unique`)
5. Application Insights (`appi-apis-prod`)

### Phase 2: Consolidated Deployment (Lazy APIs)

6. App Service Plan (`asp-consolidated-api-prod`)
7. App Service (`app-consolidated-api-prod`)
8. VNet Integration
9. Private Endpoint (`pe-app-consolidated-api`)

### Phase 3: Independent Deployments (Greedy APIs)

For each greedy API:
10. App Service Plan (`asp-{api-name}-api-prod`)
11. App Service (`app-{api-name}-api-prod`)
12. VNet Integration
13. Private Endpoint (`pe-app-{api-name}-api`)

---

## Deployment Steps

### 1. Resource Group

```bash
RESOURCE_GROUP="rg-apis-prod"
LOCATION="eastus2"

az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

### 2. Log Analytics Workspace

```bash
LOG_WORKSPACE="log-apis-prod"

az monitor log-analytics workspace create \
  --resource-group $RESOURCE_GROUP \
  --workspace-name $LOG_WORKSPACE \
  --location $LOCATION
```

### 3. Network Security Group

```bash
az network nsg create \
  --resource-group $RESOURCE_GROUP \
  --name nsg-integration-prod \
  --location $LOCATION
```

### 4. Key Vault

```bash
KEYVAULT_NAME="kv-apis-prod-$RANDOM"

az keyvault create \
  --resource-group $RESOURCE_GROUP \
  --name $KEYVAULT_NAME \
  --location $LOCATION \
  --enable-rbac-authorization true
```

### 5. Application Insights

```bash
APPINSIGHTS_NAME="appi-apis-prod"

az monitor app-insights component create \
  --resource-group $RESOURCE_GROUP \
  --app $APPINSIGHTS_NAME \
  --location $LOCATION \
  --workspace $LOG_WORKSPACE

# Note: Adaptive sampling enabled by default (production-appropriate)
```

---

## Consolidated Deployment (Lazy APIs)

### 6. App Service Plan (Consolidated)

```bash
ASP_CONSOLIDATED="asp-consolidated-api-prod"

az appservice plan create \
  --resource-group $RESOURCE_GROUP \
  --name $ASP_CONSOLIDATED \
  --is-linux \
  --sku P1V3 \
  --location $LOCATION

# Configure auto-scaling
az monitor autoscale create \
  --resource-group $RESOURCE_GROUP \
  --resource $ASP_CONSOLIDATED \
  --resource-type "Microsoft.Web/serverfarms" \
  --name "autoscale-$ASP_CONSOLIDATED" \
  --min-count 2 \
  --max-count 5 \
  --count 2

# Scale-out rule: CPU > 70%
az monitor autoscale rule create \
  --resource-group $RESOURCE_GROUP \
  --autoscale-name "autoscale-$ASP_CONSOLIDATED" \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 1

# Scale-in rule: CPU < 30%
az monitor autoscale rule create \
  --resource-group $RESOURCE_GROUP \
  --autoscale-name "autoscale-$ASP_CONSOLIDATED" \
  --condition "Percentage CPU < 30 avg 10m" \
  --scale in 1
```

### 7. App Service (Consolidated)

```bash
APP_CONSOLIDATED="app-consolidated-api-prod"

APPINSIGHTS_CONN=$(az monitor app-insights component show \
  --resource-group $RESOURCE_GROUP \
  --app $APPINSIGHTS_NAME \
  --query connectionString -o tsv)

az webapp create \
  --resource-group $RESOURCE_GROUP \
  --plan $ASP_CONSOLIDATED \
  --name $APP_CONSOLIDATED \
  --runtime "DOTNET:8.0"

az webapp config set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_CONSOLIDATED \
  --http20-enabled true \
  --min-tls-version 1.2 \
  --always-on true

az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_CONSOLIDATED \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="$APPINSIGHTS_CONN"
```

### 8-9. VNet Integration and Private Endpoint

```bash
# VNet Integration
az webapp vnet-integration add \
  --resource-group $RESOURCE_GROUP \
  --name $APP_CONSOLIDATED \
  --vnet vnet-apis-prod \
  --subnet snet-integration

# Private Endpoint
az network private-endpoint create \
  --resource-group $RESOURCE_GROUP \
  --name pe-app-consolidated-api \
  --vnet-name vnet-apis-prod \
  --subnet snet-privateendpoint \
  --private-connection-resource-id $(az webapp show \
    --resource-group $RESOURCE_GROUP \
    --name $APP_CONSOLIDATED \
    --query id -o tsv) \
  --group-id sites \
  --connection-name pe-conn-consolidated-api
```

---

## Independent Deployments (Greedy APIs)

Repeat for each greedy API (example: Orders API)

### 10. App Service Plan (Independent)

```bash
ASP_ORDERS="asp-orders-api-prod"

az appservice plan create \
  --resource-group $RESOURCE_GROUP \
  --name $ASP_ORDERS \
  --is-linux \
  --sku P1V3 \
  --location $LOCATION

# Custom auto-scaling for greedy API
az monitor autoscale create \
  --resource-group $RESOURCE_GROUP \
  --resource $ASP_ORDERS \
  --resource-type "Microsoft.Web/serverfarms" \
  --name "autoscale-$ASP_ORDERS" \
  --min-count 2 \
  --max-count 10 \
  --count 3

# Aggressive scale-out: CPU > 60%
az monitor autoscale rule create \
  --resource-group $RESOURCE_GROUP \
  --autoscale-name "autoscale-$ASP_ORDERS" \
  --condition "Percentage CPU > 60 avg 3m" \
  --scale out 2

# Conservative scale-in: CPU < 25%
az monitor autoscale rule create \
  --resource-group $RESOURCE_GROUP \
  --autoscale-name "autoscale-$ASP_ORDERS" \
  --condition "Percentage CPU < 25 avg 15m" \
  --scale in 1
```

### 11-13. App Service, VNet Integration, and Private Endpoint

```bash
APP_ORDERS="app-orders-api-prod"

# Create App Service
az webapp create \
  --resource-group $RESOURCE_GROUP \
  --plan $ASP_ORDERS \
  --name $APP_ORDERS \
  --runtime "DOTNET:8.0"

# Configure
az webapp config set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_ORDERS \
  --http20-enabled true \
  --min-tls-version 1.2 \
  --always-on true

az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_ORDERS \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="$APPINSIGHTS_CONN"

# VNet Integration
az webapp vnet-integration add \
  --resource-group $RESOURCE_GROUP \
  --name $APP_ORDERS \
  --vnet vnet-apis-prod \
  --subnet snet-integration

# Private Endpoint
az network private-endpoint create \
  --resource-group $RESOURCE_GROUP \
  --name pe-app-orders-api \
  --vnet-name vnet-apis-prod \
  --subnet snet-privateendpoint \
  --private-connection-resource-id $(az webapp show \
    --resource-group $RESOURCE_GROUP \
    --name $APP_ORDERS \
    --query id -o tsv) \
  --group-id sites \
  --connection-name pe-conn-orders-api
```

**Repeat for Customers API** (replace "orders" with "customers" in above commands)

---

## Deployment Manifest

### Purpose
Single source of truth for API placement decisions.

### File: deployment-manifest.json

```json
{
  "environment": "production",
  "lastUpdated": "2026-01-30",
  "apiClassifications": {
    "lazy": [
      {
        "name": "Examples",
        "project": "DataLayer.API.Example",
        "metrics": {
          "avgCpu": 15,
          "avgMemory": 25,
          "avgRequestsPerMin": 100
        }
      },
      {
        "name": "Products",
        "project": "DataLayer.API.Example.Products",
        "metrics": {
          "avgCpu": 20,
          "avgMemory": 30,
          "avgRequestsPerMin": 150
        }
      },
      // ... other lazy APIs
    ],
    "greedy": [
      {
        "name": "Orders",
        "project": "DataLayer.API.Example.Orders",
        "metrics": {
          "avgCpu": 75,
          "avgMemory": 80,
          "avgRequestsPerMin": 5000
        },
        "reason": "High request volume and CPU usage"
      },
      {
        "name": "Customers",
        "project": "DataLayer.API.Example.Customers",
        "metrics": {
          "avgCpu": 68,
          "avgMemory": 72,
          "avgRequestsPerMin": 3500
        },
        "reason": "High request volume"
      }
    ]
  },
  "deployment": {
    "consolidated": {
      "appService": "app-consolidated-api-prod",
      "appServicePlan": "asp-consolidated-api-prod",
      "privateEndpoint": "pe-app-consolidated-api",
      "hostname": "app-consolidated-api-prod.azurewebsites.net",
      "apis": ["Examples", "Products", "Categories", "Suppliers", "Reports", "Analytics", "Notifications", "Settings"]
    },
    "independent": [
      {
        "api": "Orders",
        "appService": "app-orders-api-prod",
        "appServicePlan": "asp-orders-api-prod",
        "privateEndpoint": "pe-app-orders-api",
        "hostname": "app-orders-api-prod.azurewebsites.net"
      },
      {
        "api": "Customers",
        "appService": "app-customers-api-prod",
        "appServicePlan": "asp-customers-api-prod",
        "privateEndpoint": "pe-app-customers-api",
        "hostname": "app-customers-api-prod.azurewebsites.net"
      }
    ]
  }
}
```

---

## Cost Estimation

### Monthly Cost Breakdown (8 Lazy + 2 Greedy)

#### Consolidated Group (8 Lazy APIs)

| Resource | SKU/Size | Quantity | Est. Monthly Cost |
|----------|----------|----------|-------------------|
| App Service Plan (Consolidated) | P1V3 (2-5 instances) | 1 | $150-375 |
| App Service | (uses plan) | 1 | Included |
| Private Endpoint | Standard | 1 | $7 |
| **Subtotal** | | | **$157-382** |

**Per-API Cost**: ~$20-48/month (for 8 lazy APIs)

#### Independent Deployments (2 Greedy APIs)

| Resource | SKU/Size | Quantity | Est. Monthly Cost |
|----------|----------|----------|-------------------|
| App Service Plans (Independent) | P1V3 (2-5 instances each) | 2 | $300-750 |
| App Services | (use plans) | 2 | Included |
| Private Endpoints | Standard | 2 | $14 |
| **Subtotal** | | | **$314-764** |

**Per-API Cost**: ~$157-382/month (for greedy APIs)

#### Shared Infrastructure

| Resource | SKU/Size | Quantity | Est. Monthly Cost |
|----------|----------|----------|-------------------|
| Cosmos DB | Serverless | 1 | $50-150 |
| Key Vault | Standard | 1 | $5 |
| Application Insights | Pay-as-you-go | 1 | $30-100 |
| Log Analytics | Pay-as-you-go | 1 | $15-30 |
| **Subtotal** | | | **$100-285** |

#### Total Production Cost

| Component | Monthly Cost |
|-----------|--------------|
| Consolidated (8 lazy) | $157-382 |
| Independent (2 greedy) | $314-764 |
| Shared Infrastructure | $100-285 |
| **TOTAL** | **$571-1,431/month** |

### Cost Comparison

| Deployment Model | Monthly Cost (10 APIs) | Savings vs Fully Independent |
|------------------|------------------------|------------------------------|
| Fully Independent | $1,500-2,000 | Baseline |
| Fully Consolidated | $227-467 | 85-88% |
| **Hybrid (8+2)** | **$571-1,431** | **28-62%** |

### Scaling Scenarios

| Configuration | Monthly Cost | Use Case |
|---------------|--------------|----------|
| 10 lazy + 0 greedy | $227-467 | All APIs low-traffic |
| 8 lazy + 2 greedy | $571-1,431 | Typical hybrid |
| 6 lazy + 4 greedy | $885-1,895 | More high-traffic APIs |
| 0 lazy + 10 greedy | $1,500-2,000 | All APIs high-traffic |

---

## Monitoring and Continuous Optimization

### Production Monitoring

Unlike non-production (100% sampling), production uses **adaptive sampling** to balance cost and observability.

### Monthly Architecture Reviews

**Schedule**: First Monday of each month

**Review Process**:
1. Analyze previous month's performance data
2. Review API classifications (any changes in patterns?)
3. Evaluate cost vs performance trade-offs
4. Identify APIs for reclassification
5. Plan architectural adjustments if needed

### KQL Queries for Production

```kusto
// APIs approaching greedy threshold
requests
| where timestamp > ago(30d)
| extend ApiName = tostring(customDimensions["api.name"])
| summarize 
    AvgDuration = avg(duration),
    RequestCount = count()
  by ApiName
| where AvgDuration > 400 or RequestCount > 1000000
| order by RequestCount desc

// Cost optimization opportunities
// Identify low-traffic independent APIs that could be consolidated
requests
| where timestamp > ago(30d)
| extend ApiName = tostring(customDimensions["api.name"])
| where ApiName in ("Orders", "Customers") // Independent APIs
| summarize RequestsPerDay = count() / 30.0 by ApiName
| where RequestsPerDay < 5000
```

---

## Disaster Recovery and High Availability

### High Availability Configuration

**App Service Plans**:
- Minimum 2 instances per plan (auto-scaling enabled)
- Deploy across availability zones (if supported in region)
- Health checks configured on all endpoints

**Private Endpoints**:
- Redundant by design (Azure-managed)
- Automatic failover within region

### Backup Strategy

1. **App Services**: 
   - Source code in Git (GitHub)
   - Configuration in Key Vault and App Service settings
   - Automated daily backups (if enabled)

2. **Cosmos DB**:
   - Continuous backup enabled (7-day retention minimum)
   - Point-in-time restore capability

3. **Key Vault**:
   - Soft-delete enabled (90-day retention)
   - Purge protection enabled

### Disaster Recovery

**RTO (Recovery Time Objective)**: 1-2 hours  
**RPO (Recovery Point Objective)**: 1 hour

**DR Plan**:
1. Maintain infrastructure-as-code (Bicep/Terraform)
2. Document deployment manifest
3. Regular DR drills (quarterly)
4. Failover procedures documented

---

## Validation and Testing

### Post-Deployment Validation

```bash
# Test consolidated endpoint
curl -k https://app-consolidated-api-prod.azurewebsites.net/api/examples/health

# Test independent endpoints
curl -k https://app-orders-api-prod.azurewebsites.net/api/orders/health
curl -k https://app-customers-api-prod.azurewebsites.net/api/customers/health

# Verify DNS resolution
nslookup app-consolidated-api-prod.azurewebsites.net
nslookup app-orders-api-prod.azurewebsites.net
nslookup app-customers-api-prod.azurewebsites.net
# All should resolve to private IPs (172.18.121.160+)
```

### Load Testing

Before going live:
1. Use Azure Load Testing or Apache JMeter
2. Test each API endpoint under expected load
3. Verify auto-scaling triggers correctly
4. Confirm no performance degradation in consolidated APIs

---

## References

- [Deployment Strategy](deployment-strategy.md) - Strategic approach
- [ADR 0004: Progressive Deployment Strategy](adr/0004-progressive-deployment-strategy.md) - Rationale
- [ADR 0005: Hybrid Production Architecture](adr/0005-hybrid-production-architecture.md) - Hybrid model
- [Consolidated Deployment Architecture](consolidated-deployment-architecture.md) - Consolidated details
- [App Service Private Endpoint Architecture](app-service-private-endpoint-architecture.md) - Network design
- [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/)
