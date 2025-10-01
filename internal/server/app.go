package server

import (
	"fmt"

	"financial-calculator/internal/config"
	"financial-calculator/internal/services"
)

type App struct {
	Cfg   config.Config
	Svcs  *services.Services
}

func New(cfg config.Config) (*App, error) {
	svcs, err := services.New(cfg)
	if err != nil {
		return nil, fmt.Errorf("services init: %w", err)
	}
	return &App{Cfg: cfg, Svcs: svcs}, nil
}
