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
	return SeedCopyDraftWithAdjustments(name, product, inputs, CampaignAdjustments{})
}

// SeedCopyDraftWithAdjustments creates a CampaignDraft with specified adjustments.
// This allows copying campaigns with their campaign-specific values (e.g., Free MBSP amounts).
func SeedCopyDraftWithAdjustments(name string, product string, inputs CampaignInputs, adjustments CampaignAdjustments) CampaignDraft {
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
		Adjustments: adjustments,
		Metadata: CampaignMetadata{
			CreatedAt: now,
			UpdatedAt: now,
			Version:   1,
		},
	}
}
