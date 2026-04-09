
import * as cognitiveservices from "@pulumi/azure-native/cognitiveservices";
import { resourceGroup } from './resource-group';
import { azureProvider } from './azure-provider';
import { aiHub, aiProject } from './ai-project';

// We use dependsOn to make the deployments happen serially.  Azure
// flips out if you try to deploy more than one at once

export const gpt5Nano = new cognitiveservices.Deployment(
    "gpt-5-4-nano-deployment",
    {
        deploymentName: "gpt-5.4-nano",
        accountName: aiHub.name,
        resourceGroupName: resourceGroup.name,
        sku: {
            name: "GlobalStandard",
            capacity: 25, // Tokens-per-minute (TPM) in thousands
        },
        properties: {
            model: {
                format: "OpenAI",
                name: "gpt-5.4-nano",
                version: "2026-03-17",
            },
            versionUpgradeOption: "OnceNewDefaultVersionAvailable",
        },
    },
    { provider: azureProvider, parent: aiHub, dependsOn: [aiProject] }
);

export const gpt5Mini = new cognitiveservices.Deployment(
    "gpt-5-4-mini-deployment",
    {
        deploymentName: "gpt-5.4-mini",
        accountName: aiHub.name,
        resourceGroupName: resourceGroup.name,
        sku: {
            name: "GlobalStandard",
            capacity: 15, // Tokens-per-minute (TPM) in thousands
        },
        properties: {
            model: {
                format: "OpenAI",
                name: "gpt-5.4-mini",
                version: "2026-03-17",
            },
            versionUpgradeOption: "OnceNewDefaultVersionAvailable",
        },
    },
    { provider: azureProvider, parent: aiHub, dependsOn: [gpt5Nano] }
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
        dependsOn: [gpt5Mini] 
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

