
import * as k8s from "@pulumi/kubernetes";

import { appDeploy } from './application';
import { aiHubEndpoint, apiKey1 } from './ai-project';
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

export const mcpServerSecret = new k8s.core.v1.Secret(
    "mcp-server-secret",
    {
        metadata: {
            name: "mcp-server-secret",
            namespace: "trustgraph"
        },
        stringData: {
            "mcp-server-secret": ""
        },
    },
    { provider: k8sProvider, dependsOn: appDeploy }
);

export const aiSecret = new k8s.core.v1.Secret(
    "openai-secret",
    {
        metadata: {
            name: "openai-credentials",
            namespace: "trustgraph"
        },
        stringData: {
            "openai-token": apiKey1,
            "openai-url": aiHubEndpoint.apply(s => s + "openai/v1"),
        },
    },
    { provider: k8sProvider, dependsOn: appDeploy }
);

