# This resources file works with Azure the cognitiveservices
# services, or what the console calls "AI Services".  This is how you use
# OpenAI on Azure.
apiVersion: v1
items:
- apiVersion: v1
  kind: Namespace
  metadata:
    name: trustgraph
  spec: {}
- apiVersion: storage.k8s.io/v1
  kind: StorageClass
  metadata:
    name: tg
  parameters:
    skuName: Standard_LRS
  provisioner: disk.csi.azure.com
  reclaimPolicy: Delete
  volumeBindingMode: WaitForFirstConsumer
- apiVersion: storage.k8s.io/v1
  kind: StorageClass
  metadata:
    name: tg
  parameters:
    skuName: Standard_LRS
  provisioner: disk.csi.azure.com
  reclaimPolicy: Delete
  volumeBindingMode: WaitForFirstConsumer
- apiVersion: v1
  kind: Namespace
  metadata:
    name: trustgraph
  spec: {}
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: agent-manager
    name: agent-manager
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: agent-manager
    template:
      metadata:
        labels:
          app: agent-manager
      spec:
        containers:
        - command:
          - agent-manager-react
          - -p
          - pulsar://pulsar:6650
          - --prompt-request-queue
          - non-persistent://tg/request/prompt-rag
          - --prompt-response-queue
          - non-persistent://tg/response/prompt-rag
          - --tool-type
          - --tool-description
          - --tool-argument
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: agent-manager
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: agent-manager
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: agent-manager
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: api-gateway
    name: api-gateway
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: api-gateway
    template:
      metadata:
        labels:
          app: api-gateway
      spec:
        containers:
        - command:
          - api-gateway
          - -p
          - pulsar://pulsar:6650
          - --timeout
          - '600'
          - --port
          - '8088'
          env:
          - name: GATEWAY_SECRET
            valueFrom:
              secretKeyRef:
                key: gateway-secret
                name: gateway-secret
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: api-gateway
          ports:
          - containerPort: 8000
            hostPort: 8000
          - containerPort: 8088
            hostPort: 8088
          resources:
            limits:
              cpu: '0.5'
              memory: 256M
            requests:
              cpu: '0.1'
              memory: 256M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: api-gateway
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    - name: api
      port: 8088
      targetPort: 8088
    selector:
      app: api-gateway
- apiVersion: v1
  kind: PersistentVolumeClaim
  metadata:
    name: cassandra
    namespace: trustgraph
  spec:
    accessModes:
    - ReadWriteOnce
    resources:
      requests:
        storage: 20G
    storageClassName: tg
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: cassandra
    name: cassandra
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: cassandra
    template:
      metadata:
        labels:
          app: cassandra
      spec:
        containers:
        - env:
          - name: JVM_OPTS
            value: -Xms300M -Xmx300M -Dcassandra.skip_wait_for_gossip_to_settle=0
          image: docker.io/cassandra:4.1.6
          name: cassandra
          ports:
          - containerPort: 9042
            hostPort: 9042
          resources:
            limits:
              cpu: '1.0'
              memory: 1000M
            requests:
              cpu: '0.5'
              memory: 1000M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
          volumeMounts:
          - mountPath: /var/lib/cassandra
            name: cassandra
        volumes:
        - name: cassandra
          persistentVolumeClaim:
            claimName: cassandra
- apiVersion: v1
  kind: Service
  metadata:
    name: cassandra
    namespace: trustgraph
  spec:
    ports:
    - name: api
      port: 9042
      targetPort: 9042
    selector:
      app: cassandra
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: chunker
    name: chunker
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: chunker
    template:
      metadata:
        labels:
          app: chunker
      spec:
        containers:
        - command:
          - chunker-recursive
          - -p
          - pulsar://pulsar:6650
          - --chunk-size
          - '1000'
          - --chunk-overlap
          - '50'
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: chunker
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: chunker
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: chunker
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: document-embeddings
    name: document-embeddings
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: document-embeddings
    template:
      metadata:
        labels:
          app: document-embeddings
      spec:
        containers:
        - command:
          - document-embeddings
          - -p
          - pulsar://pulsar:6650
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: document-embeddings
          resources:
            limits:
              cpu: '1.0'
              memory: 512M
            requests:
              cpu: '0.5'
              memory: 512M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: document-embeddings
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: document-embeddings
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: document-rag
    name: document-rag
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: document-rag
    template:
      metadata:
        labels:
          app: document-rag
      spec:
        containers:
        - command:
          - document-rag
          - -p
          - pulsar://pulsar:6650
          - --doc-limit
          - '20'
          - --prompt-request-queue
          - non-persistent://tg/request/prompt-rag
          - --prompt-response-queue
          - non-persistent://tg/response/prompt-rag
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: document-rag
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: document-rag
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: document-rag
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: embeddings
    name: embeddings
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: embeddings
    template:
      metadata:
        labels:
          app: embeddings
      spec:
        containers:
        - command:
          - embeddings-fastembed
          - -p
          - pulsar://pulsar:6650
          - -m
          - sentence-transformers/all-MiniLM-L6-v2
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: embeddings
          resources:
            limits:
              cpu: '1.0'
              memory: 400M
            requests:
              cpu: '0.5'
              memory: 400M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: embeddings
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: embeddings
- apiVersion: v1
  kind: PersistentVolumeClaim
  metadata:
    name: grafana-storage
    namespace: trustgraph
  spec:
    accessModes:
    - ReadWriteOnce
    resources:
      requests:
        storage: 20G
    storageClassName: tg
- apiVersion: v1
  data:
    dashboard.yml: "\napiVersion: 1\n\nproviders:\n\n  - name: 'trustgraph.ai'\n \
      \   orgId: 1\n    folder: 'TrustGraph'\n    folderUid: 'b6c5be90-d432-4df8-aeab-737c7b151228'\n\
      \    type: file\n    disableDeletion: false\n    updateIntervalSeconds: 30\n\
      \    allowUiUpdates: true\n    options:\n      path: /var/lib/grafana/dashboards\n\
      \      foldersFromFilesStructure: false\n\n"
  kind: ConfigMap
  metadata:
    name: prov-dash
    namespace: trustgraph
- apiVersion: v1
  data:
    datasource.yml: "apiVersion: 1\n\nprune: true\n\ndatasources:\n  - name: Prometheus\n\
      \    type: prometheus\n    access: proxy\n    orgId: 1\n    # <string> Sets\
      \ a custom UID to reference this\n    # data source in other parts of the configuration.\n\
      \    # If not specified, Grafana generates one.\n    uid: 'f6b18033-5918-4e05-a1ca-4cb30343b129'\n\
      \n    url: http://prometheus:9090\n\n    basicAuth: false\n    withCredentials:\
      \ false\n    isDefault: true\n    editable: true\n\n"
  kind: ConfigMap
  metadata:
    name: prov-data
    namespace: trustgraph
- apiVersion: v1
  data:
    dashboard.json: "{\n  \"annotations\": {\n    \"list\": [\n      {\n        \"\
      builtIn\": 1,\n        \"datasource\": {\n          \"type\": \"grafana\",\n\
      \          \"uid\": \"-- Grafana --\"\n        },\n        \"enable\": true,\n\
      \        \"hide\": true,\n        \"iconColor\": \"rgba(0, 211, 255, 1)\",\n\
      \        \"name\": \"Annotations & Alerts\",\n        \"type\": \"dashboard\"\
      \n      }\n    ]\n  },\n  \"editable\": true,\n  \"fiscalYearStartMonth\": 0,\n\
      \  \"graphTooltip\": 0,\n  \"id\": 2,\n  \"links\": [],\n  \"liveNow\": false,\n\
      \  \"panels\": [\n    {\n      \"datasource\": {\n        \"type\": \"prometheus\"\
      ,\n        \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\n      },\n   \
      \   \"fieldConfig\": {\n        \"defaults\": {\n          \"custom\": {\n \
      \           \"hideFrom\": {\n              \"legend\": false,\n            \
      \  \"tooltip\": false,\n              \"viz\": false\n            },\n     \
      \       \"scaleDistribution\": {\n              \"type\": \"linear\"\n     \
      \       }\n          }\n        },\n        \"overrides\": []\n      },\n  \
      \    \"gridPos\": {\n        \"h\": 8,\n        \"w\": 12,\n        \"x\": 0,\n\
      \        \"y\": 0\n      },\n      \"id\": 7,\n      \"options\": {\n      \
      \  \"calculate\": false,\n        \"cellGap\": 1,\n        \"color\": {\n  \
      \        \"exponent\": 0.5,\n          \"fill\": \"dark-orange\",\n        \
      \  \"mode\": \"scheme\",\n          \"reverse\": false,\n          \"scale\"\
      : \"exponential\",\n          \"scheme\": \"Oranges\",\n          \"steps\"\
      : 64\n        },\n        \"exemplars\": {\n          \"color\": \"rgba(255,0,255,0.7)\"\
      \n        },\n        \"filterValues\": {\n          \"le\": 1e-9\n        },\n\
      \        \"legend\": {\n          \"show\": true\n        },\n        \"rowsFrame\"\
      : {\n          \"layout\": \"auto\"\n        },\n        \"tooltip\": {\n  \
      \        \"mode\": \"single\",\n          \"showColorScale\": false,\n     \
      \     \"yHistogram\": false\n        },\n        \"yAxis\": {\n          \"\
      axisPlacement\": \"left\",\n          \"reverse\": false\n        }\n      },\n\
      \      \"pluginVersion\": \"11.1.4\",\n      \"targets\": [\n        {\n   \
      \       \"datasource\": {\n            \"type\": \"prometheus\",\n         \
      \   \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\n          },\n      \
      \    \"disableTextWrap\": false,\n          \"editorMode\": \"builder\",\n \
      \         \"exemplar\": false,\n          \"expr\": \"sum by(le) (rate(text_completion_duration_bucket[$__rate_interval]))\"\
      ,\n          \"format\": \"heatmap\",\n          \"fullMetaSearch\": false,\n\
      \          \"includeNullMetadata\": true,\n          \"instant\": false,\n \
      \         \"legendFormat\": \"99%\",\n          \"range\": true,\n         \
      \ \"refId\": \"A\",\n          \"useBackend\": false\n        }\n      ],\n\
      \      \"title\": \"LLM latency\",\n      \"type\": \"heatmap\"\n    },\n  \
      \  {\n      \"datasource\": {\n        \"type\": \"prometheus\",\n        \"\
      uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\n      },\n      \"fieldConfig\"\
      : {\n        \"defaults\": {\n          \"custom\": {\n            \"hideFrom\"\
      : {\n              \"legend\": false,\n              \"tooltip\": false,\n \
      \             \"viz\": false\n            },\n            \"scaleDistribution\"\
      : {\n              \"type\": \"linear\"\n            }\n          }\n      \
      \  },\n        \"overrides\": []\n      },\n      \"gridPos\": {\n        \"\
      h\": 8,\n        \"w\": 12,\n        \"x\": 12,\n        \"y\": 0\n      },\n\
      \      \"id\": 2,\n      \"options\": {\n        \"calculate\": false,\n   \
      \     \"cellGap\": 5,\n        \"cellValues\": {\n          \"unit\": \"\"\n\
      \        },\n        \"color\": {\n          \"exponent\": 0.5,\n          \"\
      fill\": \"dark-orange\",\n          \"mode\": \"scheme\",\n          \"reverse\"\
      : false,\n          \"scale\": \"exponential\",\n          \"scheme\": \"Oranges\"\
      ,\n          \"steps\": 64\n        },\n        \"exemplars\": {\n         \
      \ \"color\": \"rgba(255,0,255,0.7)\"\n        },\n        \"filterValues\":\
      \ {\n          \"le\": 1e-9\n        },\n        \"legend\": {\n          \"\
      show\": true\n        },\n        \"rowsFrame\": {\n          \"layout\": \"\
      auto\"\n        },\n        \"tooltip\": {\n          \"mode\": \"single\",\n\
      \          \"showColorScale\": false,\n          \"yHistogram\": false\n   \
      \     },\n        \"yAxis\": {\n          \"axisLabel\": \"processing status\"\
      ,\n          \"axisPlacement\": \"left\",\n          \"reverse\": false\n  \
      \      }\n      },\n      \"pluginVersion\": \"11.1.4\",\n      \"targets\"\
      : [\n        {\n          \"datasource\": {\n            \"type\": \"prometheus\"\
      ,\n            \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\n         \
      \ },\n          \"disableTextWrap\": false,\n          \"editorMode\": \"builder\"\
      ,\n          \"exemplar\": false,\n          \"expr\": \"sum by(status) (rate(processing_count_total{status!=\\\
      \"success\\\"}[$__rate_interval]))\",\n          \"format\": \"heatmap\",\n\
      \          \"fullMetaSearch\": false,\n          \"includeNullMetadata\": true,\n\
      \          \"instant\": false,\n          \"interval\": \"\",\n          \"\
      legendFormat\": \"{{status}}\",\n          \"range\": true,\n          \"refId\"\
      : \"A\",\n          \"useBackend\": false\n        }\n      ],\n      \"title\"\
      : \"Error rate\",\n      \"type\": \"heatmap\"\n    },\n    {\n      \"datasource\"\
      : {\n        \"type\": \"prometheus\",\n        \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n      },\n      \"fieldConfig\": {\n        \"defaults\": {\n          \"\
      color\": {\n            \"mode\": \"palette-classic\"\n          },\n      \
      \    \"custom\": {\n            \"axisBorderShow\": false,\n            \"axisCenteredZero\"\
      : false,\n            \"axisColorMode\": \"text\",\n            \"axisLabel\"\
      : \"\",\n            \"axisPlacement\": \"auto\",\n            \"barAlignment\"\
      : 0,\n            \"drawStyle\": \"line\",\n            \"fillOpacity\": 0,\n\
      \            \"gradientMode\": \"none\",\n            \"hideFrom\": {\n    \
      \          \"legend\": false,\n              \"tooltip\": false,\n         \
      \     \"viz\": false\n            },\n            \"insertNulls\": false,\n\
      \            \"lineInterpolation\": \"linear\",\n            \"lineWidth\":\
      \ 1,\n            \"pointSize\": 5,\n            \"scaleDistribution\": {\n\
      \              \"type\": \"linear\"\n            },\n            \"showPoints\"\
      : \"auto\",\n            \"spanNulls\": false,\n            \"stacking\": {\n\
      \              \"group\": \"A\",\n              \"mode\": \"none\"\n       \
      \     },\n            \"thresholdsStyle\": {\n              \"mode\": \"off\"\
      \n            }\n          },\n          \"mappings\": [],\n          \"thresholds\"\
      : {\n            \"mode\": \"absolute\",\n            \"steps\": [\n       \
      \       {\n                \"color\": \"green\",\n                \"value\"\
      : null\n              },\n              {\n                \"color\": \"red\"\
      ,\n                \"value\": 80\n              }\n            ]\n         \
      \ }\n        },\n        \"overrides\": []\n      },\n      \"gridPos\": {\n\
      \        \"h\": 9,\n        \"w\": 12,\n        \"x\": 0,\n        \"y\": 8\n\
      \      },\n      \"id\": 1,\n      \"options\": {\n        \"legend\": {\n \
      \         \"calcs\": [],\n          \"displayMode\": \"list\",\n          \"\
      placement\": \"bottom\",\n          \"showLegend\": true\n        },\n     \
      \   \"tooltip\": {\n          \"mode\": \"single\",\n          \"sort\": \"\
      none\"\n        }\n      },\n      \"targets\": [\n        {\n          \"datasource\"\
      : {\n            \"type\": \"prometheus\",\n            \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n          },\n          \"editorMode\": \"builder\",\n          \"expr\":\
      \ \"rate(request_latency_count[1m])\",\n          \"instant\": false,\n    \
      \      \"legendFormat\": \"{{job}}\",\n          \"range\": true,\n        \
      \  \"refId\": \"A\"\n        }\n      ],\n      \"title\": \"Request rate\"\
      ,\n      \"type\": \"timeseries\"\n    },\n    {\n      \"datasource\": {\n\
      \        \"type\": \"prometheus\",\n        \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n      },\n      \"fieldConfig\": {\n        \"defaults\": {\n          \"\
      color\": {\n            \"mode\": \"palette-classic\"\n          },\n      \
      \    \"custom\": {\n            \"axisBorderShow\": false,\n            \"axisCenteredZero\"\
      : false,\n            \"axisColorMode\": \"text\",\n            \"axisLabel\"\
      : \"\",\n            \"axisPlacement\": \"auto\",\n            \"barAlignment\"\
      : 0,\n            \"drawStyle\": \"line\",\n            \"fillOpacity\": 0,\n\
      \            \"gradientMode\": \"none\",\n            \"hideFrom\": {\n    \
      \          \"legend\": false,\n              \"tooltip\": false,\n         \
      \     \"viz\": false\n            },\n            \"insertNulls\": false,\n\
      \            \"lineInterpolation\": \"linear\",\n            \"lineWidth\":\
      \ 1,\n            \"pointSize\": 5,\n            \"scaleDistribution\": {\n\
      \              \"type\": \"linear\"\n            },\n            \"showPoints\"\
      : \"auto\",\n            \"spanNulls\": false,\n            \"stacking\": {\n\
      \              \"group\": \"A\",\n              \"mode\": \"none\"\n       \
      \     },\n            \"thresholdsStyle\": {\n              \"mode\": \"off\"\
      \n            }\n          },\n          \"mappings\": [],\n          \"thresholds\"\
      : {\n            \"mode\": \"absolute\",\n            \"steps\": [\n       \
      \       {\n                \"color\": \"green\",\n                \"value\"\
      : null\n              },\n              {\n                \"color\": \"red\"\
      ,\n                \"value\": 80\n              }\n            ]\n         \
      \ }\n        },\n        \"overrides\": []\n      },\n      \"gridPos\": {\n\
      \        \"h\": 9,\n        \"w\": 12,\n        \"x\": 12,\n        \"y\": 8\n\
      \      },\n      \"id\": 5,\n      \"options\": {\n        \"legend\": {\n \
      \         \"calcs\": [],\n          \"displayMode\": \"list\",\n          \"\
      placement\": \"bottom\",\n          \"showLegend\": true\n        },\n     \
      \   \"tooltip\": {\n          \"mode\": \"single\",\n          \"sort\": \"\
      none\"\n        }\n      },\n      \"pluginVersion\": \"10.0.0\",\n      \"\
      targets\": [\n        {\n          \"datasource\": {\n            \"type\":\
      \ \"prometheus\",\n            \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n          },\n          \"editorMode\": \"builder\",\n          \"expr\":\
      \ \"pulsar_msg_backlog\",\n          \"instant\": false,\n          \"legendFormat\"\
      : \"{{topic}}\",\n          \"range\": true,\n          \"refId\": \"A\"\n \
      \       }\n      ],\n      \"title\": \"Pub/sub backlog\",\n      \"type\":\
      \ \"timeseries\"\n    },\n    {\n      \"datasource\": {\n        \"type\":\
      \ \"prometheus\",\n        \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n      },\n      \"fieldConfig\": {\n        \"defaults\": {\n          \"\
      color\": {\n            \"fixedColor\": \"semi-dark-green\",\n            \"\
      mode\": \"palette-classic-by-name\"\n          },\n          \"custom\": {\n\
      \            \"axisBorderShow\": false,\n            \"axisCenteredZero\": false,\n\
      \            \"axisColorMode\": \"text\",\n            \"axisLabel\": \"\",\n\
      \            \"axisPlacement\": \"auto\",\n            \"fillOpacity\": 80,\n\
      \            \"gradientMode\": \"none\",\n            \"hideFrom\": {\n    \
      \          \"legend\": false,\n              \"tooltip\": false,\n         \
      \     \"viz\": false\n            },\n            \"lineWidth\": 1,\n      \
      \      \"scaleDistribution\": {\n              \"type\": \"linear\"\n      \
      \      },\n            \"thresholdsStyle\": {\n              \"mode\": \"off\"\
      \n            }\n          },\n          \"mappings\": [],\n          \"thresholds\"\
      : {\n            \"mode\": \"absolute\",\n            \"steps\": [\n       \
      \       {\n                \"color\": \"green\",\n                \"value\"\
      : null\n              },\n              {\n                \"color\": \"red\"\
      ,\n                \"value\": 80\n              }\n            ]\n         \
      \ }\n        },\n        \"overrides\": []\n      },\n      \"gridPos\": {\n\
      \        \"h\": 7,\n        \"w\": 12,\n        \"x\": 0,\n        \"y\": 17\n\
      \      },\n      \"id\": 10,\n      \"options\": {\n        \"barRadius\": 0,\n\
      \        \"barWidth\": 0.97,\n        \"fullHighlight\": false,\n        \"\
      groupWidth\": 0.7,\n        \"legend\": {\n          \"calcs\": [],\n      \
      \    \"displayMode\": \"list\",\n          \"placement\": \"bottom\",\n    \
      \      \"showLegend\": true\n        },\n        \"orientation\": \"auto\",\n\
      \        \"showValue\": \"auto\",\n        \"stacking\": \"none\",\n       \
      \ \"tooltip\": {\n          \"mode\": \"single\",\n          \"sort\": \"none\"\
      \n        },\n        \"xTickLabelRotation\": 0,\n        \"xTickLabelSpacing\"\
      : 0\n      },\n      \"pluginVersion\": \"11.1.4\",\n      \"targets\": [\n\
      \        {\n          \"datasource\": {\n            \"type\": \"prometheus\"\
      ,\n            \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\n         \
      \ },\n          \"disableTextWrap\": false,\n          \"editorMode\": \"builder\"\
      ,\n          \"exemplar\": false,\n          \"expr\": \"max by(le) (chunk_size_bucket)\"\
      ,\n          \"format\": \"heatmap\",\n          \"fullMetaSearch\": false,\n\
      \          \"includeNullMetadata\": false,\n          \"instant\": true,\n \
      \         \"legendFormat\": \"{{le}}\",\n          \"range\": false,\n     \
      \     \"refId\": \"A\",\n          \"useBackend\": false\n        }\n      ],\n\
      \      \"title\": \"Chunk size\",\n      \"type\": \"barchart\"\n    },\n  \
      \  {\n      \"datasource\": {\n        \"type\": \"prometheus\",\n        \"\
      uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\n      },\n      \"fieldConfig\"\
      : {\n        \"defaults\": {\n          \"color\": {\n            \"mode\":\
      \ \"palette-classic\"\n          },\n          \"custom\": {\n            \"\
      axisBorderShow\": false,\n            \"axisCenteredZero\": false,\n       \
      \     \"axisColorMode\": \"text\",\n            \"axisLabel\": \"\",\n     \
      \       \"axisPlacement\": \"auto\",\n            \"barAlignment\": 0,\n   \
      \         \"drawStyle\": \"line\",\n            \"fillOpacity\": 0,\n      \
      \      \"gradientMode\": \"none\",\n            \"hideFrom\": {\n          \
      \    \"legend\": false,\n              \"tooltip\": false,\n              \"\
      viz\": false\n            },\n            \"insertNulls\": false,\n        \
      \    \"lineInterpolation\": \"linear\",\n            \"lineWidth\": 1,\n   \
      \         \"pointSize\": 5,\n            \"scaleDistribution\": {\n        \
      \      \"type\": \"linear\"\n            },\n            \"showPoints\": \"\
      auto\",\n            \"spanNulls\": false,\n            \"stacking\": {\n  \
      \            \"group\": \"A\",\n              \"mode\": \"none\"\n         \
      \   },\n            \"thresholdsStyle\": {\n              \"mode\": \"off\"\n\
      \            }\n          },\n          \"mappings\": [],\n          \"thresholds\"\
      : {\n            \"mode\": \"absolute\",\n            \"steps\": [\n       \
      \       {\n                \"color\": \"green\",\n                \"value\"\
      : null\n              },\n              {\n                \"color\": \"red\"\
      ,\n                \"value\": 80\n              }\n            ]\n         \
      \ }\n        },\n        \"overrides\": []\n      },\n      \"gridPos\": {\n\
      \        \"h\": 7,\n        \"w\": 12,\n        \"x\": 12,\n        \"y\": 17\n\
      \      },\n      \"id\": 11,\n      \"options\": {\n        \"legend\": {\n\
      \          \"calcs\": [],\n          \"displayMode\": \"list\",\n          \"\
      placement\": \"bottom\",\n          \"showLegend\": true\n        },\n     \
      \   \"tooltip\": {\n          \"mode\": \"single\",\n          \"sort\": \"\
      none\"\n        }\n      },\n      \"pluginVersion\": \"11.1.4\",\n      \"\
      targets\": [\n        {\n          \"datasource\": {\n            \"type\":\
      \ \"prometheus\",\n            \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n          },\n          \"disableTextWrap\": false,\n          \"editorMode\"\
      : \"builder\",\n          \"exemplar\": false,\n          \"expr\": \"sum by(job)\
      \ (increase(rate_limit_count_total[$__rate_interval]))\",\n          \"format\"\
      : \"time_series\",\n          \"fullMetaSearch\": false,\n          \"includeNullMetadata\"\
      : true,\n          \"instant\": false,\n          \"legendFormat\": \"{{instance}}\"\
      ,\n          \"range\": true,\n          \"refId\": \"A\",\n          \"useBackend\"\
      : false\n        }\n      ],\n      \"title\": \"Rate limit events\",\n    \
      \  \"type\": \"timeseries\"\n    },\n    {\n      \"datasource\": {\n      \
      \  \"type\": \"prometheus\",\n        \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n      },\n      \"fieldConfig\": {\n        \"defaults\": {\n          \"\
      color\": {\n            \"fixedColor\": \"light-blue\",\n            \"mode\"\
      : \"palette-classic\"\n          },\n          \"custom\": {\n            \"\
      axisBorderShow\": false,\n            \"axisCenteredZero\": false,\n       \
      \     \"axisColorMode\": \"text\",\n            \"axisLabel\": \"\",\n     \
      \       \"axisPlacement\": \"auto\",\n            \"barAlignment\": 0,\n   \
      \         \"drawStyle\": \"line\",\n            \"fillOpacity\": 0,\n      \
      \      \"gradientMode\": \"none\",\n            \"hideFrom\": {\n          \
      \    \"legend\": false,\n              \"tooltip\": false,\n              \"\
      viz\": false\n            },\n            \"insertNulls\": false,\n        \
      \    \"lineInterpolation\": \"linear\",\n            \"lineWidth\": 1,\n   \
      \         \"pointSize\": 5,\n            \"scaleDistribution\": {\n        \
      \      \"type\": \"linear\"\n            },\n            \"showPoints\": \"\
      auto\",\n            \"spanNulls\": false,\n            \"stacking\": {\n  \
      \            \"group\": \"A\",\n              \"mode\": \"none\"\n         \
      \   },\n            \"thresholdsStyle\": {\n              \"mode\": \"off\"\n\
      \            }\n          },\n          \"mappings\": [],\n          \"thresholds\"\
      : {\n            \"mode\": \"absolute\",\n            \"steps\": [\n       \
      \       {\n                \"color\": \"green\",\n                \"value\"\
      : null\n              },\n              {\n                \"color\": \"red\"\
      ,\n                \"value\": 80\n              }\n            ]\n         \
      \ }\n        },\n        \"overrides\": []\n      },\n      \"gridPos\": {\n\
      \        \"h\": 8,\n        \"w\": 12,\n        \"x\": 0,\n        \"y\": 24\n\
      \      },\n      \"id\": 12,\n      \"options\": {\n        \"legend\": {\n\
      \          \"calcs\": [],\n          \"displayMode\": \"list\",\n          \"\
      placement\": \"bottom\",\n          \"showLegend\": true\n        },\n     \
      \   \"tooltip\": {\n          \"mode\": \"single\",\n          \"sort\": \"\
      none\"\n        }\n      },\n      \"pluginVersion\": \"11.1.4\",\n      \"\
      targets\": [\n        {\n          \"datasource\": {\n            \"type\":\
      \ \"prometheus\",\n            \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n          },\n          \"disableTextWrap\": false,\n          \"editorMode\"\
      : \"builder\",\n          \"expr\": \"rate(process_cpu_seconds_total[$__rate_interval])\"\
      ,\n          \"fullMetaSearch\": false,\n          \"includeNullMetadata\":\
      \ true,\n          \"instant\": false,\n          \"legendFormat\": \"{{instance}}\"\
      ,\n          \"range\": true,\n          \"refId\": \"A\",\n          \"useBackend\"\
      : false\n        }\n      ],\n      \"title\": \"CPU\",\n      \"type\": \"\
      timeseries\"\n    },\n    {\n      \"datasource\": {\n        \"type\": \"prometheus\"\
      ,\n        \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\n      },\n   \
      \   \"fieldConfig\": {\n        \"defaults\": {\n          \"color\": {\n  \
      \          \"mode\": \"palette-classic\"\n          },\n          \"custom\"\
      : {\n            \"axisBorderShow\": false,\n            \"axisCenteredZero\"\
      : false,\n            \"axisColorMode\": \"text\",\n            \"axisLabel\"\
      : \"GB\",\n            \"axisPlacement\": \"auto\",\n            \"barAlignment\"\
      : 0,\n            \"drawStyle\": \"line\",\n            \"fillOpacity\": 0,\n\
      \            \"gradientMode\": \"none\",\n            \"hideFrom\": {\n    \
      \          \"legend\": false,\n              \"tooltip\": false,\n         \
      \     \"viz\": false\n            },\n            \"insertNulls\": false,\n\
      \            \"lineInterpolation\": \"linear\",\n            \"lineWidth\":\
      \ 1,\n            \"pointSize\": 5,\n            \"scaleDistribution\": {\n\
      \              \"type\": \"linear\"\n            },\n            \"showPoints\"\
      : \"auto\",\n            \"spanNulls\": false,\n            \"stacking\": {\n\
      \              \"group\": \"A\",\n              \"mode\": \"none\"\n       \
      \     },\n            \"thresholdsStyle\": {\n              \"mode\": \"off\"\
      \n            }\n          },\n          \"mappings\": [],\n          \"thresholds\"\
      : {\n            \"mode\": \"absolute\",\n            \"steps\": [\n       \
      \       {\n                \"color\": \"green\",\n                \"value\"\
      : null\n              },\n              {\n                \"color\": \"red\"\
      ,\n                \"value\": 80\n              }\n            ]\n         \
      \ }\n        },\n        \"overrides\": []\n      },\n      \"gridPos\": {\n\
      \        \"h\": 8,\n        \"w\": 12,\n        \"x\": 12,\n        \"y\": 24\n\
      \      },\n      \"id\": 13,\n      \"options\": {\n        \"legend\": {\n\
      \          \"calcs\": [],\n          \"displayMode\": \"list\",\n          \"\
      placement\": \"bottom\",\n          \"showLegend\": true\n        },\n     \
      \   \"tooltip\": {\n          \"mode\": \"single\",\n          \"sort\": \"\
      none\"\n        }\n      },\n      \"targets\": [\n        {\n          \"datasource\"\
      : {\n            \"type\": \"prometheus\",\n            \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n          },\n          \"disableTextWrap\": false,\n          \"editorMode\"\
      : \"builder\",\n          \"expr\": \"process_resident_memory_bytes / 1073741824\"\
      ,\n          \"fullMetaSearch\": false,\n          \"includeNullMetadata\":\
      \ true,\n          \"instant\": false,\n          \"legendFormat\": \"{{instance}}\"\
      ,\n          \"range\": true,\n          \"refId\": \"A\",\n          \"useBackend\"\
      : false\n        }\n      ],\n      \"title\": \"Memory\",\n      \"type\":\
      \ \"timeseries\"\n    },\n    {\n      \"datasource\": {\n        \"type\":\
      \ \"prometheus\",\n        \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n      },\n      \"fieldConfig\": {\n        \"defaults\": {\n          \"\
      color\": {\n            \"mode\": \"thresholds\"\n          },\n          \"\
      custom\": {\n            \"align\": \"auto\",\n            \"cellOptions\":\
      \ {\n              \"type\": \"auto\"\n            },\n            \"filterable\"\
      : false,\n            \"inspect\": false\n          },\n          \"mappings\"\
      : [],\n          \"thresholds\": {\n            \"mode\": \"absolute\",\n  \
      \          \"steps\": [\n              {\n                \"color\": \"green\"\
      ,\n                \"value\": null\n              },\n              {\n    \
      \            \"color\": \"red\",\n                \"value\": 80\n          \
      \    }\n            ]\n          }\n        },\n        \"overrides\": []\n\
      \      },\n      \"gridPos\": {\n        \"h\": 7,\n        \"w\": 8,\n    \
      \    \"x\": 0,\n        \"y\": 32\n      },\n      \"id\": 14,\n      \"options\"\
      : {\n        \"cellHeight\": \"sm\",\n        \"footer\": {\n          \"countRows\"\
      : false,\n          \"fields\": \"\",\n          \"reducer\": [\n          \
      \  \"sum\"\n          ],\n          \"show\": false\n        },\n        \"\
      showHeader\": true\n      },\n      \"pluginVersion\": \"11.1.4\",\n      \"\
      targets\": [\n        {\n          \"datasource\": {\n            \"type\":\
      \ \"prometheus\",\n            \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n          },\n          \"disableTextWrap\": false,\n          \"editorMode\"\
      : \"builder\",\n          \"exemplar\": false,\n          \"expr\": \"last_over_time(params_info[$__interval])\"\
      ,\n          \"format\": \"table\",\n          \"fullMetaSearch\": false,\n\
      \          \"includeNullMetadata\": true,\n          \"instant\": true,\n  \
      \        \"legendFormat\": \"__auto\",\n          \"range\": false,\n      \
      \    \"refId\": \"A\",\n          \"useBackend\": false\n        }\n      ],\n\
      \      \"title\": \"Model parameters\",\n      \"transformations\": [\n    \
      \    {\n          \"id\": \"filterFieldsByName\",\n          \"options\": {\n\
      \            \"include\": {\n              \"names\": [\n                \"\
      model\",\n                \"job\"\n              ]\n            }\n        \
      \  }\n        },\n        {\n          \"id\": \"filterByValue\",\n        \
      \  \"options\": {\n            \"filters\": [\n              {\n           \
      \     \"config\": {\n                  \"id\": \"equal\",\n                \
      \  \"options\": {\n                    \"value\": \"\"\n                  }\n\
      \                },\n                \"fieldName\": \"model\"\n            \
      \  }\n            ],\n            \"match\": \"all\",\n            \"type\"\
      : \"exclude\"\n          }\n        }\n      ],\n      \"type\": \"table\"\n\
      \    },\n    {\n      \"datasource\": {\n        \"type\": \"prometheus\",\n\
      \        \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\n      },\n     \
      \ \"fieldConfig\": {\n        \"defaults\": {\n          \"color\": {\n    \
      \        \"mode\": \"palette-classic\"\n          },\n          \"custom\":\
      \ {\n            \"axisBorderShow\": false,\n            \"axisCenteredZero\"\
      : false,\n            \"axisColorMode\": \"text\",\n            \"axisLabel\"\
      : \"\",\n            \"axisPlacement\": \"auto\",\n            \"barAlignment\"\
      : 0,\n            \"drawStyle\": \"line\",\n            \"fillOpacity\": 0,\n\
      \            \"gradientMode\": \"none\",\n            \"hideFrom\": {\n    \
      \          \"legend\": false,\n              \"tooltip\": false,\n         \
      \     \"viz\": false\n            },\n            \"insertNulls\": false,\n\
      \            \"lineInterpolation\": \"linear\",\n            \"lineWidth\":\
      \ 1,\n            \"pointSize\": 5,\n            \"scaleDistribution\": {\n\
      \              \"type\": \"linear\"\n            },\n            \"showPoints\"\
      : \"auto\",\n            \"spanNulls\": false,\n            \"stacking\": {\n\
      \              \"group\": \"A\",\n              \"mode\": \"none\"\n       \
      \     },\n            \"thresholdsStyle\": {\n              \"mode\": \"off\"\
      \n            }\n          },\n          \"mappings\": [],\n          \"thresholds\"\
      : {\n            \"mode\": \"absolute\",\n            \"steps\": [\n       \
      \       {\n                \"color\": \"green\",\n                \"value\"\
      : null\n              },\n              {\n                \"color\": \"red\"\
      ,\n                \"value\": 80\n              }\n            ]\n         \
      \ }\n        },\n        \"overrides\": []\n      },\n      \"gridPos\": {\n\
      \        \"h\": 7,\n        \"w\": 8,\n        \"x\": 8,\n        \"y\": 32\n\
      \      },\n      \"id\": 15,\n      \"options\": {\n        \"legend\": {\n\
      \          \"calcs\": [],\n          \"displayMode\": \"list\",\n          \"\
      placement\": \"bottom\",\n          \"showLegend\": true\n        },\n     \
      \   \"tooltip\": {\n          \"mode\": \"single\",\n          \"sort\": \"\
      none\"\n        }\n      },\n      \"targets\": [\n        {\n          \"datasource\"\
      : {\n            \"type\": \"prometheus\",\n            \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n          },\n          \"disableTextWrap\": false,\n          \"editorMode\"\
      : \"builder\",\n          \"expr\": \"sum by(job) (rate(input_tokens_total[$__rate_interval]))\"\
      ,\n          \"fullMetaSearch\": false,\n          \"includeNullMetadata\":\
      \ true,\n          \"instant\": false,\n          \"legendFormat\": \"input\
      \ {{job}}\",\n          \"range\": true,\n          \"refId\": \"A\",\n    \
      \      \"useBackend\": false\n        },\n        {\n          \"datasource\"\
      : {\n            \"type\": \"prometheus\",\n            \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n          },\n          \"disableTextWrap\": false,\n          \"editorMode\"\
      : \"builder\",\n          \"expr\": \"sum by(job) (rate(output_tokens_total[$__rate_interval]))\"\
      ,\n          \"fullMetaSearch\": false,\n          \"hide\": false,\n      \
      \    \"includeNullMetadata\": true,\n          \"instant\": false,\n       \
      \   \"legendFormat\": \"output {{job}}\",\n          \"range\": true,\n    \
      \      \"refId\": \"B\",\n          \"useBackend\": false\n        }\n     \
      \ ],\n      \"title\": \"Tokens\",\n      \"type\": \"timeseries\"\n    },\n\
      \    {\n      \"datasource\": {\n        \"type\": \"prometheus\",\n       \
      \ \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\n      },\n      \"fieldConfig\"\
      : {\n        \"defaults\": {\n          \"color\": {\n            \"mode\":\
      \ \"palette-classic\"\n          },\n          \"custom\": {\n            \"\
      axisBorderShow\": false,\n            \"axisCenteredZero\": false,\n       \
      \     \"axisColorMode\": \"text\",\n            \"axisLabel\": \"$\",\n    \
      \        \"axisPlacement\": \"auto\",\n            \"barAlignment\": 0,\n  \
      \          \"drawStyle\": \"line\",\n            \"fillOpacity\": 0,\n     \
      \       \"gradientMode\": \"none\",\n            \"hideFrom\": {\n         \
      \     \"legend\": false,\n              \"tooltip\": false,\n              \"\
      viz\": false\n            },\n            \"insertNulls\": false,\n        \
      \    \"lineInterpolation\": \"linear\",\n            \"lineWidth\": 1,\n   \
      \         \"pointSize\": 5,\n            \"scaleDistribution\": {\n        \
      \      \"type\": \"linear\"\n            },\n            \"showPoints\": \"\
      auto\",\n            \"spanNulls\": false,\n            \"stacking\": {\n  \
      \            \"group\": \"A\",\n              \"mode\": \"none\"\n         \
      \   },\n            \"thresholdsStyle\": {\n              \"mode\": \"off\"\n\
      \            }\n          },\n          \"mappings\": [],\n          \"thresholds\"\
      : {\n            \"mode\": \"absolute\",\n            \"steps\": [\n       \
      \       {\n                \"color\": \"green\",\n                \"value\"\
      : null\n              },\n              {\n                \"color\": \"red\"\
      ,\n                \"value\": 80\n              }\n            ]\n         \
      \ }\n        },\n        \"overrides\": []\n      },\n      \"gridPos\": {\n\
      \        \"h\": 7,\n        \"w\": 8,\n        \"x\": 16,\n        \"y\": 32\n\
      \      },\n      \"id\": 16,\n      \"options\": {\n        \"legend\": {\n\
      \          \"calcs\": [],\n          \"displayMode\": \"list\",\n          \"\
      placement\": \"bottom\",\n          \"showLegend\": true\n        },\n     \
      \   \"tooltip\": {\n          \"mode\": \"single\",\n          \"sort\": \"\
      none\"\n        }\n      },\n      \"targets\": [\n        {\n          \"datasource\"\
      : {\n            \"type\": \"prometheus\",\n            \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n          },\n          \"disableTextWrap\": false,\n          \"editorMode\"\
      : \"builder\",\n          \"expr\": \"sum by(job) (rate(input_cost_total[$__rate_interval]))\"\
      ,\n          \"fullMetaSearch\": false,\n          \"includeNullMetadata\":\
      \ true,\n          \"instant\": false,\n          \"legendFormat\": \"input\
      \ {{job}}\",\n          \"range\": true,\n          \"refId\": \"A\",\n    \
      \      \"useBackend\": false\n        },\n        {\n          \"datasource\"\
      : {\n            \"type\": \"prometheus\",\n            \"uid\": \"f6b18033-5918-4e05-a1ca-4cb30343b129\"\
      \n          },\n          \"disableTextWrap\": false,\n          \"editorMode\"\
      : \"builder\",\n          \"expr\": \"sum by(job) (rate(output_cost_total[$__rate_interval]))\"\
      ,\n          \"fullMetaSearch\": false,\n          \"hide\": false,\n      \
      \    \"includeNullMetadata\": true,\n          \"instant\": false,\n       \
      \   \"legendFormat\": \"output {{job}}\",\n          \"range\": true,\n    \
      \      \"refId\": \"B\",\n          \"useBackend\": false\n        }\n     \
      \ ],\n      \"title\": \"Token cost\",\n      \"type\": \"timeseries\"\n   \
      \ }\n  ],\n  \"refresh\": \"5s\",\n  \"schemaVersion\": 39,\n  \"tags\": [],\n\
      \  \"templating\": {\n    \"list\": []\n  },\n  \"time\": {\n    \"from\": \"\
      now-15m\",\n    \"to\": \"now\"\n  },\n  \"timepicker\": {},\n  \"timezone\"\
      : \"\",\n  \"title\": \"Overview\",\n  \"uid\": \"b5c8abf8-fe79-496b-b028-10bde917d1f0\"\
      ,\n  \"version\": 1,\n  \"weekStart\": \"\"\n}\n"
  kind: ConfigMap
  metadata:
    name: dashboards
    namespace: trustgraph
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: grafana
    name: grafana
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: grafana
    template:
      metadata:
        labels:
          app: grafana
      spec:
        containers:
        - env:
          - name: GF_ORG_NAME
            value: trustgraph.ai
          image: docker.io/grafana/grafana:11.1.4
          name: grafana
          ports:
          - containerPort: 3000
            hostPort: 3000
          resources:
            limits:
              cpu: '1.0'
              memory: 256M
            requests:
              cpu: '0.5'
              memory: 256M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
          volumeMounts:
          - mountPath: /var/lib/grafana
            name: grafana-storage
          - mountPath: /etc/grafana/provisioning/dashboards/
            name: prov-dash
          - mountPath: /etc/grafana/provisioning/datasources/
            name: prov-data
          - mountPath: /var/lib/grafana/dashboards/
            name: dashboards
        volumes:
        - name: grafana-storage
          persistentVolumeClaim:
            claimName: grafana-storage
        - configMap:
            name: prov-dash
          name: prov-dash
        - configMap:
            name: prov-data
          name: prov-data
        - configMap:
            name: dashboards
          name: dashboards
- apiVersion: v1
  kind: Service
  metadata:
    name: grafana
    namespace: trustgraph
  spec:
    ports:
    - name: http
      port: 3000
      targetPort: 3000
    selector:
      app: grafana
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: graph-embeddings
    name: graph-embeddings
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: graph-embeddings
    template:
      metadata:
        labels:
          app: graph-embeddings
      spec:
        containers:
        - command:
          - graph-embeddings
          - -p
          - pulsar://pulsar:6650
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: graph-embeddings
          resources:
            limits:
              cpu: '1.0'
              memory: 512M
            requests:
              cpu: '0.5'
              memory: 512M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: graph-embeddings
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: graph-embeddings
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: graph-rag
    name: graph-rag
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: graph-rag
    template:
      metadata:
        labels:
          app: graph-rag
      spec:
        containers:
        - command:
          - graph-rag
          - -p
          - pulsar://pulsar:6650
          - --prompt-request-queue
          - non-persistent://tg/request/prompt-rag
          - --prompt-response-queue
          - non-persistent://tg/response/prompt-rag
          - --entity-limit
          - '50'
          - --triple-limit
          - '30'
          - --max-subgraph-size
          - '400'
          - --max-path-length
          - '2'
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: graph-rag
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: graph-rag
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: graph-rag
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: kg-extract-definitions
    name: kg-extract-definitions
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: kg-extract-definitions
    template:
      metadata:
        labels:
          app: kg-extract-definitions
      spec:
        containers:
        - command:
          - kg-extract-definitions
          - -p
          - pulsar://pulsar:6650
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: kg-extract-definitions
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: kg-extract-definitions
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: kg-extract-definitions
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: kg-extract-relationships
    name: kg-extract-relationships
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: kg-extract-relationships
    template:
      metadata:
        labels:
          app: kg-extract-relationships
      spec:
        containers:
        - command:
          - kg-extract-relationships
          - -p
          - pulsar://pulsar:6650
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: kg-extract-relationships
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: kg-extract-relationships
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: kg-extract-relationships
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: kg-extract-topics
    name: kg-extract-topics
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: kg-extract-topics
    template:
      metadata:
        labels:
          app: kg-extract-topics
      spec:
        containers:
        - command:
          - kg-extract-topics
          - -p
          - pulsar://pulsar:6650
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: kg-extract-topics
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: kg-extract-topics
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: kg-extract-topics
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: metering
    name: metering
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: metering
    template:
      metadata:
        labels:
          app: metering
      spec:
        containers:
        - command:
          - metering
          - -p
          - pulsar://pulsar:6650
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: metering
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: metering
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: metering
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: metering-rag
    name: metering-rag
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: metering-rag
    template:
      metadata:
        labels:
          app: metering-rag
      spec:
        containers:
        - command:
          - metering
          - -p
          - pulsar://pulsar:6650
          - -i
          - non-persistent://tg/response/text-completion-rag
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: metering-rag
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: metering-rag
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: metering-rag
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: pdf-decoder
    name: pdf-decoder
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: pdf-decoder
    template:
      metadata:
        labels:
          app: pdf-decoder
      spec:
        containers:
        - command:
          - pdf-decoder
          - -p
          - pulsar://pulsar:6650
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: pdf-decoder
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: pdf-decoder
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: pdf-decoder
- apiVersion: v1
  data:
    prometheus.yml: "global:\n\n  scrape_interval:     15s # By default, scrape targets\
      \ every 15 seconds.\n\n  # Attach these labels to any time series or alerts\
      \ when communicating with\n  # external systems (federation, remote storage,\
      \ Alertmanager).\n  external_labels:\n    monitor: 'trustgraph'\n\n# A scrape\
      \ configuration containing exactly one endpoint to scrape:\n# Here it's Prometheus\
      \ itself.\nscrape_configs:\n\n  # The job name is added as a label `job=<job_name>`\
      \ to any timeseries\n  # scraped from this config.\n\n  - job_name: 'pulsar'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'pulsar:8080'\n\
      \n  - job_name: 'bookie'\n    scrape_interval: 5s\n    static_configs:\n   \
      \   - targets:\n        - 'bookie:8000'\n\n  - job_name: 'zookeeper'\n    scrape_interval:\
      \ 5s\n    static_configs:\n      - targets:\n        - 'zookeeper:8000'\n\n\
      \  - job_name: 'pdf-decoder'\n    scrape_interval: 5s\n    static_configs:\n\
      \      - targets:\n        - 'pdf-decoder:8000'\n\n  - job_name: 'chunker'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'chunker:8000'\n\
      \n  - job_name: 'document-embeddings'\n    scrape_interval: 5s\n    static_configs:\n\
      \      - targets:\n        - 'document-embeddings:8000'\n\n  - job_name: 'graph-embeddings'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'graph-embeddings:8000'\n\
      \n  - job_name: 'embeddings'\n    scrape_interval: 5s\n    static_configs:\n\
      \      - targets:\n        - 'embeddings:8000'\n\n  - job_name: 'kg-extract-definitions'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'kg-extract-definitions:8000'\n\
      \n  - job_name: 'kg-extract-topics'\n    scrape_interval: 5s\n    static_configs:\n\
      \      - targets:\n        - 'kg-extract-topics:8000'\n\n  - job_name: 'kg-extract-relationships'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'kg-extract-relationships:8000'\n\
      \n  - job_name: 'metering'\n    scrape_interval: 5s\n    static_configs:\n \
      \     - targets:\n        - 'metering:8000'\n\n  - job_name: 'metering-rag'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'metering-rag:8000'\n\
      \n  - job_name: 'store-doc-embeddings'\n    scrape_interval: 5s\n    static_configs:\n\
      \      - targets:\n        - 'store-doc-embeddings:8000'\n\n  - job_name: 'store-graph-embeddings'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'store-graph-embeddings:8000'\n\
      \n  - job_name: 'store-triples'\n    scrape_interval: 5s\n    static_configs:\n\
      \      - targets:\n        - 'store-triples:8000'\n\n  - job_name: 'text-completion'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'text-completion:8000'\n\
      \n  - job_name: 'text-completion-rag'\n    scrape_interval: 5s\n    static_configs:\n\
      \      - targets:\n        - 'text-completion-rag:8000'\n\n  - job_name: 'graph-rag'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'graph-rag:8000'\n\
      \n  - job_name: 'document-rag'\n    scrape_interval: 5s\n    static_configs:\n\
      \      - targets:\n        - 'document-rag:8000'\n\n  - job_name: 'prompt'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'prompt:8000'\n\
      \n  - job_name: 'prompt-rag'\n    scrape_interval: 5s\n    static_configs:\n\
      \      - targets:\n        - 'prompt-rag:8000'\n\n  - job_name: 'query-graph-embeddings'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'query-graph-embeddings:8000'\n\
      \n  - job_name: 'query-doc-embeddings'\n    scrape_interval: 5s\n    static_configs:\n\
      \      - targets:\n        - 'query-doc-embeddings:8000'\n\n  - job_name: 'query-triples'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'query-triples:8000'\n\
      \n  - job_name: 'agent-manager'\n    scrape_interval: 5s\n    static_configs:\n\
      \      - targets:\n        - 'agent-manager:8000'\n\n  - job_name: 'api-gateway'\n\
      \    scrape_interval: 5s\n    static_configs:\n      - targets:\n        - 'api-gateway:8000'\n\
      \n  - job_name: 'workbench-ui'\n    scrape_interval: 5s\n    static_configs:\n\
      \      - targets:\n        - 'workbench-ui:8000'\n\n# Cassandra\n# qdrant\n\n"
  kind: ConfigMap
  metadata:
    name: prometheus-cfg
    namespace: trustgraph
- apiVersion: v1
  kind: PersistentVolumeClaim
  metadata:
    name: prometheus-data
    namespace: trustgraph
  spec:
    accessModes:
    - ReadWriteOnce
    resources:
      requests:
        storage: 20G
    storageClassName: tg
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: prometheus
    name: prometheus
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: prometheus
    template:
      metadata:
        labels:
          app: prometheus
      spec:
        containers:
        - image: docker.io/prom/prometheus:v2.53.2
          name: prometheus
          ports:
          - containerPort: 9090
            hostPort: 9090
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
          volumeMounts:
          - mountPath: /etc/prometheus/
            name: prometheus-cfg
          - mountPath: /prometheus
            name: prometheus-data
        volumes:
        - configMap:
            name: prometheus-cfg
          name: prometheus-cfg
        - name: prometheus-data
          persistentVolumeClaim:
            claimName: prometheus-data
- apiVersion: v1
  kind: Service
  metadata:
    name: prometheus
    namespace: trustgraph
  spec:
    ports:
    - name: http
      port: 9090
      targetPort: 9090
    selector:
      app: prometheus
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: prompt
    name: prompt
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: prompt
    template:
      metadata:
        labels:
          app: prompt
      spec:
        containers:
        - command:
          - prompt-template
          - -p
          - pulsar://pulsar:6650
          - --text-completion-request-queue
          - non-persistent://tg/request/text-completion
          - --text-completion-response-queue
          - non-persistent://tg/response/text-completion
          - --system-prompt
          - 'You are a helpful assistant that performs NLP, Natural Language Processing,
            tasks.

            '
          - --prompt
          - "agent-react=Answer the following questions as best you can. You have\n\
            access to the following functions:\n\n{% for tool in tools %}{\n    \"\
            function\": \"{{ tool.name }}\",\n    \"description\": \"{{ tool.description\
            \ }}\",\n    \"arguments\": [\n{% for arg in tool.arguments %}       \
            \ {\n            \"name\": \"{{ arg.name }}\",\n            \"type\":\
            \ \"{{ arg.type }}\",\n            \"description\": \"{{ arg.description\
            \ }}\",\n        }\n{% endfor %}\n    ]\n}\n{% endfor %}\n\nYou can either\
            \ choose to call a function to get more information, or\nreturn a final\
            \ answer.\n    \nTo call a function, respond with a JSON object of the\
            \ following format:\n\n{\n    \"thought\": \"your thought about what to\
            \ do\",\n    \"action\": \"the action to take, should be one of [{{tool_names}}]\"\
            ,\n    \"arguments\": {\n        \"argument1\": \"argument_value\",\n\
            \        \"argument2\": \"argument_value\"\n    }\n}\n\nTo provide a final\
            \ answer, response a JSON object of the following format:\n\n{\n  \"thought\"\
            : \"I now know the final answer\",\n  \"final-answer\": \"the final answer\
            \ to the original input question\"\n}\n\nPrevious steps are included in\
            \ the input.  Each step has the following\nformat in your output:\n\n\
            {\n  \"thought\": \"your thought about what to do\",\n  \"action\": \"\
            the action taken\",\n  \"arguments\": {\n      \"argument1\": action argument,\n\
            \      \"argument2\": action argument2\n  },\n  \"observation\": \"the\
            \ result of the action\",\n}\n\nRespond by describing either one single\
            \ thought/action/arguments or\nthe final-answer.  Pause after providing\
            \ one action or final-answer.\n\n{% if context %}Additional context has\
            \ been provided:\n{{context}}{% endif %}\n\nQuestion: {{question}}\n\n\
            Input:\n    \n{% for h in history %}\n{\n    \"action\": \"{{h.action}}\"\
            ,\n    \"arguments\": [\n{% for k, v in h.arguments.items() %}       \
            \ {\n            \"{{k}}\": \"{{v}}\",\n{%endfor%}        }\n    ],\n\
            \    \"observation\": \"{{h.observation}}\"\n}\n{% endfor %}"
          - 'document-prompt=Study the following context. Use only the information
            provided in the context in your response. Do not speculate if the answer
            is not found in the provided set of knowledge statements.


            Here is the context:

            {{documents}}


            Use only the provided knowledge statements to respond to the following:

            {{query}}

            '
          - 'extract-definitions=Study the following text and derive definitions for
            any discovered entities. Do not provide definitions for entities whose
            definitions are incomplete or unknown. Output relationships in JSON format
            as an array of objects with keys:

            - entity: the name of the entity

            - definition: English text which defines the entity


            Here is the text:

            {{text}}


            Requirements:

            - Do not provide explanations.

            - Do not use special characters in the response text.

            - The response will be written as plain text.

            - Do not include null or unknown definitions.

            - The response shall use the following JSON schema structure:


            ```json

            [{"entity": string, "definition": string}]

            ```'
          - 'extract-relationships=Study the following text and derive entity relationships.  For
            each relationship, derive the subject, predicate and object of the relationship.
            Output relationships in JSON format as an array of objects with keys:

            - subject: the subject of the relationship

            - predicate: the predicate

            - object: the object of the relationship

            - object-entity: FALSE if the object is a simple data type and TRUE if
            the object is an entity


            Here is the text:

            {{text}}


            Requirements:

            - You will respond only with well formed JSON.

            - Do not provide explanations.

            - Respond only with plain text.

            - Do not respond with special characters.

            - The response shall use the following JSON schema structure:


            ```json

            [{"subject": string, "predicate": string, "object": string, "object-entity":
            boolean}]

            ```

            '
          - 'extract-rows=<instructions>

            Study the following text and derive objects which match the schema provided.


            You must output an array of JSON objects for each object you discover

            which matches the schema.  For each object, output a JSON object whose
            fields

            carry the name field specified in the schema.

            </instructions>


            <schema>

            {{schema}}

            </schema>


            <text>

            {{text}}

            </text>


            <requirements>

            You will respond only with raw JSON format data. Do not provide

            explanations. Do not add markdown formatting or headers or prefixes.

            </requirements>'
          - "extract-topics=Read the provided text carefully. You will identify topics\
            \ and their definitions found in the provided text. Topics are intangible\
            \ concepts.\n\nReading Instructions:\n- Ignore document formatting in\
            \ the provided text.\n- Study the provided text carefully for intangible\
            \ concepts.\n\nHere is the text:\n{{text}}\n\nResponse Instructions: \n\
            - Do not respond with special characters.\n- Return only topics that are\
            \ concepts and unique to the provided text.\n- Respond only with well-formed\
            \ JSON.\n- The JSON response shall be an array of objects with keys \"\
            topic\" and \"definition\". \n- The response shall use the following JSON\
            \ schema structure:\n\n```json\n[{\"topic\": string, \"definition\": string}]\n\
            ```\n\n- Do not write any additional text or explanations."
          - 'kg-prompt=Study the following set of knowledge statements. The statements
            are written in Cypher format that has been extracted from a knowledge
            graph. Use only the provided set of knowledge statements in your response.
            Do not speculate if the answer is not found in the provided set of knowledge
            statements.


            Here''s the knowledge statements:

            {% for edge in knowledge %}({{edge.s}})-[{{edge.p}}]->({{edge.o}})

            {%endfor%}


            Use only the provided knowledge statements to respond to the following:

            {{query}}

            '
          - question={{question}}
          - --prompt-response-type
          - agent-react=json
          - document-prompt=text
          - extract-definitions=json
          - extract-relationships=json
          - extract-rows=json
          - extract-topics=json
          - kg-prompt=text
          - --prompt-schema
          - extract-definitions={"items":{"properties":{"definition":{"type":"string"},"entity":{"type":"string"}},"required":["entity","definition"],"type":"object"},"type":"array"}
          - extract-relationships={"items":{"properties":{"object":{"type":"string"},"object-entity":{"type":"boolean"},"predicate":{"type":"string"},"subject":{"type":"string"}},"required":["subject","predicate","object","object-entity"],"type":"object"},"type":"array"}
          - extract-topics={"items":{"properties":{"definition":{"type":"string"},"topic":{"type":"string"}},"required":["topic","definition"],"type":"object"},"type":"array"}
          - --prompt-term
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: prompt
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: prompt
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8080
      targetPort: 8080
    selector:
      app: prompt
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: prompt-rag
    name: prompt-rag
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: prompt-rag
    template:
      metadata:
        labels:
          app: prompt-rag
      spec:
        containers:
        - command:
          - prompt-template
          - -p
          - pulsar://pulsar:6650
          - -i
          - non-persistent://tg/request/prompt-rag
          - -o
          - non-persistent://tg/response/prompt-rag
          - --text-completion-request-queue
          - non-persistent://tg/request/text-completion-rag
          - --text-completion-response-queue
          - non-persistent://tg/response/text-completion-rag
          - --system-prompt
          - 'You are a helpful assistant that performs NLP, Natural Language Processing,
            tasks.

            '
          - --prompt
          - "agent-react=Answer the following questions as best you can. You have\n\
            access to the following functions:\n\n{% for tool in tools %}{\n    \"\
            function\": \"{{ tool.name }}\",\n    \"description\": \"{{ tool.description\
            \ }}\",\n    \"arguments\": [\n{% for arg in tool.arguments %}       \
            \ {\n            \"name\": \"{{ arg.name }}\",\n            \"type\":\
            \ \"{{ arg.type }}\",\n            \"description\": \"{{ arg.description\
            \ }}\",\n        }\n{% endfor %}\n    ]\n}\n{% endfor %}\n\nYou can either\
            \ choose to call a function to get more information, or\nreturn a final\
            \ answer.\n    \nTo call a function, respond with a JSON object of the\
            \ following format:\n\n{\n    \"thought\": \"your thought about what to\
            \ do\",\n    \"action\": \"the action to take, should be one of [{{tool_names}}]\"\
            ,\n    \"arguments\": {\n        \"argument1\": \"argument_value\",\n\
            \        \"argument2\": \"argument_value\"\n    }\n}\n\nTo provide a final\
            \ answer, response a JSON object of the following format:\n\n{\n  \"thought\"\
            : \"I now know the final answer\",\n  \"final-answer\": \"the final answer\
            \ to the original input question\"\n}\n\nPrevious steps are included in\
            \ the input.  Each step has the following\nformat in your output:\n\n\
            {\n  \"thought\": \"your thought about what to do\",\n  \"action\": \"\
            the action taken\",\n  \"arguments\": {\n      \"argument1\": action argument,\n\
            \      \"argument2\": action argument2\n  },\n  \"observation\": \"the\
            \ result of the action\",\n}\n\nRespond by describing either one single\
            \ thought/action/arguments or\nthe final-answer.  Pause after providing\
            \ one action or final-answer.\n\n{% if context %}Additional context has\
            \ been provided:\n{{context}}{% endif %}\n\nQuestion: {{question}}\n\n\
            Input:\n    \n{% for h in history %}\n{\n    \"action\": \"{{h.action}}\"\
            ,\n    \"arguments\": [\n{% for k, v in h.arguments.items() %}       \
            \ {\n            \"{{k}}\": \"{{v}}\",\n{%endfor%}        }\n    ],\n\
            \    \"observation\": \"{{h.observation}}\"\n}\n{% endfor %}"
          - 'document-prompt=Study the following context. Use only the information
            provided in the context in your response. Do not speculate if the answer
            is not found in the provided set of knowledge statements.


            Here is the context:

            {{documents}}


            Use only the provided knowledge statements to respond to the following:

            {{query}}

            '
          - 'extract-definitions=Study the following text and derive definitions for
            any discovered entities. Do not provide definitions for entities whose
            definitions are incomplete or unknown. Output relationships in JSON format
            as an array of objects with keys:

            - entity: the name of the entity

            - definition: English text which defines the entity


            Here is the text:

            {{text}}


            Requirements:

            - Do not provide explanations.

            - Do not use special characters in the response text.

            - The response will be written as plain text.

            - Do not include null or unknown definitions.

            - The response shall use the following JSON schema structure:


            ```json

            [{"entity": string, "definition": string}]

            ```'
          - 'extract-relationships=Study the following text and derive entity relationships.  For
            each relationship, derive the subject, predicate and object of the relationship.
            Output relationships in JSON format as an array of objects with keys:

            - subject: the subject of the relationship

            - predicate: the predicate

            - object: the object of the relationship

            - object-entity: FALSE if the object is a simple data type and TRUE if
            the object is an entity


            Here is the text:

            {{text}}


            Requirements:

            - You will respond only with well formed JSON.

            - Do not provide explanations.

            - Respond only with plain text.

            - Do not respond with special characters.

            - The response shall use the following JSON schema structure:


            ```json

            [{"subject": string, "predicate": string, "object": string, "object-entity":
            boolean}]

            ```

            '
          - 'extract-rows=<instructions>

            Study the following text and derive objects which match the schema provided.


            You must output an array of JSON objects for each object you discover

            which matches the schema.  For each object, output a JSON object whose
            fields

            carry the name field specified in the schema.

            </instructions>


            <schema>

            {{schema}}

            </schema>


            <text>

            {{text}}

            </text>


            <requirements>

            You will respond only with raw JSON format data. Do not provide

            explanations. Do not add markdown formatting or headers or prefixes.

            </requirements>'
          - "extract-topics=Read the provided text carefully. You will identify topics\
            \ and their definitions found in the provided text. Topics are intangible\
            \ concepts.\n\nReading Instructions:\n- Ignore document formatting in\
            \ the provided text.\n- Study the provided text carefully for intangible\
            \ concepts.\n\nHere is the text:\n{{text}}\n\nResponse Instructions: \n\
            - Do not respond with special characters.\n- Return only topics that are\
            \ concepts and unique to the provided text.\n- Respond only with well-formed\
            \ JSON.\n- The JSON response shall be an array of objects with keys \"\
            topic\" and \"definition\". \n- The response shall use the following JSON\
            \ schema structure:\n\n```json\n[{\"topic\": string, \"definition\": string}]\n\
            ```\n\n- Do not write any additional text or explanations."
          - 'kg-prompt=Study the following set of knowledge statements. The statements
            are written in Cypher format that has been extracted from a knowledge
            graph. Use only the provided set of knowledge statements in your response.
            Do not speculate if the answer is not found in the provided set of knowledge
            statements.


            Here''s the knowledge statements:

            {% for edge in knowledge %}({{edge.s}})-[{{edge.p}}]->({{edge.o}})

            {%endfor%}


            Use only the provided knowledge statements to respond to the following:

            {{query}}

            '
          - question={{question}}
          - --prompt-response-type
          - agent-react=json
          - document-prompt=text
          - extract-definitions=json
          - extract-relationships=json
          - extract-rows=json
          - extract-topics=json
          - kg-prompt=text
          - --prompt-schema
          - extract-definitions={"items":{"properties":{"definition":{"type":"string"},"entity":{"type":"string"}},"required":["entity","definition"],"type":"object"},"type":"array"}
          - extract-relationships={"items":{"properties":{"object":{"type":"string"},"object-entity":{"type":"boolean"},"predicate":{"type":"string"},"subject":{"type":"string"}},"required":["subject","predicate","object","object-entity"],"type":"object"},"type":"array"}
          - extract-topics={"items":{"properties":{"definition":{"type":"string"},"topic":{"type":"string"}},"required":["topic","definition"],"type":"object"},"type":"array"}
          - --prompt-term
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: prompt-rag
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: prompt-rag
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8080
      targetPort: 8080
    selector:
      app: prompt-rag
- apiVersion: v1
  kind: PersistentVolumeClaim
  metadata:
    name: zookeeper
    namespace: trustgraph
  spec:
    accessModes:
    - ReadWriteOnce
    resources:
      requests:
        storage: 1G
    storageClassName: tg
- apiVersion: v1
  kind: PersistentVolumeClaim
  metadata:
    name: bookie
    namespace: trustgraph
  spec:
    accessModes:
    - ReadWriteOnce
    resources:
      requests:
        storage: 20G
    storageClassName: tg
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: zookeeper
    name: zookeeper
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: zookeeper
    template:
      metadata:
        labels:
          app: zookeeper
      spec:
        containers:
        - command:
          - bash
          - -c
          - bin/apply-config-from-env.py conf/zookeeper.conf && bin/generate-zookeeper-config.sh
            conf/zookeeper.conf && exec bin/pulsar zookeeper
          env:
          - name: PULSAR_MEM
            value: -Xms256m -Xmx256m -XX:MaxDirectMemorySize=256m
          - name: metadataStoreUrl
            value: zk:zookeeper:2181
          image: docker.io/apachepulsar/pulsar:3.3.1
          name: zookeeper
          ports:
          - containerPort: 2181
            hostPort: 2181
          - containerPort: 2888
            hostPort: 2888
          - containerPort: 3888
            hostPort: 3888
          resources:
            limits:
              cpu: '1'
              memory: 400M
            requests:
              cpu: '0.05'
              memory: 400M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
          volumeMounts:
          - mountPath: /pulsar/data/zookeeper
            name: zookeeper
        volumes:
        - name: zookeeper
          persistentVolumeClaim:
            claimName: zookeeper
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: pulsar-init
    name: pulsar-init
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: pulsar-init
    template:
      metadata:
        labels:
          app: pulsar-init
      spec:
        containers:
        - command:
          - bash
          - -c
          - sleep 10 && bin/pulsar initialize-cluster-metadata --cluster cluster-a
            --zookeeper zookeeper:2181 --configuration-store zookeeper:2181 --web-service-url
            http://pulsar:8080 --broker-service-url pulsar://pulsar:6650
          env:
          - name: PULSAR_MEM
            value: -Xms256m -Xmx256m -XX:MaxDirectMemorySize=256m
          image: docker.io/apachepulsar/pulsar:3.3.1
          name: pulsar-init
          resources:
            limits:
              cpu: '1'
              memory: 512M
            requests:
              cpu: '0.05'
              memory: 512M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: bookie
    name: bookie
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: bookie
    template:
      metadata:
        labels:
          app: bookie
      spec:
        containers:
        - command:
          - bash
          - -c
          - bin/apply-config-from-env.py conf/bookkeeper.conf && exec bin/pulsar bookie
          env:
          - name: BOOKIE_MEM
            value: -Xms512m -Xmx512m -XX:MaxDirectMemorySize=256m
          - name: advertisedAddress
            value: bookie
          - name: bookieId
            value: bookie
          - name: clusterName
            value: cluster-a
          - name: metadataStoreUri
            value: metadata-store:zk:zookeeper:2181
          - name: zkServers
            value: zookeeper:2181
          image: docker.io/apachepulsar/pulsar:3.3.1
          name: bookie
          ports:
          - containerPort: 3181
            hostPort: 3181
          resources:
            limits:
              cpu: '1'
              memory: 800M
            requests:
              cpu: '0.1'
              memory: 800M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
          volumeMounts:
          - mountPath: /pulsar/data/bookkeeper
            name: bookie
        volumes:
        - name: bookie
          persistentVolumeClaim:
            claimName: bookie
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: pulsar
    name: pulsar
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: pulsar
    template:
      metadata:
        labels:
          app: pulsar
      spec:
        containers:
        - command:
          - bash
          - -c
          - bin/apply-config-from-env.py conf/broker.conf && exec bin/pulsar broker
          env:
          - name: PULSAR_MEM
            value: -Xms512m -Xmx512m -XX:MaxDirectMemorySize=256m
          - name: advertisedAddress
            value: pulsar
          - name: advertisedListeners
            value: external:pulsar://pulsar:6650,localhost:pulsar://localhost:6650
          - name: clusterName
            value: cluster-a
          - name: managedLedgerDefaultAckQuorum
            value: '1'
          - name: managedLedgerDefaultEnsembleSize
            value: '1'
          - name: managedLedgerDefaultWriteQuorum
            value: '1'
          - name: metadataStoreUrl
            value: zk:zookeeper:2181
          - name: zookeeperServers
            value: zookeeper:2181
          image: docker.io/apachepulsar/pulsar:3.3.1
          name: pulsar
          ports:
          - containerPort: 6650
            hostPort: 6650
          - containerPort: 8080
            hostPort: 8080
          resources:
            limits:
              cpu: '1'
              memory: 800M
            requests:
              cpu: '0.1'
              memory: 800M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: init-trustgraph
    name: init-trustgraph
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: init-trustgraph
    template:
      metadata:
        labels:
          app: init-trustgraph
      spec:
        containers:
        - command:
          - tg-init-pulsar
          - -p
          - http://pulsar:8080
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: init-trustgraph
          resources:
            limits:
              cpu: '1'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: zookeeper
    namespace: trustgraph
  spec:
    ports:
    - name: zookeeper
      port: 2181
      targetPort: 2181
    - name: zookeeper2
      port: 2888
      targetPort: 2888
    - name: zookeeper3
      port: 3888
      targetPort: 3888
    selector:
      app: zookeeper
- apiVersion: v1
  kind: Service
  metadata:
    name: bookie
    namespace: trustgraph
  spec:
    ports:
    - name: bookie
      port: 3181
      targetPort: 3181
    selector:
      app: bookie
- apiVersion: v1
  kind: Service
  metadata:
    name: pulsar
    namespace: trustgraph
  spec:
    ports:
    - name: pulsar
      port: 6650
      targetPort: 6650
    - name: admin
      port: 8080
      targetPort: 8080
    selector:
      app: pulsar
- apiVersion: v1
  kind: PersistentVolumeClaim
  metadata:
    name: qdrant
    namespace: trustgraph
  spec:
    accessModes:
    - ReadWriteOnce
    resources:
      requests:
        storage: 20G
    storageClassName: tg
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: qdrant
    name: qdrant
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: qdrant
    template:
      metadata:
        labels:
          app: qdrant
      spec:
        containers:
        - image: docker.io/qdrant/qdrant:v1.13.3
          name: qdrant
          ports:
          - containerPort: 6333
            hostPort: 6333
          - containerPort: 6334
            hostPort: 6334
          resources:
            limits:
              cpu: '1.0'
              memory: 1024M
            requests:
              cpu: '0.5'
              memory: 1024M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
          volumeMounts:
          - mountPath: /qdrant/storage
            name: qdrant
        volumes:
        - name: qdrant
          persistentVolumeClaim:
            claimName: qdrant
- apiVersion: v1
  kind: Service
  metadata:
    name: qdrant
    namespace: trustgraph
  spec:
    ports:
    - name: api
      port: 6333
      targetPort: 6333
    - name: api2
      port: 6334
      targetPort: 6334
    selector:
      app: qdrant
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: query-doc-embeddings
    name: query-doc-embeddings
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: query-doc-embeddings
    template:
      metadata:
        labels:
          app: query-doc-embeddings
      spec:
        containers:
        - command:
          - de-query-qdrant
          - -p
          - pulsar://pulsar:6650
          - -t
          - http://qdrant:6333
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: query-doc-embeddings
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: query-doc-embeddings
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8080
      targetPort: 8080
    selector:
      app: query-doc-embeddings
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: query-graph-embeddings
    name: query-graph-embeddings
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: query-graph-embeddings
    template:
      metadata:
        labels:
          app: query-graph-embeddings
      spec:
        containers:
        - command:
          - ge-query-qdrant
          - -p
          - pulsar://pulsar:6650
          - -t
          - http://qdrant:6333
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: query-graph-embeddings
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: query-graph-embeddings
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8080
      targetPort: 8080
    selector:
      app: query-graph-embeddings
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: query-triples
    name: query-triples
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: query-triples
    template:
      metadata:
        labels:
          app: query-triples
      spec:
        containers:
        - command:
          - triples-query-cassandra
          - -p
          - pulsar://pulsar:6650
          - -g
          - cassandra
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: query-triples
          resources:
            limits:
              cpu: '0.5'
              memory: 512M
            requests:
              cpu: '0.1'
              memory: 512M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: query-triples
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8080
      targetPort: 8080
    selector:
      app: query-triples
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: store-doc-embeddings
    name: store-doc-embeddings
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: store-doc-embeddings
    template:
      metadata:
        labels:
          app: store-doc-embeddings
      spec:
        containers:
        - command:
          - de-write-qdrant
          - -p
          - pulsar://pulsar:6650
          - -t
          - http://qdrant:6333
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: store-doc-embeddings
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: store-doc-embeddings
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8080
      targetPort: 8080
    selector:
      app: store-doc-embeddings
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: store-graph-embeddings
    name: store-graph-embeddings
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: store-graph-embeddings
    template:
      metadata:
        labels:
          app: store-graph-embeddings
      spec:
        containers:
        - command:
          - ge-write-qdrant
          - -p
          - pulsar://pulsar:6650
          - -t
          - http://qdrant:6333
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: store-graph-embeddings
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: store-graph-embeddings
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8080
      targetPort: 8080
    selector:
      app: store-graph-embeddings
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: store-triples
    name: store-triples
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: store-triples
    template:
      metadata:
        labels:
          app: store-triples
      spec:
        containers:
        - command:
          - triples-write-cassandra
          - -p
          - pulsar://pulsar:6650
          - -g
          - cassandra
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: store-triples
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: store-triples
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8080
      targetPort: 8080
    selector:
      app: store-triples
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: text-completion
    name: text-completion
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: text-completion
    template:
      metadata:
        labels:
          app: text-completion
      spec:
        containers:
        - command:
          - text-completion-azure-openai
          - -p
          - pulsar://pulsar:6650
          - -x
          - '4096'
          - -t
          - '0.100'
          env:
          - name: AZURE_TOKEN
            valueFrom:
              secretKeyRef:
                key: azure-token
                name: azure-openai-credentials
          - name: AZURE_ENDPOINT
            valueFrom:
              secretKeyRef:
                key: azure-endpoint
                name: azure-openai-credentials
          - name: AZURE_MODEL
            valueFrom:
              secretKeyRef:
                key: azure-model
                name: azure-openai-credentials
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: text-completion
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: text-completion
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: text-completion
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: text-completion-rag
    name: text-completion-rag
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: text-completion-rag
    template:
      metadata:
        labels:
          app: text-completion-rag
      spec:
        containers:
        - command:
          - text-completion-azure-openai
          - -p
          - pulsar://pulsar:6650
          - -x
          - '4092'
          - -t
          - '0.000'
          - -i
          - non-persistent://tg/request/text-completion-rag
          - -o
          - non-persistent://tg/response/text-completion-rag
          env:
          - name: AZURE_TOKEN
            valueFrom:
              secretKeyRef:
                key: azure-token
                name: azure-openai-credentials
          - name: AZURE_ENDPOINT
            valueFrom:
              secretKeyRef:
                key: azure-endpoint
                name: azure-openai-credentials
          - name: AZURE_MODEL
            valueFrom:
              secretKeyRef:
                key: azure-model
                name: azure-openai-credentials
          image: docker.io/trustgraph/trustgraph-flow:0.21.9
          name: text-completion-rag
          resources:
            limits:
              cpu: '0.5'
              memory: 128M
            requests:
              cpu: '0.1'
              memory: 128M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: text-completion-rag
    namespace: trustgraph
  spec:
    ports:
    - name: metrics
      port: 8000
      targetPort: 8000
    selector:
      app: text-completion-rag
- apiVersion: apps/v1
  kind: Deployment
  metadata:
    labels:
      app: workbench-ui
    name: workbench-ui
    namespace: trustgraph
  spec:
    replicas: 1
    selector:
      matchLabels:
        app: workbench-ui
    template:
      metadata:
        labels:
          app: workbench-ui
      spec:
        containers:
        - image: docker.io/trustgraph/workbench-ui:0.2.4
          name: workbench-ui
          ports:
          - containerPort: 8888
            hostPort: 8888
          resources:
            limits:
              cpu: '0.1'
              memory: 256M
            requests:
              cpu: '0.1'
              memory: 256M
          securityContext:
            runAsGroup: 0
            runAsUser: 0
        volumes: []
- apiVersion: v1
  kind: Service
  metadata:
    name: workbench-ui
    namespace: trustgraph
  spec:
    ports:
    - name: ui
      port: 8888
      targetPort: 8888
    selector:
      app: workbench-ui
kind: List
