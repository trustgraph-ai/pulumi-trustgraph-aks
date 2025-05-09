
# Deploy TrustGraph in an Azure AKS using Pulumi

## Overview

This is an installation of TrustGraph on Azure using the Kubernetes (AKS)
platform.

The full stack includes:

- It's own resource group
- An Azure Identity service principal account to run various components.
- An AKS cluster deployed in its own resource group (that's just what Azure
  does).
- A key vault and storage account for the AI components (because, required)
- Using Machine Learning Services (AI Foundry in the console), deploys
  an AI hub, workspace and serverless endpoint hosting the Phi-4 model.
- Using Cognitive Services (AI Services in the console), deploys
  an AI account and deployment running OpenAI GPT-4o-mini.
- Deploys a complete TrustGraph stack of resources in AKS

Keys and other configuration for the AI components are configured into
TrustGraph using secrets.

Although the Pulumi configuration configures both a phi-4 and an OpenAI
model, the invocation depends on the resource.yaml.* files.

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

Decide whether you want TrustGraph to use OpenAI or Phi-4.

- For Phi-3 copy resources.yaml.mls to resources.yaml
- For OpenAI copy resources.yaml.cs to resources.yaml

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
- `trustgraph-azure:environment` - Name of the environment you are deploying
  use a name like: dev, prod etc.
- `trustgraph-azure:ai-endpoint-model` - the Machine Learning Services
  model to deploy.  Look in the model catalog for a Model ID
  e.g. `azureml://registries/azureml/models/Phi-4`.
- `trustgraph-azure:ai-openai-model` - the OpenAI model name e.g. gpt-4o-mini
- `trustgraph-azure:ai-openai-version` - the OpenAI model version which is
  in date format e.g. "2024-07-18".  Use quotes so that Pulumi doesn't
  interpret as a date
- `trustgraph-azure:ai-openai-format` - model format e.g. OpenAI
- `trustgraph-azure:ai-openai-rai-policy` - the content filtering
  policy.  RAI = Responsible AI?  e.g. `Microsoft.DefaultV2`.

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
kubectl --kubeconfig kubeconfig -n trustgraph get pods
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
kubectl --kubeconfig kube.cfg port-forward service/api-gateway 8088:8088
kubectl --kubeconfig kube.cfg port-forward service/workbench-ui 8888:8888
kubectl --kubeconfig kube.cfg port-forward service/grafana 3000:3000
```

This will allow you to access Grafana and the Workbench UI from your local
browser using `http://localhost:3000` and `http://localhost:8888`
respectively.


## Deploy

```
pulumi destroy
```

Just say yes.

