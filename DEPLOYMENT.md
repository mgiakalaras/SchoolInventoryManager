# School Inventory Manager - Docker / Server Deployment

This deployment is intended for a school LAN server, for example ZimaOS, CasaOS, a Linux server, or a Windows machine with Docker Desktop.

## Quick Docker deployment

From the project folder:

```bash
docker compose up -d --build
```

Open the app from another device in the same network:

```text
http://SERVER-IP:5148
```

Example:

```text
http://192.168.1.80:5148
```

## Persistent data

The SQLite database and temporary import files are stored in:

```text
App_Data/
```

The Docker Compose file maps this folder as a volume:

```yaml
./App_Data:/app/App_Data
```

Do not commit `App_Data` to GitHub. It contains live school data.

## Useful commands

Build and start:

```bash
docker compose up -d --build
```

Show running containers:

```bash
docker ps
```

View logs:

```bash
docker logs -f school-inventory-manager
```

Stop:

```bash
docker compose down
```

Restart:

```bash
docker compose restart
```

Update from GitHub and rebuild:

```bash
git pull
docker compose up -d --build
```

## Firewall

Make sure port `5148` is allowed on the server firewall for the local/private network.

## Security note

At this stage the app is intended for use only inside the school LAN. Do not expose it directly to the public Internet until authentication, user roles, HTTPS, and backup policy are finalized.
