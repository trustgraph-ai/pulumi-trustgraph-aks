
import * as azure from "@pulumi/azure-native";
import * as azuread from "@pulumi/azuread";

import { location } from './config';

export const azureProvider = new azure.Provider(
    "azure-provider",
    {
        location: location,
    }
);

/*
export const azureADProvider = new azure.Provider(
    "azure-ad-provider",
    {
        location: location,
        tenantId: "xxx",
    }
);
*/

export const azureADProvider = undefined;

export const subscriptionId = azure.authorization.getClientConfigOutput().
apply(
    sub => sub.subscriptionId
);

export const tenantId = azure.authorization.getClientConfigOutput().apply(
    sub => sub.tenantId
);
