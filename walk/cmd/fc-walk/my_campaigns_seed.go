package main

import (
	"fmt"
	"time"
)

// SeedCopyDraft creates a CampaignDraft seeded from provided high-level inputs.
// - id := fmt.Sprintf("mc-%d", time.Now().UTC().UnixNano())
// - metadata timestamps := time.Now().UTC().Format(time.RFC3339)
// - product := param
// - inputs := param
// - adjustments := zeros
// - name := param (fallback to "New Campaign" if empty)
func SeedCopyDraft(name string, product string, inputs CampaignInputs) CampaignDraft {
	id := fmt.Sprintf("mc-%d", time.Now().UTC().UnixNano())
	if name == "" {
		name = "New Campaign"
	}
	now := time.Now().UTC().Format(time.RFC3339)
	return CampaignDraft{
		ID:          id,
		Name:        name,
		Product:     product,
		Inputs:      inputs,
		Adjustments: CampaignAdjustments{},
		Metadata: CampaignMetadata{
			CreatedAt: now,
			UpdatedAt: now,
			Version:   1,
		},
	}
}
