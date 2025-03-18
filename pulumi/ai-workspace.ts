
import * as azure from "@pulumi/azure-native";
import * as machinelearningservices
    from '@pulumi/azure-native/machinelearningservices/v20241001';

import { storageAccount } from './storage-account';
import { keyVault } from './key-vault';
import { resourceGroup } from './resource-group';
import { azureProvider } from './azure-provider';
import { location } from './config';

export const hub = new machinelearningservices.Workspace(
    "ai-hub",
    {
        resourceGroupName: resourceGroup.name,
        workspaceName: "ai-hub",
        location: location,
        tags: {},
        friendlyName: "ai-workspace",
        description: "AI workspace",
        storageAccount: storageAccount.id,
        keyVault: keyVault.id,
        hbiWorkspace: false,
        publicNetworkAccess: "Enabled",
        v1LegacyMode: false,
        enableDataIsolation: true,
        kind: "Hub",
        identity: {
            type: "SystemAssigned",
        },
        sku: {
            name: "Basic",
            tier: "Basic",
        },
    },
    { provider: azureProvider }
);

export const workspace = new machinelearningservices.Workspace(
    "ai-workspace",
    {
        resourceGroupName: resourceGroup.name,
        workspaceName: "ai-workspace",
        location: location,
        tags: {},
        friendlyName: "ai-workspace",
        description: "AI workspace",
        hbiWorkspace: false,
        v1LegacyMode: false,
        publicNetworkAccess: "Enabled",
        enableDataIsolation: true,
        hubResourceId: hub.id,
        kind: "Project",
        identity: {
            type: "SystemAssigned"
        },
        sku: {
            name: "Basic",
            tier: "Basic",
        },
    },
    { provider: azureProvider }
);
