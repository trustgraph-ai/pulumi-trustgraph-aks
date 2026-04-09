
import * as cognitiveservices from "@pulumi/azure-native/cognitiveservices";
import { azureProvider } from './azure-provider';
import { resourceGroup } from './resource-group';
import { aiHub, aiHubEndpoint } from './ai-project';
import { mistralSmall } from './ai-models';

// Get API keys after all model deployments are complete
export const apiKeys = cognitiveservices.listAccountKeysOutput(
    {
        resourceGroupName: resourceGroup.name,
        accountName: aiHub.name,
    },
    { provider: azureProvider, dependsOn: [aiHub, mistralSmall] }
);

export const apiKey1 = apiKeys.apply(keys => keys?.key1 ?? "");
export const apiKey2 = apiKeys.apply(keys => keys?.key2 ?? "");

// Re-export for convenience
export { aiHubEndpoint };
