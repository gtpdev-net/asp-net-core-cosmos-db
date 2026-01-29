# ADR 0004: Progressive Deployment Strategy - Evidence-Based Architecture Evolution

## Status

**Accepted** - 2026-01-30

## Context

We need to deploy 10-20+ REST API projects from a single ASP.NET Core solution to Azure. While we have identified multiple possible architectures (fully consolidated, fully independent, hybrid), we lack empirical data about:

1. **Actual resource consumption patterns** of individual APIs
2. **Performance impact** of co-locating multiple APIs in a single App Service
3. **Cost implications** of different deployment models in our specific context
4. **Scaling requirements** for each API under realistic load

Making architectural decisions based on assumptions rather than data carries significant risk:
- **Over-provisioning**: Deploying all APIs independently may waste resources and budget
- **Under-provisioning**: Consolidating high-resource APIs may cause performance issues
- **Incorrect optimization**: Optimizing for the wrong metrics leads to suboptimal solutions

Traditional approaches would require us to choose an architecture upfront, but this forces premature optimization without understanding actual system behavior.

## Decision

We will adopt a **progressive, evidence-based deployment strategy** that evolves from a fully consolidated non-production environment to an optimized hybrid production architecture based on empirical performance data.

### Strategy Phases

#### Phase 1: Non-Production (Fully Consolidated + Comprehensive Monitoring)

**Duration**: 3-6 months  
**Architecture**: All APIs deployed to single consolidated App Service  
**Focus**: Data collection and pattern identification  

**Rationale**:
- Non-production traffic patterns, while different from production, provide valuable relative comparisons between APIs
- Consolidation maximizes opportunities to observe API interactions and resource contention
- Lower cost allows investment in comprehensive monitoring (100% sampling, detailed telemetry)
- Failures in non-production have minimal business impact

**Key Activities**:
1. Deploy all APIs to `app-consolidated-api-nonprod`
2. Configure Application Insights with 100% sampling (no adaptive sampling)
3. Collect granular performance metrics per API
4. Generate weekly performance reports
5. Classify APIs as "greedy" (high resource consumption) or "lazy" (low resource consumption)

#### Phase 2: Production (Hybrid Architecture Based on Data)

**Start**: After 3-6 months of non-production monitoring  
**Architecture**: Consolidated deployment for lazy APIs, independent deployments for greedy APIs  

**Deployment Rules**:
- **Default to Consolidated**: APIs with low resource consumption remain in consolidated deployment
- **Isolate Greedy APIs**: APIs with high resource consumption or performance impact get independent App Services
- **Evidence Required**: Isolation decisions must be backed by non-production performance data

**Classification Criteria**:

| Classification | CPU | Memory | Request Rate | Deployment Target |
|----------------|-----|--------|--------------|-------------------|
| **Lazy** | < 30% | < 40% | < 200 req/min | Consolidated |
| **Moderate** | 30-60% | 40-70% | 200-1,000 req/min | Consolidated (monitored) |
| **Greedy** | > 60% | > 70% | > 1,000 req/min | Independent |

### Continuous Optimization

**Ongoing**: Monthly architecture reviews assess whether APIs need reclassification based on production metrics.

## Consequences

### Positive

**Risk Mitigation**:
- Validates consolidation approach before committing to production architecture
- Identifies problematic APIs early in non-production environment
- Avoids costly architectural mistakes based on assumptions

**Cost Optimization**:
- Maximizes resource sharing where appropriate (lazy APIs)
- Invests in dedicated resources only where data justifies it (greedy APIs)
- Estimated 50-70% cost savings compared to fully independent deployment

**Data-Driven Decisions**:
- Architectural choices backed by empirical evidence
- Clear, measurable criteria for API classification
- Removes subjective decision-making

**Flexibility**:
- Architecture can evolve as usage patterns change
- Easy to reclassify APIs based on new data
- No premature lock-in to specific deployment model

**Developer Experience**:
- Maintains independent development workflow (developers run individual APIs locally)
- Production deployment model transparent to development team
- No impact on development velocity

### Negative

**Delayed Production Deployment**:
- Requires 3-6 months of non-production monitoring before production deployment
- Cannot optimize production architecture until data is available
- **Mitigation**: Deploy production initially as fully consolidated if timeline critical, then refine based on data

**Monitoring Investment**:
- Requires comprehensive Application Insights configuration
- 100% sampling in non-production increases monitoring costs
- Time investment for weekly performance reviews
- **Mitigation**: Monitoring investment pays for itself through optimized production architecture

**Operational Complexity**:
- Hybrid production architecture more complex than fully consolidated or fully independent
- Requires deployment manifest to manage API placement
- More infrastructure components to manage
- **Mitigation**: Infrastructure as Code and automated CI/CD reduce manual complexity

**Potential for Mis-Classification**:
- Non-production usage patterns may not perfectly reflect production
- Risk of classifying greedy API as lazy (or vice versa) based on incomplete data
- **Mitigation**: Extended observation period (3-6 months), load testing, conservative classification for uncertain APIs

## Alternatives Considered

### Alternative 1: Fully Independent Deployment (ADR 0001)

**Approach**: Deploy each API to its own App Service from day one.

**Rejected Because**:
- **High Cost**: $1,500-2,000/month for 10 APIs (vs $571-1,431 with hybrid approach)
- **Over-Provisioning**: Most APIs likely don't need dedicated resources
- **Operational Overhead**: Managing 10-20 separate App Services significantly more complex
- **Premature Optimization**: Optimizes for independence without evidence it's needed

**When to Reconsider**: If evidence shows most/all APIs are greedy, or if regulatory requirements mandate isolation.

### Alternative 2: Fully Consolidated Deployment

**Approach**: Deploy all APIs to single consolidated App Service in all environments forever.

**Rejected Because**:
- **Noisy Neighbor Risk**: One greedy API can degrade performance of all co-located APIs
- **Scaling Limitations**: Cannot independently scale high-traffic APIs
- **Single Point of Failure**: All APIs share failure domain
- **Performance Unpredictability**: Without data, we don't know if this will work

**When to Reconsider**: If non-production data shows all APIs are lazy and co-location has no performance impact.

### Alternative 3: Application Gateway + Fully Consolidated

**Approach**: Single consolidated App Service behind Application Gateway for advanced routing and WAF.

**Rejected Because**:
- **Unnecessary Cost**: Application Gateway adds $250-400/month with minimal benefit for internal-only APIs
- **WAF Not Required**: All traffic originates from trusted on-premises network via VPN
- **Routing Not Needed**: Client DLL handles routing, no need for gateway-level routing
- **Complexity**: Additional component to manage without clear value proposition

**Reference**: [ADR 0003](0003-use-private-endpoints-only-no-application-gateway.md) - Rejected Application Gateway approach

## Implementation Notes

### Non-Production Monitoring Setup

**Application Insights Configuration**:
```json
{
  "ApplicationInsights": {
    "EnableAdaptiveSampling": false,
    "SamplingPercentage": 100,
    "EnableDependencyTracking": true,
    "EnablePerformanceCounterCollection": true
  }
}
```

**Telemetry Enrichment Middleware**:
```csharp
app.Use(async (context, next) =>
{
    var activity = Activity.Current;
    if (activity != null)
    {
        var apiName = context.Request.Path.Value?.Split('/')[2] ?? "unknown";
        activity.SetTag("api.name", apiName);
        activity.SetTag("api.project", GetProjectNameFromApi(apiName));
    }
    await next();
});
```

### Key Monitoring Queries

**Identify Greedy APIs**:
```kusto
requests
| where timestamp > ago(7d)
| extend ApiName = tostring(customDimensions["api.name"])
| summarize 
    RequestCount = count(),
    AvgDuration = avg(duration),
    P95Duration = percentile(duration, 95)
  by ApiName
| join kind=inner (
    performanceCounters
    | where name == "% Processor Time"
    | extend ApiName = tostring(customDimensions["api.name"])
    | summarize AvgCPU = avg(value) by ApiName
  ) on ApiName
| where AvgCPU > 60 or RequestCount > 100000
| order by AvgCPU desc
```

### Deployment Manifest Example

**Non-Production** (`deployment-manifest.nonprod.json`):
```json
{
  "environment": "non-production",
  "strategy": "fully-consolidated",
  "consolidated": {
    "appService": "app-consolidated-api-nonprod",
    "apis": "*"
  }
}
```

**Production** (`deployment-manifest.prod.json`):
```json
{
  "environment": "production",
  "strategy": "hybrid",
  "consolidated": {
    "appService": "app-consolidated-api-prod",
    "apis": [
      "DataLayer.API.Example",
      "DataLayer.API.Example.Products",
      "DataLayer.API.Example.Categories"
    ]
  },
  "independent": [
    {
      "appService": "app-orders-api-prod",
      "api": "DataLayer.API.Example.Orders",
      "reason": "Greedy API - CPU 75%, 5000 req/min"
    },
    {
      "appService": "app-customers-api-prod",
      "api": "DataLayer.API.Example.Customers",
      "reason": "Greedy API - CPU 68%, 3500 req/min"
    }
  ]
}
```

## Related ADRs

- [ADR 0001: Deploy Each Project as Independent Azure App Service](0001-deploy-each-project-as-independent-app-service.md) - Alternative approach (fully independent)
- [ADR 0003: Use Private Endpoints Only (No Application Gateway)](0003-use-private-endpoints-only-no-application-gateway.md) - Network architecture decision
- [ADR 0005: Hybrid Production Architecture](0005-hybrid-production-architecture.md) - Production deployment model resulting from this strategy

## References

- [Deployment Strategy Document](../deployment-strategy.md) - Complete strategy overview
- [Consolidated Deployment Architecture](../consolidated-deployment-architecture.md) - Technical implementation details
- [App Service Private Endpoint Architecture](../app-service-private-endpoint-architecture.md) - Network architecture

---

**Decision Date**: 2026-01-30  
**Decision Makers**: Architecture Team, Engineering Leadership  
**Review Date**: 2026-07-30 (6 months after non-production deployment)
