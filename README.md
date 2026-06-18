
# Deploy TrustGraph in an Azure AKS using Pulumi

## Overview

This is an installation of TrustGraph on Azure using the Kubernetes (AKS)
platform.

The full stack includes:

- Its own resource group
- A VNet with dedicated subnets for AKS nodes and AGC
- An AKS cluster with managed identity, workload identity, and the
  ALB Controller addon for Gateway API support
- Azure Application Gateway for Containers (AGC) providing public HTTPS
  access via Gateway API
- cert-manager with Let's Encrypt for automated TLS certificate management
- A key vault and storage account for the AI components
- Using AI Foundry, deploys an AI hub and project with Mistral-Large-3
- Deploys a complete TrustGraph stack of resources in AKS

Keys and other configuration for the AI components are configured into
TrustGraph using secrets.

## How it works

This uses Pulumi which is a deployment framework, similar to Terraform
but:
- Pulumi has an open source licence
- Pulumi uses general-purposes programming languages, particularly useful
  because you can use test frameworks to test the infrastructure.

Roadmap to deploy is:
- Prerequisites (one-time)
- Install Pulumi
- Setup Pulumi
- Configure your environment with Azure credentials using `az login`
- Modify the local configuration to do what you want
- Deploy
- Configure DNS
- Use the system

## Prerequisites (one-time)

The following Azure preview features must be registered on your
subscription before deploying:

```
az feature register --namespace "Microsoft.ContainerService" --name "ApplicationLoadBalancerPreview"
az provider register -n Microsoft.ContainerService
```

You can check registration status with:

```
az feature show --namespace "Microsoft.ContainerService" --name "ApplicationLoadBalancerPreview" --query "properties.state"
```

Wait until it shows `Registered` before deploying.

The following resource providers must also be registered:

```
az provider register -n Microsoft.ServiceNetworking
az provider register -n Microsoft.ContainerService
```

# Deploy

## Deploy Pulumi

Navigate to the Pulumi directory:

```
cd pulumi
```

Then:

```
npm install
```

## Setup Pulumi

You need to tell Pulumi which state to use.  You can store this in an S3
bucket, but for experimentation, you can just use local state:

```
pulumi login --local
```

If you want to encrypt secrets, you can set a passphrase.  If not, you
can set the passphrase to the empty string to avoid the password prompts.

```
export PULUMI_CONFIG_PASSPHRASE=
```

Pulumi operates in stacks, each stack is a separate deployment.  The
git repo contains the configuration for a single stack `azure`, so you
could:

```
pulumi stack init azure
```

and it will use the configuration in `Pulumi.azure.yaml`.

## Configure your environment with Azure credentials

Standard Azure client configuration e.g. use `az login` to configure
credentials.

## Modify the local configuration to do what you want

You can edit:
- settings in `Pulumi.STACKNAME.yaml` e.g. Pulumi.azure.yaml
- change `resources.yaml` with whatever you want to deploy.
  The resources.yaml file was created using the TrustGraph config portal,
  so you can re-generate your own.

The `Pulumi.STACKNAME.yaml` configuration file contains settings for:

- `trustgraph-azure:location` - Azure deployment location
- `trustgraph-azure:environment` - Name of the environment you are deploying,
  use a name like: dev, prod etc.
- `trustgraph-azure:node-size` - The VM size for AKS nodes
  e.g. `Standard_D8s_v5`
- `trustgraph-azure:node-count` - Number of nodes in the AKS cluster
- `trustgraph-azure:domain` - Domain name for TrustGraph UI
  e.g. `azure1.dev.trustgraph.ai`
- `trustgraph-azure:grafana-domain` - Domain name for Grafana
  e.g. `grafana.azure1.dev.trustgraph.ai`
- `trustgraph-azure:letsencrypt-email` - Email address for Let's Encrypt
  certificate notifications
- `trustgraph-azure:llm-quota` - LLM model quota in thousands of
  tokens-per-minute (TPM).  Default: `10`.  Increase if you hit rate
  limiting under normal usage

## Deploy

```
pulumi up
```

Just say yes.

If everything works:
- A file `kube.cfg` will also be created which provides access
  to the Kubernetes cluster.
- A file, `ssh-private.key` will contain a secret SSH
  login key for the K8s instances.  You shouldn't need to use this.
- The AGC Frontend FQDN will be output as `agcFrontendFqdn`.

To connect to the Kubernetes cluster...

```
kubectl --kubeconfig kube.cfg -n trustgraph get pods
```

An error has been observed on creation while creating the Storage Account,
stating "parallel access to resources" which hasn't been diagnosed -
assuming it must be a glitch in Azure.  The work-around on deploy errors
is to retry `pulumi up` - it's a retryable command and will continue from
where it left off.

## Configure DNS

After deployment, create two CNAME records pointing to the AGC Frontend
FQDN (shown in the `agcFrontendFqdn` output):

| Record | Type | Target |
|--------|------|--------|
| Your `domain` value | CNAME | AGC Frontend FQDN |
| Your `grafana-domain` value | CNAME | AGC Frontend FQDN |

For example:
```
azure1.dev.trustgraph.ai      CNAME  dxdkh0dpeuckfzek.fz11.alb.azure.com
grafana.azure1.dev.trustgraph.ai  CNAME  dxdkh0dpeuckfzek.fz11.alb.azure.com
```

Once DNS resolves, cert-manager will automatically provision TLS
certificates via Let's Encrypt. This typically takes a few minutes.

## Use the system

Once DNS and TLS are configured, access the services directly:

- TrustGraph UI: `https://<domain>`
- Grafana: `https://<grafana-domain>`

You can also use port-forwarding with the `kube.cfg` file if needed:

```
kubectl --kubeconfig kube.cfg -n trustgraph port-forward service/api-gateway 8088:8088
kubectl --kubeconfig kube.cfg -n trustgraph port-forward service/grafana 3000:3000
```

The IAM bootstrap token and Grafana admin password are auto-generated
by Pulumi.  After deployment, retrieve them with:
```
pulumi stack output iamToken --show-secrets
pulumi stack output grafanaPassword --show-secrets
```

Login to Grafana with username `admin` and the password from the command
above.

To use the TrustGraph API with authentication:
```
export TRUSTGRAPH_TOKEN=$(pulumi stack output iamToken --show-secrets)
```


## Destroy

```
pulumi destroy
```

Just say yes.

## Useful commands

You may need to increase CPU quota for your node VM family and location.
For example, to increase quota for Standard_DSv5 VMs in swedencentral:

```
az quota update --resource-name "standardDSv5Family" \
  --scope "/subscriptions/$(az account show --query id -o tsv)/providers/Microsoft.Compute/locations/swedencentral" \
  --limit-object value=20
```

Adjust the `--resource-name` and location to match your `node-size` and
`location` settings in `Pulumi.STACKNAME.yaml`.

## LLM quota

The Mistral-Large-3 model is deployed with a default quota of 10K
tokens-per-minute (TPM) on GlobalStandard.  If you see rate limiting
errors in the `text-completion` pod logs, increase the quota:

```yaml
trustgraph-azure:llm-quota: 20
```

Azure may also impose subscription-level quota limits on model deployments.
You can check and request increases in the Azure portal under
**AI Foundry > Quotas**.

## Changing the deployed models

To use a different model, three things need to change:

1. **Deploy the model in Azure** — edit `pulumi/ai-models.ts` to add or
   change the `cognitiveservices.Deployment` resource.  Set the
   `deploymentName`, `model.name`, `model.format`, and `model.version`
   to match the model from the Azure AI Foundry catalog.  If deploying
   multiple models, maintain a `dependsOn` chain so they deploy
   sequentially (Azure doesn't handle parallel model deployments well).

2. **Update `config.json`** — the `azure` component's `models` field
   controls which models appear in the TrustGraph UI dropdown.  Change
   the `default` and `enum` to match your deployed models:

   ```json
   {
       "name": "azure",
       "parameters": {
           "max-output-tokens": 4096,
           "models": {
               "type": "string",
               "description": "LLM model to use",
               "default": "your-model-name",
               "enum": [
                   {
                       "id": "your-model-name",
                       "description": "Your Model Display Name"
                   }
               ],
               "required": true
           }
       }
   }
   ```

   The `id` must match the `deploymentName` in `ai-models.ts`.

3. **Regenerate `resources.yaml`** — run the config generator to
   rebuild the Kubernetes resource definitions:

   ```
   ./update-config aks-k8s 2.5.16
   ```

Available models can be found in the Azure AI Foundry model catalog.

## Customizing memory settings

The `config.json` file includes an `override` component that allows
customization of memory settings for various services.  Parameters are
merged into the global parameters block.

For example, in `config.json`:

```json
{
    "name": "override",
    "parameters": {
        "cassandra-heap": "500M",
        "cassandra-memory-limit": "2000M",
        "cassandra-memory-reservation": "2000M",
        "api-gateway-memory-limit": "768M",
        "api-gateway-memory-reservation": "768M"
    }
}
```

Available parameters include:
- `cassandra-heap` - Cassandra JVM heap size
- `cassandra-memory-limit` - Cassandra container memory limit
- `cassandra-memory-reservation` - Cassandra container memory reservation
- `api-gateway-memory-limit` - API gateway container memory limit
- `api-gateway-memory-reservation` - API gateway container memory reservation

After changing memory settings, regenerate `resources.yaml`:

```
./update-config aks-k8s 2.5.16
```
