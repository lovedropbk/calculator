package parameters

import (
	"encoding/json"
	"fmt"
	"sync"
	"time"

	"github.com/shopspring/decimal"
)

// Service manages parameter sets with caching and synchronization
type Service struct {
	storage      *Storage
	cache        map[string]*ParameterSet
	currentID    string
	lastSyncTime time.Time
	mu           sync.RWMutex
	syncClient   *SyncClient
}

// NewService creates a new parameter service
func NewService() (*Service, error) {
	storage, err := NewStorage()
	if err != nil {
		return nil, fmt.Errorf("failed to create storage: %w", err)
	}

	service := &Service{
		storage:    storage,
		cache:      make(map[string]*ParameterSet),
		syncClient: NewSyncClient(),
	}

	// Initialize with default parameters if no parameters exist
	if err := service.initializeDefaults(); err != nil {
		return nil, fmt.Errorf("failed to initialize defaults: %w", err)
	}

	// Load latest parameter set into cache
	if err := service.loadLatestToCache(); err != nil {
		// Non-critical: we have defaults
		fmt.Printf("Warning: failed to load latest parameters: %v\n", err)
	}

	return service, nil
}

// LoadLatest loads the most recent cached ParameterSet
func (s *Service) LoadLatest() (*ParameterSet, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	// Check cache first
	if s.currentID != "" {
		if params, ok := s.cache[s.currentID]; ok {
			return params.Clone(), nil
		}
	}

	// Load from storage
	params, err := s.storage.LoadLatest()
	if err != nil {
		// Fall back to default parameters
		return s.getDefaultParameters(), nil
	}

	// Update cache
	s.mu.RUnlock()
	s.mu.Lock()
	s.cache[params.ID] = params
	s.currentID = params.ID
	s.mu.Unlock()
	s.mu.RLock()

	return params.Clone(), nil
}

// LoadByVersion loads a specific version of ParameterSet
func (s *Service) LoadByVersion(versionID string) (*ParameterSet, error) {
	s.mu.RLock()

	// Check cache first
	if params, ok := s.cache[versionID]; ok {
		s.mu.RUnlock()
		return params.Clone(), nil
	}
	s.mu.RUnlock()

	// Load from storage
	params, err := s.storage.Load(versionID)
	if err != nil {
		return nil, fmt.Errorf("failed to load version %s: %w", versionID, err)
	}

	// Update cache
	s.mu.Lock()
	s.cache[versionID] = params
	s.mu.Unlock()

	return params.Clone(), nil
}

// Save saves a ParameterSet with checksum validation
func (s *Service) Save(params *ParameterSet) error {
	if params == nil {
		return fmt.Errorf("parameter set is nil")
	}

	// Validate before saving
	if errors := params.Validate(); len(errors) > 0 {
		return fmt.Errorf("validation failed: %v", errors)
	}

	// Save to storage
	if err := s.storage.Save(params); err != nil {
		return fmt.Errorf("failed to save parameters: %w", err)
	}

	// Update cache
	s.mu.Lock()
	s.cache[params.ID] = params.Clone()
	s.currentID = params.ID
	s.mu.Unlock()

	return nil
}

// Validate validates a ParameterSet
func (s *Service) Validate(params *ParameterSet) []ValidationError {
	if params == nil {
		return []ValidationError{{
			Field:   "parameters",
			Message: "Parameter set is nil",
			Code:    "NIL_PARAMS",
		}}
	}
	return params.Validate()
}

// GetAvailableVersions returns all cached versions
func (s *Service) GetAvailableVersions() ([]VersionInfo, error) {
	return s.storage.GetAvailableVersions()
}

// GetCurrentVersion returns the currently loaded parameter version
func (s *Service) GetCurrentVersion() string {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.currentID
}

// SetCurrentVersion sets the current parameter version
func (s *Service) SetCurrentVersion(versionID string) error {
	params, err := s.LoadByVersion(versionID)
	if err != nil {
		return err
	}

	s.mu.Lock()
	s.currentID = params.ID
	s.mu.Unlock()

	return nil
}

// GetLastSyncTime returns the last sync time
func (s *Service) GetLastSyncTime() time.Time {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.lastSyncTime
}

// SyncFromBackend synchronizes parameters from the backend
func (s *Service) SyncFromBackend(forceSync bool) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	// Check if sync is needed
	if !forceSync && time.Since(s.lastSyncTime) < 1*time.Hour {
		return nil // Skip sync if recently synced
	}

	// Perform sync
	params, err := s.syncClient.FetchLatest()
	if err != nil {
		return fmt.Errorf("sync failed: %w", err)
	}

	if params != nil {
		// Save synced parameters
		if err := s.storage.Save(params); err != nil {
			return fmt.Errorf("failed to save synced parameters: %w", err)
		}

		// Update cache
		s.cache[params.ID] = params
		s.currentID = params.ID
		s.lastSyncTime = time.Now()
	}

	return nil
}

// RefreshCache refreshes the in-memory cache from storage
func (s *Service) RefreshCache() error {
	s.mu.Lock()
	defer s.mu.Unlock()

	// Clear existing cache
	s.cache = make(map[string]*ParameterSet)

	// Load latest
	params, err := s.storage.LoadLatest()
	if err != nil {
		return fmt.Errorf("failed to refresh cache: %w", err)
	}

	s.cache[params.ID] = params
	s.currentID = params.ID

	return nil
}

// ExportVersion exports a specific version to JSON
func (s *Service) ExportVersion(versionID string) (string, error) {
	params, err := s.LoadByVersion(versionID)
	if err != nil {
		return "", err
	}

	data, err := json.MarshalIndent(params, "", "  ")
	if err != nil {
		return "", fmt.Errorf("failed to marshal parameters: %w", err)
	}

	return string(data), nil
}

// ImportParameters imports parameters from JSON
func (s *Service) ImportParameters(jsonData string) (*ParameterSet, error) {
	var params ParameterSet
	if err := json.Unmarshal([]byte(jsonData), &params); err != nil {
		return nil, fmt.Errorf("failed to unmarshal parameters: %w", err)
	}

	// Validate
	if errors := params.Validate(); len(errors) > 0 {
		return nil, fmt.Errorf("imported parameters are invalid: %v", errors)
	}

	// Save
	if err := s.Save(&params); err != nil {
		return nil, fmt.Errorf("failed to save imported parameters: %w", err)
	}

	return &params, nil
}

// CreateVersion creates a new version based on an existing one
func (s *Service) CreateVersion(baseVersionID string, newID string, description string) (*ParameterSet, error) {
	// Load base version
	base, err := s.LoadByVersion(baseVersionID)
	if err != nil {
		return nil, fmt.Errorf("failed to load base version: %w", err)
	}

	// Clone and update
	newParams := base.Clone()
	newParams.ID = newID
	newParams.CreatedAt = time.Now()
	newParams.Description = description
	newParams.IsDefault = false

	// Save new version
	if err := s.Save(newParams); err != nil {
		return nil, fmt.Errorf("failed to save new version: %w", err)
	}

	return newParams, nil
}

// VerifyIntegrity verifies the integrity of all stored parameters
func (s *Service) VerifyIntegrity() ([]IntegrityResult, error) {
	return s.storage.VerifyIntegrity()
}

// RecoverFromCorruption attempts to recover from corruption
func (s *Service) RecoverFromCorruption(versionID string) error {
	params, err := s.storage.RecoverFromCorruption(versionID)
	if err != nil {
		return err
	}

	// Update cache if recovered
	s.mu.Lock()
	s.cache[params.ID] = params
	s.mu.Unlock()

	return nil
}

// loadLatestToCache loads the latest parameters into cache
func (s *Service) loadLatestToCache() error {
	params, err := s.storage.LoadLatest()
	if err != nil {
		return err
	}

	s.mu.Lock()
	s.cache[params.ID] = params
	s.currentID = params.ID
	s.mu.Unlock()

	return nil
}

// initializeDefaults ensures default parameters exist
func (s *Service) initializeDefaults() error {
	// Check if any parameters exist
	versions, err := s.storage.GetAvailableVersions()
	if err == nil && len(versions) > 0 {
		return nil // Parameters already exist
	}

	// Create and save default parameters
	defaults := s.getDefaultParameters()
	return s.storage.Save(defaults)
}

// getDefaultParameters returns the default/fallback parameter set
func (s *Service) getDefaultParameters() *ParameterSet {
	now := time.Now()

	return &ParameterSet{
		ID:            "2025-08",
		EffectiveDate: now,
		CreatedAt:     now,
		CreatedBy:     "system",
		Description:   "Default parameter set for Thailand MVP",
		IsDefault:     true,

		// Cost of funds curve (Thai market rates by term in months)
		CostOfFunds: map[int]float64{
			6:  0.0120, // 1.20%
			12: 0.0148, // 1.48%
			24: 0.0165, // 1.65%
			36: 0.0175, // 1.75%
			48: 0.0185, // 1.85%
			60: 0.0195, // 1.95%
			72: 0.0205, // 2.05%
			84: 0.0215, // 2.15%
		},

		// Matched funded spread
		MatchedSpread: 0.0025, // 25 bps

		// PD/LGD tables by product and segment
		PDLGDTables: map[string]PDLGDParams{
			"HP_default": {
				Product:     "HP",
				Segment:     "default",
				PD:          0.0200, // 2.00%
				LGD:         0.4500, // 45.00%
				Description: "Hire Purchase default segment",
			},
			"mySTAR_default": {
				Product:     "mySTAR",
				Segment:     "default",
				PD:          0.0250, // 2.50%
				LGD:         0.4000, // 40.00%
				Description: "mySTAR balloon finance default segment",
			},
			"F-Lease_default": {
				Product:     "F-Lease",
				Segment:     "default",
				PD:          0.0180, // 1.80%
				LGD:         0.3500, // 35.00%
				Description: "Finance Lease default segment",
			},
			"Op-Lease_default": {
				Product:     "Op-Lease",
				Segment:     "default",
				PD:          0.0150, // 1.50%
				LGD:         0.3000, // 30.00%
				Description: "Operating Lease default segment",
			},
		},

		// OPEX rates by product
		OPEXRates: map[string]float64{
			"HP":       0.0068, // 68 bps
			"mySTAR":   0.0072, // 72 bps
			"F-Lease":  0.0065, // 65 bps
			"Op-Lease": 0.0070, // 70 bps
		},

		// Economic capital parameters
		EconomicCapital: EconomicCapitalParams{
			BaseCapitalRatio:     0.1200, // 12.00%
			CapitalAdvantage:     0.0008, // 8 bps
			DTLAdvantage:         0.0003, // 3 bps
			SecurityDepAdvantage: 0.0002, // 2 bps
			OtherAdvantage:       0.0001, // 1 bp
		},

		// Central HQ add-on
		CentralHQAddOn: 0.0015, // 15 bps

		// Rounding rules
		RoundingRules: RoundingParams{
			Currency:      "THB",
			MinorUnits:    0,      // Round to whole THB
			Method:        "bank", // Banker's rounding
			DisplayRate:   4,      // Display rates to basis points
			InstallmentTo: 1,      // Round installment to nearest 1 THB
		},

		// Day count convention
		DayCountConvention: "ACT/365",

		// Campaign catalog
		CampaignCatalog: []CampaignDefinition{
			{
				ID:            "SUBDOWN-5",
				Name:          "5% Subdown",
				Type:          "subdown",
				Description:   "5% down payment subsidy from dealer",
				ValidFrom:     now,
				ValidUntil:    now.AddDate(1, 0, 0),
				Parameters:    map[string]interface{}{"subsidy_percent": 0.05},
				Funder:        "Dealer",
				StackingOrder: 1,
				Active:        true,
			},
			{
				ID:            "SUBINT-299",
				Name:          "2.99% Interest Rate",
				Type:          "subinterest",
				Description:   "Subsidized interest rate at 2.99%",
				ValidFrom:     now,
				ValidUntil:    now.AddDate(1, 0, 0),
				Parameters:    map[string]interface{}{"target_rate": 0.0299},
				Funder:        "Manufacturer",
				StackingOrder: 2,
				Active:        true,
			},
			{
				ID:            "FREE-INS",
				Name:          "Free Insurance",
				Type:          "free_insurance",
				Description:   "Free insurance coverage",
				ValidFrom:     now,
				ValidUntil:    now.AddDate(1, 0, 0),
				Parameters:    map[string]interface{}{"insurance_cost": 15000.0},
				Funder:        "Insurance Partner",
				StackingOrder: 3,
				Active:        true,
			},
			{
				ID:            "FREE-MBSP",
				Name:          "Free MBSP",
				Type:          "free_mbsp",
				Description:   "Free maintenance and service package",
				ValidFrom:     now,
				ValidUntil:    now.AddDate(1, 0, 0),
				Parameters:    map[string]interface{}{"mbsp_cost": 5000.0},
				Funder:        "Manufacturer",
				StackingOrder: 4,
				Active:        true,
			},
			{
				ID:            "CASH-DISC-2",
				Name:          "2% Cash Discount",
				Type:          "cash_discount",
				Description:   "2% cash discount on vehicle price",
				ValidFrom:     now,
				ValidUntil:    now.AddDate(1, 0, 0),
				Parameters:    map[string]interface{}{"discount_percent": 0.02},
				Funder:        "Dealer",
				StackingOrder: 5,
				Active:        true,
			},
		},
	}
}

// ConvertToEngineFormat converts ParameterSet to engine's expected format
func (s *Service) ConvertToEngineFormat(params *ParameterSet) map[string]interface{} {
	if params == nil {
		return nil
	}

	engineParams := make(map[string]interface{})

	// Convert cost of funds curve
	cofCurve := make([]map[string]interface{}, 0)
	for term, rate := range params.CostOfFunds {
		cofCurve = append(cofCurve, map[string]interface{}{
			"term_months": term,
			"rate":        decimal.NewFromFloat(rate),
		})
	}
	engineParams["cost_of_funds_curve"] = cofCurve

	// Convert matched spread
	engineParams["matched_funded_spread"] = decimal.NewFromFloat(params.MatchedSpread)

	// Convert PD/LGD tables
	pdlgd := make(map[string]interface{})
	for key, params := range params.PDLGDTables {
		pdlgd[key] = map[string]interface{}{
			"product": params.Product,
			"segment": params.Segment,
			"pd":      decimal.NewFromFloat(params.PD),
			"lgd":     decimal.NewFromFloat(params.LGD),
		}
	}
	engineParams["pd_lgd"] = pdlgd

	// Convert OPEX rates
	opexRates := make(map[string]interface{})
	for product, rate := range params.OPEXRates {
		// Engine expects keys like "HP_opex"
		key := product + "_opex"
		opexRates[key] = decimal.NewFromFloat(rate)
	}
	engineParams["opex_rates"] = opexRates

	// Convert economic capital
	engineParams["economic_capital_params"] = map[string]interface{}{
		"base_capital_ratio":     decimal.NewFromFloat(params.EconomicCapital.BaseCapitalRatio),
		"capital_advantage":      decimal.NewFromFloat(params.EconomicCapital.CapitalAdvantage),
		"dtl_advantage":          decimal.NewFromFloat(params.EconomicCapital.DTLAdvantage),
		"security_dep_advantage": decimal.NewFromFloat(params.EconomicCapital.SecurityDepAdvantage),
		"other_advantage":        decimal.NewFromFloat(params.EconomicCapital.OtherAdvantage),
	}

	// Convert other fields
	engineParams["central_hq_addon"] = decimal.NewFromFloat(params.CentralHQAddOn)
	engineParams["day_count_convention"] = params.DayCountConvention

	// Convert rounding rules
	engineParams["rounding_rules"] = map[string]interface{}{
		"currency":     params.RoundingRules.Currency,
		"minor_units":  params.RoundingRules.MinorUnits,
		"method":       params.RoundingRules.Method,
		"display_rate": params.RoundingRules.DisplayRate,
	}

	// Add metadata
	engineParams["id"] = params.ID
	engineParams["version"] = params.ID
	engineParams["effective_date"] = params.EffectiveDate

	return engineParams
}

// GetStatus returns the current service status
func (s *Service) GetStatus() map[string]interface{} {
	s.mu.RLock()
	defer s.mu.RUnlock()

	versions, _ := s.storage.GetAvailableVersions()

	return map[string]interface{}{
		"current_version":    s.currentID,
		"last_sync_time":     s.lastSyncTime,
		"cached_versions":    len(s.cache),
		"available_versions": len(versions),
		"storage_path":       s.storage.basePath,
	}
}

// CommissionPercentByProduct returns the commission percent for the given product.
// See docs/financial-calculator-architecture.md (policy section). JSON shape matches:
// { "commissionPolicy": { "version": "...", "byProductPct": {"HP": 0.01}, "notes": "..." } }
// Fallback behavior: if no policy or missing product key, return 0.0. Negative values are clamped to 0.0.
// Case-sensitive lookup; no key transformation is performed.
func (s *Service) CommissionPercentByProduct(product string) float64 {
	// Empty product → no default
	if product == "" {
		return 0.0
	}

	s.mu.RLock()
	params := s.cache[s.currentID]
	s.mu.RUnlock()

	// Try policy lookups with key synonyms first (clamp negatives to 0)
	if params != nil && params.CommissionPolicy.ByProductPct != nil {
		for _, key := range commissionKeysFor(product) {
			if v, ok := params.CommissionPolicy.ByProductPct[key]; ok {
				if v < 0 {
					return 0.0
				}
				return v
			}
		}
	}

	// Fallback defaults when policy missing or product key not present
	switch product {
	case "HP", "HirePurchase":
		return 0.03
	case "mySTAR", "BalloonHP", "Balloon":
		return 0.07
	case "F-Lease", "FinanceLease", "Finance Lease", "FLease", "F Lease":
		return 0.07
	case "Op-Lease", "OperatingLease", "Operating Lease", "OpLease", "Op Lease":
		return 0.07
	default:
		return 0.0
	}
}

// CommissionPolicyVersion returns the configured commission policy version.
// If the policy is missing or version is empty, returns the empty string.
func (s *Service) CommissionPolicyVersion() string {
	s.mu.RLock()
	params := s.cache[s.currentID]
	s.mu.RUnlock()

	if params == nil {
		return ""
	}
	return params.CommissionPolicy.Version
}

// commissionKeysFor returns known key synonyms for a product for commission policy lookup.
func commissionKeysFor(product string) []string {
	switch product {
	case "HP", "HirePurchase", "Hire Purchase", "hp", "Hp":
		return []string{"HP", "HirePurchase", "Hire Purchase"}
	case "mySTAR", "mystar", "MySTAR", "BalloonHP", "Balloon":
		return []string{"mySTAR", "BalloonHP", "Balloon"}
	case "F-Lease", "FinanceLease", "Finance Lease", "FLease", "F Lease", "Financing Lease":
		return []string{"F-Lease", "FinanceLease", "Finance Lease", "FLease", "F Lease", "Financing Lease"}
	case "Op-Lease", "OperatingLease", "Operating Lease", "OpLease", "Op Lease":
		return []string{"Op-Lease", "OperatingLease", "Operating Lease", "OpLease", "Op Lease"}
	default:
		return []string{product}
	}
}
