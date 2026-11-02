# Local certificate

The gateway ends TLS for the public names `api.healthyfac.test`, `auth.healthyfac.test`, and `app.healthyfac.test`.
It needs a certificate from a local certificate authority. Git ignores every file in this folder except this file.

## Create the certificate

Run these commands once, from the repository root:

```powershell
winget install FiloSottile.mkcert
mkcert -install
mkcert -cert-file docker/certs/healthyfac.test.pem -key-file docker/certs/healthyfac.test-key.pem api.healthyfac.test auth.healthyfac.test app.healthyfac.test
```

- `mkcert -install` adds the local certificate authority to the Windows store, which Chrome and Edge use.
- Firefox needs its own store. `mkcert -install` updates it when Firefox is installed.

## Files

| File | Used by |
| --- | --- |
| `healthyfac.test.pem` | The certificate. Compose mounts it into the gateway container as `/certs/healthyfac.test.pem`. |
| `healthyfac.test-key.pem` | The private key. Compose mounts it read-only. Never commit it and never copy it into an image. |

The launch profile `https` of the gateway reads the same files.
