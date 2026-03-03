
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
            name: "GlobalStandard", // Best for GPT-4o in 2026
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

/*
export const gpt4oMini = new cognitiveservices.Deployment(
    "gpt-4o-mini-deployment",
    {
        deploymentName: "gpt-4o-mini",
        accountName: aiHub.name,
        resourceGroupName: resourceGroup.name,
        sku: {
            name: "GlobalStandard", // Best for GPT-4o in 2026
            capacity: 10, // Tokens-per-minute (TPM) in thousands
        },
        properties: {
            model: {
                format: "OpenAI",
                name: "gpt-4o-mini",
                version: "2024-07-18", // Standard stable version for early 2026
            },
            versionUpgradeOption: "OnceNewDefaultVersionAvailable",
        },
    },
    { provider: azureProvider, parent: aiHub }
);
*/

/*

// 4. Deployment: Mistral-Large-3
export const mistralDeployment = new (cognitiveservices.Deployment as any)(
    "mistral-large-deployment",
    {
        deploymentName: "mistral-large-3",
        accountName: aiHub.name,
        resourceGroupName: resourceGroup.name,
        sku: {
            name: "GlobalStandard", 
            capacity: 1, // MaaS models often use 1 for 'Standard' tier
        },
        properties: {
            model: {
                format: "Mistral AI", // Crucial for routing to Mistral
                name: "mistral-large-2411", // Mistral Large 3's internal API name
                version: "1",
            },
        },
    },
    { provider: azureProvider, parent: aiHub, dependsOn: [gpt4oDeployment] }
);

*/

