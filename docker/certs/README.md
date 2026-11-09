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

## Verify stack

The isolated verify stack (`scripts/verify-stack.ps1`) runs in slots 1-4, one slot for each worktree. Slot `n` uses the names `api.s<n>.hfverify.test`, `auth.s<n>.hfverify.test`, and `app.s<n>.hfverify.test`, so the Keycloak cookies of a stack never overwrite the dev sign-in or the sign-in of another slot.

The certificate is a wildcard certificate for `*.s1.hfverify.test`, `*.s2.hfverify.test`, `*.s3.hfverify.test`, and `*.s4.hfverify.test`. The script creates `hfverify.test.pem` and `hfverify.test-key.pem` in this folder with mkcert when they are missing. It creates them again for all 4 names when a certificate lacks a slot name. Git ignores both files.

Add this line to `C:\Windows\System32\drivers\etc\hosts` once, as an administrator. It holds the 12 names of slots 1-4:

```text
127.0.0.1 api.s1.hfverify.test auth.s1.hfverify.test app.s1.hfverify.test api.s2.hfverify.test auth.s2.hfverify.test app.s2.hfverify.test api.s3.hfverify.test auth.s3.hfverify.test app.s3.hfverify.test api.s4.hfverify.test auth.s4.hfverify.test app.s4.hfverify.test
```
