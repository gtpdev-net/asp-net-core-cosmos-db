# ADR 0005: Hybrid Production Architecture - Optimized Resource Allocation

## Status

**Accepted** - 2026-01-30  
**Supersedes**: Portions of [ADR 0001](0001-deploy-each-project-as-independent-app-service.md) (fully independent approach)

## Context

Following 3-6 months of comprehensive monitoring in the non-production environment (see [ADR 0004: Progressive Deployment Strategy](0004-progressive-deployment-strategy.md)), we have empirical data about API resource consumption patterns. This data reveals that:

1. **APIs exhibit vastly different resource profiles**: Some APIs ("greedy") consistently consume 60-80% CPU and handle thousands of requests per minute, while others ("lazy") average 10-25% CPU with hundreds of requests per minute.

2. **Co-location impact varies**: Lazy APIs co-located together show minimal performance degradation (< 5%), while greedy APIs significantly impact co-located services (20-40% P95 response time increase).

3. **Scaling requirements differ**: Greedy APIs trigger auto-scaling frequently, while lazy APIs rarely need additional resources.

4. **Cost implications are significant**: Deploying all APIs independently costs $1,500-2,000/month, while strategic consolidation could reduce costs to $571-1,431/month (28-62% savings).

### Example Classification from Non-Production Data

| API | Avg CPU | Avg Memory | Req/Min | Classification | Production Decision |
|-----|---------|------------|---------|----------------|---------------------|
| Examples API | 15% | 25% | 100 | Lazy | Consolidated |
| Products API | 20% | 30% | 150 | Lazy | Consolidated |
| Categories API | 10% | 20% | 50 | Lazy | Consolidated |
| Suppliers API | 18% | 28% | 80 | Lazy | Consolidated |
| Reports API | 25% | 35% | 200 | Lazy | Consolidated |
| Analytics API | 22% | 32% | 120 | Lazy | Consolidated |
| Notifications API | 12% | 22% | 60 | Lazy | Consolidated |
| Settings API | 8% | 18% | 30 | Lazy | Consolidated |
| **Orders API** | **75%** | **80%** | **5,000** | **Greedy** | **Independent** |
| **Customers API** | **68%** | **72%** | **3,500** | **Greedy** | **Independent** |

**Key Insight**: 80% of APIs are suitable for consolidation, while 20% require isolation. Deploying all APIs independently would waste resources on the 80% that don't need dedicated infrastructure.

## Decision

We will deploy production using a **hybrid architecture** that strategically consolidates lazy APIs while isolating greedy APIs based on empirical evidence from non-production monitoring.

### Architecture Model

```
Production Environment:

┌──────────────────────────────────────────────────────────────┐
│ Consolidated App Service: app-consolidated-api-prod         │
│ ├── Examples API (Lazy)                                     │
│ ├── Products API (Lazy)                                     │
│ ├── Categories API (Lazy)                                   │
│ ├── Suppliers API (Lazy)                                    │
│ ├── Reports API (Lazy)                                      │
│ ├── Analytics API (Lazy)                                    │
│ ├── Notifications API (Lazy)                                │
│ └── Settings API (Lazy)                                     │
│                                                              │
│ App Service Plan: P1V3 Premium (2-5 instances)              │
│ Cost: $150-375/month                                         │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│ Independent App Service: app-orders-api-prod                │
│ └── Orders API (Greedy)                                     │
│                                                              │
│ App Service Plan: P1V3 Premium (2-5 instances)              │
│ Cost: $150-375/month                                         │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│ Independent App Service: app-customers-api-prod             │
│ └── Customers API (Greedy)                                  │
│                                                              │
│ App Service Plan: P1V3 Premium (2-5 instances)              │
│ Cost: $150-375/month                                         │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│ Shared Infrastructure                                        │
│ ├── Cosmos DB (Serverless)                                  │
│ ├── Key Vault                                               │
│ ├── Application Insights                                    │
│ └── Private Endpoints (11 total: 1 consolidated + 2 greedy  │
│     + backends)                                              │
│                                                              │
│ Cost: $100-285/month                                         │
└──────────────────────────────────────────────────────────────┘

Total Monthly Cost: $571-1,431/month
Cost per API: $57-143/month
Savings vs Fully Independent: 28-62%
```

### Deployment Rules

#### Rule 1: Default to Consolidation

New APIs start in the consolidated deployment unless:
- Pre-launch analysis indicates high resource requirements
- Business criticality demands isolation
- Regulatory requirements mandate separation

#### Rule 2: Isolate Based on Evidence

An API moves from consolidated to independent deployment when non-production or production data shows:

**Resource Consumption Thresholds**:
- Sustained CPU utilization > 60% over 7+ days
- Sustained memory utilization > 70% over 7+ days
- Request rate > 1,000 req/min sustained

**Performance Impact**:
- Co-located APIs show > 20% P95 response time degradation
- Error rate increases when this API is under load
- Auto-scaling triggers consistently due to this API alone

**Scaling Mismatch**:
- API requires different auto-scaling profile than consolidated group
- Scaling consolidated group for this API wastes resources for others

#### Rule 3: Continuous Review

APIs are reviewed monthly for potential reclassification:
- **Greedy → Lazy**: If resource consumption drops below thresholds for 60+ days, consider consolidation
- **Lazy → Greedy**: If resource consumption exceeds thresholds, plan for isolation

### Network Architecture

**Consistent Network Model**: All deployments use [Private Endpoint Architecture](../app-service-private-endpoint-architecture.md)

**Network Configuration**:
- **VNet**: 172.18.121.0/24 (existing)
- **Private Endpoint Subnet**: 172.18.121.160/27 (27 IPs)
  - Consolidated App Service: 172.18.121.160
  - Orders API: 172.18.121.161
  - Customers API: 172.18.121.162
  - Cosmos DB: 172.18.121.163
  - Key Vault: 172.18.121.164
- **VNet Integration Subnet**: 172.18.121.192/26 (59 IPs)
  - Shared by all App Services for outbound connectivity

**Private DNS Resolution**:
- `app-consolidated-api-prod.azurewebsites.net` → 172.18.121.160
- `app-orders-api-prod.azurewebsites.net` → 172.18.121.161
- `app-customers-api-prod.azurewebsites.net` → 172.18.121.162

### Client DLL Routing

**Implementation**: Client DLL maintains API-to-hostname mapping

```csharp
public class ApiClient
{
    private readonly Dictionary<string, string> _apiHosts = new()
    {
        // Lazy APIs → Consolidated
        ["examples"] = "app-consolidated-api-prod.azurewebsites.net",
        ["products"] = "app-consolidated-api-prod.azurewebsites.net",
        ["categories"] = "app-consolidated-api-prod.azurewebsites.net",
        ["suppliers"] = "app-consolidated-api-prod.azurewebsites.net",
        ["reports"] = "app-consolidated-api-prod.azurewebsites.net",
        ["analytics"] = "app-consolidated-api-prod.azurewebsites.net",
        ["notifications"] = "app-consolidated-api-prod.azurewebsites.net",
        ["settings"] = "app-consolidated-api-prod.azurewebsites.net",
        
        // Greedy APIs → Independent
        ["orders"] = "app-orders-api-prod.azurewebsites.net",
        ["customers"] = "app-customers-api-prod.azurewebsites.net"
    };

    public async Task<T> GetAsync<T>(string apiName, string endpoint)
    {
        var host = _apiHosts[apiName];
        var url = $"https://{host}/api/{apiName}/{endpoint}";
        return await _httpClient.GetFromJsonAsync<T>(url);
    }
}
```

**Benefits**:
- On-premises applications don't need to know deployment topology
- Easy to reclassify APIs by updating DLL configuration
- Single DLL update affects all clients
- Supports canary deployments and gradual migration

## Consequences

### Positive

**Cost Optimization**:
- **28-62% savings** compared to fully independent deployment ($571-1,431 vs $1,500-2,000/month)
- Maximizes resource sharing where appropriate (8 lazy APIs)
- Invests in dedicated resources only where evidence justifies (2 greedy APIs)
- Avoids over-provisioning 80% of APIs

**Performance Optimization**:
- Lazy APIs unaffected by each other (validated in non-production)
- Greedy APIs cannot degrade lazy API performance (isolated)
- Each greedy API can scale independently based on its needs
- No noisy neighbor effects between greedy and lazy APIs

**Operational Flexibility**:
- Easy to reclassify APIs based on changing patterns
- Can add new APIs to consolidated group by default
- Can extract individual APIs without major architectural changes
- Supports gradual evolution as requirements change

**Risk Mitigation**:
- Decision backed by 3-6 months of empirical data
- Proven in non-production before production deployment
- Clear criteria for when to adjust architecture
- Reduced risk of performance issues vs fully consolidated approach

### Negative

**Increased Complexity**:
- More infrastructure components than fully consolidated (3 App Services vs 1)
- Deployment manifest required to track API placement
- Client DLL must maintain hostname mappings
- **Mitigation**: Infrastructure as Code automates provisioning, CI/CD handles complexity

**Monitoring Overhead**:
- Must monitor multiple App Services
- Need to track performance across consolidated and independent deployments
- Requires ongoing classification reviews
- **Mitigation**: Centralized Application Insights, automated alerting, monthly review process

**Potential for Sub-Optimal Classification**:
- Non-production patterns may not perfectly match production
- Risk of classifying greedy API as lazy (performance risk) or lazy as greedy (cost waste)
- **Mitigation**: Conservative classification (when in doubt, isolate), continuous monitoring with re-classification

**Client DLL Coupling**:
- Clients depend on DLL for correct hostname routing
- DLL updates required when reclassifying APIs
- **Mitigation**: Centralized DLL distribution, versioning, fallback mechanisms

### Neutral

**Alternative Access Patterns**:
- Could use Application Gateway for unified hostname, but adds $250-400/month cost
- Could use API Management for advanced governance, but adds complexity
- Current approach (Private Endpoints + Client DLL) is simplest for internal-only access

## Alternatives Considered

### Alternative 1: Fully Consolidated Deployment

**Approach**: Deploy all 10 APIs to single consolidated App Service

**Pros**:
- Lowest cost ($227-467/month)
- Simplest infrastructure (1 App Service)
- Easiest to manage

**Cons**:
- **Rejected**: Non-production data shows Orders API and Customers API significantly impact co-located APIs
- Performance degradation > 20% for lazy APIs when greedy APIs are under load
- Cannot independently scale greedy APIs
- Single failure domain for all APIs

**When to Reconsider**: If greedy APIs are optimized to become lazy (< 30% CPU, < 40% memory).

### Alternative 2: Fully Independent Deployment

**Approach**: Deploy each API to its own App Service (10 separate App Services)

**Pros**:
- Maximum isolation (no noisy neighbor effects)
- Independent scaling per API
- Independent deployment cadence

**Cons**:
- **Rejected**: 80% of APIs (8 lazy APIs) don't need dedicated resources
- Cost: $1,500-2,000/month (2.6-3.5x hybrid approach)
- Operational overhead: managing 10 App Services vs 3
- Over-provisioning wastes budget that could fund other initiatives

**When to Reconsider**: If most/all APIs become greedy, or if regulatory requirements mandate complete isolation.

### Alternative 3: Different Consolidation Groupings

**Approach**: Group APIs by domain or team rather than resource consumption

**Pros**:
- Logical grouping by business function
- Team ownership clarity

**Cons**:
- **Rejected**: Non-production data shows resource consumption, not domain logic, determines optimal grouping
- Risk of putting greedy and lazy APIs together
- No performance benefit over evidence-based grouping

**When to Reconsider**: Never. Resource-based grouping is superior for performance and cost optimization.

## Implementation

### Deployment Manifest

**File**: `DataLayer.API.Example.Deployment/deployment-manifest.prod.json`

```json
{
  "environment": "production",
  "strategy": "hybrid",
  "version": "1.0",
  "lastUpdated": "2026-01-30",
  
  "consolidated": {
    "appService": "app-consolidated-api-prod",
    "appServicePlan": "asp-consolidated-prod",
    "privateEndpoint": "pe-consolidated-api-prod",
    "autoScale": {
      "minInstances": 2,
      "maxInstances": 5,
      "rules": [
        { "metric": "CpuPercentage", "threshold": 70, "action": "scaleOut" },
        { "metric": "MemoryPercentage", "threshold": 75, "action": "scaleOut" }
      ]
    },
    "apis": [
      {
        "name": "DataLayer.API.Example",
        "displayName": "Examples API",
        "classification": "lazy",
        "resourceProfile": { "cpu": "15%", "memory": "25%", "requestRate": "100/min" },
        "classificationDate": "2026-01-30"
      },
      {
        "name": "DataLayer.API.Example.Products",
        "displayName": "Products API",
        "classification": "lazy",
        "resourceProfile": { "cpu": "20%", "memory": "30%", "requestRate": "150/min" },
        "classificationDate": "2026-01-30"
      }
      // ... remaining lazy APIs
    ]
  },
  
  "independent": [
    {
      "appService": "app-orders-api-prod",
      "appServicePlan": "asp-orders-prod",
      "privateEndpoint": "pe-orders-api-prod",
      "api": {
        "name": "DataLayer.API.Example.Orders",
        "displayName": "Orders API",
        "classification": "greedy",
        "resourceProfile": { "cpu": "75%", "memory": "80%", "requestRate": "5000/min" },
        "classificationDate": "2026-01-30",
        "isolationReason": "High resource consumption (CPU > 60%, Memory > 70%, Request rate > 1000/min). Co-location causes 25% P95 response time degradation for other APIs."
      },
      "autoScale": {
        "minInstances": 2,
        "maxInstances": 10,
        "rules": [
          { "metric": "CpuPercentage", "threshold": 75, "action": "scaleOut" },
          { "metric": "HttpQueueLength", "threshold": 100, "action": "scaleOut" }
        ]
      }
    },
    {
      "appService": "app-customers-api-prod",
      "appServicePlan": "asp-customers-prod",
      "privateEndpoint": "pe-customers-api-prod",
      "api": {
        "name": "DataLayer.API.Example.Customers",
        "displayName": "Customers API",
        "classification": "greedy",
        "resourceProfile": { "cpu": "68%", "memory": "72%", "requestRate": "3500/min" },
        "classificationDate": "2026-01-30",
        "isolationReason": "High resource consumption. Triggers frequent auto-scaling events."
      },
      "autoScale": {
        "minInstances": 2,
        "maxInstances": 8,
        "rules": [
          { "metric": "CpuPercentage", "threshold": 70, "action": "scaleOut" },
          { "metric": "MemoryPercentage", "threshold": 75, "action": "scaleOut" }
        ]
      }
    }
  ]
}
```

### Infrastructure as Code

**Bicep Module**: `modules/hybrid-deployment.bicep`

```bicep
@description('Deployment manifest configuration')
param manifest object

@description('Environment name')
param environment string

@description('Azure region')
param location string = resourceGroup().location

// Consolidated App Service Plan
resource consolidatedPlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: manifest.consolidated.appServicePlan
  location: location
  sku: {
    name: 'P1V3'
    tier: 'PremiumV3'
    capacity: manifest.consolidated.autoScale.minInstances
  }
  properties: {
    reserved: false
  }
}

// Consolidated App Service
resource consolidatedAppService 'Microsoft.Web/sites@2023-01-01' = {
  name: manifest.consolidated.appService
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: consolidatedPlan.id
    httpsOnly: true
    publicNetworkAccess: 'Disabled'
    virtualNetworkSubnetId: vnetIntegrationSubnet.id
  }
}

// Independent App Services (loop)
resource independentPlans 'Microsoft.Web/serverfarms@2023-01-01' = [for api in manifest.independent: {
  name: api.appServicePlan
  location: location
  sku: {
    name: 'P1V3'
    tier: 'PremiumV3'
    capacity: api.autoScale.minInstances
  }
}]

resource independentAppServices 'Microsoft.Web/sites@2023-01-01' = [for (api, i) in manifest.independent: {
  name: api.appService
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: independentPlans[i].id
    httpsOnly: true
    publicNetworkAccess: 'Disabled'
    virtualNetworkSubnetId: vnetIntegrationSubnet.id
  }
}]

// Private Endpoints (consolidated + independent)
// ... (PE configuration)
```

### CI/CD Pipeline

**GitHub Actions Workflow**: `.github/workflows/deploy-production.yml`

```yaml
name: Deploy Production (Hybrid)

on:
  push:
    branches: [main]
  workflow_dispatch:

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Load deployment manifest
        id: manifest
        run: |
          echo "manifest=$(cat DataLayer.API.Example.Deployment/deployment-manifest.prod.json | jq -c .)" >> $GITHUB_OUTPUT
      
      - name: Deploy infrastructure
        uses: azure/arm-deploy@v2
        with:
          template: ./DataLayer.API.Example.Deployment/infrastructure/main.bicep
          parameters: manifest='${{ steps.manifest.outputs.manifest }}'
      
      - name: Build solution
        run: dotnet publish DataLayer.API.Example.sln -c Release -o ./publish
      
      - name: Deploy consolidated App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: app-consolidated-api-prod
          package: ./publish/DataLayer.API.Example.Deployment
      
      - name: Deploy independent App Services
        run: |
          # Parse manifest and deploy each independent API
          jq -c '.independent[]' deployment-manifest.prod.json | while read api; do
            appService=$(echo $api | jq -r '.appService')
            apiProject=$(echo $api | jq -r '.api.name')
            
            az webapp deploy \
              --name $appService \
              --src-path ./publish/$apiProject \
              --type zip
          done
```

## Monitoring and Ongoing Management

### Monthly Architecture Review

**Objective**: Assess whether APIs should be reclassified

**Review Checklist**:
- [ ] Review performance metrics for all APIs (CPU, memory, request rate)
- [ ] Identify any APIs exceeding classification thresholds
- [ ] Assess cost vs performance trade-offs
- [ ] Document any recommended reclassifications
- [ ] Update deployment manifest if changes approved
- [ ] Schedule deployment for any architectural adjustments

### Reclassification Process

**Greedy → Lazy** (Consolidate):
1. Verify lazy thresholds met for 60+ consecutive days
2. Estimate cost savings from consolidation
3. Assess risk of performance impact on consolidated group
4. Approval from architecture review board
5. Update deployment manifest
6. Deploy to consolidated App Service
7. Monitor for 30 days
8. Decommission independent App Service if successful

**Lazy → Greedy** (Isolate):
1. Document threshold violations and performance impact
2. Create independent App Service infrastructure
3. Update deployment manifest
4. Deploy to independent App Service
5. Update client DLL routing
6. Monitor for 30 days to validate improvement
7. Remove from consolidated deployment

## Related ADRs

- [ADR 0001: Deploy Each Project as Independent Azure App Service](0001-deploy-each-project-as-independent-app-service.md) - Alternative (fully independent)
- [ADR 0003: Use Private Endpoints Only (No Application Gateway)](0003-use-private-endpoints-only-no-application-gateway.md) - Network architecture
- [ADR 0004: Progressive Deployment Strategy](0004-progressive-deployment-strategy.md) - Strategy that led to this decision

## References

- [Deployment Strategy Document](../deployment-strategy.md) - Overall strategy
- [Consolidated Deployment Architecture](../consolidated-deployment-architecture.md) - Consolidated portion of hybrid architecture
- [App Service Private Endpoint Architecture](../app-service-private-endpoint-architecture.md) - Network implementation

---

**Decision Date**: 2026-01-30  
**Decision Makers**: Architecture Team, Engineering Leadership, based on non-production data analysis  
**Review Date**: 2026-04-30 (First quarterly review)
