
import * as cognitiveservices from "@pulumi/azure-native/cognitiveservices";
import { resourceGroup } from './resource-group';
import { azureProvider } from './azure-provider';
import { aiHub, aiProject } from './ai-project';
import { llmQuota } from './config';

export const mistralLarge = new cognitiveservices.Deployment(
    "mistral-large-deployment",
    {
        deploymentName: "mistral-large-3",
        accountName: aiHub.name,
        resourceGroupName: resourceGroup.name,
        sku: {
            name: "GlobalStandard",
            capacity: llmQuota,
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
        dependsOn: [aiProject],
    }
);
