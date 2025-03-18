
import * as fs from 'fs';
import * as k8s from "@pulumi/kubernetes";

import { k8sProvider } from './cluster';

const resourceDefs = fs.readFileSync("../resources.yaml", {encoding: "utf-8"});

export const appDeploy = new k8s.yaml.v2.ConfigGroup(
    "resources",
    {
        yaml: resourceDefs,
        skipAwait: true,
    },
    { provider: k8sProvider }
);

