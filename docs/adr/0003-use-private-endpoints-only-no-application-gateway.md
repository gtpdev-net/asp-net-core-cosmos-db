# ADR 0003: Use Private Endpoints Only (No Application Gateway)

**Date**: 2026-01-30  
**Status**: Accepted  
**Supersedes**: [ADR 0002: Use Regular App Service with Application Gateway](0002-use-regular-app-service-with-application-gateway.md)

## Context

After detailed analysis of requirements and deployment architecture, we identified that Application Gateway provides limited functional value for our specific use case:

- **Internal-only traffic**: 100% trusted sources via VPN Gateway
- **Client abstraction**: Client DLL encapsulates all API hostnames
- **Simple requirements**: Stateless APIs with no advanced routing needs
- **Cost consideration**: Application Gateway adds $250-400/month with minimal benefit

### Key Requirements Validation

| Requirement | App Gateway Needed? | Our Situation |
|-------------|---------------------|---------------|
| WAF Protection | Only for untrusted traffic | ✅ 100% internal, trusted sources |
| Certificate Management | Helpful for custom certs | ✅ Azure-managed certificates (free) |
| Advanced Routing | Needed for complex scenarios | ✅ Stateless APIs, direct access |
| Centralized Logging | Nice to have | ✅ Application Insights per API sufficient |
| Client Simplification | Depends on client architecture | ✅ Client DLL hides backend topology |
| Request/Response Manipulation | Only if needed | ✅ Not required |

## Decision

**We will use Private Endpoints only, without Application Gateway.**

### Architecture

```
On-Premises Applications
    ↓ (VPN Gateway - existing)
Private DNS Resolution (existing)
    ↓ (privatelink.azurewebsites.net)
Private Endpoints (172.18.121.160/27)
    ↓
App Services (10-20 APIs)
```

### Access Pattern

- Each App Service accessible via: `app-{name}-api-prod.azurewebsites.net`
- Private DNS automatically resolves to Private Endpoint IPs (172.18.121.160-187)
- Client DLL encapsulates all hostnames and routing logic
- On-premises applications only interact with client DLL

### Example Client DLL Pattern

```csharp
public class ApiClient
{
    private readonly HttpClient _httpClient;
    
    public ApiClient()
    {
        _httpClient = new HttpClient();
    }
    
    public async Task<Examples> GetExamples()
    {
        // Hostname encapsulated - clients don't know backend topology
        var response = await _httpClient.GetAsync(
            "https://app-examples-api-prod.azurewebsites.net/api/examples");
        return await response.Content.ReadAsAsync<Examples>();
    }
    
    public async Task<Orders> GetOrders()
    {
        var response = await _httpClient.GetAsync(
            "https://app-orders-api-prod.azurewebsites.net/api/orders");
        return await response.Content.ReadAsAsync<Orders>();
    }
}
```

## Consequences

### Positive

- **Cost Savings**: $250-400/month saved by eliminating Application Gateway
- **Reduced Complexity**: Fewer Azure resources to manage and monitor
- **Simplified Architecture**: Direct Private Endpoint connectivity with no intermediary layer
- **Free Certificates**: Azure-managed certificates with automatic renewal
- **Faster Deployment**: Fewer resources to provision and configure
- **Lower Latency**: One less network hop between client and API
- **Easier Troubleshooting**: Simpler network path for diagnosing issues

### Negative

- **No Centralized WAF**: Must rely on App Service built-in security features
  - *Mitigation*: Internal traffic is trusted; implement API authentication/authorization
- **No Centralized Routing**: Each API accessed independently
  - *Mitigation*: Client DLL encapsulates routing logic; clients unaware of backend URLs
- **No URL Rewriting**: Cannot modify requests/responses at gateway level
  - *Mitigation*: Not required for current use case
- **Multiple Endpoints**: 10-20 different hostnames instead of one
  - *Mitigation*: Client DLL provides single interface; hostnames hidden from end users
- **No Centralized Rate Limiting**: Rate limiting must be per-API
  - *Mitigation*: Implement rate limiting in App Service or API code if needed

## Implementation Notes

### Certificate Management

- Use **Azure-managed certificates** for `*.azurewebsites.net` domains (free, automatic)
- No custom certificates required since using default Azure domains
- Certificates automatically renewed by Azure

### Security Controls

Since removing Application Gateway (WAF), implement these compensating controls:

1. **Authentication/Authorization**:
   - Use Azure AD (Entra ID) authentication for APIs
   - Implement OAuth 2.0 / JWT token validation
   - Configure App Service Easy Auth if appropriate

2. **Network Security**:
   - Private Endpoints ensure no public internet exposure
   - NSG rules on integration subnet for outbound traffic control
   - VPN Gateway provides encrypted connectivity from on-premises

3. **API Security**:
   - Input validation in API code
   - Rate limiting at API level (ASP.NET Core middleware)
   - Logging and monitoring via Application Insights

4. **Access Restrictions**:
   - Configure App Service access restrictions to allow traffic only from VPN Gateway/on-premises IP ranges
   - Lock down Private Endpoint subnet with NSG rules

### Monitoring and Observability

- **Application Insights**: Per-API telemetry, distributed tracing
- **Log Analytics**: Centralized logs from all App Services
- **Azure Monitor**: Alerts for availability, performance, and errors
- **NSG Flow Logs**: Network traffic analysis

### When to Reconsider

Re-evaluate this decision if:

1. **Compliance requirements** mandate WAF for all applications
2. **Untrusted clients** need access to APIs (B2B partners, third-party integrations)
3. **Advanced routing** becomes necessary (A/B testing, canary deployments at gateway level)
4. **API versioning strategy** requires centralized routing (e.g., `/v1/` vs `/v2/` routing)
5. **Centralized rate limiting/throttling** becomes a requirement across all APIs
6. **Cost is less important** than operational simplicity of single endpoint

## Alternatives Considered

### Alternative 1: Keep Application Gateway (Original ADR 0002)

**Rejected** because:
- Limited functional benefit for internal-only, trusted traffic
- $250-400/month cost not justified by requirements
- Adds complexity without corresponding value
- WAF protection unnecessary for 100% internal traffic

### Alternative 2: Azure API Management (APIM)

**Rejected** because:
- Even more expensive than Application Gateway ($500-1000+/month for Basic tier)
- Overkill for simple internal API access
- Features like developer portal, monetization, partner access not needed
- Adds significant complexity

### Alternative 3: Azure Front Door

**Rejected** because:
- Designed for global distribution (not needed for single-region internal APIs)
- More expensive than Application Gateway
- Features like CDN, global routing not applicable

## Related Documentation

- [ADR 0001: Deploy Each Project as Independent Azure App Service](0001-deploy-each-project-as-independent-app-service.md) - Independent deployment model (for greedy APIs)
- [ADR 0002: Use Regular App Service with Application Gateway](0002-use-regular-app-service-with-application-gateway.md) - **SUPERSEDED**
- [ADR 0004: Progressive Deployment Strategy](0004-progressive-deployment-strategy.md) - Evidence-based deployment approach
- [ADR 0005: Hybrid Production Architecture](0005-hybrid-production-architecture.md) - Uses Private Endpoints for all deployments
- [Deployment Strategy](../deployment-strategy.md) - Overall strategic approach
- [App Service Private Endpoint Architecture](../app-service-private-endpoint-architecture.md) - Detailed network architecture
- [Consolidated Deployment Architecture](../consolidated-deployment-architecture.md) - Consolidated deployment model (for lazy APIs)
- [Azure Resource Requirements](../azure-resource-requirements.md) - Infrastructure specifications

## References

- [Azure App Service Private Endpoints](https://learn.microsoft.com/en-us/azure/app-service/networking/private-endpoint)
- [Azure-managed Certificates for App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate#create-a-free-managed-certificate)
- [App Service Access Restrictions](https://learn.microsoft.com/en-us/azure/app-service/app-service-ip-restrictions)
- [Private DNS Integration](https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-dns)
