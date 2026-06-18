
import * as pulumi from "@pulumi/pulumi";
import * as azure from "@pulumi/azure-native";
import * as managedidentity from "@pulumi/azure-native/managedidentity";
import * as authorization from "@pulumi/azure-native/authorization";
import * as random from "@pulumi/random";

import { resourceGroup } from './resource-group';
import { prefix } from './config';
import { cluster } from './cluster';
import { agcSubnet } from './networking';
import { subscriptionId } from './azure-provider';

const nodeResourceGroup = pulumi.interpolate`${prefix}-node-rg`;

const albIdentity = managedidentity.getUserAssignedIdentityOutput({
    resourceGroupName: nodeResourceGroup,
    resourceName: pulumi.interpolate`applicationloadbalancer-${prefix}`,
}, { dependsOn: [cluster] });

const albPrincipalId = albIdentity.principalId;

// AppGw for Containers Configuration Manager on the resource group
const configManagerAssignmentId = new random.RandomUuid("alb-config-manager-id", {});
export const configManagerRole = new authorization.RoleAssignment(
    "alb-config-manager",
    {
        principalId: albPrincipalId,
        principalType: authorization.PrincipalType.ServicePrincipal,
        roleDefinitionId: pulumi.interpolate`/subscriptions/${subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/fbc52c3f-28ad-4303-a892-8a056630b8f1`,
        scope: resourceGroup.id,
        roleAssignmentName: configManagerAssignmentId.result,
    },
);

// Network Contributor on the AGC subnet
const networkContribAssignmentId = new random.RandomUuid("alb-network-contrib-id", {});
export const networkContribRole = new authorization.RoleAssignment(
    "alb-network-contributor",
    {
        principalId: albPrincipalId,
        principalType: authorization.PrincipalType.ServicePrincipal,
        roleDefinitionId: pulumi.interpolate`/subscriptions/${subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/4d97b98b-1d4f-4787-a291-c67834d212e7`,
        scope: agcSubnet.id,
        roleAssignmentName: networkContribAssignmentId.result,
    },
);

// Reader on the node resource group is auto-created by the ALB
// Controller addon — no need to manage it here.
