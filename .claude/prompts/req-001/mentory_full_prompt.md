# Mentory — Enterprise System Design Prompt (for Claude)

## 🧠 Role & Context
You are a **Senior Product Owner, Cloud Architect, Systems Designer, and UX Lead** collaborating to design a production-ready enterprise SaaS platform.

Design a **complete system specification** for **Mentory** with real-world rigor.

---

## 🌍 System Overview
Mentory is a **multi-tenant SaaS platform** for incubators to support entrepreneurs through:

- Diagnostic assessments  
- Personalized mentoring plans  
- Structured learning (modular knowledge system)  
- Mentorship execution (sessions, tasks, tracking)

---

## 🧱 Core Principles
- Multi-tenant architecture (Incubator as tenant)
- Context-driven access (server-side authoritative context)
- Backend-enforced security (never trust frontend)
- Template → Clone → Override pattern
- Human-in-the-loop decisions
- Full auditability

---

## 👥 User Model

### Global
- Global Admin

### Incubator
- Incubator Admin
- Project Coordinator

### Operational
- Mentor
- Entrepreneur

### External
- Sponsor (read-only)

### Rules
- One global user identity
- Users can have multiple roles across projects/incubators
- Roles are **contextual**

---

## 🔐 Session & Context
After login, user selects context:

- Incubator
- Project
- Role

Rules:
- Stored server-side (DB)
- Not client-controlled
- One active session only
- One context at a time
- Auto-select if only one context exists

---

## 🔒 Authorization
- RBAC + granular permissions:
  - Module
  - Action
  - Resource
- Context-aware validation
- Backend always enforces access
- No access via shared URLs without permission

---

## 🏢 Multi-Tenant Hierarchy
Platform → Incubators → Projects → Users

---

## 🔄 Project Lifecycle
Stages (configurable):
1. Registration  
2. Forms  
3. Analysis  
4. Learning Assignment  
5. Mentoring  
6. Final Evaluation  
7. Closure  

- Manual transitions
- States: Not started / In progress / Completed
- Drives UX guidance

---

## 💳 Subscription System
- Versioned plans
- Features:
  - Boolean
  - Quantitative (e.g., project limits)
- Overrides:
  - Positive only
  - Accumulative
  - No expiration
- No payment integration (admin-managed)

---

## 📊 Diagnostic Module

### Form Engine
- Global templates (by subscription)
- Project-level cloned forms

### Questions
- Types: text, numeric, options
- Linked to topic
- Optional block
- Ordered

### Customization
- Clone + modify
- Sync modes:
  - Disconnected
  - Partial sync

---

## 🧠 Knowledge Structure

Hierarchy:
- Knowledge Structure  
  → Modules  
    → Topics  
      → Subjects  
        → Resources  

### Definitions
- Module = learning route
- Topic = core analytical unit
- Subject = content block
- Resource = video/link/file

### Rules
- Topics link diagnostic ↔ learning
- Same clone/customization pattern as forms

---

## 🔗 Diagnostic → Knowledge
- Each question maps to a topic
- Selecting a form implies selecting a knowledge structure

---

## 🎯 Mentoring Plan

### Flow
1. Diagnostic completed  
2. System suggests topics  
3. Mentor + entrepreneur adjust  
4. Final plan saved  

---

## 📅 Mentoring Execution

### Scheduling Engine
Inputs:
- Subjects duration
- Sessions/week
- Hours/session

Outputs:
- Auto-generated calendar

### Sessions
- Flexible (non-linear)
- Mentor can adjust topics dynamically

### Logs
- Notes per session
- Topics covered
- Decisions

### Assignments
- Linked to subjects
- Submission + review + feedback

---

## 🔔 Notification System

### Requirements
- Centralized
- Auditable
- No duplication

### Model
- Notification (content)
- Recipients (users/groups)
- Delivery tracking

### Types
- Immediate
- Scheduled (e.g., weekly summaries)

### Examples
- Session reminders
- Task reminders
- Weekly mentor agenda

### Preferences
- User-configurable per role/context

### Channels
- Email (initial)
- Extendable to SMS / in-app

---

## 📦 Required Output from Claude

### 1. Functional Specification
### 2. System Architecture
### 3. Data Model
### 4. Authorization Design
### 5. Key Workflows
### 6. Multi-Tenant Strategy
### 7. Security Model
### 8. Extensibility Strategy

---

## ⚠️ Instructions
- Be explicit
- No simplification
- Design for production
- Use structured sections
- State assumptions when needed
