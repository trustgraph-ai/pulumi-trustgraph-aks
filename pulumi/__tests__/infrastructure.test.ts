import * as pulumi from "@pulumi/pulumi";
import * as fs from "fs";

jest.mock('fs');
const mockedFs = fs as jest.Mocked<typeof fs>;

const createdResources: Array<{type: string, name: string, inputs: any}> = [];
let resourceCount = 0;

describe("Infrastructure Creation", () => {
    beforeAll(() => {
        mockedFs.readFileSync.mockReturnValue(`
apiVersion: v1
kind: Namespace
metadata:
  name: trustgraph
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: test-app
  namespace: trustgraph
spec:
  replicas: 1
        `);

        mockedFs.writeFile.mockImplementation(
            (_path: any, _data: any, callback: any) => {
                if (typeof callback === 'function') {
                    callback(null);
                }
            }
        );

        pulumi.runtime.setAllConfig({
            "project:environment": "test",
            "project:location": "swedencentral",
            "project:node-size": "Standard_D8s_v5",
            "project:node-count": "2",
            "project:domain": "test.dev.trustgraph.ai",
            "project:grafana-domain": "grafana.test.dev.trustgraph.ai",
            "project:letsencrypt-email": "test@trustgraph.ai",
        });

        pulumi.runtime.setMocks({
            newResource: function(args: pulumi.runtime.MockResourceArgs): {id: string, state: any} {
                resourceCount++;
                createdResources.push({
                    type: args.type,
                    name: args.name,
                    inputs: args.inputs
                });

                const mockId = `mock-${args.type}-${args.name}-${resourceCount}`;
                let state: any = {
                    ...args.inputs,
                    id: mockId,
                    name: args.inputs.name || args.name,
                };

                if (args.type === "azure-native:resources:Resource") {
                    state.name = args.inputs.resourceName || args.name;
                    state.properties = {
                        fqdn: "mock-cluster.hcp.swedencentral.azmk8s.io",
                    };
                }

                if (args.type === "azure-native:cognitiveservices:Account") {
                    state.properties = {
                        endpoint: "https://mock-endpoint.cognitiveservices.azure.com/",
                        endpoints: {
                            "AI Foundry API": "https://mock-ai-foundry.api.azureml.ms",
                        },
                    };
                }

                if (args.type === "azure-native:cognitiveservices:Project") {
                    state.properties = {
                        endpoints: {
                            "AI Foundry API": "https://mock-project.api.azureml.ms",
                        },
                    };
                }

                if (args.type === "azure-native:servicenetworking:FrontendsInterface") {
                    state.fqdn = "mock-frontend.fz11.alb.azure.com";
                }

                if (args.type === "azure-native:managedidentity:UserAssignedIdentity") {
                    state.principalId = "mock-principal-id";
                }

                if (args.type === "tls:index/privateKey:PrivateKey") {
                    state.publicKeyOpenssh = "ssh-rsa AAAA mock-key";
                    state.privateKeyOpenssh = "-----BEGIN OPENSSH PRIVATE KEY-----\nmock\n-----END OPENSSH PRIVATE KEY-----";
                }

                if (args.type === "random:index/randomPassword:RandomPassword") {
                    state.result = "mock-random-password-value";
                }

                if (args.type === "random:index/randomUuid:RandomUuid") {
                    state.result = "00000000-0000-0000-0000-000000000000";
                }

                return { id: mockId, state };
            },
            call: function(args: pulumi.runtime.MockCallArgs) {
                if (args.token === "azure-native:containerservice:listManagedClusterUserCredentials") {
                    return {
                        kubeconfigs: [{
                            value: Buffer.from("mock-kubeconfig-content").toString("base64"),
                        }],
                    };
                }
                if (args.token === "azure-native:authorization:getClientConfig") {
                    return {
                        subscriptionId: "00000000-0000-0000-0000-000000000000",
                        tenantId: "00000000-0000-0000-0000-000000000001",
                    };
                }
                if (args.token === "azure-native:cognitiveservices:listAccountKeys") {
                    return {
                        key1: "mock-api-key-1",
                        key2: "mock-api-key-2",
                    };
                }
                if (args.token === "azure-native:managedidentity:getUserAssignedIdentity") {
                    return {
                        principalId: "mock-alb-principal-id",
                    };
                }
                return args.inputs;
            },
        });
    });

    test("infrastructure creates all expected resources", async () => {
        await expect(import("../index")).resolves.toBeDefined();

        await new Promise(resolve => setTimeout(resolve, 100));

        // Uncomment to debug resource types:
        // console.log(createdResources.map(r => `${r.type} - ${r.name} - ${r.inputs.metadata?.name || ''}`).join('\n'));

        expect(createdResources.length).toBeGreaterThan(0);

        // Azure provider
        const provider = createdResources.find(r => r.type === "pulumi:providers:azure-native");
        expect(provider).toBeDefined();

        // Resource group
        const rg = createdResources.find(r => r.type === "azure-native:resources:ResourceGroup");
        expect(rg).toBeDefined();
        expect(rg?.inputs.resourceGroupName).toBe("trustgraph-test");

        // Networking
        const vnet = createdResources.find(r => r.type === "azure-native:network:VirtualNetwork");
        expect(vnet).toBeDefined();
        expect(vnet?.inputs.virtualNetworkName).toBe("trustgraph-test-vnet");

        const subnets = createdResources.filter(r => r.type === "azure-native:network:Subnet");
        expect(subnets.length).toBe(2);
        const aksSubnet = subnets.find(s => s.inputs.subnetName === "aks-nodes");
        const agcSubnet = subnets.find(s => s.inputs.subnetName === "agc");
        expect(aksSubnet).toBeDefined();
        expect(agcSubnet).toBeDefined();
        expect(agcSubnet?.inputs.delegations?.[0]?.serviceName).toBe(
            "Microsoft.ServiceNetworking/trafficControllers"
        );

        // AKS cluster (generic ARM resource)
        const cluster = createdResources.find(r => r.type === "azure-native:resources:Resource");
        expect(cluster).toBeDefined();
        expect(cluster?.inputs.resourceProviderNamespace).toBe("Microsoft.ContainerService");
        expect(cluster?.inputs.resourceType).toBe("managedClusters");
        expect(cluster?.inputs.apiVersion).toBe("2025-10-02-preview");

        // AGC resources
        const trafficController = createdResources.find(
            r => r.type === "azure-native:servicenetworking:TrafficControllerInterface"
        );
        const agcFrontend = createdResources.find(
            r => r.type === "azure-native:servicenetworking:FrontendsInterface"
        );
        const agcAssociation = createdResources.find(
            r => r.type === "azure-native:servicenetworking:AssociationsInterface"
        );
        expect(trafficController).toBeDefined();
        expect(agcFrontend).toBeDefined();
        expect(agcAssociation).toBeDefined();

        // AI resources
        const aiAccount = createdResources.find(
            r => r.type === "azure-native:cognitiveservices:Account"
        );
        const aiProject = createdResources.find(
            r => r.type === "azure-native:cognitiveservices:Project"
        );
        const aiDeployment = createdResources.find(
            r => r.type === "azure-native:cognitiveservices:Deployment"
        );
        expect(aiAccount).toBeDefined();
        expect(aiProject).toBeDefined();
        expect(aiDeployment).toBeDefined();
        expect(aiDeployment?.inputs.deploymentName).toBe("mistral-large-3");

        // K8s provider
        const k8sProvider = createdResources.find(r => r.type === "pulumi:providers:kubernetes");
        expect(k8sProvider).toBeDefined();

        // cert-manager
        const certManagerNs = createdResources.find(
            r => r.type === "kubernetes:core/v1:Namespace" && r.inputs.metadata?.name === "cert-manager"
        );
        expect(certManagerNs).toBeDefined();

        const helmCharts = createdResources.filter(r => r.type === "kubernetes:helm.sh/v4:Chart");
        const certManagerChart = helmCharts.find(r => r.name === "cert-manager");
        expect(certManagerChart).toBeDefined();

        // Gateway
        const gateway = createdResources.find(
            r => r.type === "kubernetes:gateway.networking.k8s.io/v1:Gateway"
        );
        expect(gateway).toBeDefined();
        expect(gateway?.inputs.metadata?.name).toBe("trustgraph-gateway");

        // ClusterIssuer
        const letsEncryptIssuer = createdResources.find(
            r => r.type === "kubernetes:cert-manager.io/v1:ClusterIssuer"
        );
        expect(letsEncryptIssuer).toBeDefined();
        expect(letsEncryptIssuer?.inputs.metadata?.name).toBe("letsencrypt-prod");

        // Certificates with bootstrap annotations
        const certs = createdResources.filter(
            r => r.type === "kubernetes:cert-manager.io/v1:Certificate"
        );
        const uiCert = certs.find(r => r.inputs.metadata?.name === "ui-cert");
        const grafanaCert = certs.find(r => r.inputs.metadata?.name === "grafana-cert");
        expect(uiCert).toBeDefined();
        expect(grafanaCert).toBeDefined();
        expect(uiCert?.inputs.metadata?.annotations?.["cert-manager.io/issue-temporary-certificate"]).toBe("true");
        expect(grafanaCert?.inputs.metadata?.annotations?.["acme.cert-manager.io/http01-edit-in-place"]).toBe("true");

        // HTTPRoutes
        const routes = createdResources.filter(
            r => r.type === "kubernetes:gateway.networking.k8s.io/v1:HTTPRoute"
        );
        const uiRoute = routes.find(r => r.inputs.metadata?.name === "ui-route");
        const grafanaRoute = routes.find(r => r.inputs.metadata?.name === "grafana-route");
        expect(uiRoute).toBeDefined();
        expect(grafanaRoute).toBeDefined();

        // HTTPRoutes attach to both HTTP and HTTPS listeners
        expect(uiRoute?.inputs.spec?.parentRefs?.length).toBe(2);
        expect(grafanaRoute?.inputs.spec?.parentRefs?.length).toBe(2);

        // Grafana health check
        const healthCheck = createdResources.find(
            r => r.type === "kubernetes:alb.networking.azure.io/v1:HealthCheckPolicy"
        );
        expect(healthCheck).toBeDefined();

        // Kubernetes secrets
        const secrets = createdResources.filter(r => r.type === "kubernetes:core/v1:Secret");
        const iamSecret = secrets.find(s => s.inputs.metadata?.name === "iam-bootstrap-token");
        const grafanaSecret = secrets.find(s => s.inputs.metadata?.name === "grafana-secret");
        const aiSecret = secrets.find(s => s.inputs.metadata?.name === "azure-ai-credentials");
        expect(iamSecret).toBeDefined();
        expect(grafanaSecret).toBeDefined();
        expect(aiSecret).toBeDefined();
        expect(iamSecret?.inputs.metadata?.namespace).toBe("trustgraph");
        expect(grafanaSecret?.inputs.metadata?.namespace).toBe("trustgraph");
        expect(aiSecret?.inputs.metadata?.namespace).toBe("trustgraph");

        // Role assignments for ALB Controller
        const roleAssignments = createdResources.filter(
            r => r.type === "azure-native:authorization:RoleAssignment"
        );
        expect(roleAssignments.length).toBe(2);
    });
});
