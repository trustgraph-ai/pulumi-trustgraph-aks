
import * as azure from "@pulumi/azure-native";

import { location } from './config';

export const azureProvider = new azure.Provider(
    "azure-provider",
    {
        location: location,
    }
);

export const subscriptionId = azure.authorization.getClientConfigOutput().
apply(
    sub => sub.subscriptionId
);

export const tenantId = azure.authorization.getClientConfigOutput().apply(
    sub => sub.tenantId
);
