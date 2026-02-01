# ADR 0006: Deploy Entire Solution as Single App Service

## Status

Rejected

## Date

2026-01-30

## Context

This option was considered as part of ADR 0001, which evaluated deployment strategies for a multi-project ASP.NET Core solution targeting Azure App Service. This approach would consolidate all REST API projects into a single ASP.NET Core application and deploy as one App Service instance.

## Decision

This option was rejected in favor of deploying each project as an independent App Service.

## Reasoning for Rejection

### Technical Complexity

- **Significant Refactoring Required**: Consolidating multiple independent REST API projects into a single executable requires substantial code changes and architectural restructuring
- **Single Entry Point Constraint**: Would need to merge all project endpoints, middleware pipelines, and configuration into a unified Program.cs and startup configuration
- **Dependency Conflicts**: Risk of package version conflicts and dependency resolution issues when merging multiple projects
- **Build Complexity**: Increased complexity in project references, shared code organization, and build processes

### Operational Limitations

- **Coupled Deployment Lifecycle**: All APIs would deploy together, preventing independent release cycles
- **No Independent Scaling**: Cannot scale individual APIs based on their specific load patterns
- **Shared Resource Pool**: All APIs compete for the same compute, memory, and connection resources
- **Single Point of Failure**: Issues in one API domain could impact all other domains

### Maintenance Burden

- **Code Organization**: Merging projects would blur domain boundaries and make the codebase harder to navigate
- **Testing Complexity**: Integration and deployment testing becomes more complex with a monolithic application
- **Team Coordination**: Requires tight coordination between teams working on different domains
- **Rollback Challenges**: Cannot rollback individual API changes without affecting all domains

### Long-term Considerations

- **Architectural Anti-pattern**: Moves away from microservices principles toward a monolithic architecture
- **Limited Future Flexibility**: Harder to migrate individual services to containers, serverless, or other deployment models
- **Scalability Constraints**: Cannot independently optimize hosting plans, regions, or infrastructure per domain
- **Vendor Lock-in**: Deeper coupling to App Service specifics makes it harder to adopt alternative hosting strategies

## Alternative Chosen

Deploy each project as an independent Azure App Service (see ADR 0001), which provides:
- Independent deployment and scaling capabilities
- Clear domain boundaries and separation of concerns
- Operational flexibility and fault isolation
- Better alignment with microservices architecture principles

## References

- [ADR 0001: Deploy Each Project as Independent Azure App Service](0001-deploy-each-project-as-independent-app-service.md)
- [Microservices architecture style](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/microservices)
