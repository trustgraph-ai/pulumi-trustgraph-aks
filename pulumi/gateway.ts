
import * as k8s from "@pulumi/kubernetes";

import { k8sProvider } from './cluster';
import { domain, grafanaDomain } from './config';
import { trafficController, frontend, association } from './agc';
import { letsEncryptIssuer } from './cert-manager';

export const gateway = new k8s.apiextensions.CustomResource(
    "trustgraph-gateway",
    {
        apiVersion: "gateway.networking.k8s.io/v1",
        kind: "Gateway",
        metadata: {
            name: "trustgraph-gateway",
            namespace: "trustgraph",
            annotations: {
                "alb.networking.azure.io/alb-id": trafficController.id,
            },
        },
        spec: {
            gatewayClassName: "azure-alb-external",
            listeners: [
                {
                    name: "http",
                    protocol: "HTTP",
                    port: 80,
                    allowedRoutes: {
                        namespaces: { from: "Same" },
                    },
                },
                {
                    name: "https-ui",
                    protocol: "HTTPS",
                    port: 443,
                    hostname: domain,
                    tls: {
                        certificateRefs: [{
                            name: "ui-tls",
                        }],
                    },
                    allowedRoutes: {
                        namespaces: { from: "Same" },
                    },
                },
                {
                    name: "https-grafana",
                    protocol: "HTTPS",
                    port: 443,
                    hostname: grafanaDomain,
                    tls: {
                        certificateRefs: [{
                            name: "grafana-tls",
                        }],
                    },
                    allowedRoutes: {
                        namespaces: { from: "Same" },
                    },
                },
            ],
            addresses: [{
                type: "alb.networking.azure.io/alb-frontend",
                value: frontend.name,
            }],
        },
    },
    { provider: k8sProvider, dependsOn: [association] }
);

export const uiCertificate = new k8s.apiextensions.CustomResource(
    "ui-cert",
    {
        apiVersion: "cert-manager.io/v1",
        kind: "Certificate",
        metadata: {
            name: "ui-cert",
            namespace: "trustgraph",
            annotations: {
                "cert-manager.io/issue-temporary-certificate": "true",
                "acme.cert-manager.io/http01-edit-in-place": "true",
            },
        },
        spec: {
            secretName: "ui-tls",
            issuerRef: {
                name: "letsencrypt-prod",
                kind: "ClusterIssuer",
            },
            dnsNames: [domain],
        },
    },
    { provider: k8sProvider, dependsOn: [letsEncryptIssuer, gateway] }
);

export const grafanaCertificate = new k8s.apiextensions.CustomResource(
    "grafana-cert",
    {
        apiVersion: "cert-manager.io/v1",
        kind: "Certificate",
        metadata: {
            name: "grafana-cert",
            namespace: "trustgraph",
            annotations: {
                "cert-manager.io/issue-temporary-certificate": "true",
                "acme.cert-manager.io/http01-edit-in-place": "true",
            },
        },
        spec: {
            secretName: "grafana-tls",
            issuerRef: {
                name: "letsencrypt-prod",
                kind: "ClusterIssuer",
            },
            dnsNames: [grafanaDomain],
        },
    },
    { provider: k8sProvider, dependsOn: [letsEncryptIssuer, gateway] }
);

export const uiRoute = new k8s.apiextensions.CustomResource(
    "ui-route",
    {
        apiVersion: "gateway.networking.k8s.io/v1",
        kind: "HTTPRoute",
        metadata: {
            name: "ui-route",
            namespace: "trustgraph",
        },
        spec: {
            parentRefs: [
                {
                    name: "trustgraph-gateway",
                    sectionName: "https-ui",
                },
                {
                    name: "trustgraph-gateway",
                    sectionName: "http",
                },
            ],
            hostnames: [domain],
            rules: [{
                backendRefs: [{
                    name: "trustgraph-ui",
                    port: 8888,
                }],
            }],
        },
    },
    { provider: k8sProvider, dependsOn: [gateway] }
);

export const grafanaHealthCheck = new k8s.apiextensions.CustomResource(
    "grafana-health-check",
    {
        apiVersion: "alb.networking.azure.io/v1",
        kind: "HealthCheckPolicy",
        metadata: {
            name: "grafana-health-check",
            namespace: "trustgraph",
        },
        spec: {
            targetRef: {
                group: "",
                kind: "Service",
                name: "grafana",
                namespace: "trustgraph",
            },
            default: {
                interval: "5s",
                timeout: "3s",
                healthyThreshold: 1,
                unhealthyThreshold: 3,
                http: {
                    host: "localhost",
                    path: "/api/health",
                    match: {
                        statusCodes: [{ start: 200, end: 299 }],
                    },
                },
            },
        },
    },
    { provider: k8sProvider, dependsOn: [gateway] }
);

export const grafanaRoute = new k8s.apiextensions.CustomResource(
    "grafana-route",
    {
        apiVersion: "gateway.networking.k8s.io/v1",
        kind: "HTTPRoute",
        metadata: {
            name: "grafana-route",
            namespace: "trustgraph",
        },
        spec: {
            parentRefs: [
                {
                    name: "trustgraph-gateway",
                    sectionName: "https-grafana",
                },
                {
                    name: "trustgraph-gateway",
                    sectionName: "http",
                },
            ],
            hostnames: [grafanaDomain],
            rules: [{
                backendRefs: [{
                    name: "grafana",
                    port: 3000,
                }],
            }],
        },
    },
    { provider: k8sProvider, dependsOn: [gateway] }
);
