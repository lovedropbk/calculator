package services

import (
	"fmt"

	"financial-calculator/internal/config"
	"financial-calculator/internal/services/adapters"
	"financial-calculator/internal/services/enginesvc"
)

type Services struct {
	Engines *enginesvc.EngineService
	Adapters *adapters.Adapters
}

func New(cfg config.Config) (*Services, error) {
	ad, err := adapters.New()
	if err != nil {
		return nil, fmt.Errorf("adapters: %w", err)
	}
	eng, err := enginesvc.New(ad)
	if err != nil {
		return nil, fmt.Errorf("engine: %w", err)
	}
	return &Services{Engines: eng, Adapters: ad}, nil
}
