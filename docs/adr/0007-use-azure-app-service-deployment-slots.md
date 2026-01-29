# ADR 0007: Use Azure App Service Deployment Slots

## Status

Rejected

## Date

2026-01-30

## Context

This option was considered as part of ADR 0001, which evaluated deployment strategies for a multi-project ASP.NET Core solution targeting Azure App Service. This approach would deploy all projects to a single App Service using different deployment slots for each domain API.

## Decision

This option was rejected in favor of deploying each project as an independent App Service.

## Reasoning for Rejection

### Misuse of Deployment Slots

- **Not Designed for This Purpose**: Deployment slots are intended for staging/production environments and testing changes before production deployment, not for hosting different applications or services
- **Architectural Misalignment**: Using slots to separate domain APIs violates the intended design pattern of deployment slots
- **Best Practice Violation**: Goes against Microsoft's documented best practices for deployment slot usage

### Technical Limitations

- **Single Application Constraint**: All slots share the same App Service configuration baseline and are designed to run the same application
- **Slot Swap Limitations**: Slot swap operations are designed for blue-green deployments, not for managing multiple distinct APIs
- **Configuration Complexity**: Difficult to maintain independent configurations for fundamentally different applications within slots
- **Routing Challenges**: No native way to route external traffic to specific slots for production use; slots are typically accessed via special URLs

### Resource and Cost Constraints

- **Slot Limitations**: Standard and Premium tiers have limited deployment slots (5-20 depending on tier)
- **Shared Resources**: All slots share the same App Service Plan resources, limiting independent scaling
- **No Independent Scaling**: Cannot scale individual domain APIs independently based on their specific needs
- **Cost Inefficiency**: Requires higher-tier App Service Plans to get sufficient slots, but without the benefits of true isolation

### Operational Issues

- **Deployment Confusion**: Developers would need to remember which slot corresponds to which domain API
- **Monitoring Complexity**: Application Insights and logging would be more difficult to configure and interpret across slots
- **No Traffic Isolation**: Cannot implement independent rate limiting, authentication, or traffic management per API
- **Maintenance Overhead**: Managing multiple APIs through slot management tools adds unnecessary complexity

### Security and Isolation Concerns

- **Shared Security Context**: All slots share the same App Service security boundary and managed identity
- **No Network Isolation**: Cannot implement independent VNet integration or network security policies per slot
- **Compliance Challenges**: Difficult to meet compliance requirements that mandate service isolation
- **Secret Management**: Sharing Key Vault references and secrets across fundamentally different applications is problematic

### Scalability and Growth Limitations

- **Limited Growth**: As more domain APIs are added, will quickly exhaust available deployment slots
- **No Independent Lifecycle**: Cannot independently upgrade runtime versions, frameworks, or dependencies per API
- **Migration Path Unclear**: No clear evolution path from slots to proper independent services

## Alternative Chosen

Deploy each project as an independent Azure App Service (see ADR 0001), which provides:
- Proper use of deployment slots within each App Service for their intended purpose (staging/production)
- True isolation and independence for each domain API
- Ability to scale and configure each service appropriately
- Clear operational boundaries and ownership

## References

- [ADR 0001: Deploy Each Project as Independent Azure App Service](0001-deploy-each-project-as-independent-app-service.md)
- [Set up staging environments in Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/deploy-staging-slots)
- [Azure App Service deployment best practices](https://learn.microsoft.com/en-us/azure/app-service/deploy-best-practices)
