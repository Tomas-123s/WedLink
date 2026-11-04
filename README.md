# WedLink

WedLink is a custom digital wedding invitation management system for Egypt. The service is managed by an administrator: the admin creates an order, collects wedding details, prepares a private preview, and publishes the invitation after customer approval.

**Project status:** Core admin, orders, guest invitations, lifecycle jobs, dashboard, and audit features are implemented. Automated tests run with the command below; release readiness still depends on the deployment environment and manual browser review.

The public marketing website and design showcase are at `/`. See [public website requirements](Documentation/Requirements/Public-Website.md) for demo-data and contact configuration details.

## Technology

- Backend: C#, ASP.NET Core Web API, Entity Framework Core, SQL Server
- Frontend: React, TypeScript, React Router, TanStack Query
- Primary language: Arabic (RTL)
- Default currency: EGP

## Project layout

```text
Backend/        ASP.NET Core API, Business, and DataAccess projects
Frontend/       React and TypeScript application
Documentation/  Requirements, architecture, API plan, and ERD
```

## Run locally

### Backend

Configure the local database connection as described in [Database setup](Documentation/Database/LocalSetup.md), then run:

```powershell
dotnet run --project Backend\WedLink.API --launch-profile https
```

Health endpoint: `https://localhost:7270/api/v1/health`
Development OpenAPI document: `https://localhost:7270/openapi/v1.json`
Admin authentication setup: see [database setup](Documentation/Database/LocalSetup.md) and [authentication API](Documentation/API/Admin-Authentication.md).

### Frontend

```powershell
cd Frontend
npm install
npm run dev
```

Open the local URL printed by Vite.

The public homepage and design gallery are served at `/`; the protected admin interface remains at `/admin`. To configure the WhatsApp contact, copy `Frontend/.env.example` to `Frontend/.env.local` and replace the example phone with the business number in international digits (without `+` or spaces). Vite picks up changes after restarting the frontend server. Keep `.env.local` out of Git.

## Verify changes

From the repository root, restore and build the backend, then run the automated suite:

```powershell
dotnet restore Backend\WedLink.sln
dotnet build Backend\WedLink.sln --no-restore
dotnet test Backend\WedLink.sln --no-build
```

Build the frontend separately:

```powershell
cd Frontend
npm ci
npm run build
```

Test API integration cases use an isolated EF Core in-memory database. They do not exercise SQL Server query behavior or a deployed browser session.

## Before deployment

- Configure the SQL Server connection string and storage path outside source control.
- Keep API keys, passwords, and signing credentials in .NET User Secrets locally or the deployment secret store; do not paste them into source, logs, screenshots, or issue reports.
- Apply EF migrations using the deployment process and create the first SuperAdmin through `WedLink.AdminSetup`.
- Serve the API and frontend over HTTPS; the admin cookies require secure transport.
- Set the API environment variable `PublicSite__BaseUrl` to the HTTPS frontend origin (for example, `https://wedlink.example`). Invitation sharing uses this same-origin URL for crawler metadata and links back to `/invite/{slug}`; keep the `/api` reverse proxy on that origin.
- Configure persistent private media storage and a scheduled database backup with a tested restore procedure.
- Review the [local setup and backup notes](Documentation/Database/LocalSetup.md) and environment limitations before production use.

## Documentation

- [Business decisions](Documentation/Requirements/Decisions.md)
- [Future invitation experience requirements](Documentation/Requirements/Future-Invitation-Experience.md)
- [Public website requirements](Documentation/Requirements/Public-Website.md)
- [Development roadmap](Documentation/Planning/Development-Roadmap.md)
- [Architecture](Documentation/Architecture/Architecture.md)
- [API plan](Documentation/API/API-Plan.md)
- [API error responses](Documentation/API/Error-Responses.md)
- [ERD source](Documentation/ERD/ERD.md)
- [ERD visual preview](Documentation/ERD/ERD-Preview.html)

## Copyright

All Rights Reserved. © 2026 WedLink.

The source code is protected by copyright. No general permission is granted to use, copy, modify, or redistribute this code. Permission must be obtained from the copyright owner. No open-source license is provided.
