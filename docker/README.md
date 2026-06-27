# Toloka-enhanced Prowlarr — Docker

A drop-in replacement for the official LinuxServer Prowlarr image, identical except
for the enhanced **Toloka.to** indexer built from the `feature/toloka-enhancements`
branch. It is an *overlay*: only `Prowlarr.Core.dll` is rebuilt and swapped onto
`lscr.io/linuxserver/prowlarr` (the React frontend is untouched), so your existing
config, port and volumes all work unchanged.

## Use a published image (recommended)

Images are published to GHCR by the **Docker (Toloka)** GitHub Action on every push
to the branch:

```
ghcr.io/maksii/prowlarr-toloka:latest
```

> After the first CI run, make the package **Public** in
> GitHub → your profile → Packages → `prowlarr-toloka` → Package settings,
> so others can `docker pull` without a token.

Run it:

```bash
docker compose -f docker/docker-compose.yml up -d
# http://localhost:9696  →  Indexers → Add → "Toloka"
```

or plain `docker run`:

```bash
docker run -d --name prowlarr-toloka \
  -e PUID=1000 -e PGID=1000 -e TZ=Etc/UTC \
  -p 9696:9696 \
  -v ./config:/config \
  ghcr.io/maksii/prowlarr-toloka:latest
```

## Build locally

```bash
# from the repo root (build context must be the root)
docker build -f docker/Dockerfile.toloka -t prowlarr-toloka .
docker run -d -p 9696:9696 -v ./config:/config prowlarr-toloka
```

No local .NET SDK needed — the build runs entirely inside the container.

## How it stays in sync with upstream

`Dockerfile.toloka` pins `BASE_TAG` to the upstream Prowlarr version this branch is
based on (currently **nightly-2.5.0.5422-ls3**, the develop/nightly channel). When
you rebase the branch onto a newer Prowlarr, bump `BASE_TAG` to the matching
[LinuxServer tag](https://hub.docker.com/r/linuxserver/prowlarr/tags) so the swapped
DLL stays ABI-compatible with the rest of the image.

> **Do not enable Prowlarr/LSIO auto-update** for this container — an in-place update
> would re-download stock Prowlarr and overwrite the Toloka DLL. Update by pulling a
> new image instead.
