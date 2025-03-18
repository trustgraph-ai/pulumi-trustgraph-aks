
import * as pulumi from "@pulumi/pulumi";
import * as random from "@pulumi/random";
import * as azuread from "@pulumi/azuread";
import * as azure from "@pulumi/azure-native";

import { azureADProvider, subscriptionId } from './azure-provider';
import { resourceGroup } from './resource-group';
import { prefix } from './config';

// A standard global Azure AI Administrator role.  Too much power, but,
// seems the same as though Azure AI Developer role
const roleId = "b78c5d69-af96-48a3-bf8d-a8b4d589de94";

// Create an Azure AD Application
const clusterApplication = new azuread.Application(
    "cluster-application",
    {
        displayName: prefix,
        description: "TrustGraph cluster deployment",
    },
    { provider: azureADProvider }
);

// Create a service principal for the application
export const clusterPrincipal = new azuread.ServicePrincipal(
    "service-principal",
    {
        clientId: clusterApplication.clientId,
        description: "TrustGraph"
    },
    { provider: azureADProvider }
);

// Create a password for the service principal
export const clusterPrincipalPassword = new azuread.ServicePrincipalPassword(
    "service-principal-password",
    {
        servicePrincipalId: clusterPrincipal.id,
        displayName: "TrustGraph cluster password",
        endDate: "2099-01-01T00:00:00Z",
    },
    { provider: azureADProvider }
);

const scope = pulumi.all(
    [subscriptionId, resourceGroup.name]
).apply(
    ([subs, rg]) =>
        `subscriptions/${subs}/resourceGroups/${rg}`
);

const roleDefinitionId = pulumi.interpolate`/subscriptions/${subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/${roleId}`;

const assignmentId = new random.RandomUuid(
    "assignment-id",
    {}
);

const appRoleAssignment = new azure.authorization.RoleAssignment(
    "role-assignment",
    {
        principalId: clusterPrincipal.objectId,
        principalType: azure.authorization.PrincipalType.ServicePrincipal,
        roleDefinitionId: roleDefinitionId,
        scope: scope,
        roleAssignmentName: assignmentId.result,
    },
    { provider: azureADProvider }
);
