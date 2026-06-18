
import * as servicenetworking from "@pulumi/azure-native/servicenetworking";

import { resourceGroup } from './resource-group';
import { location, prefix } from './config';
import { azureProvider } from './azure-provider';
import { agcSubnet } from './networking';

export const trafficController =
    new servicenetworking.TrafficControllerInterface(
        "agc",
        {
            trafficControllerName: `${prefix}-agc`,
            resourceGroupName: resourceGroup.name,
            location: location,
        },
        { provider: azureProvider }
    );

export const frontend =
    new servicenetworking.FrontendsInterface(
        "agc-frontend",
        {
            frontendName: `${prefix}-frontend`,
            resourceGroupName: resourceGroup.name,
            trafficControllerName: trafficController.name,
            location: location,
        },
        { provider: azureProvider }
    );

export const association =
    new servicenetworking.AssociationsInterface(
        "agc-association",
        {
            associationName: `${prefix}-association`,
            resourceGroupName: resourceGroup.name,
            trafficControllerName: trafficController.name,
            associationType: servicenetworking.AssociationType.Subnets,
            subnet: {
                id: agcSubnet.id,
            },
            location: location,
        },
        { provider: azureProvider }
    );

export const agcFqdn = frontend.fqdn;
