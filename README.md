# Proposly

Proposly is a SaaS web application built with **.NET + React** that helps companies create professional business offers, generate PDF documents, send them to clients via email, and manage the full lifecycle of projects from offer acceptance through to delivery and cost tracking.

---

## Overview

Most small and medium-sized businesses still manage offers in Word, Excel, or copy-paste workflows. Proposly replaces that entirely — giving teams a single, centralized platform to create offers, track client communication, run projects, and understand their real profitability.

The platform is multi-tenant by design. Every company operates in a fully isolated environment: users, offers, projects, and documents are never shared or visible across tenants.

---

## Features

### Offer Management
- Create professional offers with client details, line items, quantities, unit prices, and optional discount percentages
- Automatic calculation of subtotals, discounts, VAT, and final totals per item and overall
- Generate professionally formatted PDF documents including company logo, sender and client information, an itemised table, and all totals
- Send generated PDFs directly to clients via email — no email client configuration required
- Store and retrieve generated PDFs from local storage or S3-compatible cloud storage

### Company & User Management
- Each company manages its own profile: name, full address, and logo (automatically embedded in PDFs)
- Multiple users per company with role-based access
- Secure authentication using JWT tokens
- Automatic multi-tenant data isolation — every database query is scoped to the active company

### Project Management
- Create projects linked to an accepted offer or independently
- Track project status through its full lifecycle: **Planning → Active → On Hold → Completed / Cancelled**
- Set a project budget separately from the offer amount to manage internal margin
- Assign team members to a project with a role and a per-project hourly rate
- Log time entries per team member and optionally per task
- Define tasks with estimated hours, status, and due dates
- Group tasks under milestones to track delivery phases
- Record expenses by category (materials, equipment, subcontractors, travel, other)

### Financial Tracking & Dashboard
- **Labor cost** — calculated automatically from logged hours × member hourly rate
- **Total cost** — labor cost + all project expenses
- **Hours tracked** — estimated vs actual, per task and overall
- **Profitability** — revenue (accepted offer amount) minus total project cost
- Snapshot-based financial integrity: offer amounts and hourly rates are recorded at the time of linking or logging, so historical records are never affected by later changes

---

## Architecture

Proposly is built on **Clean Architecture** with two distinct bounded contexts.

### Bounded Contexts

**Offer Management** — the company as seller. Core concepts: `Offer`, `OfferItem`, `Client`. The outcome is a sent offer and a client response.

**Project Management** — the company as executor. Core concepts: `Project`, `Task`, `Milestone`, `TeamMember`, `TimeEntry`, `Expense`. The outcome is a delivered project with full cost visibility.

The two contexts communicate through a well-defined integration point: when an offer is accepted, a project can be created from it. The accepted amount is stored as a snapshot on the project and never mutated by changes to the original offer.

### Layer Structure

```
/src
  /OfferManagement
    /Domain          # Offer, OfferItem, Client — entities, value objects, interfaces
    /Application     # Command and query handlers (CQRS via MediatR), validators
    /Infrastructure  # EF Core repositories, PDF generator, email service, file storage
  /ProjectManagement
    /Domain          # Project, Task, Milestone, TeamMember, TimeEntry, Expense
    /Application     # Command and query handlers, validators
    /Infrastructure  # EF Core repositories, reporting queries
  /SharedKernel      # Money, Address, TenantId — shared value objects only
  /API               # Controllers, JWT middleware, tenant resolver, request pipeline
```

### Key Technical Decisions

- **Rich domain entities** — business logic (price calculations, status transitions, cost aggregation) lives inside entities and domain services, not in anemic service classes
- **Value objects** — `Money` and `Quantity` are typed to prevent primitive obsession and accidental mixing
- **CQRS with MediatR** — commands and queries are fully separated; write operations go through command handlers, reads through query handlers
- **FluentValidation** — request validation is wired into the MediatR pipeline as a behaviour, keeping controllers clean
- **EF Core global query filters** — every entity carries a `CompanyId` and every query is automatically scoped to the active tenant, enforced at the ORM level
- **Snapshot pattern** — financial figures (offer amounts, hourly rates) are recorded at the moment of use, not referenced dynamically, preserving historical accuracy

---

## Technology Stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, C# |
| Frontend | React, TypeScript |
| Database | PostgreSQL / SQL Server |
| ORM | Entity Framework Core |
| CQRS |
| Validation | FluentValidation |
| PDF generation | QuestPDF |
| Email | MailKit / SendGrid |
| Authentication | JWT Bearer tokens |
| Logging | Serilog |

---

## Domain Model

### Project entity (key fields)

```csharp
public class Project
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }       // tenant isolation
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string ClientName { get; private set; }
    public Guid? LinkedOfferId { get; private set; }
    public Money? OfferedAmount { get; private set; } // snapshot at link time
    public Money Budget { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? Deadline { get; private set; }
    public ProjectStatus Status { get; private set; }

    public IReadOnlyCollection<ProjectMember> Members { get; }
    public IReadOnlyCollection<ProjectTask> Tasks { get; }
    public IReadOnlyCollection<Milestone> Milestones { get; }
    public IReadOnlyCollection<Expense> Expenses { get; }

    public Money CalculateLaborCost();
    public Money CalculateTotalCost();
    public Money CalculateProfitability();
}
```

### Project status transitions

```
Planning ──► Active ──► On Hold ──► Active
                  │
                  ├──► Completed
                  └──► Cancelled
```

---

## Roadmap

- [ ] Offer status tracking (draft, sent, accepted, rejected)
- [ ] Convert accepted offer directly into a project in one action
- [ ] Invoice generation from completed projects
- [ ] Company financial details (IBAN, VAT ID) on PDF documents
- [ ] Offer and project history with full audit log
- [ ] Subscription tiers with feature limits (number of offers, users, storage)
- [ ] Client portal — a shareable link where clients can view and accept offers online
- [ ] Notification system — email alerts for offer expiry, milestone due dates, budget thresholds

---

## Deployment

Proposly is designed to run on affordable cloud infrastructure. The recommended setup is a single VPS (e.g. Hetzner) with Docker Compose for the initial launch, with a clear path to scale horizontally as user volume grows. S3-compatible object storage (Hetzner Object Storage, AWS S3, Backblaze B2) is used for PDF files, keeping the application servers stateless.

---

## License

MIT
