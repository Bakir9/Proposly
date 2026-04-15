# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Proposly** is a multi-tenant SaaS platform for small and medium businesses to manage offers, projects, costs, and profitability. Two bounded contexts: **OfferManagement** (seller side) and **ProjectManagement** (execution side).

## Commands

```bash
# Run the API
dotnet run --project src/Proposly.API/Proposly.API.csproj

# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/Proposly.Domain.Tests
```

## Solution Structure

```
src/
├── Proposly.Shared/        # Value objects, base classes — no dependencies
├── Proposly.Domain/        # Entities, repository interfaces — references Shared
├── Proposly.Application/   # CQRS handlers, service interfaces — references Domain
├── Proposly.Infrastructure/# EF Core, repos, external services — references Application + Domain
└── Proposly.API/           # Controllers, middleware, DI wiring — references Application + Infrastructure

tests/
├── Proposly.Domain.Tests/
├── Proposly.Application.Tests/
└── Proposly.Integration.Tests/
```

**Dependency flow:** `API → Application → Domain ← Infrastructure` — all reference `Shared`

## Layer Responsibilities

### Proposly.Shared
- `Primitives/Entity.cs`, `AggregateRoot.cs`, `IDomainEvent.cs` — base classes
- `ValueObjects/Money.cs`, `Address.cs` — typed value objects used across both contexts
- `Interfaces/ITenantEntity.cs`, `IAuditableEntity.cs` — marker interfaces

### Proposly.Domain
Organized by bounded context as namespaces:
```
OfferManagement/
  Entities/       → Offer, OfferItem, Client
  ValueObjects/
  Events/
  Repositories/   → IOfferRepository (interfaces only, no EF Core here)
ProjectManagement/
  Entities/       → Project, Task, Milestone, TeamMember, TimeEntry, Expense
  ValueObjects/
  Events/
  Repositories/
```

### Proposly.Application
```
Abstractions/     → ICommand, ICommandHandler, IQuery, IQueryHandler (custom CQRS, no MediatR)
OfferManagement/
  Commands/       → e.g. CreateOfferCommand + CreateOfferCommandHandler
  Queries/
ProjectManagement/
  Commands/
  Queries/
```
Service interfaces (e.g. `IEmailService`, `IPdfService`, `IFileStorage`) also live here.

### Proposly.Infrastructure
```
Persistence/
  Configurations/ → EF Core entity type configurations (IEntityTypeConfiguration<T>)
  Repositories/   → Concrete repository implementations
Services/
  Email/
  Pdf/
  Storage/
```

### Proposly.API
- Controllers inject `ICommandHandler<T>` and `IQueryHandler<T, R>` directly from DI
- `Program.cs` has `TODO` markers for `AddApplication()` and `AddInfrastructure()` extension methods

## Key Design Rules

**Multi-tenancy**: Every entity implements `ITenantEntity` (`CompanyId`). EF Core global query filters enforce tenant isolation — every query must be scoped automatically.

**Snapshot pattern**: Financial figures (offer amounts, hourly rates) are stored as snapshots at the moment of use. Never reference live data retroactively.

**Rich domain entities**: Business logic (price calculations, status transitions, profitability) lives in entities, not handlers or services.

**CQRS without MediatR**: Commands and queries use custom `ICommandHandler` / `IQueryHandler` interfaces injected directly via DI. No pipeline library.

**Value objects**: Use `Money` for all financial figures. Never use raw `decimal` for money across layers.

## Planned Tech Stack

| Concern | Library |
|---|---|
| ORM | Entity Framework Core (PostgreSQL) |
| Validation | FluentValidation |
| PDF | QuestPDF |
| Email | MailKit / SendGrid |
| Auth | JWT Bearer tokens |
| Logging | Serilog |
| Frontend | React + TypeScript (not yet scaffolded) |
