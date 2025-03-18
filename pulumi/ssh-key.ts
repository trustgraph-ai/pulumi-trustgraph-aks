
import * as tls from "@pulumi/tls";

export const sshKey = new tls.PrivateKey(
    "ssh-key", {
          algorithm: "RSA",
          rsaBits: 2048,
    }
);

