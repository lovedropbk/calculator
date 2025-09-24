//go:build windows

package main

// UIState captures only the input fields needed for validation/orchestration.
// Keep this independent of Walk runtime so it can be tested as pure functions.
type UIState struct {
	Product    string  // "HP" | "mySTAR" | "F-Lease" | "Op-Lease"
	PriceExTax float64 // THB
	TermMonths int     // months
	BalloonPct float64 // percent, e.g., 10.0 means 10%
}

// ValidationError is a typed error describing why inputs are invalid.
type ValidationError struct {
	Reason string
}

func (e *ValidationError) Error() string {
	return e.Reason
}

// validateInputs enforces minimal guards that must pass before any compute.
// Rules:
// - PriceExTax > 0
// - TermMonths > 0
// - For mySTAR: BalloonPct >= 0 and if BalloonPct > 0 then TermMonths >= 6
func validateInputs(s UIState) error {
	if s.PriceExTax <= 0 {
		return &ValidationError{Reason: "price must be positive"}
	}
	if s.TermMonths <= 0 {
		return &ValidationError{Reason: "term must be positive"}
	}
	if s.Product == "mySTAR" {
		if s.BalloonPct < 0 {
			return &ValidationError{Reason: "balloon percent must be ≥ 0"}
		}
		if s.BalloonPct > 0 && s.TermMonths < 6 {
			return &ValidationError{Reason: "term must be ≥ 6 when balloon > 0 for mySTAR"}
		}
	}
	return nil
}

// shouldCompute returns true only when validateInputs returns nil.
func shouldCompute(s UIState) bool {
	return validateInputs(s) == nil
}
