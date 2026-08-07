# Ledger Bookkeeping

ASP.NET Web Forms monthly reconciliation tracker backed by SQL Server.

## Prerequisites

- Visual Studio 2022 with ASP.NET and web development workload
- SQL Server Express (`SARUF-RATUL\SQLEXPRESS`) with Windows Authentication
- .NET Framework 4.8.1

## Database Setup

1. Open SQL Server Management Studio and connect to `SARUF-RATUL\SQLEXPRESS`.
2. Run migrations in order:

```sql
database/migrations/V001__InitialSchema.sql
database/migrations/V002__RolesAndAudit.sql
database/migrations/V003__UserPasswords.sql
```

3. Bootstrap the default Sysadmin account:

```sql
database/seed/bootstrap_sysadmin.sql
```

4. Optional sample companies:

```sql
database/seed/dev_seed.sql
```

## Sign In

The app starts at **Login.aspx**. Default credentials after bootstrap:

| Username | Password |
|----------|----------|
| `admin` | `Ledger123!` |

Usernames are case-insensitive (stored uppercase). Change the default password after first login via **Admin → Users**.

## Run Locally

1. Open [`XsiBookkeeping.sln`](XsiBookkeeping.sln) in Visual Studio 2022.
2. Restore NuGet packages (Build → Restore NuGet Packages).
3. Confirm IIS Express settings:
   - **Anonymous Authentication**: Enabled
   - **Windows Authentication**: Disabled
4. Press **F5** — you should see the **Sign in** page.

Default URL: `https://localhost:44350/Login.aspx`

Use **Sign out** in the top nav to log out.

## Pages

| Page | Purpose |
|------|---------|
| **Login** | Username/password sign-in (shown on app start) |
| **Overview** | Current-period summary, up-to-date companies, overdue list |
| **Tasks** | Monthly reconciliation checklist, filters, comments |
| **Report** | Overall completion stats |
| **Admin / Users** | Sysadmin user and role management |
| **Admin / Audit Log** | Sysadmin audit trail |

## Roles

| Role | Capabilities |
|------|----------------|
| **User** | Reconcile, comment, delete own comments |
| **Admin** | + manage companies/accounts, delete any comment |
| **Sysadmin** | + manage users/roles, view audit log |

## Tech Stack

- ASP.NET Web Forms (.NET Framework 4.8.1)
- Forms Authentication (login page + sign out)
- SQL Server + Dapper
- Bootstrap 5.3 (CDN) + custom Ledger theme
