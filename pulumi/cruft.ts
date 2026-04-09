
/*

const hubTemplate = pulumi.all([
    resourceGroup.name, storageAccount.id, keyVault.id
]).apply(
    ([rg, st, kv]) => {
        return {
            "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
            "contentVersion": "1.0.0.0",
            "parameters": {
            },
            "variables": {},
            "resources": [
                {
                    "type": "Microsoft.MachineLearningServices/workspaces",
                    "apiVersion": "2024-10-01",
                    "name": "ai-hub",
                    "properties": {
                        friendlyName: "ai-hub",
                        description: "TrustGraph AI hub",
                        storageAccount: st,
                        keyVault: kv,
                        hbiWorkspace: false,
                        managedNetwork: {
                            isolationMode: "Disabled",
                        },
                        v1LegacyMode: false,
                        softDeleteEnabled: false,
                        storageHnsEnabled: false,
                        publicNetworkAccess: "Enabled",
                        enableDataIsolation: true,
                    },
                    identity: {
                        type: "SystemAssigned"
                    },
                    sku: {
                        name: "Basic",
                        tier: "Basic",
                    },
                }
            ]
        };
    }
);

const hub = new azure.resources.Deployment(
    "ai-hub",
    {
        properties: {
            debugSetting: {
                detailLevel: "requestContent, responseContent",
            },
            mode: azure.resources.DeploymentMode.Complete,
            template: hubTemplate,
        },
        resourceGroupName: resourceGroup.name,
        deploymentName: "ai-hub",
//        location: location,
    },
    { provider: azureProvider }
);

*/

// ------------------------------------------------------------------------
/*

const workspace = new machinelearningservices.Workspace(
    "ai-workspace",
    {
        workspaceName: "trustgraph",
        friendlyName: "trustgraph",
        description: "TrustGraph AI endpoints",
        resourceGroupName: resourceGroup.name,
        location: location,
        storageAccount: storageAccount.id,
        keyVault: keyVault.id,
        publicNetworkAccess: "Enabled",
        kind: "Hub",
        identity: {
            type: "SystemAssigned"
        },
        sku: {
            name: "Basic",
            tier: "Basic",
        },
        hbiWorkspace: false,
        v1LegacyMode: false,
    },
    { provider: azureProvider }
);

*/

/*
const modelSubs = new azure.machinelearningservices.MarketplaceSubscription(
    "model-subscription",
    {
        marketplaceSubscriptionProperties: {
            modelId: "azureml://registries/azureml/models/Phi-4",
        },
        name: "Phi-4",
        resourceGroupName: resourceGroup.name,
        workspaceName: workspace.name,
    },
    { provider: azureProvider }
);
*/

/*
const endpoint = new azure.machinelearningservices.ServerlessEndpoint(
    "model-endpoint",
    {
        resourceGroupName: resourceGroup.name,
        workspaceName: workspace.name,
//        endpointName: "trustgraph-phi3",   // FIXME: Make unique
        location: location,
        kind: "Serverless",
        serverlessEndpointProperties: {
            offer: {
                offerName: "azureml://registries/azureml/models/Phi-4",
                publisher: "Microsoft",
//            model_id: "azureml://registries/azureml/models/Phi-4",
            }
        },
    },
    { provider: azureProvider }
);
*/

// ------------------------------------------------------------------------

/*

const aiAccount = new azure.cognitiveservices.Account(
    "ai-account",
    {
        resourceGroupName: resourceGroup.name,
        location: location,
        properties: {
            // FIXME: Make this unique
            customSubDomainName: "trustgraph0003",
            publicNetworkAccess: "Enabled", // Adjust as needed
        },
        sku: {
            name: "S0",
        },
//        kind: "AIServices",
        kind: "OpenAI",  // Important: Use OpenAI kind, even for Phi-3 (?)
    },
    { provider: azureProvider }
);

const aiEndpoint = aiAccount.properties.apply(
    props => props.endpoints["Azure AI Model Inference API"]
);


const model = new azure.cognitiveservices.Deployment(
    "phi3-deployment",
    {
        accountName: aiAccount.name,
        resourceGroupName: resourceGroup.name,
        deploymentName: "phi-3",
        properties: {
            model: {
                format: "OpenAI",
                name: "phi-4",
                version: "7",
                
            },
            scaleSettings: {
                capacity: 1,
                scaleType: "Manual", // Or "Standard" for auto-scaling
            },
        },
        sku: {
            name: "Standard",
        },
    },
    { provider: azureProvider }
);

const endpointKeys = azure.cognitiveservices.listAccountKeysOutput(
    { accountName: aiAccount.name, resourceGroupName: resourceGroup.name },
    { provider: azureProvider }
);

const endpointKey : pulumi.Output<string> = endpointKeys.key1!.apply(
    key => {
        if (key)
            return key;
        throw "No model key!";
    }
);

*/
