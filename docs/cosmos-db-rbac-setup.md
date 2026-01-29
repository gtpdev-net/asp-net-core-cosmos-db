# Cosmos DB RBAC Setup

## Overview

This document describes how to grant data plane permissions to users or service principals for Azure Cosmos DB using Azure Role-Based Access Control (RBAC).

## Background

Azure Cosmos DB supports two types of access control:
- **Control Plane**: Manages the Cosmos DB account itself (create/delete databases, containers, etc.)
- **Data Plane**: Manages the data within Cosmos DB (read, write, query documents)

When using Azure Active Directory (Entra ID) authentication with `DefaultAzureCredential`, users need explicit data plane permissions to access and manipulate data in Cosmos DB.

## Required Role

**Cosmos DB Built-in Data Contributor** - Provides full read/write access to data in the Cosmos DB account, including:
- Read and write documents
- Query data
- Create and delete documents
- Manage stored procedures, triggers, and UDFs

## Granting Data Plane Permissions

### Prerequisites

1. Azure CLI installed and authenticated (`az login`)
2. Appropriate subscription selected
3. User's principal ID (object ID from Azure AD)
4. Contributor or Owner access to the Cosmos DB account

### Command

```bash
# Set variables
COSMOS_ACCOUNT="data-layer-api"
RESOURCE_GROUP="DataLayerAPI"
USER_PRINCIPAL_ID="85a720ce-065e-4639-9653-b9ffba5666e4"

# Get the Cosmos DB account scope
COSMOS_SCOPE=$(az cosmosdb show \
  --name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query id -o tsv)

# Assign the role
az cosmosdb sql role assignment create \
  --account-name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --role-definition-name "Cosmos DB Built-in Data Contributor" \
  --principal-id $USER_PRINCIPAL_ID \
  --scope $COSMOS_SCOPE
```

### Getting Your Principal ID

To get your user's principal ID (object ID):

```bash
az ad signed-in-user show --query id -o tsv
```

For a service principal or managed identity:

```bash
az ad sp show --id <app-id> --query id -o tsv
```

## Verification

After granting permissions, you can verify the role assignment:

```bash
az cosmosdb sql role assignment list \
  --account-name $COSMOS_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query "[].{PrincipalId:principalId, Role:roleDefinitionId}" -o table
```

## Application Configuration

Once RBAC is configured, your application can authenticate using `DefaultAzureCredential` without requiring connection strings or access keys:

```csharp
var cosmosClient = new CosmosClient(
    accountEndpoint,
    new DefaultAzureCredential(),
    cosmosClientOptions);
```

The `DefaultAzureCredential` will automatically use:
1. Environment variables (in deployed environments)
2. Managed Identity (when running in Azure)
3. Azure CLI credentials (during local development)
4. Visual Studio / VS Code credentials (during local development)

## Additional Roles

Azure Cosmos DB provides other built-in roles:

- **Cosmos DB Built-in Data Reader**: Read-only access to data
- **Cosmos DB Built-in Contributor**: Manage account settings but not data
- **Cosmos DB Operator**: Manage account operations without data access

To use a different role, replace the `--role-definition-name` parameter with the desired role name.

## Security Best Practices

1. **Principle of Least Privilege**: Grant only the minimum permissions required
2. **Use Managed Identities**: For applications running in Azure, use Managed Identities instead of user accounts
3. **Separate Environments**: Use different Cosmos DB accounts for dev/test/prod with appropriate permissions
4. **Audit Access**: Regularly review role assignments using Azure Policy or Azure Monitor
5. **Avoid Keys**: Prefer RBAC over connection strings and access keys for better security and auditability

## Troubleshooting

### "Authorization Failed" Errors

If you receive authorization errors after granting permissions:

1. **Wait for propagation**: Role assignments can take a few minutes to propagate
2. **Verify subscription**: Ensure you're using the correct Azure subscription
3. **Check principal ID**: Verify the principal ID matches your user/service principal
4. **Token refresh**: Clear cached credentials or restart your application

### Checking Current Subscription

```bash
az account show --query "{Name:name, SubscriptionId:id}" -o table
```

### Switching Subscriptions

```bash
az account set --subscription <subscription-name-or-id>
```

## References

- [Azure Cosmos DB RBAC Documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-rbac)
- [DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)
- [Cosmos DB Security Best Practices](https://learn.microsoft.com/en-us/azure/cosmos-db/database-security)
