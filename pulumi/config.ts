
import * as pulumi from "@pulumi/pulumi";

const cfg = new pulumi.Config();

export const environment = cfg.require("environment");
export const location = cfg.require("location");

export const tags : { [key : string] : string } = {
};

export const tagsSep = Object.entries(tags).map(
    (x : string[]) => (x[0] + "=" + x[1])
).join(",");

export const prefix = "trustgraph-" + environment;

export const vmSize = cfg.require("node-size");
export const vmCount = Number(cfg.require("node-count"));

export const domain = cfg.require("domain");
export const grafanaDomain = cfg.require("grafana-domain");
export const letsencryptEmail = cfg.require("letsencrypt-email");
export const llmQuota = Number(cfg.get("llm-quota") || "10");
