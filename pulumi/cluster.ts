
import * as pulumi from "@pulumi/pulumi";
import * as containerservice from "@pulumi/azure-native/containerservice";
import * as k8s from "@pulumi/kubernetes";

import { resourceGroup } from './resource-group';
import { vmSize, vmCount, location, prefix } from './config';
import { sshKey } from './ssh-key';
import {
    clusterPrincipal, clusterPrincipalPassword
} from './service-principal';
import { azureProvider } from './azure-provider';

// Create an Azure Kubernetes Service (AKS) cluster
export const cluster = new containerservice.ManagedCluster(
    "cluster",
    {
        resourceName: prefix,
        resourceGroupName: resourceGroup.name,
        agentPoolProfiles: [{
            count: vmCount,
            maxPods: 110,
            mode: "System",
            name: "agentpool",
            osType: "Linux",
            type: "VirtualMachineScaleSets",
            vmSize: vmSize,
        }],
        dnsPrefix: pulumi.interpolate`${resourceGroup.name}-aks`,
        enableRBAC: true,
        kubernetesVersion: "1.32.0",
        linuxProfile: {
            adminUsername: "aksuser",
            ssh: {
                publicKeys: [
                    {
                        keyData: sshKey.publicKeyOpenssh
                    }
                ],
            },
        },
        nodeResourceGroup: pulumi.interpolate`${resourceGroup.name}-node-rg`,
        servicePrincipalProfile: {
            clientId: clusterPrincipal.clientId,
            secret: clusterPrincipalPassword.value,
        },
        sku: {
            name: "Base",
            tier: containerservice.ManagedClusterSKUTier.Free,
        },
    },
    {
        provider: azureProvider,
    }
);

// Export the kubeconfig
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

// Create a Kubernetes provider instance using the kubeconfig
export const k8sProvider = new k8s.Provider(
    "k8s-provider",
    {
        kubeconfig: kubeconfig,
    }
);

