package main

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"

	"financial-calculator/parameters"

	enginetypes "github.com/financial-calculator/engines/types"
)

func main() {
	port := os.Getenv("FC_API_PORT")
	if port == "" {
		port = "8123"
	}

	// Initialize parameter service (with defaults if none exist)
	psvc, err := parameters.NewService()
	if err != nil {
		log.Fatalf("failed to init parameter service: %v", err)
	}

	// Preload engine ParameterSet from current parameters
	params, err := psvc.LoadLatest()
	if err != nil {
		log.Printf("warning: loading latest parameters failed, proceeding with in-memory defaults: %v", err)
		// NewService() initializes defaults; LoadLatest should have returned defaults on error.
	}
	enginePS := convertParametersToEngine(params)

	mux := http.NewServeMux()
	mux.HandleFunc("/healthz", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("ok"))
	})
	mux.Handle("/api/v1/parameters/current", cors(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		resp := map[string]any{
			"parameter_set": params,
			"engine_parameter_set": enginePS,
			"commission_policy_version": psvc.CommissionPolicyVersion(),
		}
		writeJSON(w, http.StatusOK, resp)
	})))
	mux.Handle("/api/v1/commission/auto", cors(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		q := r.URL.Query().Get("product")
		pct := psvc.CommissionPercentByProduct(q)
		resp := map[string]any{
			"product": q,
			"percent": pct,
			"policyVersion": psvc.CommissionPolicyVersion(),
		}
		writeJSON(w, http.StatusOK, resp)
	})))
	mux.Handle("/api/v1/campaigns/catalog", cors(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		writeJSON(w, http.StatusOK, mapCatalogToEngineCampaigns(params))
	})))
	mux.Handle("/api/v1/campaigns/summaries", cors(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			w.WriteHeader(http.StatusMethodNotAllowed)
			return
		}
		var req CampaignSummariesRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			writeJSON(w, http.StatusBadRequest, map[string]string{"error": fmt.Sprintf("invalid json: %v", err)})
			return
		}
		// Compute summaries
		summaries, err := generateSummaries(enginePS, psvc, req.Deal, req.State, req.Campaigns)
		if err != nil {
			writeJSON(w, http.StatusInternalServerError, map[string]string{"error": err.Error()})
			return
		}
		writeJSON(w, http.StatusOK, summaries)
	})))
	mux.Handle("/api/v1/calculate", cors(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			w.WriteHeader(http.StatusMethodNotAllowed)
			return
		}
		var req enginetypes.CalculationRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			writeJSON(w, http.StatusBadRequest, map[string]string{"error": fmt.Sprintf("invalid json: %v", err)})
			return
		}
		// Enforce parameter set to current enginePS unless explicitly overridden
		if req.ParameterSet.ID == "" {
			req.ParameterSet = enginePS
		}
		// Optional: derive separated IDC components for UI
		if req.Options == nil {
			req.Options = map[string]any{}
		}
		if _, ok := req.Options["derive_idc_from_cf"]; !ok {
			req.Options["derive_idc_from_cf"] = true
		}
		res, err := calculate(enginePS, req)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, map[string]string{"error": err.Error()})
			return
		}
		writeJSON(w, http.StatusOK, res)
	})))

	addr := ":" + port
	log.Printf("fc-api listening on %s", addr)
	if err := http.ListenAndServe(addr, mux); err != nil {
		log.Fatal(err)
	}
}

func writeJSON(w http.ResponseWriter, code int, v any) {
	w.Header().Set("Content-Type", "application/json")
	w.Header().Set("Cache-Control", "no-store")
	w.WriteHeader(code)
	json.NewEncoder(w).Encode(v)
}

// Simple permissive CORS for local dev with WinUI 3
func cors(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Access-Control-Allow-Origin", "*")
		w.Header().Set("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
		w.Header().Set("Access-Control-Allow-Headers", "Content-Type")
		if r.Method == http.MethodOptions {
			w.WriteHeader(http.StatusNoContent)
			return
		}
		next.ServeHTTP(w, r)
	})
}
