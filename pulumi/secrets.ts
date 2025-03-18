
import * as k8s from "@pulumi/kubernetes";

import { appDeploy } from './application';
import { endpointToken, endpointChatUri } from './ai-endpoint';
import { openaiToken, openaiUri, openaiDeployment } from './ai-openai';
import { k8sProvider } from './cluster';

export const gatewaySecret = new k8s.core.v1.Secret(
    "gateway-secret",
    {
        metadata: {
            name: "gateway-secret",
            namespace: "trustgraph"
        },
        stringData: {
            "gateway-secret": ""
        },
    },
    { provider: k8sProvider, dependsOn: appDeploy }
);

export const endpointSecret = new k8s.core.v1.Secret(
    "ai-secret",
    {
        metadata: {
            name: "azure-ai-credentials",
            namespace: "trustgraph"
        },
        stringData: {
            "azure-token": endpointToken,
            "azure-endpoint": endpointChatUri,
        },
    },
    { provider: k8sProvider, dependsOn: appDeploy }
);

export const openaiSecret = new k8s.core.v1.Secret(
    "openai-secret",
    {
        metadata: {
            name: "azure-openai-credentials",
            namespace: "trustgraph"
        },
        stringData: {
            "azure-token": openaiToken,
            "azure-endpoint": openaiUri,
            "azure-model": openaiDeployment.name,
        },
    },
    { provider: k8sProvider, dependsOn: appDeploy }
);

