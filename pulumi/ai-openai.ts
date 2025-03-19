
import * as cogsvcs from "@pulumi/azure-native/cognitiveservices/v20241001";
import * as random from "@pulumi/random";

import { resourceGroup } from './resource-group';
import { azureProvider } from './azure-provider';
import { location, prefix } from './config';
import {
    aiOpenaiModel, aiOpenaiFormat, aiOpenaiVersion, aiOpenaiRaiPolicy
} from './config';

const randId = new random.RandomUuid(
    "openai-account-suffix",
    {}
);

export const accountName = randId.result.apply(
    id => prefix + "-" + id.replace("-", "").substring(0, 8)
);

export const account = new cogsvcs.Account(
    "account",
    {
        resourceGroupName: resourceGroup.name,
        accountName: accountName,
        kind: "AIServices",
        properties: {
            customSubDomainName: accountName,
            publicNetworkAccess: "Enabled",
        },
        sku: {
            name: "S0",
        },
        location: location,
    },
    { provider: azureProvider }
);

export const openaiDeployment = new cogsvcs.Deployment(
    "openai-deployment",
    {
        resourceGroupName: resourceGroup.name,
        accountName: account.name,
        deploymentName: prefix + "-openai",
        properties: {
            model: {
                name: aiOpenaiModel,
                format: aiOpenaiFormat,
                version: aiOpenaiVersion,
            },
            raiPolicyName: aiOpenaiRaiPolicy,
            versionUpgradeOption: "OnceNewDefaultVersionAvailable",
        },
        sku: {
            capacity: 250,
            name: "GlobalStandard",
        },
    },
    { provider: azureProvider }
);

export const openaiUri = account.properties.endpoint;

export const openaiKeys = cogsvcs.listAccountKeysOutput(
    {
        accountName: account.name,
        resourceGroupName: resourceGroup.name,
    },
    { provider: azureProvider }
);

export const openaiToken = openaiKeys.apply(
    keys => keys.key1
).apply(
    key => {
        if (key)
            return key;
        throw "No model key!";
    }
);

