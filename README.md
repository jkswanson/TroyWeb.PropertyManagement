# Property Management Application

An ASP.NET Core MVC application for managing rental properties, rental applications, and lease approvals.

Built as a technical assessment for Troy Web Consulting.

## Technology Stack

- **.NET 10** — ASP.NET Core MVC
- **C#** — Application logic and domain models
- **Entity Framework Core** — Data access and migrations
- **SQL Server LocalDB** — Development database
- **ASP.NET Core Identity** — Authentication and role-based authorization
- **Bootstrap** — Responsive user interface
- **MSTest** — Automated testing
- **SQLite In-Memory** — Integration test database
- **Bogus** — Sample data generation

## Solution Structure

| Project | Responsibility |
|---|---|
| `PropertyManagement.Web` | MVC controllers, Razor views, authentication, and application startup |
| `PropertyManagement.Core` | Domain entities, enums, and shared constants |
| `PropertyManagement.Infrastructure` | EF Core database context, entity configurations, migrations, and sample data seeding |
| `PropertyManagement.Tests` | Automated integration tests |

## Prerequisites

- .NET 10 SDK
- Visual Studio 2026 or another .NET 10-compatible development environment
- SQL Server LocalDB

## Getting Started

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Confirm the database connection string in `PropertyManagement.Web/appsettings.json` points to your SQL Server LocalDB instance.
4. Set `PropertyManagement.Web` as the startup project.
5. Build and run the application.

**Database setup is automatic.** On startup, the application applies pending EF Core migrations and seeds demonstration data. There is no need to manually create the database or run migration commands.

The sample-data seeding process is designed to avoid duplicating existing demonstration records on subsequent application starts.

## Demo Accounts

The application automatically seeds the following accounts on first startup:

| Role | Email | Password |
|---|---|---|
| Property Manager | manager@example.com | Test1234! |
| Applicant | applicant@example.com | Test1234! |
| Applicant | applicant2@example.com | Test1234! |
| Applicant (Sample Data) | demo.applicant01@example.com | Test1234! |

Ten total demo applicant accounts are created, numbered
`demo.applicant01@example.com` through `demo.applicant10@example.com`.

All demo accounts have confirmed email addresses and can sign in immediately.

> These credentials are for local demonstration and assessment purposes only.

## Features

### Applicant

- Register and sign in.
- Browse available rental units.
- View property information and monthly rental rates.
- Create and edit draft rental applications.
- Enter residence history.
- Submit applications for review.
- Track application status.
- Update applications returned for corrections.
- Withdraw eligible applications.

### Property Manager

- View submitted rental applications.
- Review applicant information and residence history.
- Approve, return, or deny applications.
- Require review comments when returning or denying an application.
- Record application status history.
- Automatically create a 12-month lease upon approval.
- Prevent overlapping leases for the same rental unit.

### Business Rules

- Applicants can access only their own applications.
- Property managers have access to the application review workflow.
- Only submitted applications can be approved, returned, or denied.
- Lease approval requires a start date.
- Lease start dates must be in the future (tomorrow or later); same-day and past-date leases are not permitted.
- Lease overlap checks execute within a serializable database transaction.
- Rental application status changes are recorded in an audit history.
- Active leases are considered when determining unit availability.

## Sample Data

The database seeder generates demonstration data, including:

- 5 properties
- 50 rental units
- 4 unit types
- Sample applicant and property manager accounts
- 25 rental applications in various statuses
- Residence histories
- Sample approved leases

## Running Tests

Run the automated tests using Visual Studio Test Explorer, or execute:

```bash
dotnet test
```

The test project uses MSTest and SQLite in-memory databases, allowing tests to run without modifying the development SQL Server database.

Tests cover database operations, rental application review decisions, lease creation, overlapping lease prevention, and applicant withdrawal rules.

## Design Notes

The solution separates domain entities, infrastructure concerns, MVC presentation, and tests into dedicated projects.

Entity Framework Core manages persistence and database migrations. ASP.NET Core Identity provides authentication and role-based access control.

Lease creation and overlap validation are performed transactionally to protect application and lease consistency.

Date-time values are stored in UTC, with relevant timestamps converted to the browser's local time for display. Lease and residence dates are treated as calendar dates.

## Assessment Scope

This application demonstrates a functional rental application workflow with sample data, automated tests, authentication, authorization, and core business-rule enforcement.

It is intended as a technical assessment rather than a production-ready property management platform.