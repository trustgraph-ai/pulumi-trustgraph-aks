import * as pulumi from "@pulumi/pulumi";

pulumi.runtime.setMocks({
    newResource: function(args: pulumi.runtime.MockResourceArgs): {id: string, state: any} {
        return {
            id: args.inputs.name + "_id",
            state: args.inputs,
        };
    },
    call: function(args: pulumi.runtime.MockCallArgs) {
        return args.inputs;
    },
});

describe("Configuration Loading", () => {
    beforeEach(() => {
        pulumi.runtime.setAllConfig({
            "project:environment": "test",
            "project:location": "swedencentral",
            "project:node-size": "Standard_D8s_v5",
            "project:node-count": "2",
            "project:domain": "test.dev.trustgraph.ai",
            "project:grafana-domain": "grafana.test.dev.trustgraph.ai",
            "project:letsencrypt-email": "test@trustgraph.ai",
        });
    });

    afterEach(() => {
        jest.resetModules();
    });

    test("should load required configuration values", async () => {
        const config = await import("../config");

        expect(config.environment).toBe("test");
        expect(config.location).toBe("swedencentral");
    });

    test("should generate correct prefix based on environment", async () => {
        const config = await import("../config");

        expect(config.prefix).toBe("trustgraph-test");
    });

    test("should have correct node configuration", async () => {
        const config = await import("../config");

        expect(config.vmSize).toBe("Standard_D8s_v5");
        expect(config.vmCount).toBe(2);
    });

    test("should load domain configuration", async () => {
        const config = await import("../config");

        expect(config.domain).toBe("test.dev.trustgraph.ai");
        expect(config.grafanaDomain).toBe("grafana.test.dev.trustgraph.ai");
        expect(config.letsencryptEmail).toBe("test@trustgraph.ai");
    });

    test("should default llm-quota to 10", async () => {
        const config = await import("../config");

        expect(config.llmQuota).toBe(10);
    });

    test("should use custom llm-quota when set", async () => {
        pulumi.runtime.setAllConfig({
            "project:environment": "test",
            "project:location": "swedencentral",
            "project:node-size": "Standard_D8s_v5",
            "project:node-count": "2",
            "project:domain": "test.dev.trustgraph.ai",
            "project:grafana-domain": "grafana.test.dev.trustgraph.ai",
            "project:letsencrypt-email": "test@trustgraph.ai",
            "project:llm-quota": "20",
        });

        const config = await import("../config");

        expect(config.llmQuota).toBe(20);
    });

    test("should handle missing environment configuration", async () => {
        pulumi.runtime.setAllConfig({
            "project:location": "swedencentral",
            "project:node-size": "Standard_D8s_v5",
            "project:node-count": "2",
            "project:domain": "test.dev.trustgraph.ai",
            "project:grafana-domain": "grafana.test.dev.trustgraph.ai",
            "project:letsencrypt-email": "test@trustgraph.ai",
        });

        await expect(import("../config")).rejects.toThrow();
    });

    test("should handle missing location configuration", async () => {
        pulumi.runtime.setAllConfig({
            "project:environment": "test",
            "project:node-size": "Standard_D8s_v5",
            "project:node-count": "2",
            "project:domain": "test.dev.trustgraph.ai",
            "project:grafana-domain": "grafana.test.dev.trustgraph.ai",
            "project:letsencrypt-email": "test@trustgraph.ai",
        });

        await expect(import("../config")).rejects.toThrow();
    });

    test("should handle missing domain configuration", async () => {
        pulumi.runtime.setAllConfig({
            "project:environment": "test",
            "project:location": "swedencentral",
            "project:node-size": "Standard_D8s_v5",
            "project:node-count": "2",
            "project:grafana-domain": "grafana.test.dev.trustgraph.ai",
            "project:letsencrypt-email": "test@trustgraph.ai",
        });

        await expect(import("../config")).rejects.toThrow();
    });
});
