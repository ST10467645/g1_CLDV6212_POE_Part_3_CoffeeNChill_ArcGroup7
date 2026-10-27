# CoffeeNChill Canteen Management System

> **Notes for the marker**
> - **Video quality:** YouTube lowers the quality automatically. Please click the gear icon > **Quality** > **1080p** before watching. The videos were uploaded in 1080p.
> - **Commits by Thamsanqa Ncube (ST10482062):** some of his commits can show under a different name or without his picture on GitHub. They are his work. See the [Commit Authorship Note](#commit-authorship-note) for the proof and the command that lists them under his name.

**Module:** CLDV6212 (Cloud Development B), The Independent Institute of Education | **Group 7 (ARC)** | **Lecturer:** Farshida Wania

This repository holds the whole POE. **Part 3** (API gateway, JWT and Linux VPS) is the current work and comes first. **Part 2** (queue triggers and Docker Compose) and **Part 1** (menu, staff documents and Docker images) follow below, each in its own section so every part can be marked on its own.

**Jump to:** [Part 3](#part-3--api-gateway-jwt-and-linux-vps-v30) | [Part 2](#part-2--queue-triggers-and-docker-compose-v20) | [Part 1](#part-1--menu-staff-documents-and-docker-v10) | [Contributions and commit evidence](#team-contributions-and-commit-evidence) | [Project structure](#project-structure) | [AI disclosure](#ai-disclosure) | [References](#references)

## Group Members
| Name | Student Number |
|---|---|
| Kandyce Smit | ST10467645 |
| Kristen Keve | ST10472683 |
| Thamsanqa Ncube | ST10482062 |

## Repository Note (private repository and public mirror)
All three parts were built in repositories created through the university's GitHub Classroom, which are **private**. For assessment, each part is **mirrored with its full branch and commit history to a public repository**, the same way as Part 1.

| | Private working repository (GitHub Classroom) | Public repository (submission) |
|---|---|---|
| Part 1 | https://github.com/EMGPSD/cldv6212-g1-2026-poe-part1-st10467645.git | https://github.com/ST10467645/CLDV6212-g1-CoffeeNChill-ArcGroup7.git |
| Part 2 | https://github.com/EMGPSD/Group_1_CLDV6212_Part-2-_Group_7_on_ARC.git | https://github.com/ST10467645/CLDV6212_Part-2-g1-CoffeeNChill-ArcGroup7.git |
| Part 3 | waiting for link | https://github.com/ST10467645/g1_CLDV6212_POE_Part_3_CoffeeNChill_ArcGroup7 |

What the mirror contains: every branch (`main`, `kandyce-part2-producer`, `kristen-part2-processor`, `thami-part2-compose`), every commit with its original author and date, and the merge commits created when each pull request was merged. What it does **not** contain: the pull request pages and review comments, because GitHub keeps those only in the repository where they were opened. Screenshots of the merged pull requests are in the submission document. The Part 1 project was migrated into the Part 2 repository, so Part 2 contains all Part 1 code unchanged.

## Team Contributions and Commit Evidence
Each member owns one of the three main parts of Part 2 (25 marks each in the brief) and committed from their own GitHub account on their own branch. Work was merged into `main` through pull requests, in an agreed order, so there were no merge conflicts.

The commit and pull request amounts are based on the repository history available at the time that this ReadMe was written

| Member | Branch | Part 2 area | Commits | Pull requests merged |
|---|---|---|---|---|
| Kandyce Smit (ST10467645) | `kandyce-part2-producer` | Queue producer: `SubmitOrder`, DTOs, validation, Base64 encoding, `host.json`, sample payloads, Postman collection, README | 30 | 10 |
| Kristen Keve (ST10472683) | `kristen-part2-processor` | Queue trigger and order tracking: `ProcessOrderQueue`, Orders table, status life cycle, `UpdateOrderStatus`, poison queue | 15 | 11 |
| Thamsanqa Ncube (ST10482062) | `thami-part2-compose` | Docker: `Dockerfile`, `docker-compose.yml`, Docker Hub `v2.0` images, network, volume, Docker Postman checks | 11 | 7 |

Anyone can reproduce the numbers from a clone with these commands (they work in PowerShell, Command Prompt and Git Bash):
```text
git fetch --all
git shortlog -sn --all --no-merges
git log --all --no-merges --format="%h  %ad  %an  %s" --date=short
git log --all --graph --oneline --decorate
```
GitHub's Insights > Contributors page only counts commits whose e-mail address is linked to a GitHub account, so the terminal output above is the complete record.

Branch and commit conventions: one branch per person and feature area, commit messages in the form `feat:`, `fix:`, `docs:`, `test:` followed by what changed (for example `feat: add queue-triggered order processing with status lifecycle`), commits spread over several days, and every branch merged by pull request.

## Commit Authorship Note
GitHub's profile pictures and the Insights > Contributors page count a commit for a person only when the e-mail address inside the commit is verified on that person's GitHub account. Some commits by **Thamsanqa Ncube (ST10482062, GitHub: Ncube-Thamsanqa)** were made with a different Git name or e-mail (Thami Ncube <ST10482062@imconnect.edu.za>), so GitHub can show them under another name or without his picture. They are his work: his branches `thami-part2-compose` and `thami-part3-deploy`, the pull requests he opened and the Docker, VPS and orders work in this repository are all his.

To remove any doubt the repository has a `.mailmap` file that maps his Git identities to his one name, so the terminal commands under Team Contributions list all of his commits under **Thamsanqa Ncube**:
```text
git shortlog -sn --all --no-merges
git log --all --no-merges --author="Thamsanqa" --format="%h  %ad  %an <%ae>  %s" --date=short
```

---

# Part 3 — API Gateway, JWT and Linux VPS (v3.0)

**Jump to:** [Part 3](#part-3--api-gateway-jwt-and-linux-vps-v30) | [Architecture](#architecture-part-3) | [API Endpoints](#part-3-api-endpoints) | [Feature Descriptions](#part-3-feature-descriptions) | [Testing](#part-3-testing) | [CI/CD Pipeline](#cicd-pipeline-bonus)

## What the System Does
Part 3 puts one secure front door, an **ASP.NET Core Web API gateway** (`CoffeeNChill.Gateway`), in front of the Part 2 system. Customers and staff only talk to the gateway. The gateway checks who the caller is with a **JSON Web Token (JWT)**, checks the caller's **role** (Admin, Staff or Customer), and then sends the request on to Azure Table Storage, Azure Blob Storage, the Azure Storage Queue or the Part 2 Azure Functions app (Microsoft Learn, 2026ae; Microsoft Learn, 2026x). The Part 2 Functions are not changed. They now work as the background workers behind the gateway.

| | Project | Job |
|---|---|---|
| Front door | `src/CoffeeNChill.Gateway` (new in Part 3) | Login, tokens, roles, validation, Swagger, menu, documents, orders and user management (Admin only) |
| Back room | `src/CoffeeNChill.Functions` (Part 2, unchanged) | Queue worker that moves an order `Received` → `Preparing` → `Ready` → `Collected`, and the order lookups |

## Architecture (Part 3)
```mermaid
flowchart LR
  C["Client: Swagger, Postman or a phone"] -->|"HTTP port 5000, Bearer JWT"| G["Gateway: ASP.NET Core Web API"]
  subgraph VPS["Linux VPS: Docker Compose, network coffeenchill-net"]
    G -->|"Users and Menu tables, staff-docs blobs, order messages"| A[("Azurite: Table, Blob, Queue")]
    G -->|"typed HttpClient: order list, order, status"| F["Azure Functions: queue worker and order lookups"]
    F -->|"queue trigger, Orders table"| A
  end
  GH["GitHub Actions"] -->|"push v3.0 and 3.0.N"| DH[("Docker Hub")]
  GH -->|"SSH: docker compose pull and up -d"| VPS
  DH -->|"images"| VPS
```
**How a request travels:**
1. The client calls the gateway on port 5000. Public routes (menu, place an order, status, register, login, health) need no token.
2. For protected routes the JWT bearer middleware checks the signature, issuer, audience and expiry, and `[Authorize(Roles = "...")]` checks the role (401 or 403 if not).
3. The controller checks the request body (DTO validation, one 400 list for every problem).
4. The gateway reads or writes Azure Table or Blob Storage itself, puts an order message on the queue, or forwards order lookups to the Functions app.
5. The global exception middleware turns any failure into the same JSON error and a 503 when storage is down.
6. Only port 5000 is public. The Functions and Azurite are bound to `127.0.0.1` on the server.

## Changelog (Part 3, so far)
- **Added** the gateway project with controllers, DTOs, mappers, a typed `HttpClient` to the Functions app and Swagger documentation (Microsoft Learn, 2026z; Microsoft Learn, 2026aa).
- **Added** the `Users` Azure Table (PartitionKey = role, RowKey = email in lower case, `PasswordHash`, `FullName`, `CreatedDate`).
- **Added** `POST /api/auth/register` and `POST /api/auth/login`. Passwords are stored as **BCrypt** hashes (BcryptNet, 2026a). Login returns a signed JWT with the claims Name, Email and Role (Microsoft Learn, 2026ah).
- **Added** role-based access control with `[Authorize(Roles = "...")]` on every protected endpoint (Microsoft Learn, 2026af).
- **Added** a seeded first Admin account, created at start-up from configuration (Microsoft Learn, 2026ab; Microsoft Learn, 2026ac).
- **Added** menu endpoints that anyone can read and only Admin and Staff can change.
- **Added** staff document endpoints on Azure Blob Storage: upload, list, download, delete (Microsoft Learn, 2026am; Microsoft Learn, 2026an; Microsoft Learn, 2026ao).
- **Added** order endpoints: place an order on the queue (public), check an order status (public), list orders and update a status (Admin and Staff).
- **Added** a multi-stage `Dockerfile` for the gateway image (Docker, 2026j; Microsoft Learn, 2026aj).
- **Changed** the repository layout: Part 2 project moved to `src/CoffeeNChill.Functions` with `git mv`, so the history is kept.
- **Kept** all Part 1 and Part 2 behaviour working and unchanged.
- **Added** profile endpoints: a logged-in user reads and updates their own profile. The email is read from the token claims (Microsoft Learn, 2026ap), and a new password needs the current password.
- **Added** Admin user management: list users (optional `?role=` filter) and delete a user. An Admin cannot delete their own account.
- **Changed** the Admin seeder to retry in the background until storage is ready, because Docker starts Azurite and the gateway at the same time (Microsoft Learn, 2026ab).
- **Added** global exception middleware: every error returns the same JSON, the real error goes to the log only, and a storage outage returns 503 (Microsoft Learn, 2026y; Microsoft Learn, 2026aq).
- **Added** `docker-compose.yml` for Azurite, the Functions and the gateway. Secrets come from a git-ignored `.env` file (Docker, 2026k).
- **Added** Swagger documentation on every endpoint: a summary, parameter text and all the response codes (Microsoft Learn, 2026z).
- **Changed** `docker-compose.yml`: log rotation on all services so container logs cannot fill the server disk (Docker, 2026m).
- **Added** the Linux VPS: an Azure virtual machine with Ubuntu 24.04 running the three services from the Docker Hub `v3.0` images, with a restart policy and only port 5000 public (Docker, 2026n; Microsoft Learn, 2026ar). The steps are in `docs/vps-deployment-runbook.md`.
- **Added** the CI/CD pipeline `.github/workflows/deploy.yml`: on every push to `main` it builds the code, builds and pushes version-tagged images (`v3.0` and `3.0.<run number>`) and deploys them to the VPS over SSH (GitHub, 2026a; GitHub, 2026b; Docker, 2026l; Appleboy, 2026a).
- **Added** the Postman collection **CoffeeNChill Part 3** (78 tests) with environments for Visual Studio, Docker and the VPS (Postman, 2025).

## How Access Is Controlled
A **JWT** is a signed text token. The gateway builds it after a correct login and the client sends it back in the header `Authorization: Bearer <token>`. It carries the user's **Name**, **Email** and **Role** and expires after **2 hours**. A JWT is **signed, not encrypted**: anyone can read what is inside it, but nobody can change it without the secret key, so a changed token is rejected (Microsoft Learn, 2026x).

| Who | What they can do |
|---|---|
| **Public** (no token) | View the menu, place an order, check an order status, register (always as a Customer), log in, health check |
| **Customer** (logged in) | Everything public, plus view and change own profile |
| **Staff** | Everything above, plus change the menu, manage staff documents, list orders and update an order status |
| **Admin** | Everything Staff can do, plus create Staff and Admin accounts, list users and delete users |

| Code | Meaning in this API |
|---|---|
| **401 Unauthorized** | No token, or the token is wrong or expired |
| **403 Forbidden** | The token is valid, but the role is not allowed to use this endpoint |
| **409 Conflict** | That email is already registered |

**Safe registration.** Anyone can register, but without an Admin token the account is always a **Customer**, even if the request asks for another role. Only a logged-in Admin can create Staff or Admin accounts. The first Admin is created at start-up from configuration, so nobody can make themselves an Admin.

**Login does not give hints.** A wrong password and an unknown email both return the same message, `Invalid email or password.`, so nobody can find out which emails are registered.

## Part 3 API Endpoints
Local base URL (Visual Studio): `http://localhost:5100/api`. Swagger: `http://localhost:5100/swagger`. Live and Docker URLs are added in the VPS section.

| Method | Route | Who | Goes to | Success | Errors |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Public (Customer only). Admin token to create Staff or Admin | Users table | 201 | 400, 409 |
| POST | `/api/auth/login` | Public | Users table, returns a JWT | 200 | 400, 401 |
| GET | `/api/health` | Public | the gateway itself | 200 | - |
| GET | `/api/menu` | Public | Menu table | 200 | 503 |
| GET | `/api/menu/category/{category}` | Public | Menu table | 200 | 404 |
| POST | `/api/menu` | Admin, Staff | Menu table | 201 | 400, 401, 403, 409 |
| PUT | `/api/menu/{category}/{sku}` | Admin, Staff | Menu table | 200 | 400, 401, 403, 404 |
| DELETE | `/api/menu/{category}/{sku}` | Admin, Staff | Menu table | 204 | 401, 403, 404 |
| POST | `/api/documents/upload` | Admin, Staff | Blob container `staff-docs` | 201 | 400, 401, 403 |
| GET | `/api/documents` | Admin, Staff | Blob container | 200 | 401, 403 |
| GET | `/api/documents/download/{fileName}` | Admin, Staff | Blob container | 200 | 401, 403, 404 |
| DELETE | `/api/documents/{fileName}` | Admin, Staff | Blob container | 204 | 401, 403, 404 |
| POST | `/api/orders/queue` | Public | Queue `order-processing-queue` | 202 | 400, 503 |
| GET | `/api/orders/{orderId}` | Public | Part 2 Functions (status only) | 200 | 404, 503 |
| GET | `/api/orders` | Admin, Staff | Part 2 Functions (optional `?status=`) | 200 | 400, 401, 403, 503 |
| GET | `/api/orders/{orderDate}/{orderId}` | Admin, Staff | Part 2 Functions | 200 | 401, 403, 404 |
| PUT | `/api/orders/{orderDate}/{orderId}/status` | Admin, Staff | Part 2 Functions | 200 | 400, 401, 403, 404 |
| GET | `/api/profile/mine` | Any logged-in user | Users table | 200 | 401 |
| PUT | `/api/profile/mine` | Any logged-in user | Users table | 200 | 400, 401, 409 |
| GET | `/api/users` (optional `?role=`) | Admin | Users table | 200 | 400, 401, 403 |
| DELETE | `/api/users/{email}` | Admin | Users table | 204 | 400, 401, 403, 404 |

## Users Table
| Column | Value |
|---|---|
| PartitionKey | the role: `Admin`, `Staff` or `Customer` |
| RowKey | the email address in lower case |
| PasswordHash | the BCrypt hash (starts with `$2a$` or `$2b$`). The real password is never stored |
| FullName | the person's name |
| CreatedDate | when the account was created (UTC) |

## Configuration
Values come from `appsettings.json`, `appsettings.Development.json` (kept out of Git) or environment variables. In Docker and on the VPS a double underscore replaces the colon, for example `Jwt__Key` (Microsoft Learn, 2026ac).

| Key | Example | Notes |
|---|---|---|
| `AzureStorageConnectionString` | `UseDevelopmentStorage=true` | In Docker it points to the `azurite` service |
| `Jwt:Key` | at least 32 characters | **Secret.** Never committed. The app refuses to start without it |
| `Jwt:Issuer` / `Jwt:Audience` | `CoffeeNChillGateway` / `CoffeeNChillUsers` | Checked on every request |
| `Jwt:ExpiryHours` | `2` | Token lifetime |
| `Functions:BaseUrl` | `http://localhost:7071/api` (Visual Studio) | The Part 2 Functions app |
| `SeedAdmin:Email`, `Password`, `FullName` | from settings | Creates the first Admin if none exists |
| `Swagger:Enabled` | `true` | Shows Swagger, also when deployed |

## Demo Accounts
| Role | Email | Password |
|---|---|---|
| Customer | `student@coffeenchill.ac.za` | `Student#2026` |
| Staff | `barista@coffeenchill.ac.za` | `Barista#2026` |

The Admin account is created from settings at start-up. **Its password is given only in the submission document**, not in this public README.

## Running Part 3 Locally (Visual Studio)
1. Start the storage and the Functions: `docker compose up azurite functions -d` (Docker Desktop must be running).
2. Create `src/CoffeeNChill.Gateway/appsettings.Development.json` with a `Jwt:Key` of at least 32 characters, the `SeedAdmin` values and `"Swagger": { "Enabled": true }`. This file is in `.gitignore` on purpose, so the secret key is never public.
3. Open `CoffeeNChill.slnx`, set `CoffeeNChill.Gateway` as the startup project, choose the **http** profile and press F5. Swagger opens at `http://localhost:5100/swagger`.
4. **Log in:** run `POST /api/auth/login`, copy only the `token` text, click **Authorize**, paste it and click **Authorize**. The padlocks close and protected endpoints work.

## Part 3 Feature Descriptions
### JWT Authentication, Users and Roles (Kandyce)
The gateway keeps its users in an Azure Table called `Users`, with the role as the PartitionKey and the lower-case email as the RowKey. A person registers with a name, an email and a password. The password is hashed with BCrypt, which adds a random salt and is slow on purpose, and only the hash is saved (BcryptNet, 2026a). On login the typed password is checked against the stored hash and, when it matches, `JwtTokenService` builds a `JwtSecurityToken` with the claims Name, Email and Role, signs it with HMAC-SHA256 and sets a 2-hour expiry (Microsoft Learn, 2026ah). The JWT bearer middleware checks the signature, issuer, audience and expiry on every request, and `[Authorize(Roles = "...")]` checks the role (Microsoft Learn, 2026x; Microsoft Learn, 2026af). Anyone may register, but an anonymous caller only ever becomes a Customer. Only an Admin token can create Staff or Admin accounts, and the first Admin is seeded at start-up by a hosted service (Microsoft Learn, 2026ab). The 401 and 403 answers are returned as JSON, not an empty page. Request data is checked with data annotations, and every validation problem is returned in one list (Microsoft Learn, 2026ad).
I also wrote the CI/CD pipeline and the Postman collection. The pipeline is a GitHub Actions workflow that builds the code, builds and pushes version-tagged Docker images and deploys them to the VPS over SSH with a key pair made only for the pipeline, so no password is stored anywhere (GitHub, 2026a; GitHub, 2026b; Docker, 2026l; Appleboy, 2026a). The Postman collection logs in once per role, stores the tokens in collection variables and sends them as a Bearer token, so the same requests test the gateway on my PC, in Docker and on the VPS (Postman, 2025).

### Menu and Staff Documents (Kristen)
<!-- Kristen  -->

### Orders, Docker and the VPS (Thami)
<!-- Thami  -->

## Part 3 Testing
The Part 3 Postman collection is in `docs/Postman Collection/Part 3/`: the collection **CoffeeNChill Part 3** (78 tests) and three environments. Individual member collections are in `docs/Postman Collection/Part 3/Individual Member Tests/`. Every request has a saved test script that follows Postman's test-script pattern (Postman, 2025). The collection sends the JWT as a Bearer token: folder **0 Setup** logs in as Admin, creates the Staff and Customer demo accounts if they are missing, and saves one token per role.

| Environment | `baseUrl` | Use it when |
|---|---|---|
| Local Gateway (Visual Studio) | `http://localhost:5100/api` | the gateway runs from Visual Studio and `docker compose up azurite functions -d` is running |
| Local Gateway (Docker) | `http://localhost:5000/api` | the whole stack runs from the Docker Hub images (`docker compose up -d`) |
| VPS Live | `http://20.197.62.154:5000/api` | the live server |

| Folder | Tests | What it proves |
|---|---|---|
| 0 Setup | 5 | Admin login, Staff and Customer accounts created, three tokens saved |
| 1 Auth | 13 | register (201, 409, 400, role stays Customer), login (200 and the safe 401), profile, user list (200, 403, 401) |
| 2 Menu | 12 | public read, create, duplicate, invalid, update, delete, with 401 and 403 |
| 2 Documents | 9 | upload, wrong type, list, download, delete, with 401 and 403 |
| 3 Orders | 14 | place an order (202), validation, status, update rules, list with 401 and 403 |
| 4 Deployment checks | 9 | health, Swagger page, OpenAPI file, JSON errors for 401, 404 and 405, a real JWT, role refusal |
| 5 RBAC matrix | 16 | the same four protected requests called with no token, Customer, Staff and Admin |

**How to run:**
1. Import the collection and the three environments (Import button in Postman).
2. Choose an environment. For **VPS Live** change `baseUrl` and `siteUrl` to the server address, and type the Admin password (it is in the submission document, not in this README) into the **Current value** of `adminPassword`.
3. In **2 Documents** open requests `D3` and `D4`, Body tab, and select a file again (Postman does not export attached files): a `.pdf` for D3 and the `.txt` for D4 from `docs/staff-docs_TestDocuments/`.
4. Run the collection with the Runner and a **delay of 3000 ms**. Expected: **78 passed, 0 failed**.
5. The 90 second order life cycle (`Received`, `Preparing`, `Ready`, `Collected`) is checked by hand, not in the run.

## CI/CD Pipeline (bonus)
The workflow `.github/workflows/deploy.yml` runs on every push to `main` (a change that only touches `.md` files or `docs/` does not start it). It follows the five steps of the brief (GitHub, 2026a):

| Step | What happens | Piece used |
|---|---|---|
| 1 | Check out the code and install the .NET 9 SDK | `actions/checkout`, `actions/setup-dotnet` |
| 2 | Build the gateway and the Functions (the run fails here if the code does not compile) | `dotnet build --configuration Release` |
| 3 | Set up Docker Buildx | `docker/setup-buildx-action` (Docker, 2026l) |
| 4 | Log in to Docker Hub and push the gateway and Functions images with two tags: `v3.0` and `3.0.<run number>` | `docker/login-action`, `docker/build-push-action` (Docker, 2026l) |
| 5 | SSH into the VPS and run `docker compose pull`, `docker compose up -d` | `appleboy/ssh-action` (Appleboy, 2026a) |

The secrets `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN`, `VPS_HOST`, `VPS_USER` and `VPS_SSH_KEY` are stored in the repository's GitHub secrets and never in the code (GitHub, 2026b). The SSH key is a dedicated key pair made only for the pipeline. The runs are listed on the repository's **Actions** tab. There is no unit-test step because the project has no unit tests.

---

# Part 2 — Queue Triggers and Docker Compose (v2.0)

## What the System Does
Part 2 adds **asynchronous order processing**. A customer's order is validated and placed on an Azure Storage Queue, and a background function picks it up, saves it in an `Orders` table and moves it through its life cycle (Microsoft Learn, 2026g). Putting a queue between the customer and the kitchen means the customer gets an instant reply and orders are handled one by one instead of crashing the system at peak times (Mrzyglod and Camden, 2020). The whole stack starts with one `docker compose up` using version-tagged images pulled from Docker Hub (Docker, 2026f).

1. **SubmitOrder** (HTTP) checks the order, builds the JSON message, encodes it as Base64 and puts it on `order-processing-queue`. It answers **202 Accepted** straight away.
2. **ProcessOrderQueue** (queue trigger) decodes the Base64 back to JSON, reads it into an order, checks it again and saves it to the `Orders` table with status `Received`.
3. The same function then waits 30 seconds between each status and updates the table row: `Received` → `Preparing` → `Ready` → `Collected`. Every step is written to the log.
4. If a message cannot be processed it is retried and, after 5 attempts, Azure moves it to `order-processing-queue-poison`.

## Changelog (Part 2)
- **Added** `POST /api/orders/queue` (`SubmitOrder`): validation, JSON message, Base64 encoding, UTC timestamp and order number, 202 Accepted, error handling for an unavailable queue.
- **Added** `ProcessOrderQueue`: queue-triggered function with Base64 decoding, saving to the `Orders` table and the 30-second status life cycle.
- **Added** `GET /api/orders`, `GET /api/orders/{orderDate}/{orderId}`, `GET /api/orders/{orderId}` (status only) and `PUT /api/orders/{orderDate}/{orderId}/status`.
- **Added** poison queue handling (`order-processing-queue-poison`) with `maxDequeueCount` set to 5 in `host.json`, and an optional `ENABLE_POISON_TEST` switch.
- **Added** `docker-compose.yml`: Azurite and Functions, custom bridge network `coffeenchill-net`, volume `azurite-data`, Docker Hub `v2.0` images, API on port **7071**.
- **Added** DTO classes in the `DTOs` folder (requests, responses and mappers) and sample JSON payloads in `docs/sample-payloads/`.
- **Added** a new Postman collection and a new `baseUrl` environment for port 7071.
- **Changed** `host.json`: `messageEncoding` is `none` (Base64 is done in our code), `maxPollingInterval` 2 seconds, `maxDequeueCount` 5.
- **Changed** code-comment attributions standardised to Harvard referencing.
- **Removed** the unused `Function1` template.
- **Kept** all Part 1 endpoints working and unchanged.

## Architecture
```mermaid
flowchart LR
  P[Postman or client] -->|POST /api/orders/queue| S[SubmitOrder]
  S -->|JSON then Base64| Q[(order-processing-queue)]
  Q -->|queue trigger| T[ProcessOrderQueue]
  T -->|Received, Preparing, Ready, Collected| O[(Orders table)]
  Q -.->|fails 5 times| X[(order-processing-queue-poison)]
  P -->|GET and PUT| G[GetOrders, GetOrderById, GetOrderStatus, UpdateOrderStatus]
  G --> O
  subgraph Docker Compose on network coffeenchill-net
    S
    T
    G
    A[Azurite: blob 10000, queue 10001, table 10002]
  end
```
`docker compose up` starts two containers, `azurite` and `functions`, on the network `coffeenchill-net`. The functions container reaches Azurite by the service name `azurite`.

## Prerequisites
- Docker Desktop
- Postman
- Git
- Azure Storage Explorer (to show the queue and the Orders table)
- Visual Studio 2022 (Azure development workload) and the .NET 9 SDK, only to run from source

## Quick Start (one command)
```text
git clone https://github.com/ST10467645/CLDV6212_Part-2-g1-CoffeeNChill-ArcGroup7.git
cd CLDV6212_Part-2-g1-CoffeeNChill-ArcGroup7
docker compose up
```
1. Docker pulls both `v2.0` images from Docker Hub (nothing is built locally), creates the network and volume and starts the stack. A 404 from Azurite in the first seconds is normal.
2. Wait until the functions log lists the functions and says the host has started.
3. The API is at **`http://localhost:7071/api`**. Visual Studio must **not** be running.
4. Stop with `Ctrl+C`, then `docker compose down`. Add `-v` only if you want to delete the stored Azurite data.
5. If a port is busy, run `docker ps -a` and remove old CoffeeNChill containers with `docker stop` and `docker rm`.

Connect Azure Storage Explorer with **Local & attached > Storage Accounts > Emulator - Default Ports (Key)** to see the `order-processing-queue` queue and the `Orders` table that the containers are using.

## What Is In docker-compose.yml
| Setting | Value | Why |
|---|---|---|
| `image` | `st10482062/coffeenchill-functions:v2.0` and `st10482062/coffeenchill-azurite:v2.0` | Compose pulls the exact published version from Docker Hub (Docker, 2026f) |
| `build` | not used | The images come from Docker Hub, nothing is built on the marker's machine |
| `ports` | `7071:80` (functions), `10000`, `10001`, `10002` (Azurite) | 7071 on the computer maps to port 80 inside the functions container. Azurite uses blob 10000, queue 10001, table 10002 |
| `networks` | custom bridge `coffeenchill-net` | The containers find each other by service name (`azurite`), not `localhost` (Docker, 2026e) |
| `volumes` | `azurite-data:/data` | Queue and table data survive `docker compose down` (Docker, 2026g) |
| `environment` | `AzureWebJobsStorage` (points to `azurite`), `STATUS_STEP_DELAY_SECONDS` = 30, `ENABLE_POISON_TEST` = false | Connection string, seconds between statuses, and the optional failure switch |
| `depends_on` | `azurite` | Azurite is started before the functions |
| `restart` | `unless-stopped` | The functions container retries if Azurite is not ready yet |

The images were built from the multi-stage `Dockerfile`, tagged `v2.0` and pushed to Docker Hub (Docker, 2026c; Docker, 2026d; Docker, 2026h; Microsoft Learn, 2026b). The commands are in `docker-commands.md`.

## Docker Hub Images
- Functions: https://hub.docker.com/r/st10482062/coffeenchill-functions (`v1.0`, `v2.0`)
- Azurite: https://hub.docker.com/r/st10482062/coffeenchill-azurite (`v1.0`, `v2.0`)

## Part 2 API Endpoints
The base URL is `http://localhost:7071/api`, both in Docker and when running from Visual Studio (never run both at once).

| Method | Route | Description | Success | Errors |
|---|---|---|---|---|
| POST | `/api/orders/queue` | Validate an order and place it on `order-processing-queue` | 202 | 400, 503, 500 |
| GET | `/api/orders` | List orders (optional `?status=Received`) | 200 | 400, 500 |
| GET | `/api/orders/{orderDate}/{orderId}` | Get one order from the table | 200 | 404 |
| GET | `/api/orders/{orderId}` | Get only the order id and current status | 200 | 404 |
| PUT | `/api/orders/{orderDate}/{orderId}/status` | Move an order to the next status (`{"Status":"Preparing"}`) | 200 | 400, 404 |
| Queue | `ProcessOrderQueue` | Saves queued orders to `Orders` and tracks the status | - | poison queue |

Example request body (more in `docs/sample-payloads/`):
```json
{
  "CustomerName": "Jane Smith",
  "SelectedItemSKUs": ["COF-001", "PAS-104"],
  "TotalPrice": 65.00
}
```
`OrderId` and `OrderTimestamp` may be left out. The server then generates the order number (`ORD-2026-XXXXXX`) and the UTC timestamp.

## How Bad or Incomplete Data Is Handled
| What the client sends | What the system does |
|---|---|
| Empty body or text that is not JSON | 400 Bad Request, nothing is queued |
| Customer name missing or blank | 400 with an `errors` list, nothing is queued |
| No items, or a blank item SKU | 400 with an `errors` list, nothing is queued |
| Total price 0 or negative | 400 with an `errors` list, nothing is queued |
| Several problems at once | One 400 that lists all of them |
| Valid order | 202 Accepted with the order id and date, the message is queued |
| Queue or Azurite not reachable | The error is caught and logged, the reply is 503 Service Unavailable within a few seconds (500 for any other unexpected error), the app does not crash |
| A bad message reaches the queue directly (for example added by hand) | The trigger tries 5 times, then Azure moves it to `order-processing-queue-poison`. The app keeps running |
| PUT with an unknown status word, or skipping a step | 400 Bad Request |
| PUT or GET for an order that does not exist | 404 Not Found |

## Queue Settings (host.json)
```json
{
  "version": "2.0",
  "extensions": {
    "queues": {
      "messageEncoding": "none",
      "maxPollingInterval": "00:00:02",
      "maxDequeueCount": 5
    }
  }
}
```
- `messageEncoding: none`: the queue stores exactly the text we send. The Base64 step is done in our own code, so it can be shown and explained.
- `maxPollingInterval`: the trigger checks the queue every 2 seconds at most, so orders start quickly.
- `maxDequeueCount: 5`: after 5 failed attempts a message goes to the poison queue (Microsoft Learn, 2026l).

## JSON and Base64
The producer turns the order into a JSON string and then into Base64 text before queueing it (Microsoft Learn, 2026w). The trigger turns the Base64 text back into bytes and then into the JSON string before reading it into an order (Microsoft Learn, 2026o; Microsoft Learn, 2026p; Microsoft Learn, 2026s). **Base64 is an encoding, not encryption.** It only changes how the data is written so it can be stored and sent safely. Anyone can decode it.

## Order Status Life Cycle
`Received` → `Preparing` → `Ready` → `Collected`. The function waits **30 seconds** between statuses to simulate the kitchen (the setting is `STATUS_STEP_DELAY_SECONDS`, default 30). The lecturer said a timer can be used; we used an asynchronous `Task.Delay`, which does the same job without blocking the thread (Microsoft Learn, 2026j). The sample payloads are in `docs/sample-payloads/`. The stages are visible in the container log and in the `Orders` table (PartitionKey = order date `yyyy-MM-dd`, RowKey = order id).

## Part 2 Feature Descriptions
### Order Queue Producer (Kandyce)
The order producer is the first half of the asynchronous ordering workflow. `SubmitOrder` is an HTTP-triggered function that accepts a new order as JSON at `POST /api/orders/queue` (Microsoft Learn, 2025b). Before anything is queued, it checks that the body is valid JSON, that the customer name is filled in, that at least one item is selected with no blank SKUs and that the total price is above zero. If anything is wrong, a 400 response lists every problem at once so the client can fix them in one go. When the client leaves them out, the server generates the order number and a UTC timestamp. The validated order is serialised to a JSON string and encoded to Base64 in the code, then sent to `order-processing-queue` with the Azure Storage Queues client library (Microsoft Learn, 2026q; Microsoft Learn, 2026w). `host.json` uses `messageEncoding: none` so that the queue stores exactly the Base64 text we send. If the queue cannot be reached, the function catches the error (Microsoft Learn, 2026r), logs it and returns a 503 Service Unavailable within a few seconds instead of crashing. On success it answers 202 Accepted straight away, because the order is processed later in the background (IIE, 2026). `GetOrders`, `GetOrderById` and `GetOrderStatus` read the `Orders` table and return a clean response object (DTO), with a 404 for an unknown order and a 400 for an invalid status filter (Microsoft Learn, 2026c).

### Queue Trigger and Order Tracking (Kristen)
`ProcessOrderQueue` is the second half of the workflow and runs automatically whenever a message arrives on `order-processing-queue` (Microsoft Learn, 2026g). It decodes the Base64 message back to JSON (Microsoft Learn, 2026o; Microsoft Learn, 2026p), reads it into an `Order` object (Microsoft Learn, 2026s) and saves an entry in the `Orders` table with the order date as the PartitionKey, the OrderId as the RowKey and the status `Received` (Microsoft Learn, 2026c; Microsoft Learn, 2026k). It then simulates the barista life cycle by waiting 30 seconds between statuses (Microsoft Learn, 2026j; Microsoft Learn, 2026m; Microsoft Learn, 2026n) and moving the order through `Preparing` and `Ready` to `Collected`, updating the table row at each step and logging every change. A `PUT` endpoint, `UpdateOrderStatus`, lets staff move an order forward one step at a time and returns a 400 for an invalid or skipped status and a 404 for an unknown order (Microsoft Learn, 2026t). If a message cannot be processed, the error is logged and the exception is rethrown, so after 5 attempts the runtime moves the message to `order-processing-queue-poison` instead of blocking the queue (Microsoft Learn, 2026l).

### Docker Compose Orchestration (Thami)
One `docker-compose.yml` in the repository root starts the Azurite emulator and the Functions app together with a single `docker compose up` (Docker, 2026f). Both services use version-tagged `v2.0` images from Docker Hub, so a clean machine only needs Docker to run the whole system. The services share a custom bridge network so the Functions container reaches Azurite by the service name `azurite` rather than `localhost` (Docker, 2026e), and a named volume keeps Azurite's data when the containers are removed (Docker, 2026g). The images were built from the multi-stage Dockerfile, tagged `v2.0` and pushed to Docker Hub (Docker, 2026c; Docker, 2026d; Docker, 2026h; Microsoft Learn, 2026b). I also added the Docker checks to the Postman collection, which run with Visual Studio closed.

## Part 2 Testing
The Part 2 Postman collection is in `docs/Postman Collection/Part 2/`. It is a separate collection from Part 1: the collection **CoffeeNChill Part 2** and the environment **Docker Compose (7071)**, whose `baseUrl` is `http://localhost:7071/api`. Individual member test collections are in `docs/Postman Collection/Part 2/Individual Member Tests/`.

1. Start the stack with `docker compose up` and close Visual Studio.
2. In Postman import the collection and the environment, and select **Docker Compose (7071)**.
3. Run the collection with the Runner (delay 3000 ms). Every request has a saved test script that follows Postman's test-script pattern (Postman, 2025).
4. Open Azure Storage Explorer and check the `Orders` table: the new order moves through the four statuses.

| Folder | What it tests |
|---|---|
| Queue Producer | valid order (202) and invalid orders: missing name, empty items, price 0 (400) |
| Order Tracking | order saved in the table, `GET` by id and by status |
| Status Updates | `PUT` next status (200), skipped step (400), unknown word (400), unknown order (404) |
| Manual checks | final status is `Collected` about 100 seconds after the order (not part of the automatic run) |
| Docker Checks | the same tests against the Docker Compose stack with Visual Studio closed |
| Poison queue | see below (manual) |

**Poison queue demonstration.** Because the app works, no message fails and `order-processing-queue-poison` does **not exist yet**: Azure creates it only when a message fails. To prove it works, open the `order-processing-queue` queue in Azure Storage Explorer, choose **Add Message** and type `this is not valid JSON or Base64`. The trigger fails 5 times (visible in the log) and the message appears in the new `order-processing-queue-poison` queue. `SubmitOrder` blocks bad data with a 400, so adding the message by hand is how we test the trigger's own defence. (A second method: set `ENABLE_POISON_TEST` to `true` for the `functions` service, run `docker compose up -d`, place an order, then set it back to `false`.)

**Seeing a message wait in the queue (Visual Studio only):** add `"AzureWebJobs.ProcessOrderQueue.Disabled": "true"` to `Values` in `local.settings.json`, run the app, place an order and open the queue in Storage Explorer. Remove the line afterwards.

**Evidence:** screenshots are in `docs/screenshots_part2/` (one folder per member) and in the submission document.

## Part 2 Video and Document
- Video: https://youtu.be/iP3w9mEHbPM (shows `docker compose up`, the application starting, messages moving through the queue into the Orders table, and the Postman tests running)
- Submission document: [CLDV6212 CoffeeNChill Part 2.pdf](docs/Final%20Submission%20Document/Part2/CLDV6212%20CoffeeNChill%20Part%202.pdf)

### Video title cards
The video opens and closes with an image and each presenter is introduced by a title card. All five images are AI-generated (see the image credits under AI Disclosure).

| Intro | Outro |
|---|---|
| ![Video intro image (generated with ChatGPT)](docs/video-assets/Part2_Intro_Image.png) | ![Video outro image (generated with ChatGPT)](docs/video-assets/Outropng.png) |

| Kandyce | Thami | Kristen |
|---|---|---|
| ![Kandyce title card (generated with Claude)](docs/video-assets/Part2_Card_IntroToKandyce.png) | ![Thami title card (generated with Claude)](docs/video-assets/Part2_Card_IntroToThami.png) | ![Kristen title card (generated with Claude)](docs/video-assets/Part2_Card_IntroToKristen.png) |

*Intro and outro images: generated with ChatGPT (OpenAI). Presenter title cards: generated with Claude (Anthropic). Prompts written by the team.*

Set the video quality to 1080p HD in YouTube's settings; it defaults to a lower resolution.

---

## Running From Source (optional)
1. Start Azurite in Docker:
```text
docker compose up azurite -d
```
2. Make sure `local.settings.json` is in the project root with `"AzureWebJobsStorage": "UseDevelopmentStorage=true"` and `"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"`.
3. Open the solution in Visual Studio and press Ctrl+F5. The API is then at `http://localhost:7071/api`.

Never run Visual Studio and the full `docker compose up` at the same time. Both would read the same queue and the Azurite ports would clash.

## Project Structure
```text
CoffeeNChill (repository root)
├── .github/workflows/deploy.yml        CI/CD pipeline (bonus)
├── src/
│   ├── CoffeeNChill.Gateway/           Part 3 API gateway
│   │   ├── Controllers/                Auth, Profile, Users, Menu, Documents, Orders, Health
│   │   ├── DTOs/                       request and response classes
│   │   ├── Middleware/                 GlobalExceptionMiddleware.cs
│   │   ├── Models/                     UserEntity, MenuItemEntity
│   │   ├── Services/                   JwtTokenService, PasswordHasher, AdminSeeder, UserStore, FunctionsClient
│   │   ├── Dockerfile  Program.cs  appsettings.json  CoffeeNChill.Gateway.csproj
│   └── CoffeeNChill.Functions/         Part 2 Azure Functions (unchanged)
│       ├── DTOs/  Functions/  Models/
│       ├── Dockerfile  host.json  Program.cs  CoffeeNChillFunctions.csproj
├── docs/
│   ├── Postman Collection/             Part 1, Part 2, Part 3 (with Individual Member Tests/)
│   ├── Final Submission Document/      Part 1, Part 2 and Part 3 PDFs
│   ├── sample-payloads/  staff-docs_TestDocuments/  video-assets/
│   └── vps-deployment-runbook.md       how the VPS was built and updated
├── docker-compose.yml                  Azurite, Functions and gateway from Docker Hub v3.0
├── .env.example                        names of the secret settings (the real .env is never committed)
├── docker-commands.md
├── .mailmap                            maps Thami's Git identities to one name
├── .gitignore
├── CoffeeNChill.slnx
└── README.md
```
`bin/` and `obj/` are build output and are excluded through `.gitignore`. Every source file starts with an attribution comment that uses the same reference letters as the list below.
`bin/` and `obj/` are build output and are excluded through `.gitignore`. Every source file starts with an attribution comment that uses the same reference letters as the list below; the full attribution register is in the submission document.

## Important Notes
- **Storage:** the brief mentions Azure File Share. On lecturer guidance, staff documents use Azure Blob Storage (container `staff-docs`) because File Share emulation is not reliably supported in Azurite.
- **.NET version:** the project was scaffolded on .NET 10 and downgraded to .NET 9 for compatibility with the official Azure Functions Docker base images.
- **Ports:** Azurite serves Blob on 10000, Queue on 10001 and Table on 10002. The API is on 7071 both in Docker and in Visual Studio, so never run both at once.
- **Secrets:** the only connection string in the repository is the public Azurite development key, which is the same on every machine.
- **HTTP only (Part 3):** the live gateway is served over HTTP on port 5000 (no certificate), so a token could be read on the network. A real system would add HTTPS with a domain name and a reverse proxy. This is a known limitation of this POE.
- **Storage on the VPS:** the VPS uses the Azurite emulator inside Docker, as in Parts 1 and 2, not a paid Azure Storage account. Its data lives in the Docker volume `azurite-data`.
- **Secrets:** the real JWT key and Admin password exist only in the `.env` file on the VPS and in the submission document. Only `.env.example` is committed.

## AI Disclosure
AI tools were used during this project:
- **Claude (Anthropic)** was used to generate the three presenter title cards shown in the video (see the image credits below).
- **ChatGPT (OpenAI)** was used to generate the video intro and outro images.
**Part 3:**
- It also reviewed our drafts (for example the reference list and the README) and helped write Postman tests.
- No AI voices are used in the video. Every member speaks in their own voice.
- <the Part 3 video images:  ChatGPT (OpenAI) made them, OCT and Kandyce wrote the prompt, as in the Part 2 table>

### Image credits (AI-generated images)
| Image (file in `docs/video-assets/`) | Where it is used | Generated with | Month | Prompt written by |
|---|---|---|---|---|
| `Part2_Intro_Image.png` | Start of the video | ChatGPT (OpenAI) + Claude (Anthropic) | October 2026 | Kandyce Smit |
| `Outropng.png` | End of the video | ChatGPT (OpenAI) | October 2026 | Kandyce Smit |
| `Part2_Card_IntroToKandyce.png` | Title card before Kandyce's producer segment: pink coffee-cup mascot with envelopes for the Base64 queue messages | Claude (Anthropic) | October 2026 | Kandyce Smit |
| `Part2_Card_IntroToThami.png` | Title card before Thami's Docker segment: blue coffee-cup mascot with a whale carrying containers for the v2.0 Docker Hub images | Claude (Anthropic) | October 2026 | Kandyce Smit |
| `Part2_Card_IntroToKristen.png` | Title card before Kristen's trigger segment: green coffee-cup mascot with the four status steps, Received to Collected, 30 seconds apart | Claude (Anthropic) | October 2026 | Kandyce Smit |

The images are decorative video graphics. They contain no code or results, and the text on the cards (names, student numbers, roles) was checked against the contributions table above. They are credited here and not in the reference list because they are our own AI-generated material and not published sources.

---

# Part 1 — Menu, Staff Documents and Docker (v1.0)

## Project Overview
CoffeeNChill is a campus canteen that struggles with queues during lecture breaks. Part 1 replaced the paper menu and filing cabinet with Azure Table Storage, Azure Blob Storage and HTTP-triggered Azure Functions running against the Azurite emulator (Microsoft Learn, 2022; Microsoft Learn, 2025a; Microsoft Learn, 2025b; Microsoft Learn, 2026e).

## Part 1 Features
### Menu Management (Kandyce)
The Menu Management feature uses Microsoft Azure's Table Storage to digitalise CoffeeNChill's chalkboard menu for their coffee shop. With five HTTP triggered Azure Functions that handle full CRUD operations of the menu items (creating, reading, updating, and deleting): create a new menu item (Microsoft Learn, 2025b), read all available menu items (Microsoft Learn, 2026c), read only specific menu items by category (Microsoft Learn, 2022), and updating and deleting menu items (Microsoft Learn, 2026c). This storage system stores each menu item with its category as the Partition Key and a unique SKU as the Row Key. In addition, it includes name, description, price and status fields for each menu item. Validation logic has been implemented in each function to validate the input data when creating a new record (validation for bad request 400 when missing data exists). Also, there have been existence checks included for update and delete operations to prevent unexpected errors from occurring (return a 404 Not Found instead of an unhandled exception).

### Staff Documents (Kristen)
The Staff Documents feature allows the canteen staff to upload, view and download important documents using Azure Blob Storage. When a file is uploaded the system checks that it's either a PDF, PNG or JPEG before storing it, so other file types can't be uploaded by mistake (Microsoft Learn, 2026f). The content type is stored with each file so that downloads report the correct file type (Microsoft Learn, 2026u; Microsoft Learn, 2026v). All the uploaded documents are stored in a container called "staff-docs" and users can list every file that's currently stored, along with its name, size and the date it was last modified (Microsoft Learn, 2026e). Files are also downloaded using streaming, which sends the file to the user instead of loading the whole thing into memory first making it more efficient for larger files (Microsoft Learn, 2026d). Each function also includes error handling and logging so any problems such as a missing file or a storage connection issue are caught and logged instead of crashing the app.

### Docker (Thami)
My contribution to Part 1 of the POE was the Docker setup. I wrote a multi-stage Dockerfile for the Azure Functions project, the beginning stage used the full .NET 9 SDK image to compile and publish the code, the second stage then copied only the compiled output into the lightweight Azure Functions runtime image, keeping the final image small and clean (Microsoft Learn, 2026a) (Microsoft Learn, 2026b). I also built and tagged both images with version tag v1.0 and published them directly to Docker Hub under the account st10482062: "coffeenchill-functions" for the Azure Functions application and "coffeenchill-azurite" as a re-tagged instance of the official Azurite image (Docker, 2026a), (Docker, 2026b), (Docker, 2026c), (Docker, 2026d). Azurite also runs in its own standalone Docker container with the "--skipApiVersionCheck" flag bound to ports 10000, 10001, and 10002, and both containers communicate with each other over a shared Docker network called "coffeenchill-net" (Microsoft Learn, 2025a), (Microsoft Learn, 2026b).

## Part 1 API Endpoints
### Menu (Azure Table Storage)
| Method | Route | Description |
|---|---|---|
| POST | /api/menu | Create a new menu item |
| GET | /api/menu | Get all menu items |
| GET | /api/menu/category/{category} | Get menu items by category |
| PUT | /api/menu/{category}/{id} | Update a menu item |
| DELETE | /api/menu/{category}/{id} | Delete a menu item |

### Staff Documents (Azure Blob Storage)
| Method | Route | Description |
|---|---|---|
| POST | /api/documents/upload | Upload a staff document (multipart/form-data) |
| GET | /api/documents | List staff documents with metadata |
| GET | /api/documents/download/{fileName} | Download a staff document |

---

## Part 1 Docker Images (v1.0)
Built, tagged and pushed to public Docker Hub repositories (Docker, 2026a; Docker, 2026b). Part 1 runs on port **7071** (`http://localhost:7071/api`).
- Functions: https://hub.docker.com/r/st10482062/coffeenchill-functions?tag=v1.0 (`v1.0`)
- Azurite: https://hub.docker.com/r/st10482062/coffeenchill-azurite?tag=v1.0 (`v1.0`)

## Part 1 Testing
Postman collections and the environment are in `docs/Postman Collection/Part 1/`: `CoffeeNChill Part 1 - Successful Tests`, `CoffeeNChill Part 1 - Invalid Tests` and the `Local Azurite` environment (`baseUrl` = `http://localhost:7071/api`). Import them, select **Local Azurite** and run each collection. Every request has a saved test script that follows Postman's test-script pattern (Postman, 2025).

Sample staff documents are in `docs/staff-docs_TestDocuments/` (three PDFs, an allowed type, and one `.txt`, which is not an allowed upload type). Postman does not export attached files, only a local path, so re-select a file in the Body tab of the Upload request before running it.

## Part 1 Video and Document
- Video: https://youtu.be/suzi50aHtrU?si=HFO_WMRrx3LRTdMr
- Submission document: [CLDV6212_Part_1_Final_Submission.pdf](docs/Final%20Submission%20Document/CLDV6212_Part_1_Final_Submission.pdf)

---

## References
Appleboy, 2026a. SSH Remote Commands (ssh-action). [Online]
Available at: https://github.com/marketplace/actions/ssh-remote-commands
[Accessed 26 October 2026].

BcryptNet, 2026a. BCrypt.Net - Bringing updates to the original bcrypt package. [Online]
Available at: https://github.com/BcryptNet/bcrypt.net
[Accessed 20 October 2026].

Docker, 2026a. Build and push your first image. [Online]
Available at: https://docs.docker.com/get-started/introduction/build-and-push-first-image/
[Accessed 04 September 2026].

Docker, 2026b. Docker Hub quickstart. [Online]
Available at: https://docs.docker.com/docker-hub/quickstart/
[Accessed 04 September 2026].

Docker, 2026c. Push images to a repository. [Online]
Available at: https://docs.docker.com/docker-hub/repos/manage/hub-images/push/
[Accessed 04 September 2026].

Docker, 2026d. Build, tag, and publish an image. [Online]
Available at: https://docs.docker.com/get-started/docker-concepts/building-images/build-tag-and-publish-an-image/
[Accessed 04 September 2026].

Docker, 2026e. Define and manage networks in Docker Compose. [Online]
Available at: https://docs.docker.com/reference/compose-file/networks/
[Accessed 29 September 2026].

Docker, 2026f. Compose file reference. [Online]
Available at: https://docs.docker.com/reference/compose-file/
[Accessed 29 September 2026].

Docker, 2026g. Define and manage volumes in Docker Compose. [Online]
Available at: https://docs.docker.com/reference/compose-file/volumes/
[Accessed 29 September 2026].

Docker, 2026h. Dockerfile reference. [Online]
Available at: https://docs.docker.com/reference/dockerfile/
[Accessed 09 October 2026].

Docker, 2026j. Multi-stage builds. [Online]
Available at: https://docs.docker.com/build/building/multi-stage/
[Accessed 20 October 2026].

Docker, 2026k. Set, use, and manage variables in a Compose file with interpolation. [Online]
Available at: https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/
[Accessed 22 October 2026].

Docker, 2026l. Docker Build GitHub Actions. [Online]
Available at: https://docs.docker.com/build/ci/github-actions/
[Accessed 26 October 2026].

Docker, 2026m. JSON File logging driver. [Online]
Available at: https://docs.docker.com/engine/logging/drivers/json-file/
[Accessed 26 October 2026].

Docker, 2026n. Install Docker Engine on Ubuntu. [Online]
Available at: https://docs.docker.com/engine/install/ubuntu/
[Accessed 26 October 2026].

GitHub, 2026a. Workflow syntax for GitHub Actions. [Online]
Available at: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax
[Accessed 26 October 2026].

GitHub, 2026b. Using secrets in GitHub Actions. [Online]
Available at: https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-secrets
[Accessed 26 October 2026].

IIE, 2026. CLDV6212 and PROG6212 lecturer course examples, notes and POE brief (Parts 1, 2 and 3): Azure Functions, Table and Blob Storage, queue triggers and Docker Compose. [Lecture notes]. Cloud Development B. The Independent Institute of Education.

Microsoft Learn, 2022. What is Azure Table storage? [Online]
Available at: https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview
[Accessed 04 September 2026].

Microsoft Learn, 2025a. Install and run Azurite emulator. [Online]
Available at: https://learn.microsoft.com/en-us/azure/storage/common/storage-install-azurite
[Accessed 04 September 2026].

Microsoft Learn, 2025b. Azure Functions HTTP trigger. [Online]
Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejsv4%2Cfunctionsv2&pivots=programming-language-csharp
[Accessed 04 September 2026].

Microsoft Learn, 2026a. Create your first containerized Azure Functions. [Online]
Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-deploy-container
[Accessed 04 September 2026].

Microsoft Learn, 2026b. Work with containers and Azure Functions. [Online]
Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-custom-container
[Accessed 04 September 2026].

Microsoft Learn, 2026c. Azure Tables client library for .NET - version 12.12.0. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
[Accessed 04 September 2026].

Microsoft Learn, 2026d. Download a blob with .NET. [Online]
Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-download
[Accessed 04 September 2026].

Microsoft Learn, 2026e. Introduction to Azure Blob Storage. [Online]
Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-introduction
[Accessed 04 September 2026].

Microsoft Learn, 2026f. MultipartReader Class. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.webutilities.multipartreader?view=aspnetcore-10.0
[Accessed 04 September 2026].

Microsoft Learn, 2026g. Azure Queue storage trigger for Azure Functions. [Online]
Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger
[Accessed 29 September 2026].

Microsoft Learn, 2026h. Conditional operator. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator
[Accessed 30 September 2026].

Microsoft Learn, 2026i. String.Join Method. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/system.string.join?view=net-10.0
[Accessed 30 September 2026].

Microsoft Learn, 2026j. Task.Delay Method. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.delay?view=net-10.0
[Accessed 01 October 2026].

Microsoft Learn, 2026k. TableClient.UpsertEntityAsync Method. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient.upsertentityasync?view=azure-dotnet
[Accessed 01 October 2026].

Microsoft Learn, 2026l. Poison messages in Azure Functions. [Online]
Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger#poison-messages
[Accessed 01 October 2026].

Microsoft Learn, 2026m. Environment.GetEnvironmentVariable Method. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/system.environment.getenvironmentvariable?view=net-10.0
[Accessed 01 October 2026].

Microsoft Learn, 2026n. Int32.TryParse Method. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/system.int32.tryparse?view=net-10.0
[Accessed 05 October 2026].

Microsoft Learn, 2026o. Convert.FromBase64String Method. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/system.convert.frombase64string?view=net-10.0
[Accessed 08 October 2026].

Microsoft Learn, 2026p. Encoding.GetString Method. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding.getstring?view=net-10.0
[Accessed 08 October 2026].

Microsoft Learn, 2026q. Azure Queue Storage client library for .NET. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage.queues-readme?view=azure-dotnet
[Accessed 04 September 2026].

Microsoft Learn, 2026r. RequestFailedException Class. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.requestfailedexception?view=azure-dotnet
[Accessed 01 October 2026].

Microsoft Learn, 2026s. JsonSerializer.Deserialize Method. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer.deserialize?view=net-10.0
[Accessed 05 October 2026].

Microsoft Learn, 2026t. StreamReader.ReadToEndAsync Method. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/system.io.streamreader.readtoendasync?view=net-10.0
[Accessed 05 October 2026].

Microsoft Learn, 2026u. BlobHttpHeaders Class. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.models.blobhttpheaders?view=azure-dotnet
[Accessed 04 September 2026].

Microsoft Learn, 2026v. BlobUploadOptions.HttpHeaders Property. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.models.blobuploadoptions.httpheaders?view=azure-dotnet
[Accessed 04 September 2026].

Microsoft Learn, 2026w. Convert.ToBase64String Method. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/system.convert.tobase64string?view=net-10.0
[Accessed 09 October 2026].

Microsoft Learn, 2026x. Configure JWT bearer authentication in ASP.NET Core. [Online]
Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication
[Accessed 19 October 2026].

Microsoft Learn, 2026y. Write custom ASP.NET Core middleware. [Online]
Available at: https://learn.microsoft.com/aspnet/core/fundamentals/middleware/write
[Accessed 19 October 2026].

Microsoft Learn, 2026z. Get started with Swashbuckle and ASP.NET Core. [Online]
Available at: https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle
[Accessed 19 October 2026].

Microsoft Learn, 2026aa. Make HTTP requests using IHttpClientFactory in ASP.NET Core. [Online]
Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
[Accessed 19 October 2026].

Microsoft Learn, 2026ab. Background tasks with hosted services in ASP.NET Core. [Online]
Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
[Accessed 19 October 2026].

Microsoft Learn, 2026ac. Configuration in ASP.NET Core. [Online]
Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/
[Accessed 19 October 2026].

Microsoft Learn, 2026ad. Model validation in ASP.NET Core MVC. [Online]
Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
[Accessed 20 October 2026].

Microsoft Learn, 2026ae. Create web APIs with ASP.NET Core. [Online]
Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/
[Accessed 20 October 2026].

Microsoft Learn, 2026af. Role-based authorization in ASP.NET Core. [Online]
Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
[Accessed 20 October 2026].

Microsoft Learn, 2026ah. JwtSecurityToken Class. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt.jwtsecuritytoken
[Accessed 20 October 2026].

Microsoft Learn, 2026aj. Tutorial: Containerize a .NET app. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/core/docker/build-container
[Accessed 20 October 2026].

Microsoft Learn, 2026am. Upload files in ASP.NET Core. [Online]
Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
[Accessed 21 October 2026].

Microsoft Learn, 2026an. List blobs with .NET. [Online]
Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-list
[Accessed 21 October 2026].

Microsoft Learn, 2026ao. Delete and restore a blob with .NET. [Online]
Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-delete
[Accessed 21 October 2026].

Microsoft Learn, 2026ap. ClaimsPrincipal.FindFirst Method. [Online]
Available at: https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimsprincipal.findfirst?view=net-10.0
[Accessed 22 October 2026].

Microsoft Learn, 2026aq. Handle errors in ASP.NET Core. [Online]
Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling
[Accessed 22 October 2026].

Microsoft Learn, 2026ar. Quickstart: Create a Linux virtual machine in the Azure portal. [Online]
Available at: https://learn.microsoft.com/en-us/azure/virtual-machines/linux/quick-create-portal
[Accessed 26 October 2026].

Mrzyglod, K. and Camden, R., 2020. Azure for Developers: Implement rich Azure PaaS ecosystems using containers, serverless services, and storage solutions. 2nd ed. Birmingham: Packt Publishing.

Postman, 2025. Write scripts to test API response data in Postman. [Online]
Available at: https://learning.postman.com/docs/tests-and-scripts/write-scripts/test-scripts/
[Accessed 01 September 2026].