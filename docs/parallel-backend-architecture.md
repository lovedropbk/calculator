Parallel Backend (fc-svc) — Architecture Draft

Goals
- Mirror the MVP fc-api REST interface while keeping a clean, modular, production-grade layout.
- Decouple HTTP, application assembly, and engines so we can evolve protocols (REST -> gRPC), authorization, and configuration independently.

Modules
- cmd/fc-svc: thin composition root and process entrypoint.
- internal/config: env-based runtime configuration (port, CORS).
- internal/server: application assembly (wires services to ports).
- internal/ports/httpserver: HTTP router and middleware.
- internal/services:
  - adapters: bridges to repo-native services (parameters.Service).
  - enginesvc: thin wrappers around engines (calculator, campaigns) and parameter mapping.

Endpoints (parity with fc-api)
- GET /healthz
- GET /api/v1/parameters/current
- GET /api/v1/commission/auto?product=HP
- GET /api/v1/campaigns/catalog
- POST /api/v1/campaigns/summaries
- POST /api/v1/calculate

Parameter Mapping
- parameters.ParameterSet -> engines/types.ParameterSet
  - Sort cost of funds curve by term.
  - OPEX key transform: "HP" -> "HP_opex" etc.
  - Economic capital BaseCapitalRatio set to 8.8% per HQ guidance.

Commission Policy
- Commission lookup delegated to parameters.Service via adapter; campaigns engine uses this via SetCommissionLookup.

Next Enhancements
- Add build/version endpoint (GET /version).
- Add JWT/Windows auth middleware (optional).
- Support gRPC port side-by-side with REST.
- Add structured logging and request tracing.
