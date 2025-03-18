
import * as resources from "@pulumi/azure-native/resources";

import { azureProvider } from './azure-provider';
import { prefix } from './config';

// Create an Azure Resource Group
export const resourceGroup = new resources.ResourceGroup(
    "resource-group",
    {
        resourceGroupName: prefix,
    },
    { provider: azureProvider }
);
