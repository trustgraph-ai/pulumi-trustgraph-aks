
import * as pulumi from "@pulumi/pulumi";
import * as cognitiveservices from "@pulumi/azure-native/cognitiveservices";
import { location, prefix } from './config';
import { azureProvider } from './azure-provider';
import { resourceGroup } from './resource-group';
import * as random from "@pulumi/random";

// Random ID used for a resource name, must be unique across the universe
const randId = new random.RandomUuid(
    "random-suffix",
    {}
);

export const accountSuffix = randId.result.apply(
    id => prefix + "-" + id.replace("-", "").substring(0, 10)
);

export const customDomain = randId.result.apply(
    id => prefix + "-" + id.replace("-", "")
);

// The Hub (The parent AI Services account)
export const aiHub = new cognitiveservices.Account(
    "ai-account",
    {
        accountName: prefix,
        resourceGroupName: resourceGroup.name,
        location: location,
        kind: "AIServices", 
        sku: { name: "S0" },
        identity: { type: "SystemAssigned" },
        properties: {
            customSubDomainName: customDomain,
            publicNetworkAccess: "Enabled",
            allowProjectManagement: true, 
        },
    },
    { provider: azureProvider }
);

// The project (the child resource)
// Note: We use the 'Project' class from the cognitiveservices namespace
export const aiProject = new cognitiveservices.Project(
    "ai-project",
    {
        projectName: `${prefix}-project`,
        accountName: aiHub.name,
        resourceGroupName: resourceGroup.name,
        location: location,
        identity: { type: "SystemAssigned" },
        properties: {
            description: "TrustGraph AI project",
            displayName: "TrustGraph AI project",
        },
    },
    {
        provider: azureProvider,
        parent: aiHub, // Setting the Pulumi parent-child relationship
        dependsOn: [aiHub],
    }
);


/*
// Exports
export const aiHubEndpoint = aiHub.properties.apply(p => p.endpoint);
export const aiProjectApiUrl = aiProject.properties.apply(
    p => p.endpoints?.["AI Foundry API"]
);

*/
