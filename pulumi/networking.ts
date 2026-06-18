
import * as network from "@pulumi/azure-native/network";

import { resourceGroup } from './resource-group';
import { location, prefix } from './config';
import { azureProvider } from './azure-provider';

export const vnet = new network.VirtualNetwork(
    "vnet",
    {
        virtualNetworkName: `${prefix}-vnet`,
        resourceGroupName: resourceGroup.name,
        location: location,
        addressSpace: {
            addressPrefixes: ["10.0.0.0/8"],
        },
    },
    { provider: azureProvider }
);

export const aksSubnet = new network.Subnet(
    "aks-subnet",
    {
        subnetName: "aks-nodes",
        resourceGroupName: resourceGroup.name,
        virtualNetworkName: vnet.name,
        addressPrefix: "10.240.0.0/16",
    },
    { provider: azureProvider }
);

export const agcSubnet = new network.Subnet(
    "agc-subnet",
    {
        subnetName: "agc",
        resourceGroupName: resourceGroup.name,
        virtualNetworkName: vnet.name,
        addressPrefix: "10.1.0.0/24",
        delegations: [{
            name: "agc-delegation",
            serviceName: "Microsoft.ServiceNetworking/trafficControllers",
        }],
    },
    { provider: azureProvider }
);
