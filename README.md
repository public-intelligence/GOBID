# GOB-ID

Shared identity and access control platform for government applications. Multiple applications share a single sign-on and a common administration of users, institutions, roles, permissions and access policies.

## Projects

| Folder                 | Description                                          | Stack       |
|------------------------|------------------------------------------------------|-------------|
| `GobID-BackEnd/`       | GOB-ID API: authentication, authorization and tokens | .NET 8      |
| `GobID-FrontEnd/`      | GOB-ID web application and administration console    | Angular 20  |
| `IdentityHub-BackEnd/` | IdentityHub API: person and company lookups          | .NET 10     |

## Attributions and Authorship

> *“Modern cross-institutional services require trusted identity, permissions, and discoverability.”*
>
> — ***Rainier Mallol***, *The Operative Republic*

### Architecture

Software Architecture Design by ***Rainier Mallol***, alongside the best practices outlined in his vision paper [*The Operative Republic: Building an Operating System for Dominican Government Services*](https://publicintelligence.ai/govos).

The paper argues that the next phase of public-sector modernization will be defined less by portals and more by **Digital Public Infrastructure**: shared, interoperable capabilities that many institutions can reuse instead of rebuilding. Its architecture, **Federated Bounded-Context Microservices with Centralized Orchestration (FeBMO)**, lets each institution retain ownership of its systems and data, while shared identity, workflow governance, audit trails and service standards bring them together as one coherent service environment. Each system is designed to create value on its own and to become more powerful when connected to the others.

GOB-ID is the shared identity layer of that architecture. It provides authentication, authorization, role-based (RBAC) and attribute-based (ABAC) access control, and an application catalogue shared across subscribed institutions, so every application relies on one single sign-on instead of building its own. Its model keeps three questions apart, *who you are*, *where you work* and *what you can do*, so identity, institutional relationships and application permissions stay consistent across the whole ecosystem. Alongside it, **IdentityHub** provides person and company lookups, in line with the paper's intent to reuse official sources of truth without replacing the legal responsibilities of the institutions that own them.

**[Read the vision paper →](https://publicintelligence.ai/govos)**

### Engineering

**Principal Engineer**

- José Cid (cidtroncoso)

**Engineers**

- Angel Peña
- Frailin Ogando
- Cristian Jimenez

*Built to help move government services from fragmented coordination to structured execution.*
