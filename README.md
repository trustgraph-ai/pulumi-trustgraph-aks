
# Deploy TrustGraph in an Azure AKS using Pulumi

## Overview

This is an installation of TrustGraph on Azure using the Kubernetes (AKS)
platform.

The full stack includes:

- Its own resource group
- An Azure Identity service principal account to run various components.
- An AKS cluster deployed in its own resource group (that's just what Azure
  does).
- A key vault and storage account for the AI components (because, required)
- Using AI Foundry, deploys an AI hub and project with 4 model deployments:
  - gpt-4o
  - gpt-4o-mini
  - Mistral-Large-3
  - mistral-small-2503
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
- Install Pulumi
- Setup Pulumi
- Configure your environment with Azure credentials using `az login`
- Modify the local configuration to do what you want
- Deploy
- Use the system

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

To connect to the Kubernetes cluster...

```
kubectl --kubeconfig kube.cfg -n trustgraph get pods
```

An error has been observed on creation while creating the Storage Account,
stating "parallel access to resources" which hasn't been diagnosed -
assuming it must be a glitch in Azure.  The work-around on deploy errors
is to retry `pulumi up` - it's a retryable command and will continue from
where it left off.

## Use the system

To get access to TrustGraph using the `kube.cfg` file, set up some
port-forwarding.  You'll need multiple terminal windows to run each of
these commands:

```
kubectl --kubeconfig kube.cfg -n trustgraph port-forward service/api-gateway 8088:8088
kubectl --kubeconfig kube.cfg -n trustgraph port-forward service/workbench-ui 8888:8888
kubectl --kubeconfig kube.cfg -n trustgraph port-forward service/grafana 3000:3000
```

This will allow you to access Grafana and the Workbench UI from your local
browser using `http://localhost:3000` and `http://localhost:8888`
respectively.


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

## Changing the deployed models

The AI models are defined in `pulumi/ai-models.ts`. Each model is deployed
as a `cognitiveservices.Deployment` resource. To change the models:

1. Edit `pulumi/ai-models.ts` to add, remove, or modify model deployments
2. Ensure the `dependsOn` chain is maintained so models deploy sequentially
   (Azure doesn't handle parallel model deployments well)
3. Update `resources.yaml` to reference the models you want TrustGraph to use

Available models can be found in the Azure AI Foundry model catalog.

## How the config was built

The AI models specified in `config.json` should match the models deployed
by Pulumi (gpt-4o, gpt-4o-mini, Mistral-Large-3, mistral-small-2503).

```
rm -rf env
python3 -m venv env
. env/bin/activate
pip install --no-cache --upgrade git+https://github.com/trustgraph-ai/trustgraph-templates@master
tg-build-deployment -i config.json -t 1.8 -v 1.8.20 --platform aks-k8s -R > resources.yaml
```

