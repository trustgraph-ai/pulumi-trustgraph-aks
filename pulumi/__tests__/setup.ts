import * as fs from 'fs';

jest.mock('fs');
const mockedFs = fs as jest.Mocked<typeof fs>;

mockedFs.readFileSync.mockImplementation((filePath: any, options: any) => {
    if (typeof filePath === 'string' && filePath.includes('resources.yaml')) {
        return `
apiVersion: v1
kind: Namespace
metadata:
  name: trustgraph
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: test-config
  namespace: trustgraph
data:
  test: "value"
`;
    }
    return jest.requireActual('fs').readFileSync(filePath, options);
});

mockedFs.writeFile.mockImplementation(
    (_path: any, _data: any, callback: any) => {
        if (typeof callback === 'function') {
            callback(null);
        }
    }
);
