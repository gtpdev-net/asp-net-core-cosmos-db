# ADR 0001: Deploy Each Project as Independent Azure App Service

## Status

**Accepted** (Partial Implementation)  
**Note**: This ADR describes fully independent deployment. The organization has adopted a [hybrid approach (ADR 0005)](0005-hybrid-production-architecture.md) that uses independent deployment for "greedy" APIs only.

## Date

2026-01-30

## Context

The ASP.NET Core solution currently contains a single REST API project (DataLayer.API.Example) for the 'Examples' domain, with plans to add additional projects for other domains in the future. We needed to determine the optimal deployment strategy for Azure App Service that would accommodate multiple domain-specific REST API projects within the same solution.

### Options Considered

1. **Deploy entire solution as a single App Service**: Consolidate all REST API projects into a single ASP.NET Core application and deploy as one App Service instance.
2. **Deploy each project as an independent App Service** [_Accepted approach_]: Deploy each REST API project as a separate App Service, allowing for independent scaling, deployment, and management.
3. **Use Azure App Service deployment slots**: Deploy all projects to a single App Service using different deployment slots.

### Technical Constraints

- Azure App Service expects a single entry point per deployment
- ASP.NET Core solutions with multiple independent REST API projects compile to separate executables
- Consolidating multiple projects into a single executable would require significant refactoring and architectural changes
- Each domain API should maintain separation of concerns and independent lifecycle management

## Decision

We will deploy each project within the solution as an independent Azure App Service.

### Rationale

- **Independent Scaling**: Each domain API can scale independently based on its specific load and performance requirements
- **Independent Deployment**: APIs can be deployed, updated, and rolled back without affecting other domains
- **Fault Isolation**: Issues in one API do not directly impact the availability of other APIs
- **Clear Boundaries**: Maintains domain-driven design principles with clear service boundaries
- **Flexibility**: Allows different configuration, monitoring, and management policies per API
- **Development Velocity**: Teams can work on and deploy different APIs independently without coordination overhead
- **Cost Management**: Enables granular cost tracking and optimization per domain
- **Technical Feasibility**: No code refactoring required; each project can be deployed as-is

### Implementation Approach

- Each project will be deployed to its own App Service instance
- App Services can share the same App Service Plan for cost optimization (if appropriate)
- Use the PROJECT app setting to specify which .csproj file to deploy when needed
- Implement CI/CD pipelines with independent workflows per project
- Use Azure Resource Groups to organize and manage related resources

## Consequences

### Positive

- Greater operational flexibility and independence for each domain API
- Simplified debugging and troubleshooting with isolated deployments
- Easier to implement different security, monitoring, and compliance requirements per API
- Natural alignment with microservices architecture principles
- Future-proof for potential migration to container-based deployments

### Negative

- Increased number of Azure resources to manage
- Potential for higher costs if each App Service uses a separate App Service Plan
- More complex infrastructure-as-code and deployment automation setup
- Additional configuration and management overhead

### Mitigation Strategies

- Use shared App Service Plans where appropriate to reduce costs
- Implement infrastructure-as-code (Bicep/Terraform) to standardize and automate resource provisioning
- Use Azure DevOps or GitHub Actions for centralized CI/CD management
- Implement consistent monitoring and logging across all App Services using Application Insights
- Consider Azure API Management as a facade layer for unified API management in the future

## Related Decisions

- [ADR 0004: Progressive Deployment Strategy](0004-progressive-deployment-strategy.md) - Evidence-based approach to deployment
- [ADR 0005: Hybrid Production Architecture](0005-hybrid-production-architecture.md) - Production implementation using selective independent deployment
- [Deployment Strategy](../deployment-strategy.md) - Overall strategic approach combining consolidated and independent deployments
- [Consolidated Deployment Architecture](../consolidated-deployment-architecture.md) - Alternative consolidation approach for lazy APIs

## References

- [Configure an ASP.NET Core app for Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-language-dotnetcore)
- [App Service Plan overview](https://learn.microsoft.com/en-us/azure/app-service/overview-hosting-plans)
- [Microservices architecture style](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/microservices)
