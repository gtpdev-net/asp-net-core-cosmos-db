# ADR 0002: Use Regular App Service with Application Gateway for Internal Access

## Status

**Superseded** by [ADR 0003: Use Private Endpoints Only (No Application Gateway)](0003-use-private-endpoints-only-no-application-gateway.md)

---

**Original Status**: Accepted

## Date

2026-01-30

## Context

Following the decision in [ADR 0001](0001-deploy-each-project-as-independent-app-service.md) to deploy each project as an independent Azure App Service, we need to determine the best approach for providing internal-only access to these APIs. Due to organizational constraints, all API access must be 100% internal, accessible only from on-premises applications through a dedicated Private Endpoint.

We need to evaluate deployment and networking models that satisfy the following requirements:
- Internal-only access (no public endpoints)
- Single entry point from on-premises network
- Ability to route traffic to multiple independent App Services
- Cost-effective solution for current and planned scale
- Compliance with organizational security policies

### Options Considered

1. **Regular App Service with Application Gateway** [Accepted]: Deploy App Services in multi-tenant infrastructure with Private Endpoints and VNet Integration, using Azure Application Gateway as the internal routing layer
2. **ILB App Service Environment (ASE)**: Deploy App Services into a dedicated, fully isolated App Service Environment with an Internal Load Balancer

## Decision

We will use Regular Azure App Service with Application Gateway for internal access routing.

### Rationale

#### Cost Effectiveness

- **Regular App Service**: Pay-per-App Service Plan pricing (~$50-200+/month depending on tier) scales with actual usage
- **Application Gateway**: Fixed cost (~$125+/month for v2) provides centralized routing for all APIs
- **Total Cost**: Predictable and reasonable for our current scale (1 API, planned growth to multiple APIs)
- **ILB ASE Alternative**: Base cost starts at ~$1000+/month regardless of usage, not justified for our current scale

#### Appropriate Scale

- **Current State**: Single API (Examples domain)
- **Planned Growth**: Multiple domain APIs over time
- **Regular App Service**: Well-suited for this growth trajectory
- **ILB ASE**: More appropriate for 10+ applications or organizations requiring dedicated infrastructure

#### Technical Capabilities

- **Application Gateway**: Provides Layer 7 routing, SSL/TLS termination, Web Application Firewall, and health monitoring
- **Private Endpoints**: Enable secure, internal-only access to App Services
- **VNet Integration**: Allows App Services to communicate within the VNet
- **Flexible Routing**: Supports both path-based and host-based routing strategies

#### Operational Flexibility

- **Independent Scaling**: Each App Service can scale independently within shared or dedicated App Service Plans
- **Deployment Independence**: Can deploy, update, and manage each API without affecting others
- **Resource Optimization**: Can share App Service Plans where appropriate while maintaining separation
- **Migration Path**: Can migrate to ILB ASE or container-based solutions if requirements change

#### Multi-Tenant Acceptability

- **Security Posture**: Multi-tenant App Service infrastructure is acceptable given our security requirements
- **Isolation**: Network-level isolation via Private Endpoints and VNet Integration provides sufficient boundaries
- **Compliance**: No regulatory requirements mandate dedicated infrastructure
- **Risk Assessment**: Azure's multi-tenant security model meets our organizational standards

### Implementation Approach

#### Network Architecture

1. **Azure Virtual Network**: Dedicated VNet with proper subnet segmentation
2. **Private Endpoint**: Single entry point from on-premises network for App Service access
3. **Application Gateway**: Deployed with private frontend IP, configured with backend pools for each App Service
4. **VNet Integration**: Each App Service integrates with VNet for internal communication
5. **Network Security Groups**: Applied to subnets to control traffic flow

#### Routing Strategy

- Application Gateway configured with path-based or host-based routing rules
- Each domain API receives traffic through dedicated routing rules
- Health probes ensure backend availability
- SSL/TLS termination handled at Application Gateway

#### Security Controls

- No public endpoints exposed on App Services
- IP restrictions on App Services limiting access to Application Gateway subnet
- Web Application Firewall enabled on Application Gateway
- Managed identities for service-to-service authentication
- Azure Key Vault for secrets and certificate management

## Consequences

### Positive

- **Cost-Effective**: Appropriate pricing for current and planned scale
- **Proven Architecture**: Standard pattern with extensive Microsoft documentation and community support
- **Operational Simplicity**: Familiar deployment and management model
- **Flexibility**: Can independently scale, deploy, and configure each component
- **Growth Accommodation**: Architecture supports adding new APIs without major changes
- **Feature-Rich**: Application Gateway provides advanced traffic management capabilities

### Negative

- **Multi-Component Architecture**: Requires coordination between App Services, Application Gateway, and networking components
- **Multi-Tenant Infrastructure**: Shares underlying compute with other Azure customers (mitigated by network isolation)
- **Configuration Complexity**: More components to configure compared to ILB ASE
- **Multiple Networking Layers**: Requires Private Endpoints, VNet Integration, and Application Gateway configuration

### Mitigation Strategies

- **Infrastructure as Code**: Use Bicep or Terraform to standardize and automate provisioning
- **Documentation**: Maintain comprehensive architecture documentation (see [app-service-application-gateway-architecture.md](../app-service-application-gateway-architecture.md))
- **Monitoring**: Implement centralized logging and monitoring across all components
- **CI/CD**: Automate deployment pipelines to reduce manual configuration errors
- **Runbooks**: Create operational procedures for common tasks and troubleshooting

## Future Considerations

### When to Reconsider

Re-evaluate this decision if:
- Number of deployed APIs exceeds 10-15 applications
- Compliance requirements mandate dedicated infrastructure
- Performance requirements exceed multi-tenant capabilities
- Cost analysis shows ILB ASE would be more economical at scale
- Organizational policy changes require complete network isolation

### Evolution Path

This architecture can evolve to:
- **Azure API Management**: Add API gateway layer for advanced governance
- **ILB App Service Environment**: Migrate to dedicated infrastructure if scale justifies cost
- **Container-Based**: Move to Azure Container Apps or AKS for containerized workloads
- **Multi-Region**: Expand to Azure Front Door for global distribution

## Related Decisions

- [ADR 0001: Deploy Each Project as Independent Azure App Service](0001-deploy-each-project-as-independent-app-service.md)
- [ADR 0008: Use ILB App Service Environment (Rejected)](0008-use-ilb-app-service-environment.md)

## References

- [Architecture Documentation: App Service + Application Gateway](../app-service-application-gateway-architecture.md)
- [App Service networking features](https://learn.microsoft.com/azure/app-service/networking-features)
- [Using private endpoints for Azure App Service](https://learn.microsoft.com/azure/app-service/networking/private-endpoint)
- [What is Azure Application Gateway?](https://learn.microsoft.com/azure/application-gateway/overview)
- [Integrate your app with an Azure virtual network](https://learn.microsoft.com/azure/app-service/overview-vnet-integration)
