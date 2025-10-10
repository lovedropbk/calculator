package httpserver

import (
	"encoding/json"
	"fmt"
	"net/http"

	"financial-calculator/internal/server"
	enginetypes "github.com/financial-calculator/engines/types"
)

type writeJSONFunc func(http.ResponseWriter, int, any)

func NewRouter(app *server.App) http.Handler {
	mux := http.NewServeMux()

	writeJSON := func(w http.ResponseWriter, code int, v any) {
		w.Header().Set("Content-Type", "application/json")
		w.Header().Set("Cache-Control", "no-store")
		if app.Cfg.EnableCORS {
			w.Header().Set("Access-Control-Allow-Origin", "*")
			w.Header().Set("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
			w.Header().Set("Access-Control-Allow-Headers", "Content-Type")
		}
		w.WriteHeader(code)
		_ = json.NewEncoder(w).Encode(v)
	}

	// Preflight
	mux.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		if r.Method == http.MethodOptions {
			if app.Cfg.EnableCORS {
				w.Header().Set("Access-Control-Allow-Origin", "*")
				w.Header().Set("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
				w.Header().Set("Access-Control-Allow-Headers", "Content-Type")
			}
			w.WriteHeader(http.StatusNoContent)
			return
		}
		w.WriteHeader(http.StatusNotFound)
		fmt.Fprintf(w, "not found")
	})

	mux.HandleFunc("/healthz", func(w http.ResponseWriter, r *http.Request) {
		writeJSON(w, http.StatusOK, map[string]string{"status": "ok"})
	})

	mux.HandleFunc("/api/v1/parameters/current", func(w http.ResponseWriter, r *http.Request) {
		params, _ := app.Svcs.Adapters.Params.LoadLatest()
		writeJSON(w, http.StatusOK, map[string]any{
			"parameter_set":             params,
			"engine_parameter_set":      app.Svcs.Engines.EngineParameterSet(),
			"commission_policy_version": app.Svcs.Adapters.Params.CommissionPolicyVersion(),
		})
	})

	mux.HandleFunc("/api/v1/commission/auto", func(w http.ResponseWriter, r *http.Request) {
		product := r.URL.Query().Get("product")
		pct := app.Svcs.Adapters.Params.CommissionPercentByProduct(product)
		writeJSON(w, http.StatusOK, map[string]any{
			"product":       product,
			"percent":       pct,
			"policyVersion": app.Svcs.Adapters.Params.CommissionPolicyVersion(),
		})
	})

	mux.HandleFunc("/api/v1/campaigns/catalog", func(w http.ResponseWriter, r *http.Request) {
		writeJSON(w, http.StatusOK, app.Svcs.Engines.Catalog(app.Svcs.Adapters))
	})

	mux.HandleFunc("/api/v1/campaigns/summaries", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			w.WriteHeader(http.StatusMethodNotAllowed)
			return
		}
		var req struct {
			Deal      enginetypes.Deal       `json:"deal"`
			State     enginetypes.DealState  `json:"state"`
			Campaigns []enginetypes.Campaign `json:"campaigns"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			writeJSON(w, http.StatusBadRequest, map[string]string{"error": fmt.Sprintf("invalid json: %v", err)})
			return
		}
		s := app.Svcs.Engines.Summaries(req.Deal, req.State, req.Campaigns)
		writeJSON(w, http.StatusOK, s)
	})

	mux.HandleFunc("/api/v1/calculate", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			w.WriteHeader(http.StatusMethodNotAllowed)
			return
		}
		var req enginetypes.CalculationRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			writeJSON(w, http.StatusBadRequest, map[string]string{"error": fmt.Sprintf("invalid json: %v", err)})
			return
		}
		res, err := app.Svcs.Engines.Calculate(req)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, map[string]string{"error": err.Error()})
			return
		}
		writeJSON(w, http.StatusOK, res)
	})

	return mux
}
