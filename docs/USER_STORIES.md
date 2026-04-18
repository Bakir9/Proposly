# Proposly — User Stories

---

## 1. Auth & Onboarding

---

### US-001 — Register a new company

**As a** new user who wants to manage offers and projects,
**I want to** register my company with my personal account,
**So that** I get my own isolated workspace where none of my data is visible to other companies.

**Acceptance Criteria:**
- I can fill in my first name, last name, email, password, and company name.
- After registration, my company and my Owner account are created atomically — if one fails, neither is created.
- I receive a JWT token immediately after registration so I don't need to log in separately.
- My email must be unique across the platform — registering with an existing email shows a clear error.
- Password must be at least 8 characters.
- I am assigned the `Owner` role automatically.

**Notes:**
- Company name is required — it is the primary identifier of the tenant.
- No email verification in MVP; can be added later.

---

### US-002 — Log in

**As a** registered user,
**I want to** log in with my email and password,
**So that** I can access my company's workspace and resume my work.

**Acceptance Criteria:**
- I enter my email and password and receive a JWT token on success.
- The token contains my user ID, company ID, email, and role — so the app can personalize the UI without extra API calls.
- If email or password is wrong I see a generic "Invalid credentials" message (no leaking which field was wrong).
- The token is stored in the browser and attached automatically to all subsequent API requests.
- If my token expires I am redirected to the login page.

---

### US-003 — Log out

**As a** logged-in user,
**I want to** log out,
**So that** my session is cleared on this device.

**Acceptance Criteria:**
- Clicking log out removes the token from the browser.
- I am redirected to the login page immediately.
- All in-memory data (cached queries) is cleared.

---

## 2. User Management

---

### US-004 — Invite a team member

**As an** Owner or Admin,
**I want to** invite a new team member by email,
**So that** they can log in and collaborate on projects and offers within my company.

**Acceptance Criteria:**
- I provide the invitee's first name, last name, email, and role (Admin or Member).
- A new user account is created under my company.
- The invited user can log in with a temporary password or via a reset link (MVP: set a password directly on invite).
- I cannot invite someone who already has an account with that email in my company.
- Regular Members cannot invite other users — only Owner and Admin can.

---

### US-005 — View team members

**As any** authenticated user,
**I want to** see the list of all team members in my company,
**So that** I know who is part of the team and what their roles are.

**Acceptance Criteria:**
- I see each member's full name, email, and role.
- The list is sorted alphabetically by last name.
- I can see my own account highlighted or labeled "You".

---

### US-006 — Change a member's role

**As an** Owner,
**I want to** change a team member's role,
**So that** I can promote a Member to Admin or demote an Admin back to Member as responsibilities change.

**Acceptance Criteria:**
- I can select any user except myself and change their role.
- The change takes effect immediately — the user's next API call reflects the new role.
- Owners cannot demote themselves (there must always be at least one Owner).
- Admins cannot change roles — only the Owner can.

---

### US-007 — Remove a team member

**As an** Owner or Admin,
**I want to** remove a team member from the company,
**So that** they lose access to all company data immediately.

**Acceptance Criteria:**
- After removal, the user's token is effectively revoked (or at minimum their next request returns 401).
- Time entries and expenses they logged are preserved — data is not deleted, just the user account is deactivated.
- I cannot remove myself.
- I cannot remove the only Owner.

---

### US-008 — View my own profile

**As any** authenticated user,
**I want to** view my own profile,
**So that** I can confirm my name, email, and role are correct.

**Acceptance Criteria:**
- I can navigate to a "My Profile" section and see my full name, email, role, and company name.
- Editing profile details (name, password) is a future story — read-only in MVP.

---

## 3. Clients

---

### US-009 — Create a client

**As an** Owner or Admin,
**I want to** create a client record,
**So that** I can associate offers with the correct company or person.

**Acceptance Criteria:**
- I enter the client's company name, contact person name, email, phone, and optional address (street, city, country, postal code).
- All fields except company name are optional.
- The client is scoped to my company — other tenants cannot see it.
- After saving, I am taken to the client detail page.

---

### US-010 — List clients

**As any** authenticated user,
**I want to** see all clients in my company,
**So that** I can find existing clients before creating a new offer.

**Acceptance Criteria:**
- I see each client's company name, contact person, and email.
- I can search/filter by name.
- The list shows how many offers each client has.

---

### US-011 — View client detail

**As any** authenticated user,
**I want to** view a client's full details and their offer history,
**So that** I can understand the business relationship at a glance.

**Acceptance Criteria:**
- I see all contact details.
- I see a list of all offers for this client (title, status, total, date).
- I can navigate directly to any of those offers.

---

### US-012 — Edit a client

**As an** Owner or Admin,
**I want to** edit a client's details,
**So that** I can keep contact information up to date.

**Acceptance Criteria:**
- I can update any field (name, contact person, email, phone, address).
- Changes do not affect already-sent offers — those are snapshots.

---

## 4. Offers

---

### US-013 — Create a draft offer

**As an** Owner or Admin,
**I want to** create a new offer for a client,
**So that** I have a draft I can build out before sending it.

**Acceptance Criteria:**
- I select an existing client (required).
- I enter an offer title and optional notes/description.
- I add one or more line items: description, quantity, and unit price.
- Each line item shows a computed line total (quantity × unit price).
- The offer total is calculated automatically as the sum of all line items.
- The offer is saved in `Draft` status — it is not visible to the client yet.
- I can save and come back to edit it later.

---

### US-014 — Edit a draft offer

**As an** Owner or Admin,
**I want to** edit a draft offer,
**So that** I can refine line items, fix mistakes, or adjust pricing before sending.

**Acceptance Criteria:**
- I can add, remove, and update line items.
- I can change the title, notes, and client.
- Editing is only possible while the offer is in `Draft` status — sent/accepted/rejected offers are locked.
- Total recalculates live as I update line items.

---

### US-015 — Send an offer

**As an** Owner or Admin,
**I want to** mark an offer as Sent,
**So that** it is locked and I can track whether the client accepts or rejects it.

**Acceptance Criteria:**
- Clicking "Send" transitions the offer from `Draft` → `Sent`.
- After this point, line items and pricing cannot be edited.
- The sent date is recorded.
- An `OfferSent` domain event is raised (used for email notification in later story).

---

### US-016 — Accept an offer

**As an** Owner or Admin,
**I want to** mark an offer as Accepted,
**So that** I can record that the client agreed and optionally create a project from it.

**Acceptance Criteria:**
- I can only accept an offer that is in `Sent` status.
- Status transitions to `Accepted`.
- An `OfferAccepted` domain event is raised.
- I am prompted (not forced) to create a linked project immediately.

---

### US-017 — Reject an offer

**As an** Owner or Admin,
**I want to** mark an offer as Rejected,
**So that** the record reflects the client declined and I can stop following up.

**Acceptance Criteria:**
- I can only reject an offer that is in `Sent` status.
- Status transitions to `Rejected`.
- An optional rejection reason can be recorded.

---

### US-018 — List offers

**As any** authenticated user,
**I want to** see all offers in my company with their status and total,
**So that** I can track my sales pipeline at a glance.

**Acceptance Criteria:**
- I see offer title, client name, status (color-coded), total amount, and creation date.
- I can filter by status (Draft / Sent / Accepted / Rejected / Expired).
- I can sort by date or total.

---

### US-019 — View offer detail

**As any** authenticated user,
**I want to** see the full detail of an offer,
**So that** I can review what was proposed and at what price.

**Acceptance Criteria:**
- I see the offer header (title, status, client, dates).
- I see all line items (description, qty, unit price, line total).
- I see the grand total.
- I see notes/description.
- I see action buttons appropriate to the current status (e.g., "Send" on Draft, "Accept/Reject" on Sent).

---

### US-020 — Download offer as PDF

**As an** Owner or Admin,
**I want to** download an offer as a PDF,
**So that** I can email it to the client or print it for a meeting.

**Acceptance Criteria:**
- The PDF contains my company name, client details, offer title, line items, total, and notes.
- The PDF is professionally formatted and ready to send externally.
- Available for offers in any status (but especially Sent).

---

### US-021 — Send offer PDF by email

**As an** Owner or Admin,
**I want to** send the offer PDF directly to the client's email from within the app,
**So that** I don't need to download and attach it manually.

**Acceptance Criteria:**
- The app sends an email to the client's email address on file.
- The email contains a short message and the offer PDF as an attachment.
- I see confirmation that the email was sent.
- A sent timestamp is recorded on the offer.

---

## 5. Projects

---

### US-022 — Create a project

**As an** Owner or Admin,
**I want to** create a project,
**So that** I can track the execution of work and measure actual costs against what was offered.

**Acceptance Criteria:**
- I enter a project name, optional description, start date, and deadline.
- I can optionally link the project to an accepted offer — this sets the project's budget from the offer total.
- If no offer is linked, I can set a manual budget.
- The project starts in `Active` status.

---

### US-023 — List projects

**As any** authenticated user,
**I want to** see all projects in my company,
**So that** I have an overview of what work is ongoing, completed, or paused.

**Acceptance Criteria:**
- I see each project's name, status, deadline, and a quick cost vs. budget indicator.
- I can filter by status (Active / Completed / OnHold / Cancelled).

---

### US-024 — View project detail

**As any** authenticated user,
**I want to** view a project's full detail,
**So that** I can see tasks, team, time entries, expenses, and overall progress.

**Acceptance Criteria:**
- I see project header: name, status, dates, linked offer (if any), budget.
- I see tabs or sections for: Tasks, Team, Time Log, Expenses, Milestones.
- I see a cost summary: total logged hours × rates + expenses vs. budget.

---

### US-025 — Update a project

**As an** Owner or Admin,
**I want to** update a project's details,
**So that** I can correct the name, adjust the deadline, or change the status.

**Acceptance Criteria:**
- I can update name, description, deadline, budget, and status.
- Status transitions are: Active ↔ OnHold, Active → Completed, Active → Cancelled.
- Completed and Cancelled projects are locked — no new time or expenses can be logged.

---

### US-026 — Add team members to a project

**As an** Owner or Admin,
**I want to** add team members to a project with their hourly rate,
**So that** the system can calculate the cost of their logged time accurately.

**Acceptance Criteria:**
- I select a user from the company's member list.
- I enter their hourly rate for this project (can differ per project).
- I can remove a member from the project (their past time entries remain).

---

### US-027 — Log time on a project

**As any** project member,
**I want to** log time I spent working on a project,
**So that** the actual labor cost is tracked and reflected in profitability.

**Acceptance Criteria:**
- I select a date, enter hours worked (decimal allowed, e.g. 1.5), and optionally a description.
- I can only log time on projects I am a member of.
- Each time entry records: user, date, hours, description, and the hourly rate snapshot at the time of logging.
- I can edit or delete my own time entries; Admins/Owners can edit/delete any entry.

---

### US-028 — Add an expense to a project

**As an** Owner or Admin,
**I want to** record an expense on a project,
**So that** non-labor costs (travel, software, materials) are included in the total project cost.

**Acceptance Criteria:**
- I enter: amount, currency, category (Travel / Software / Materials / Other), date, and optional description.
- The expense is immediately reflected in the project's total cost.
- I can edit or delete expenses.

---

### US-029 — Manage tasks on a project

**As an** Owner or Admin,
**I want to** create and manage tasks within a project,
**So that** the team has a clear list of what needs to be done and who is responsible.

**Acceptance Criteria:**
- I can create a task with: title, optional description, assignee (from project members), due date, and status.
- Task statuses: `Todo` → `InProgress` → `Done`.
- Assigned members can update the status of their own tasks.
- I can view all tasks in a list, filterable by status and assignee.

---

### US-030 — Manage milestones on a project

**As an** Owner or Admin,
**I want to** set milestones on a project,
**So that** I can track key delivery dates and mark when they are reached.

**Acceptance Criteria:**
- I can create a milestone with a title and target date.
- I can mark a milestone as completed (records the actual completion date).
- Milestones are shown on the project detail in chronological order.

---

## 6. Profitability & Reporting

---

### US-031 — View project profitability

**As an** Owner or Admin,
**I want to** see the profitability breakdown of a project,
**So that** I can immediately tell if we are making or losing money.

**Acceptance Criteria:**
- I see: offer amount (revenue), total labor cost (hours × rates), total expenses, gross margin (revenue − costs), and margin percentage.
- Costs update in real time as time entries and expenses are added.
- If no offer is linked, revenue shows as the manual budget.
- Color-coding: green if margin > 0, red if negative.

---

### US-032 — Dashboard overview

**As an** Owner,
**I want to** see a dashboard when I log in,
**So that** I get an instant snapshot of the business without digging into individual records.

**Acceptance Criteria:**
- Shows: number of open offers (Draft + Sent), total value of open offers.
- Shows: number of active projects, total revenue locked (Accepted offers).
- Shows: total logged hours this month across all projects.
- Shows: a list of the 5 most recently updated offers and projects.
- All numbers are scoped to my company only.

---

## Status Overview

| # | Story | Status |
|---|---|---|
| US-001 | Register company | Done |
| US-002 | Log in | Done |
| US-003 | Log out | Done |
| US-004 | Invite team member | Done |
| US-005 | View team members | Done |
| US-006 | Change member role | Done |
| US-007 | Remove team member | Done |
| US-008 | View my profile | Done |
| US-009 | Create client | Done |
| US-010 | List clients | Done |
| US-011 | View client detail | Done |
| US-012 | Edit client | Done |
| US-013 | Create draft offer | Done |
| US-014 | Edit draft offer | Done |
| US-015 | Send offer | Done |
| US-016 | Accept offer | Done |
| US-017 | Reject offer | Done |
| US-018 | List offers | Done |
| US-019 | View offer detail | Done |
| US-020 | Download offer PDF | Done |
| US-021 | Send offer by email | Done |
| US-022 | Create project | Done |
| US-023 | List projects | Done |
| US-024 | View project detail | Done |
| US-025 | Update project | Done |
| US-026 | Add team to project | Done |
| US-027 | Log time | Done |
| US-028 | Add expense | Done |
| US-029 | Manage tasks | Done |
| US-030 | Manage milestones | Done |
| US-031 | Project profitability | Done |
| US-032 | Dashboard | Done |
