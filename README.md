# WedLink

WedLink is a custom digital wedding invitation management system for Egypt. The service is managed by an administrator: the admin creates an order, collects wedding details, prepares a private preview, and publishes the invitation after customer approval.

**Project status:** Foundation and planning are in place; feature development is ongoing.

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

### Frontend

```powershell
cd Frontend
npm install
npm run dev
```

Open the local URL printed by Vite.

## Documentation

- [Business decisions](Documentation/Requirements/Decisions.md)
- [Architecture](Documentation/Architecture/Architecture.md)
- [API plan](Documentation/API/API-Plan.md)
- [API error responses](Documentation/API/Error-Responses.md)
- [ERD source](Documentation/ERD/ERD.md)
- [ERD visual preview](Documentation/ERD/ERD-Preview.html)

## Copyright

All Rights Reserved. © 2026 WedLink.

The source code is protected by copyright. No general permission is granted to use, copy, modify, or redistribute this code. Permission must be obtained from the copyright owner. No open-source license is provided.
