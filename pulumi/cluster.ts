
import * as pulumi from "@pulumi/pulumi";
import * as resources from "@pulumi/azure-native/resources";
import * as containerservice from "@pulumi/azure-native/containerservice";
import * as k8s from "@pulumi/kubernetes";

import { resourceGroup } from './resource-group';
import { vmSize, vmCount, location, prefix } from './config';
import { sshKey } from './ssh-key';
import { azureProvider } from './azure-provider';
import { aksSubnet } from './networking';

export const cluster = new resources.Resource(
    "cluster",
    {
        resourceProviderNamespace: "Microsoft.ContainerService",
        resourceType: "managedClusters",
        parentResourcePath: "",
        apiVersion: "2025-10-02-preview",
        resourceGroupName: resourceGroup.name,
        resourceName: prefix,
        location: location,
        identity: {
            type: "SystemAssigned",
        },
        properties: {
            kubernetesVersion: "1.35.5",
            dnsPrefix: pulumi.interpolate`${resourceGroup.name}-aks`,
            enableRBAC: true,
            agentPoolProfiles: [{
                count: vmCount,
                maxPods: 110,
                mode: "System",
                name: "agentpool",
                osType: "Linux",
                type: "VirtualMachineScaleSets",
                vmSize: vmSize,
                vnetSubnetID: aksSubnet.id,
            }],
            linuxProfile: {
                adminUsername: "aksuser",
                ssh: {
                    publicKeys: [
                        {
                            keyData: sshKey.publicKeyOpenssh,
                        },
                    ],
                },
            },
            nodeResourceGroup: pulumi.interpolate`${resourceGroup.name}-node-rg`,
            networkProfile: {
                networkPlugin: "azure",
                networkPluginMode: "overlay",
                serviceCidr: "10.2.0.0/16",
                dnsServiceIP: "10.2.0.10",
            },
            oidcIssuerProfile: { enabled: true },
            securityProfile: {
                workloadIdentity: { enabled: true },
            },
            ingressProfile: {
                gatewayAPI: {
                    installation: "Standard",
                },
                applicationLoadBalancer: {
                    enabled: true,
                },
            },
        },
        sku: {
            name: "Base",
            tier: "Free",
        },
    },
    {
        provider: azureProvider,
    }
);

export const kubeconfig = pulumi.all(
    [cluster.name, resourceGroup.name]
).apply(
    ([clusterName, rgName]) => {
        return containerservice.listManagedClusterUserCredentialsOutput({
            resourceGroupName: rgName,
            resourceName: clusterName,
        }).kubeconfigs[0].value.apply(
            kubeconfig => Buffer.from(kubeconfig, "base64").toString()
        );
    }
);

export const k8sProvider = new k8s.Provider(
    "k8s-provider",
    {
        kubeconfig: kubeconfig,
    }
);
