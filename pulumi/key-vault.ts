
import * as azure from "@pulumi/azure-native";

import { resourceGroup } from './resource-group';
import { location } from './config';
import { azureProvider, tenantId } from './azure-provider';

export const keyVault = new azure.keyvault.Vault(
    "key-vault",
    {
        resourceGroupName: resourceGroup.name,
        location: location,
        properties: {
            tenantId: tenantId,
            sku: {
                family: azure.keyvault.SkuFamily.A,
                name: azure.keyvault.SkuName.Standard,
            },
        },
    },
    { provider: azureProvider }
);
