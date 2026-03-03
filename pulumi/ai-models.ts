
import * as cognitiveservices from "@pulumi/azure-native/cognitiveservices";
import { resourceGroup } from './resource-group';
import { azureProvider } from './azure-provider';
import { aiHub } from './ai-project';

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
                version: "2024-11-20", // Standard stable version for early 2026
            },
            versionUpgradeOption: "OnceNewDefaultVersionAvailable",
        },
    },
    { provider: azureProvider, parent: aiHub }
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
    { provider: azureProvider, parent: aiHub }
);

export const mistralLarge3 = new cognitiveservices.Deployment(
    "mistral-large-deployment",
    {
        deploymentName: "mistral-large-3",
        accountName: aiHub.name,
        resourceGroupName: resourceGroup.name,
        sku: {
            name: "GlobalStandard", 
            capacity: 1,
        },
        properties: {
            model: {
                format: "AzureML", 
                name: "azureml://registries/azureml-mistral/models/Mistral-Large-3/versions/1",
                version: "1",
                modelSource: "azureml://registries/azureml-mistral",
            },
        },
    },
    {
        provider: azureProvider,
        parent: aiHub,
    }
);



