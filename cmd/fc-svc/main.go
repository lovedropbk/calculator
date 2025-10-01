package main

import (
	"log"
	"net/http"
	"os"

	"financial-calculator/internal/config"
	"financial-calculator/internal/ports/httpserver"
	"financial-calculator/internal/server"
)

func main() {
	cfg := config.FromEnv()

	// Initialize application server (engines + parameter service)
	app, err := server.New(cfg)
	if err != nil {
		log.Fatalf("init failed: %v", err)
	}

	mux := httpserver.NewRouter(app)

	addr := cfg.ListenAddr()
	log.Printf("fc-svc listening on %s (env FC_SVC_PORT=%s)", addr, os.Getenv("FC_SVC_PORT"))
	if err := http.ListenAndServe(addr, httpserver.CORS(cfg.EnableCORS, mux)); err != nil {
		log.Fatal(err)
	}
}
