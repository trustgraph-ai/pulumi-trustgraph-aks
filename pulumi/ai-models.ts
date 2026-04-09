
import * as cognitiveservices from "@pulumi/azure-native/cognitiveservices";
import { resourceGroup } from './resource-group';
import { azureProvider } from './azure-provider';
import { aiHub, aiProject } from './ai-project';

// We use dependsOn to make the deployments happen serially.  Azure
// flips out if you try to deploy more than one at once

export const gpt4o = new cognitiveservices.Deployment(
    "gpt-4o-deployment",
    {
        deploymentName: "gpt-4o",
        accountName: aiHub.name,
        resourceGroupName: resourceGroup.name,
        sku: {
            name: "Standard",
            capacity: 10, // Tokens-per-minute (TPM) in thousands
        },
        properties: {
            model: {
                format: "OpenAI",
                name: "gpt-4o",
                version: "2024-11-20",
            },
            versionUpgradeOption: "OnceNewDefaultVersionAvailable",
        },
    },
    { provider: azureProvider, parent: aiHub, dependsOn: [aiProject] }
);

export const gpt4oMini = new cognitiveservices.Deployment(
    "gpt-4o-mini-deployment",
    {
        deploymentName: "gpt-4o-mini",
        accountName: aiHub.name,
        resourceGroupName: resourceGroup.name,
        sku: {
            name: "Standard",
            capacity: 10, // Tokens-per-minute (TPM) in thousands
        },
        properties: {
            model: {
                format: "OpenAI",
                name: "gpt-4o-mini",
                version: "2024-07-18",
            },
            versionUpgradeOption: "OnceNewDefaultVersionAvailable",
        },
    },
    { provider: azureProvider, parent: aiHub, dependsOn: [gpt4o] }
);

export const mistralLarge = new cognitiveservices.Deployment(
    "mistral-large-deployment",
    {
        deploymentName: "mistral-large-3",
        accountName: aiHub.name,
        resourceGroupName: resourceGroup.name,
        sku: {
            name: "GlobalStandard", 
            capacity: 10, // Tokens-per-minute (TPM) in thousands
        },
        properties: {
            model: {
                format: "Mistral AI", 
                name: "Mistral-Large-3",
                version: "1",
            },
        },
    },
    {
        provider: azureProvider,
        parent: aiHub,
        dependsOn: [gpt4oMini] 
    }
);

export const mistralSmall = new cognitiveservices.Deployment(
    "mistral-small-deployment",
    {
        deploymentName: "mistral-small",
        accountName: aiHub.name,
        resourceGroupName: resourceGroup.name,
        sku: {
            name: "GlobalStandard", 
            capacity: 1,
        },
        properties: {
            model: {
                format: "Mistral AI", 
                name: "mistral-small-2503", 
                version: "1",
            },
        },
    },
    {
        provider: azureProvider,
        parent: aiHub,
        dependsOn: [mistralLarge],
    }
);

