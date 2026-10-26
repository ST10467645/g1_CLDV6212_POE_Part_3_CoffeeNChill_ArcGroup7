# CoffeeNChill Part 3: VPS Provisioning and Deployment Runbook

This is the step-by-step record of how the CoffeeNChill system was put on a Linux VPS, and how to check, update and rebuild it. Every command was run for real during Part 3 (Mon 19 Oct to Mon 26 Oct 2026).

## 1. What runs on the server

| Service | Image | Port on the server | Open to the internet? |
|---|---|---|---|
| `gateway` | `st10482062/coffeenchill-gateway:v3.0` | 5000 (maps to 8080 inside) | **Yes**, the only public door |
| `functions` | `st10482062/coffeenchill-functions:v3.0` | 127.0.0.1:7071 | No (the Functions have no login) |
| `azurite` | `st10482062/coffeenchill-azurite:v3.0` | 127.0.0.1:10000 to 10002 | No |

One compose file (`docker-compose.yml`) starts all three on the network `coffeenchill-net`. Data is kept in the volume `azurite-data`. Secrets are read from a `.env` file that exists only on the server. Each container keeps at most 3 log files of 10 MB (log rotation), so logs cannot fill the disk.

## 2. The server

| Item | Value |
|---|---|
| Provider | Microsoft Azure virtual machine (Azure for Students credit) |
| Name / resource group | `vm-coffeenchill` / `rg-coffeenchill-part3` |
| Region | Central India |
| Size | Standard B2als_v2 (2 vCPU, 4 GiB RAM, x64), about 0.0246 USD per hour |
| Operating system | Ubuntu Server 24.04 LTS x64 |
| Login | user `azureuser`, SSH keys only (no password) |
| Public IP | `20.197.62.154` (Static, Standard SKU, resource `ip-coffeenchill`) |
| Firewall (Network Security Group `vm-coffeenchill-nsg`) | inbound **22** (SSH) and **5000** (gateway) only |
| Auto-shutdown | off |

## 3. Create the VM (Azure portal)

1. portal.azure.com > **Virtual machines** > **Create** > **Azure virtual machine**.
2. Basics: resource group `rg-coffeenchill-part3`, name `vm-coffeenchill`, a region the student policy allows (Central India worked), availability **No infrastructure redundancy required**, image Ubuntu Server 24.04 LTS x64, size Standard B2als_v2, authentication **SSH public key** (generate a new key pair `coffeenchill-key`), username `azureuser`, inbound port **SSH (22)**.
3. Disks: Standard SSD.
4. Networking: create the public IP `ip-coffeenchill` with SKU **Standard** and assignment **Static**. NIC security group **Basic** with SSH (22).
5. Management: auto-shutdown **off**.
6. **Review + create**, then **Create**, then **Download private key and create resource**. Keep the `.pem` outside Git (for example `~/.ssh/coffeenchill-key.pem`). It is the only copy and it is never committed.
7. After deployment: VM > **Networking** > **Network settings** > **Create port rule** > **Inbound port rule**: destination port `5000`, protocol TCP, action Allow, priority `310`, name `Allow-Gateway-5000`.

## 4. Connect

On Windows (PowerShell), lock the key file once, then connect:

```powershell
icacls "$env:USERPROFILE\.ssh\coffeenchill-key.pem" /inheritance:r
icacls "$env:USERPROFILE\.ssh\coffeenchill-key.pem" /grant:r "$($env:USERNAME):(R)"
ssh -i "$env:USERPROFILE\.ssh\coffeenchill-key.pem" azureuser@20.197.62.154
```

On Linux or macOS:

```bash
ssh -i ~/.ssh/coffeenchill-key.pem azureuser@20.197.62.154
```

The first connection asks to trust the host fingerprint: type `yes`. A prompt `azureuser@vm-coffeenchill:~$` means you are inside.

## 5. Install Docker Engine and the Compose plugin

```bash
sudo apt-get update
sudo apt-get upgrade -y
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker $USER
mkdir -p ~/coffeenchill
```

Log out (`exit`) and connect again so the `docker` group applies. Then check:

```bash
docker --version
docker compose version
docker run --rm hello-world
free -h
```

The server has about 3.8 GiB of memory, so no swap file was needed. (On a 1 GiB size, add a 2 GB swap file with `fallocate`, `mkswap` and `swapon`.)

## 6. Deploy

1. On your own PC, copy the compose file (it contains no secrets). Never copy a `.env` or a `.pem`:

   ```powershell
   scp -i "$env:USERPROFILE\.ssh\coffeenchill-key.pem" .\docker-compose.yml azureuser@20.197.62.154:~/coffeenchill/docker-compose.yml
   ```

2. On the server create the secret `.env`. The random values are made on the server and never travel:

   ```bash
   cd ~/coffeenchill
   JWT=$(openssl rand -hex 32)
   PASS="Cn-$(openssl rand -hex 6)-Aa1"
   cat > .env <<EOF
   IMAGE_TAG=v3.0
   JWT_KEY=$JWT
   SEED_ADMIN_EMAIL=admin@coffeenchill.ac.za
   SEED_ADMIN_PASSWORD=$PASS
   EOF
   chmod 600 .env
   echo "Admin password: $PASS"
   ```

   Store the printed Admin password in a password manager. It is shown once. Check the file with `ls -l .env` (expect `-rw-------`) and `cut -d= -f1 .env` (expect only the four names).

3. Start:

   ```bash
   docker compose config --quiet
   docker compose pull
   docker compose up -d
   docker compose ps
   ```

4. The gateway creates the first Admin account by itself. It retries until Azurite is ready, so a "could not seed the Admin (attempt 1 of 10)" warning right after start-up is normal.

## 7. Check that it works

| Check | Command or action | Expected |
|---|---|---|
| Containers | `docker compose ps` | 3 services `running` |
| Health on the server | `curl -i http://localhost:5000/api/health` | 200 |
| Health from outside | `curl.exe -i http://20.197.62.154:5000/api/health` | 200 |
| Swagger | open `http://20.197.62.154:5000/swagger` (also on a phone with Wi-Fi off) | the page loads |
| Admin login | `POST /api/auth/login`, Authorize, `GET /api/users` | 200, the Admin is listed |
| Private ports | from your PC `Test-NetConnection 20.197.62.154 -Port 7071` (and 10000) | `TcpTestSucceeded : False` |
| Restart policy | `sudo reboot`, wait 2 minutes, `docker compose ps` | 3 services Up again by themselves |

Wait about 20 seconds after a start before the first request. A call straight after start-up can return 503 while Azurite is still starting.

## 8. Update the system

- **Automatic (CI/CD):** a push to `main` runs the GitHub Actions workflow, which builds and tests, pushes `v3.0` and a build-numbered tag to Docker Hub, then connects over SSH and runs `cd ~/coffeenchill && docker compose pull && docker compose up -d`.
- **By hand:**

  ```bash
  cd ~/coffeenchill
  docker compose pull
  docker compose up -d
  docker image prune -f
  ```

  `pull` downloads newer versions of the three images from Docker Hub. `up -d` recreates only the containers whose image changed and leaves the others running.
- **Roll back:** set `IMAGE_TAG` in `.env` to an older tag, then `docker compose pull && docker compose up -d`.

## 9. Everyday commands

| Job | Command |
|---|---|
| See the logs | `docker compose logs gateway --tail 50` |
| Restart one service | `docker compose restart gateway` |
| Stop everything (data stays) | `docker compose down` |
| Delete everything including the data | `docker compose down -v` (careful) |
| Disk use | `df -h` and `docker system df` |

## 10. Save credit: stop and start the VM

- **Stop:** Azure portal > `vm-coffeenchill` > **Stop**, then wait for **Stopped (deallocated)**. Do not shut down from inside Linux, because that keeps billing compute.
- **Start:** portal > **Start**, wait for **Running**, then connect. The containers come back by themselves (restart policy) and the static IP does not change.

## 11. Troubleshooting

| Problem | Likely cause | Fix |
|---|---|---|
| `RequestDisallowedByAzure` when creating the VM | the student subscription only allows some regions | pick another region from the portal list (Central India worked) |
| `ZonalAllocationFailed` | no free capacity in that availability zone | set availability to **No infrastructure redundancy required**, or try another size |
| Swagger does not open from outside | no inbound rule for 5000 | add the rule in the Network Security Group |
| `Permission denied (publickey)` | wrong key file or user | use `azureuser` and the `.pem` of this server |
| `UNPROTECTED PRIVATE KEY FILE` | key file permissions too open | repeat the two `icacls` lines |
| Gateway restarts again and again | `.env` is missing or `JWT_KEY` is shorter than 32 characters | check `cut -d= -f1 .env`, then `docker compose logs gateway` |
| Admin login returns 401 | the Admin already existed with another password | the seeder never overwrites an existing Admin, so use the API to change it, or `docker compose down -v` for a fresh start |
| `pull access denied` | the Docker Hub repository is private or the tag is missing | make the repository Public and check the tag |
| 503 from the gateway | Azurite or Functions is down or still starting | wait 20 seconds, then `docker compose ps` and `docker compose up -d` |
| After a reboot one container is `Created` or missing | a container was killed while Docker was recreating it | `docker compose down` then `docker compose up -d` (no `-v`, the data stays) |
| Docker Desktop on the PC fails with "read-only file system" | the Docker virtual disk or build cache was damaged | quit Docker Desktop, `wsl --shutdown`, reopen, then `docker builder prune -af` |

## 12. Security notes

- Only port 5000 (the gateway) and port 22 (SSH, keys only) are open. The Functions and Azurite are bound to `127.0.0.1`, because the Functions have no login of their own.
- Secrets (`.env`, the SSH private key, the Docker Hub token) are never in Git. The pipeline's key and token are stored as GitHub secrets.
- The gateway runs over **HTTP** (no domain, so no certificate). A login token could be read on the network. A real system needs HTTPS, for example a domain name and a certificate behind a reverse proxy. This is a known limitation of this project.
- Azurite is a storage emulator. It was used on the server because no Azure Storage credit was available. With real Azure Storage only the connection string would change.

## Sources

Docker (2026n). Install Docker Engine on Ubuntu. [Online]
Available at: https://docs.docker.com/engine/install/ubuntu/
[Accessed 26 October 2026].

Docker (2026m). JSON File logging driver. [Online]
Available at: https://docs.docker.com/engine/logging/drivers/json-file/
[Accessed 26 October 2026].

Microsoft Learn (2026ar). Quickstart: Create a Linux virtual machine in the Azure portal. [Online]
Available at: https://learn.microsoft.com/en-us/azure/virtual-machines/linux/quick-create-portal
[Accessed 26 October 2026].