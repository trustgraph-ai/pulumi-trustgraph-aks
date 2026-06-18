
# Gateway API + TLS on Kubernetes: Lessons Learned

Notes from implementing public HTTPS access on Azure AKS using
Gateway API, Application Gateway for Containers (AGC), cert-manager,
and Let's Encrypt.  Written to help guide similar work on other cloud
providers.

## Architecture overview

The pattern is:

1. Cloud-managed load balancer fronts traffic (AGC on Azure, similar
   on GCP/AWS)
2. Kubernetes Gateway API defines listeners (HTTP + HTTPS per hostname)
3. HTTPRoutes attach to listeners and route to backend services
4. cert-manager handles TLS certificate lifecycle via Let's Encrypt
   ACME HTTP-01 challenges

## The chicken-and-egg problem

This was the hardest issue.  It affects any cloud-managed Gateway API
implementation that validates the entire Gateway resource atomically.

The problem:
- HTTPS listeners reference TLS secrets (Kubernetes secrets containing
  certs)
- Those secrets don't exist yet because cert-manager needs to create them
- cert-manager creates them by completing ACME HTTP-01 challenges
- HTTP-01 challenges require HTTP traffic to reach the cluster
- The cloud load balancer won't program ANY routes (including HTTP)
  until ALL listeners are valid
- All listeners can't be valid without the TLS secrets
- Deadlock

This is specific to cloud-managed gateways that treat the Gateway as an
atomic unit.  In-cluster gateways (Envoy Gateway, Nginx Gateway Fabric)
don't have this problem -- they'll happily serve HTTP while logging
warnings about missing TLS secrets.

### The solution: temporary certificate annotations

cert-manager supports two annotations on Certificate resources that
break the deadlock:

```yaml
annotations:
  cert-manager.io/issue-temporary-certificate: "true"
  acme.cert-manager.io/http01-edit-in-place: "true"
```

- `issue-temporary-certificate` creates a self-signed placeholder cert
  immediately, so the TLS secret exists and all Gateway listeners
  become valid
- `http01-edit-in-place` tells cert-manager to reuse the existing
  HTTPRoute for ACME challenges instead of creating a new one, avoiding
  conflicts

This was confirmed as the expected approach by a Microsoft engineer on
https://github.com/Azure/AKS/issues/5509.

The resulting flow:
1. Pulumi creates Certificate resources with the annotations
2. cert-manager immediately creates self-signed placeholder secrets
3. Gateway sees valid TLS secrets, all listeners become valid
4. Cloud load balancer programs all routes
5. ACME HTTP-01 challenges succeed because HTTP traffic flows
6. cert-manager replaces the self-signed certs with real Let's Encrypt
   certs
7. Single `pulumi up`, no manual intervention

### HTTPRoutes must attach to the HTTP listener

A second part of the problem: the ACME challenge solver needs HTTP
traffic to reach it.  Make sure your application HTTPRoutes reference
both the HTTPS and HTTP listeners:

```typescript
parentRefs: [
    { name: "gateway", sectionName: "https-ui" },
    { name: "gateway", sectionName: "http" },
],
```

If routes only attach to HTTPS listeners, there's no HTTP routing for
ACME challenges.

## Azure-specific: AGC and ALB Controller

### Preview API versions

The ALB Controller addon for AKS requires a preview API version that
the standard Pulumi Azure Native SDK doesn't support.  We used the
generic ARM resource (`resources.Resource`) to target the preview API
directly:

```typescript
import * as resources from "@pulumi/azure-native/resources";

const cluster = new resources.Resource("cluster", {
    resourceProviderNamespace: "Microsoft.ContainerService",
    resourceType: "managedClusters",
    parentResourcePath: "",
    apiVersion: "2025-10-02-preview",
    // ... full cluster definition as properties
});
```

This is more reliable than trying to use parameterised/generated preview
SDK packages, which we found too fragile for production use.

The kubeconfig can still be retrieved using the standard typed SDK:

```typescript
import * as containerservice from "@pulumi/azure-native/containerservice";
containerservice.listManagedClusterUserCredentialsOutput({...});
```

### Cluster configuration for Gateway API

The AKS cluster needs these in its properties:

```json
{
    "ingressProfile": {
        "gatewayAPI": { "installation": "Standard" },
        "applicationLoadBalancer": { "enabled": true }
    },
    "oidcIssuerProfile": { "enabled": true },
    "securityProfile": { "workloadIdentity": { "enabled": true } },
    "networkProfile": {
        "networkPlugin": "azure",
        "networkPluginMode": "overlay"
    }
}
```

Azure CNI Overlay is required for AGC.

### Prerequisites

The `ApplicationLoadBalancerPreview` feature must be registered on the
subscription before deployment:

```
az feature register --namespace "Microsoft.ContainerService" \
  --name "ApplicationLoadBalancerPreview"
az provider register -n Microsoft.ContainerService
az provider register -n Microsoft.ServiceNetworking
```

Registration can take a few minutes.  Check status with:

```
az feature show --namespace "Microsoft.ContainerService" \
  --name "ApplicationLoadBalancerPreview" \
  --query "properties.state"
```

### ALB Controller role assignments

The ALB Controller runs under a managed identity that Azure creates
automatically in the node resource group.  It needs two role
assignments:

1. **AppGw for Containers Configuration Manager** on the resource group
   (role ID `fbc52c3f-28ad-4303-a892-8a056630b8f1`)
2. **Network Contributor** on the AGC subnet
   (role ID `4d97b98b-1d4f-4787-a291-c67834d212e7`)

Azure also auto-creates a **Reader** role on the node resource group.
Do NOT manage that in Pulumi -- it causes a 409 conflict because Azure
already created it.

The managed identity name follows the pattern
`applicationloadbalancer-{cluster-name}` and lives in the node resource
group.

### Networking

AGC requires its own subnet with a delegation:

```typescript
delegations: [{
    name: "agc-delegation",
    serviceName: "Microsoft.ServiceNetworking/trafficControllers",
}]
```

We used a /24 for the AGC subnet and a /16 for the AKS nodes, both
within a single VNet.

### AGC resources

Three resources make up the AGC infrastructure:
- `TrafficControllerInterface` -- the load balancer itself
- `FrontendsInterface` -- the public-facing frontend (provides the FQDN)
- `AssociationsInterface` -- links the frontend to the AGC subnet

The frontend FQDN (e.g. `xxx.fz11.alb.azure.com`) is what users point
their DNS CNAME records at.

### Health checks

AGC health-probes backends by requesting `/` by default.  If a backend
returns a redirect (e.g. Grafana redirects `/` to `/login`), AGC marks
it unhealthy.  Use a `HealthCheckPolicy` custom resource to set a
proper health check path:

```typescript
{
    apiVersion: "alb.networking.azure.io/v1",
    kind: "HealthCheckPolicy",
    spec: {
        targetRef: {
            group: "", kind: "Service",
            name: "grafana", namespace: "trustgraph",
        },
        default: {
            http: { path: "/api/health" },
        },
    },
}
```

## Applicability to other cloud providers

### What transfers directly

- The cert-manager + Let's Encrypt pattern (Helm chart, ClusterIssuer,
  Certificate resources with temporary cert annotations)
- The Gateway + HTTPRoute structure
- The multi-parentRef pattern for HTTP + HTTPS listeners
- The chicken-and-egg problem and its solution -- any cloud-managed
  gateway implementation that does atomic validation will hit this

### What will differ per provider

- The GatewayClass name (`azure-alb-external` on Azure)
- Gateway annotations for linking to the cloud load balancer
- Gateway address format
- How the load balancer infrastructure is provisioned (AGC on Azure,
  GKE Gateway Controller on GCP, AWS Load Balancer Controller on EKS)
- Role/IAM assignments for the gateway controller
- Networking requirements (subnet delegations, security groups)
- Health check configuration (CRD format varies)
- Whether you need preview API workarounds (hopefully not)

### Alternative: in-cluster gateways

If the cloud-managed gateway proves too painful, consider Envoy Gateway
or Nginx Gateway Fabric.  They run as pods in the cluster behind a
standard LoadBalancer service.  Benefits:

- No chicken-and-egg problem (they serve HTTP immediately)
- No cloud-specific CRDs for health checks
- Portable across providers
- Simpler IAM (no controller identity to manage)

Trade-offs:

- You manage the gateway software yourself
- No cloud-native WAF/DDoS integration
- The LoadBalancer service gives you an IP, not a managed FQDN
