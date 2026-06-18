
import * as k8s from "@pulumi/kubernetes";
import * as pulumi from "@pulumi/pulumi";
import * as random from "@pulumi/random";

import { appDeploy } from './application';
import { aiHubEndpoint, apiKey1 } from './ai-keys';
import { k8sProvider } from './cluster';

export const iamBootstrapToken = new random.RandomPassword(
    "iam-bootstrap-token",
    {
        length: 32,
        special: false,
    },
);

export const grafanaAdminPassword = new random.RandomPassword(
    "grafana-admin-password",
    {
        length: 16,
        special: true,
        overrideSpecial: "!@#$%^&*",
    },
);

export const iamSecret = new k8s.core.v1.Secret(
    "iam-bootstrap-token",
    {
        metadata: {
            name: "iam-bootstrap-token",
            namespace: "trustgraph"
        },
        stringData: {
            "token": pulumi.interpolate`tg_${iamBootstrapToken.result}`,
        },
    },
    { provider: k8sProvider, dependsOn: appDeploy }
);

export const grafanaSecret = new k8s.core.v1.Secret(
    "grafana-secret",
    {
        metadata: {
            name: "grafana-secret",
            namespace: "trustgraph"
        },
        stringData: {
            "password": grafanaAdminPassword.result,
        },
    },
    { provider: k8sProvider, dependsOn: appDeploy }
);

export const aiSecret = new k8s.core.v1.Secret(
    "azure-ai-credentials",
    {
        metadata: {
            name: "azure-ai-credentials",
            namespace: "trustgraph"
        },
        stringData: {
            "azure-token": apiKey1,
            "azure-endpoint": aiHubEndpoint.apply(s => s.replace(/\/+$/, "") + "/openai/v1/chat/completions"),
        },
    },
    { provider: k8sProvider, dependsOn: appDeploy }
);

