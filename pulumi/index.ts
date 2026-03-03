
import * as fs from 'fs';
//import { kubeconfig } from './cluster';
import { sshKey } from './ssh-key';
//import * as application from './application';
//import * as secrets from './secrets';
import { accountSuffix } from './ai-project';

sshKey.privateKeyOpenssh.apply(
    (key : string) => {
        fs.writeFile(
            "ssh-private.key",
            key,
            err => {
                if (err) {
                    console.log(err);
                    throw(err);
                } else {
                    console.log("Wrote private key.");
                }
            }
        );
    }
);

accountSuffix.apply(console.log);

/*
kubeconfig.apply(
    (key : string) => {
        fs.writeFile(
            "kube.cfg",
            key,
            err => {
                if (err) {
                    console.log(err);
                    throw(err);
                } else {
                    console.log("Wrote kube.cfg.");
                }
            }
        );
    }
);
*/

const save = [
//    application,
//    secrets,
];

