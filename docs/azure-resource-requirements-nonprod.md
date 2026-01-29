# Azure Resource Requirements - Non-Production Environment

## Document Information

**Date**: 2026-01-30  
**Status**: Active  
**Deployment Model**: Fully Consolidated  
**Related Documentation**: 
- [Deployment Strategy](deployment-strategy.md) - Overall strategic approach
- [ADR 0004: Progressive Deployment Strategy](adr/0004-progressive-deployment-strategy.md) - Evidence-based approach
- [Consolidated Deployment Architecture](consolidated-deployment-architecture.md) - Technical architecture
- [App Service Private Endpoint Architecture](app-service-private-endpoint-architecture.md) - Network architecture

## Overview

This document outlines the Azure resources required for the **Non-Production environment** using a **fully consolidated deployment** model. All APIs are deployed to a single App Service, maximizing cost efficiency while enabling comprehensive monitoring to inform production architecture decisions.

**Non-Production Purpose**:
- Test consolidated deployment approach
- Gather performance metrics and resource consumption data
- Identify "greedy" vs "lazy" API patterns
- Validate architecture before production deployment
- Minimize costs during testing phase

**Environment Characteristics**:
- **Architecture**: Fully consolidated (single App Service hosting all APIs)
- **Cost**: ~$227-312/month (85% savings vs independent deployment)
- **Monitoring**: Comprehensive telemetry with 100% sampling
- **IP Address Space**: 172.18.121.0/24 (shared with production)
- **Hybrid Connectivity**: Leverages existing VPN Gateway

## Prerequisites

- Azure subscription with appropriate permissions (Contributor or Owner role)
- Access to Azure Portal: https://portal.azure.com
- Azure CLI installed (for CLI-based steps): https://aka.ms/azure-cli
- **Existing infrastructure**: VPN Gateway, Private DNS zones, Private Endpoint subnet (172.18.121.160/27)
- **Verified hybrid connectivity**: On-premises to Azure networking operational

## Resource Overview

```
Resource Group (rg-apis-nonprod)
│
├── Networking
│   ├── Virtual Network (vnet-apis-nonprod) - 172.18.121.0/24 [SHARED]
│   │   ├── snet-privateendpoint (172.18.121.160/27) [EXISTING - SHARED]
│   │   ├── snet-integration (172.18.121.192/26) [SHARED]
│   │   └── Business Layer (172.18.121.0/25) [AVAILABLE]
│   │
│   └── Network Security Group (nsg-integration-nonprod)
│
├── Compute
│   └── App Service Plan (asp-consolidated-api-nonprod) - P1V3
│       └── App Service (1 consolidated host)
│           └── app-consolidated-api-nonprod
│               ├── Examples API (/api/examples)
│               ├── Orders API (/api/orders)
│               ├── Customers API (/api/customers)
│               └── ... (All APIs - 10-20+)
│
├── Private Connectivity
│   └── Private Endpoint (1 endpoint)
│       └── pe-app-consolidated-api
│
├── Security & Secrets
│   └── Key Vault (kv-apis-nonprod-unique)
│
└── Monitoring & Logging (CRITICAL for data collection)
    ├── Log Analytics Workspace (log-apis-nonprod)
    └── Application Insights (appi-apis-nonprod)
        └── 100% sampling enabled (no adaptive sampling)
```

## Deployment Strategy

### Key Differences from Production

| Aspect | Non-Production | Production |
|--------|----------------|------------|
| **Architecture** | Fully consolidated | Hybrid (consolidated + independent) |
| **App Services** | 1 (hosting all APIs) | Variable (based on greedy/lazy classification) |
| **Private Endpoints** | 1 | Variable (1 for consolidated + 1 per greedy API) |
| **Cost** | ~$227-312/month | ~$571-1,431/month |
| **Monitoring** | 100% sampling | Adaptive sampling |
| **Purpose** | Testing & data collection | Production workloads |

## Network Requirements

### IP Address Planning

**Shared Address Space**: Non-production shares the VNet with production but uses separate resource groups and App Services.

| Subnet Name | Address Range | Usable IPs | Purpose | Shared with Prod |
|-------------|---------------|------------|---------|------------------|
| Available | `172.18.121.0/25` | 123 | Reserved for Business Layer | Yes |
| Available | `172.18.121.128/27` | 27 | Reserved for future services | Yes |
| `snet-privateendpoint` | `172.18.121.160/27` | 27 | Private Endpoints (shared across environments) | **Yes** |
| `snet-integration` | `172.18.121.192/26` | 59 | App Service VNet Integration (shared) | **Yes** |

**Non-Production IP Consumption**:
- **Private Endpoints**: 1 IP (for consolidated App Service)
- **VNet Integration**: 2-3 IPs (single App Service with 1-2 instances)
- **Total**: ~3-4 IPs (minimal footprint)

### DNS Requirements

**Private DNS Zones** (existing, shared with production):
- `privatelink.azurewebsites.net` - App Service resolution
- `privatelink.documents.azure.com` - Cosmos DB resolution
- `privatelink.database.windows.net` - Azure SQL resolution
- `privatelink.vaultcore.azure.net` - Key Vault resolution

**Non-Production Hostname**:
- `app-consolidated-api-nonprod.azurewebsites.net` → Private Endpoint IP (172.18.121.160+)

### Network Security

**NSG Rules** (nsg-integration-nonprod):
- Allow outbound to Private Endpoint subnet (172.18.121.160/27) on ports 443, 1433
- Allow outbound to Internet (Azure services)
- Default deny all other outbound

## Deployment Sequence

1. Resource Group (`rg-apis-nonprod`)
2. Log Analytics Workspace (`log-apis-nonprod`)
3. Network Security Group (`nsg-integration-nonprod`)
4. Key Vault (`kv-apis-nonprod-unique`)
5. Application Insights (`appi-apis-nonprod`) **with 100% sampling**
6. App Service Plan (`asp-consolidated-api-nonprod`)
7. App Service (`app-consolidated-api-nonprod`)
8. VNet Integration (use existing `snet-integration`)
9. Private Endpoint (use existing `snet-privateendpoint`)

---

## 1. Resource Group

### Azure Portal Deployment

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Click **Create a resource** → **Resource groups**
3. Configure:
   - **Subscription**: Select your subscription
   - **Resource group**: `rg-apis-nonprod`
   - **Region**: `East US 2` (or your region)
4. Click **Review + create** → **Create**

### Azure CLI Alternative

```bash
RESOURCE_GROUP="rg-apis-nonprod"
LOCATION="eastus2"

az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

---

## 2. Log Analytics Workspace

### Azure Portal Deployment

1. Search for **Log Analytics workspaces**
2. Click **Create**
3. Configure:
   - **Resource group**: `rg-apis-nonprod`
   - **Name**: `log-apis-nonprod`
   - **Region**: Same as resource group
4. Click **Review + Create** → **Create**

### Azure CLI Alternative

```bash
LOG_WORKSPACE="log-apis-nonprod"

az monitor log-analytics workspace create \
  --resource-group $RESOURCE_GROUP \
  --workspace-name $LOG_WORKSPACE \
  --location $LOCATION
```

---

## 3. Network Security Group

### Azure Portal Deployment

1. Search for **Network security groups**
2. Click **Create**
3. Configure:
   - **Resource group**: `rg-apis-nonprod`
   - **Name**: `nsg-integration-nonprod`
   - **Region**: Same as resource group
4. Click **Review + create** → **Create**
5. Associate with existing `snet-integration` subnet (multi-environment shared subnet)

### Azure CLI Alternative

```bash
az network nsg create \
  --resource-group $RESOURCE_GROUP \
  --name nsg-integration-nonprod \
  --location $LOCATION
```

---

## 4. Key Vault

### Azure Portal Deployment

1. Search for **Key vaults**
2. Click **Create**
3. Configure:
   - **Resource group**: `rg-apis-nonprod`
   - **Key vault name**: `kv-apis-nonprod-$RANDOM` (globally unique)
   - **Region**: Same as resource group
   - **Pricing tier**: `Standard`
4. **Access configuration**: `Azure role-based access control`
5. **Networking**: `Allow public access from all networks` (or Private Endpoint)
6. Click **Review + create** → **Create**

### Azure CLI Alternative

```bash
KEYVAULT_NAME="kv-apis-nonprod-$RANDOM"

az keyvault create \
  --resource-group $RESOURCE_GROUP \
  --name $KEYVAULT_NAME \
  --location $LOCATION \
  --enable-rbac-authorization true
```

---

## 5. Application Insights (CRITICAL Configuration)

### Purpose
**PRIMARY PURPOSE**: Collect comprehensive performance data to inform production architecture decisions.

### Critical Configuration
- **Sampling**: **DISABLED** (100% telemetry collection)
- **Reason**: Need accurate data to classify APIs as "greedy" vs "lazy"
- **API-Level Tracking**: Custom dimensions to identify individual APIs

### Azure Portal Deployment

1. Search for **Application Insights**
2. Click **Create**
3. Configure:
   - **Resource group**: `rg-apis-nonprod`
   - **Name**: `appi-apis-nonprod`
   - **Region**: Same as resource group
   - **Log Analytics Workspace**: Select `log-apis-nonprod`
4. Click **Review + create** → **Create**

### Post-Deployment: Disable Sampling

1. Navigate to Application Insights
2. Click **Usage and estimated costs**
3. Click **Data sampling**
4. Set **Sampling percentage**: `100%` (disable adaptive sampling)
5. Save

### Azure CLI Alternative

```bash
APPINSIGHTS_NAME="appi-apis-nonprod"

az monitor app-insights component create \
  --resource-group $RESOURCE_GROUP \
  --app $APPINSIGHTS_NAME \
  --location $LOCATION \
  --workspace $LOG_WORKSPACE

# Disable sampling
az monitor app-insights component update \
  --resource-group $RESOURCE_GROUP \
  --app $APPINSIGHTS_NAME \
  --set 'properties.samplingPercentage=100'
```

---

## 6. App Service Plan

### Purpose
Host the single consolidated App Service containing all APIs.

### Sizing Recommendation

| Environment | SKU | Instances | Monthly Cost |
|-------------|-----|-----------|--------------|
| Non-Production | P1V3 Premium | 1-2 | ~$150-300 |

**Note**: Use same tier as production (P1V3) to ensure performance data is representative.

### Azure Portal Deployment

1. Search for **App Service plans**
2. Click **Create**
3. Configure:
   - **Resource group**: `rg-apis-nonprod`
   - **Name**: `asp-consolidated-api-nonprod`
   - **Operating System**: `Linux` (or Windows based on app)
   - **Region**: Same as resource group
   - **Pricing tier**: `Premium V3 P1V3`
4. Click **Review + create** → **Create**

### Azure CLI Alternative

```bash
APP_SERVICE_PLAN="asp-consolidated-api-nonprod"

az appservice plan create \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_PLAN \
  --is-linux \
  --sku P1V3 \
  --location $LOCATION
```

---

## 7. App Service (Consolidated Deployment)

### Purpose
Single App Service hosting all API projects (Examples, Orders, Customers, etc.)

### Deployment Model
Uses the **DataLayer.API.Example.Deployment** project which loads all API controllers via:
```csharp
builder.Services.AddControllers()
    .AddApplicationPart(typeof(DataLayer.API.Example.Program).Assembly)
    .AddApplicationPart(typeof(DataLayer.API.Example.Orders.Program).Assembly)
    .AddApplicationPart(typeof(DataLayer.API.Example.Customers.Program).Assembly);
```

### Azure Portal Deployment

1. Search for **App Services**
2. Click **Create** → **Web App**
3. **Basics**:
   - **Resource group**: `rg-apis-nonprod`
   - **Name**: `app-consolidated-api-nonprod` (globally unique)
   - **Publish**: `Code`
   - **Runtime stack**: `.NET 8 (LTS)`
   - **Operating System**: Match App Service Plan
   - **Region**: Same as resource group
   - **App Service Plan**: `asp-consolidated-api-nonprod`
4. **Monitoring**:
   - **Enable Application Insights**: `Yes`
   - **Application Insights**: `appi-apis-nonprod`
5. Click **Review + create** → **Create**

### Post-Deployment Configuration

1. Navigate to App Service
2. **Configuration** → **Application settings**:
   ```json
   APPLICATIONINSIGHTS_CONNECTION_STRING=[from Key Vault or App Insights]
   EnableAdaptiveSampling=false
   SamplingPercentage=100
   ```
3. **Configuration** → **General settings**:
   - **HTTPS Only**: `On`
   - **HTTP version**: `2.0`
   - **Minimum TLS version**: `1.2`

### Azure CLI Alternative

```bash
APP_NAME="app-consolidated-api-nonprod"

APPINSIGHTS_CONN=$(az monitor app-insights component show \
  --resource-group $RESOURCE_GROUP \
  --app $APPINSIGHTS_NAME \
  --query connectionString -o tsv)

az webapp create \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --name $APP_NAME \
  --runtime "DOTNET:8.0"

az webapp config set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --http20-enabled true \
  --min-tls-version 1.2

az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --settings \
    APPLICATIONINSIGHTS_CONNECTION_STRING="$APPINSIGHTS_CONN" \
    EnableAdaptiveSampling=false \
    SamplingPercentage=100
```

---

## 8. VNet Integration

### Purpose
Enable App Service to access backend resources (Cosmos DB, SQL, Key Vault) via Private Endpoints.

### Azure Portal Deployment

1. Navigate to App Service (`app-consolidated-api-nonprod`)
2. Click **Networking** → **VNet integration**
3. Click **Add VNet integration**
4. Configure:
   - **Virtual Network**: `vnet-apis-nonprod` (or shared VNet)
   - **Subnet**: `snet-integration`
5. Click **OK**

### Azure CLI Alternative

```bash
az webapp vnet-integration add \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --vnet vnet-apis-nonprod \
  --subnet snet-integration
```

---

## 9. Private Endpoint

### Purpose
Provide private, internal-only inbound connectivity to the consolidated App Service.

### Azure Portal Deployment

1. Navigate to App Service (`app-consolidated-api-nonprod`)
2. Click **Networking** → **Private endpoints**
3. Click **Add**
4. Configure:
   - **Name**: `pe-app-consolidated-api-nonprod`
   - **Virtual network**: `vnet-apis-nonprod`
   - **Subnet**: `snet-privateendpoint` (existing: 172.18.121.160/27)
   - **Integrate with private DNS zone**: `Yes`
5. Click **OK**

### Azure CLI Alternative

```bash
az network private-endpoint create \
  --resource-group $RESOURCE_GROUP \
  --name pe-app-consolidated-api-nonprod \
  --vnet-name vnet-apis-nonprod \
  --subnet snet-privateendpoint \
  --private-connection-resource-id $(az webapp show \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --query id -o tsv) \
  --group-id sites \
  --connection-name pe-conn-consolidated-api-nonprod
```

---

## Monitoring Configuration (CRITICAL)

### Purpose
Collect comprehensive performance data to classify APIs as "greedy" (high resource usage) or "lazy" (low resource usage) for production architecture decisions.

### Required Configuration

1. **Application Insights Settings**:
   - Sampling: 100% (no adaptive sampling)
   - Custom dimensions: Add `api.name` and `api.project` to all telemetry
   - Performance counters: Enable CPU, Memory, Request metrics

2. **Azure Workbooks**:
   Create custom workbook for API comparison:
   - Request volume per API
   - CPU/Memory consumption per API
   - Response time percentiles (P50, P95, P99) per API
   - Error rates per API

3. **KQL Queries** (for weekly reviews):

```kusto
// API Resource Consumption Analysis
requests
| where timestamp > ago(7d)
| extend ApiName = tostring(customDimensions["api.name"])
| summarize 
    RequestCount = count(),
    AvgDuration = avg(duration),
    P95Duration = percentile(duration, 95),
    FailureRate = countif(success == false) * 100.0 / count()
  by ApiName, bin(timestamp, 1h)
| order by RequestCount desc

// Identify Greedy APIs
performanceCounters
| where timestamp > ago(7d)
| where name == "% Processor Time" or name == "Available Bytes"
| extend ApiName = tostring(customDimensions["api.name"])
| summarize 
    AvgCPU = avgif(value, name == "% Processor Time"),
    AvgMemoryMB = avgif(value / 1024 / 1024, name == "Available Bytes")
  by ApiName
| where AvgCPU > 60 or AvgMemoryMB < 2048
| order by AvgCPU desc
```

### Weekly Performance Reviews

**Schedule**: Every Monday, review previous week's data

**Process**:
1. Run KQL queries to analyze API performance
2. Identify APIs with:
   - High CPU (>60%) or Memory (>70%) usage = **Greedy**
   - Low CPU (<30%) and Memory (<40%) usage = **Lazy**
3. Document findings in performance review log
4. Update API classification spreadsheet

---

## Cost Estimation

### Monthly Cost Breakdown

| Resource | SKU/Size | Quantity | Est. Monthly Cost |
|----------|----------|----------|-------------------|
| App Service Plan | P1V3 | 1 | ~$150 |
| App Service | (uses plan) | 1 | Included |
| Private Endpoint | Standard | 1 | ~$7 |
| Key Vault | Standard | 1 | ~$5 |
| Application Insights | Pay-as-you-go (100% sampling) | 1 | ~$30-80 |
| Log Analytics | Pay-as-you-go | 1 | ~$10-20 |
| VNet | Standard | Shared | ~$0 (shared) |
| **Total** | | | **~$227-312/month** |

**Comparison**:
- **Fully Independent (10 APIs)**: ~$1,500-2,000/month
- **Non-Production Consolidated**: ~$227-312/month
- **Savings**: **85% ($1,273-1,688/month)**

### Cost Per API

- **Per-API Cost**: ~$23-31/month (for 10 APIs)
- Extremely cost-efficient for testing phase

---

## Deployment Validation

### 1. Verify All APIs Accessible

From on-premises or VPN connection:

```bash
# Test Examples API
curl -k https://app-consolidated-api-nonprod.azurewebsites.net/api/examples/health

# Test Orders API
curl -k https://app-consolidated-api-nonprod.azurewebsites.net/api/orders/health

# Test Customers API
curl -k https://app-consolidated-api-nonprod.azurewebsites.net/api/customers/health
```

### 2. Verify Application Insights Data Collection

1. Navigate to Application Insights
2. Check **Live metrics** - should show all API requests
3. Verify custom dimensions include `api.name` and `api.project`
4. Confirm sampling is 100% (no data loss)

### 3. Test VNet Integration

```bash
# From App Service console (Kudu)
curl https://kv-apis-nonprod-unique.vault.azure.net
# Should resolve to private IP (172.18.121.160+)
```

---

## Next Steps

After deploying non-production environment:

1. **Deploy Application Code**: Deploy DataLayer.API.Example.Deployment project
2. **Configure Monitoring Dashboards**: Create Azure Workbooks for API comparison
3. **Start Monitoring Period**: Collect data for minimum 3 months
4. **Weekly Performance Reviews**: Analyze and document API behavior patterns
5. **Classify APIs**: Determine which are "greedy" vs "lazy"
6. **Plan Production Deployment**: Use data to design hybrid production architecture

---

## References

- [Deployment Strategy](deployment-strategy.md) - Overall approach
- [Consolidated Deployment Architecture](consolidated-deployment-architecture.md) - Technical details
- [App Service Private Endpoint Architecture](app-service-private-endpoint-architecture.md) - Network design
- [ADR 0004: Progressive Deployment Strategy](adr/0004-progressive-deployment-strategy.md) - Strategic rationale
