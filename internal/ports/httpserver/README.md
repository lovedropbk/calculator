This is the modular, parallel backend (fc-svc) HTTP layer.

- Router: internal/ports/httpserver/router.go
- App assembly: internal/server/app.go
- Services: internal/services/
  - adapters: parameter service adapter
  - enginesvc: thin wrapper around engines (calculator, campaigns) with parameter mapping

Endpoints: mirror the MVP fc-api service
- GET /healthz
- GET /api/v1/parameters/current
- GET /api/v1/commission/auto?product=HP
- GET /api/v1/campaigns/catalog
- POST /api/v1/campaigns/summaries
- POST /api/v1/calculate

Environment
- FC_SVC_PORT (default 8223)
- FC_SVC_CORS (default enabled unless set to 0)
- FC_SVC_LOG_JSON (future)
- FC_SVC_ALLOW_INSECURE (future)
