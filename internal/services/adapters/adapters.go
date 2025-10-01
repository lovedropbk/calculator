package adapters

import (
	"financial-calculator/parameters"
)

type Adapters struct {
	Params *parameters.Service
}

func New() (*Adapters, error) {
	ps, err := parameters.NewService()
	if err != nil {
		return nil, err
	}
	return &Adapters{Params: ps}, nil
}
