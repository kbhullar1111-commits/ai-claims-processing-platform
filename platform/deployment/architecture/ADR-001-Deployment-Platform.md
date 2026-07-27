
# Architecture Decision Record (ADR-001)

## Title

**Deployment Platform Architecture**

---

## Status

**Accepted**

---

## Context

The AI Claims Processing Platform consists of multiple independently deployable components.

Current deployable artifacts include:

* Gateway Service
* Claims Service
* Document Service
* Notification Service
* Fraud Service
* Payment Service
* Azure Function

The deployment platform must determine the **minimal safe deployment set** after each source code change while remaining independent of CI/CD tooling and deployment technologies.

The platform should support future expansion to additional technologies (Angular, Java, Python, Infrastructure-as-Code) without requiring architectural changes.

---

# Problem Statement

Traditional CI/CD pipelines embed deployment logic directly in YAML.

Example:

```yaml
if: contains(changedFiles, 'services/fraud')
```

As repositories grow this leads to:

* duplicated logic
* technology coupling
* difficult maintenance
* poor extensibility

Deployment logic should instead be modeled as a reusable platform capability.

---

# Decision

The deployment platform will be implemented as an independent application following Clean Architecture.

The platform will consume repository metadata and produce a deployment plan.

CI/CD pipelines become orchestration layers rather than decision engines.

---

# Architecture

```text
                     Repository

            Source Code + Manifest

                     │

                     ▼

          Repository Change Provider

                     │

                 ChangeSet

                     │

                     ▼

            Repository Manifest

                     │

                     ▼

          Artifact Resolution Engine

        ┌───────────┼───────────────┐

        ▼           ▼               ▼

     .NET        Angular       Terraform

     Resolver    Resolver       Resolver

        └───────────┼───────────────┘

                    ▼

             Artifact Impact

                    ▼

           Deployment Planner

                    ▼

            Deployment Plan

                    ▼

       GitHub / Azure DevOps / Jenkins

                    ▼

          Azure / Kubernetes / AWS
```

---

# Responsibilities

## Repository Change Provider

Responsible for:

* obtaining changed files

Not responsible for:

* dependency analysis
* deployment decisions

---

## Repository Manifest

Responsible for:

* describing deployable artifacts
* describing repository metadata
* describing dependency roots

Not responsible for:

* runtime deployment

---

## Artifact Resolution Engine

Responsible for:

* technology-specific dependency analysis

Example implementations:

* .NET Resolver
* Angular Resolver
* Java Resolver
* Python Resolver
* Terraform Resolver

---

## Deployment Planner

Responsible for:

* determining minimal safe deployment set
* applying deployment policies
* producing deployment plan

Not responsible for:

* executing deployment

---

## CI/CD Platform

Responsible for:

* executing deployment plan

Not responsible for:

* determining deployment plan

---

# Guiding Principles

1. Technology-neutral architecture.
2. Configuration over hard-coded logic.
3. Independent deployable artifacts.
4. Manifest-driven repository knowledge.
5. Pipeline orchestration instead of pipeline intelligence.
6. Extensible through technology-specific analyzers.
7. Separation between planning and execution.

---

# Consequences

### Advantages

* reusable deployment platform
* minimal deployments
* technology agnostic
* maintainable
* testable
* scalable

### Trade-offs

* additional component to maintain
* repository manifest must remain accurate
* slightly higher initial complexity

---

# Future Evolution

The architecture supports future additions without redesign.

Examples:

* Angular UI deployment
* Terraform deployments
* Helm deployments
* Blue/Green deployment planning
* Canary deployment planning
* AI-assisted deployment recommendations
* Repository visualization
* Dependency graph generation
