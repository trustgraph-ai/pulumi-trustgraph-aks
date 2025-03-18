
import * as azure from "@pulumi/azure-native";
import * as random from "@pulumi/random";

import { resourceGroup } from './resource-group';
import { location, prefix } from './config';
import { azureProvider } from './azure-provider';

const randId = new random.RandomUuid(
    "storage-account-suffix",
    {}
);

export const storageAccountName = randId.result.apply(
    id => "trustgraph" + id.replace("-", "").substring(0, 8)
);

export const storageAccount = new azure.storage.StorageAccount(
    "storage-account", {
        accountName: storageAccountName,
        kind: azure.storage.Kind.StorageV2,
        location: location,
        resourceGroupName: resourceGroup.name,
        sku: {
            name: "Standard_LRS",
        },
    },
    { provider: azureProvider }
);
