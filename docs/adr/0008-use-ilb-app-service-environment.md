# ADR 0008: Use ILB App Service Environment

## Status

Rejected

## Date

2026-01-30

## Context

This option was considered as part of [ADR 0002](0002-use-regular-app-service-with-application-gateway.md), which evaluated networking and deployment approaches for providing internal-only access to multiple Azure App Service deployments. This approach would deploy all App Services into a dedicated ILB (Internal Load Balancer) App Service Environment, providing complete network isolation within the VNet.

## Decision

This option was rejected in favor of Regular App Service with Application Gateway.

## Reasoning for Rejection

### Cost Prohibitive for Current Scale

- **Base Cost**: ILB ASE has a minimum cost of approximately $1,000+/month regardless of usage
- **Dedicated Infrastructure**: Pay for reserved compute capacity even when not fully utilized
- **Current Need**: We are deploying a single API initially with plans to add more over time
- **Cost-Benefit Analysis**: At our current and projected scale (1-5 APIs in near term), the cost cannot be justified
- **Break-Even Point**: ILB ASE becomes cost-effective typically at 10+ applications or with specific compliance requirements

### Over-Engineering for Requirements

- **Isolation Requirements**: Our security requirements are met by network-level isolation using Private Endpoints and VNet Integration
- **Compliance**: No regulatory mandates require dedicated infrastructure or complete tenant isolation
- **Multi-Tenant Acceptable**: Azure's multi-tenant App Service model provides sufficient security boundaries for our use case
- **Risk Profile**: Organizational risk assessment does not identify multi-tenant infrastructure as a concern

### Operational Complexity

- **ASE Management**: Requires managing an entire dedicated environment including OS patching, scaling decisions, and capacity planning
- **Deployment Overhead**: ASE provisioning takes 2-4 hours, complicating infrastructure automation
- **Specialized Knowledge**: Requires specialized Azure networking and ASE expertise for operations
- **Limited Flexibility**: Less flexibility in resource allocation compared to pay-per-use App Service Plans

### Feature Overlap Without Added Value

- **Internal Access**: Both solutions provide internal-only access; Application Gateway achieves this with Private Endpoints
- **Load Balancing**: ILB provides basic load balancing; Application Gateway offers more advanced Layer 7 capabilities
- **Routing**: Application Gateway provides superior routing features (path-based, host-based, URL rewriting)
- **WAF**: Application Gateway includes Web Application Firewall; ASE requires additional configuration
- **Monitoring**: Both integrate with Application Insights and Azure Monitor equally

### Scalability Constraints

- **Fixed Capacity**: ASE requires pre-provisioning capacity; scaling requires careful planning
- **Resource Limits**: Each ASE has limits on number of App Service Plans and instances
- **Growth Flexibility**: Regular App Service allows more granular scaling per application
- **Multi-Region**: Expanding to multiple regions requires additional ASE deployments

### Lock-In Concerns

- **Azure-Specific**: Deeper lock-in to Azure-specific infrastructure patterns
- **Migration Complexity**: More difficult to migrate to container-based solutions (AKS, Container Apps)
- **Vendor Dependency**: Harder to adopt multi-cloud or hybrid strategies in the future
- **Technology Evolution**: Container platforms are the future; ASE represents legacy infrastructure approach

### Development and Testing Overhead

- **Dev/Test Environments**: Would need separate ASE instances for each environment, multiplying costs
- **CI/CD Complexity**: More complex deployment pipelines and environment provisioning
- **Local Development**: Harder to replicate ASE environment characteristics locally
- **Experimentation**: High cost barrier to testing new services or proof-of-concepts

### Networking Simplification Not Realized

- **Expected Benefit**: Simpler networking was a potential advantage of ILB ASE
- **Reality**: Still requires careful VNet design, subnet planning, and NSG configuration
- **Application Gateway Value**: Application Gateway provides additional routing, SSL termination, and WAF capabilities
- **Comparable Complexity**: Overall networking complexity is similar between the two approaches

## Alternative Chosen

Regular Azure App Service with Application Gateway (see [ADR 0002](0002-use-regular-app-service-with-application-gateway.md)), which provides:
- Cost-effective solution appropriate for our scale
- Sufficient security and network isolation
- Advanced Layer 7 routing capabilities
- Flexibility for future growth and technology evolution
- Familiar operational model with extensive documentation

## When to Reconsider

ILB App Service Environment should be reconsidered if:

### Scale Justification
- Number of deployed APIs exceeds 10-15 applications
- Cost analysis shows break-even point has been reached
- High-traffic scenarios where dedicated compute is more economical

### Compliance Requirements
- New regulatory requirements mandate dedicated infrastructure
- Organizational policy changes require complete tenant isolation
- Industry-specific compliance standards necessitate ASE

### Performance Requirements
- Performance profiles require dedicated, predictable compute resources
- Multi-tenant infrastructure unable to meet SLA requirements
- Noisy neighbor concerns become demonstrable issues

### Strategic Direction
- Organization commits to Azure-only strategy long-term
- Migration from on-premises to Azure requires ASE-like isolation
- Legacy applications require specific ASE features not available in regular App Service

## References

- [ADR 0002: Use Regular App Service with Application Gateway](0002-use-regular-app-service-with-application-gateway.md)
- [App Service Environment overview](https://learn.microsoft.com/azure/app-service/environment/overview)
- [App Service Environment networking](https://learn.microsoft.com/azure/app-service/environment/networking)
- [App Service pricing](https://azure.microsoft.com/pricing/details/app-service/)
- [Architecture Documentation: App Service + Application Gateway](../app-service-application-gateway-architecture.md)
