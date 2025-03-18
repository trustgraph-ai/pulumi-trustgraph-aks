
import * as azure from "@pulumi/azure-native";
import * as mls from "@pulumi/azure-native/machinelearningservices/v20241001";
import * as cogsvcs from "@pulumi/azure-native/cognitiveservices/v20241001";

import { workspace } from './ai-workspace';
import { resourceGroup } from './resource-group';
import { location, prefix } from './config';
import { azureProvider } from './azure-provider';

const endpoint = new mls.ServerlessEndpoint("ai-endpoint", {
    name: workspace.name.apply(w => `${prefix}-phi-4`),
    resourceGroupName: resourceGroup.name,
    workspaceName: workspace.name,
    location: location,
    serverlessEndpointProperties: {
        authMode: "key",
        modelSettings: {
            modelId: "azureml://registries/azureml/models/Phi-4",
        },
    },
    identity: {
        type: "None",
    },
    sku: {
        name: "Consumption",
    },
}, {
    provider: azureProvider,
    dependsOn: [workspace],
});


export const endpointUri = endpoint.serverlessEndpointProperties.apply(
    props => props.inferenceEndpoint.uri
);

export const endpointChatUri = endpointUri.apply(
    uri => uri + "/v1/chat/completions"
);

export const endpointKeys = mls.listServerlessEndpointKeysOutput(
    {
        name: endpoint.name,
        resourceGroupName: resourceGroup.name,
        workspaceName: workspace.name,
    },
    { provider: azureProvider }
);

export const endpointToken = endpointKeys.apply(
    keys => keys.primaryKey
).apply(
    key => {
        if (key)
            return key;
        throw "No model key!";
    }
);

