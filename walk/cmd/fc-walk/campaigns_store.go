package main

import (
	"encoding/json"
	"errors"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"
)

// MARK: Schema & Types

// CampaignsFileVersion is the version of the persisted JSON file schema.
const CampaignsFileVersion = 1

// ErrFutureVersion is returned when a campaigns file has a newer version than this app supports.
var ErrFutureVersion = errors.New("future campaigns file version")

// CampaignInputs represents high-level deal inputs persisted for a custom campaign.
type CampaignInputs struct {
	PriceExTaxTHB        float64 `json:"priceExTaxTHB"`
	DownpaymentPercent   float64 `json:"downpaymentPercent"`
	DownpaymentTHB       float64 `json:"downpaymentTHB"`
	TermMonths           int     `json:"termMonths"`
	BalloonPercent       float64 `json:"balloonPercent"`
	RateMode             string  `json:"rateMode"`             // "fixed_rate" | "target_installment"
	CustomerRateAPR      float64 `json:"customerRateAPR"`      // e.g., 0.0399 means 3.99% p.a.
	TargetInstallmentTHB float64 `json:"targetInstallmentTHB"` // THB
}

// CampaignAdjustments represents the campaign-specific adjustments (MVP fields).
type CampaignAdjustments struct {
	CashDiscountTHB     float64 `json:"cashDiscountTHB"`
	SubdownTHB          float64 `json:"subdownTHB"`
	
	// Actual costs for benefits (what it actually costs the finance company)
	FreeInsuranceCostTHB float64 `json:"freeInsuranceCostTHB"`
	FreeMBSPCostTHB      float64 `json:"freeMBSPCostTHB"`
	
	// Legacy fields - kept for backward compatibility, migrate on load if new fields are 0
	IDCFreeInsuranceTHB float64 `json:"idcFreeInsuranceTHB,omitempty"` // Deprecated: use FreeInsuranceCostTHB
	IDCFreeMBSPTHB      float64 `json:"idcFreeMBSPTHB,omitempty"`      // Deprecated: use FreeMBSPCostTHB
	
	IDCOtherTHB         float64 `json:"idcOtherTHB,omitempty"`
}

// CampaignMetadata holds bookkeeping information for a campaign record.
type CampaignMetadata struct {
	CreatedAt string `json:"createdAt"`         // RFC3339
	UpdatedAt string `json:"updatedAt"`         // RFC3339
	Version   int    `json:"version,omitempty"` // per-record version (optional)
}

// CampaignDraft is the persisted record for one custom campaign.
type CampaignDraft struct {
	ID          string              `json:"id"` // stable UUID
	Name        string              `json:"name"`
	Product     string              `json:"product"` // "HP" | "mySTAR" | "F-Lease" | "Op-Lease"
	Inputs      CampaignInputs      `json:"inputs"`
	Adjustments CampaignAdjustments `json:"adjustments"`
	Metadata    CampaignMetadata    `json:"metadata"`
}

// campaignsFile is the on-disk JSON payload.
type campaignsFile struct {
	FileVersion int             `json:"fileVersion"`
	Campaigns   []CampaignDraft `json:"campaigns"`
}

// MARK: Store (AppData-backed)

var campaignsStoreMu sync.Mutex

// campaignsPathResolver can be overridden by tests to control the storage location.
var campaignsPathResolver = defaultCampaignsPathResolver

// resolveCampaignsFilePath returns the per-user file path for campaigns.json.
func resolveCampaignsFilePath() (string, error) {
	return campaignsPathResolver()
}

// defaultCampaignsPathResolver mirrors stateFilePath() logic:
// Primary: %AppData%\FinancialCalculator\campaigns.json
// Fallback: ./walk/bin/campaigns.json
func defaultCampaignsPathResolver() (string, error) {
	if appData := os.Getenv("APPDATA"); strings.TrimSpace(appData) != "" {
		dir := filepath.Join(appData, "FinancialCalculator")
		if err := os.MkdirAll(dir, 0o755); err == nil {
			return filepath.Join(dir, "campaigns.json"), nil
		}
	}
	if err := os.MkdirAll("walk/bin", 0o755); err != nil {
		return "", err
	}
	return filepath.Join("walk", "bin", "campaigns.json"), nil
}

// LoadCampaigns reads campaigns from disk.
// - If the file does not exist: returns empty list, current file version, nil error.
// - If JSON is corrupted: returns nil slice, version=0, and a non-nil error.
// - If fileVersion > current: returns parsed campaigns and ErrFutureVersion.
// - If fileVersion <= current: returns parsed campaigns and the file's version.
func LoadCampaigns() ([]CampaignDraft, int, error) {
	path, err := resolveCampaignsFilePath()
	if err != nil {
		return nil, CampaignsFileVersion, err
	}

	var cf campaignsFile
	_, err = readJSONFile(path, &cf)
	if err != nil {
		if os.IsNotExist(err) {
			return []CampaignDraft{}, CampaignsFileVersion, nil
		}
		return nil, 0, err
	}

	if cf.FileVersion == 0 {
		cf.FileVersion = 1
	}

	if cf.FileVersion > CampaignsFileVersion {
		return cf.Campaigns, cf.FileVersion, ErrFutureVersion
	}

	// v1 or older (no-op migration for MVP): return as-is with the file's version.
	return cf.Campaigns, cf.FileVersion, nil
}

// SaveCampaigns writes the entire list atomically and is serialized by a process-wide mutex.
// It does not mutate the input records (timestamps are treated as data owned by the caller).
func SaveCampaigns(list []CampaignDraft) error {
	path, err := resolveCampaignsFilePath()
	if err != nil {
		return err
	}

	campaignsStoreMu.Lock()
	defer campaignsStoreMu.Unlock()

	if err := ensureParentDir(path); err != nil {
		return err
	}

	payload := campaignsFile{
		FileVersion: CampaignsFileVersion,
		Campaigns:   list,
	}
	return atomicWriteJSON(path, payload)
}

// ClearCampaigns truncates the list to empty and persists it atomically.
func ClearCampaigns() error {
	return SaveCampaigns([]CampaignDraft{})
}

// MARK: Helpers

// ensureParentDir ensures the directory for a path exists.
func ensureParentDir(path string) error {
	dir := filepath.Dir(path)
	return os.MkdirAll(dir, 0o755)
}

// readJSONFile reads a JSON file into out. If out is *campaignsFile,
// the returned int is the fileVersion field, otherwise 0.
func readJSONFile(path string, out any) (int, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return 0, err
	}
	if err := json.Unmarshal(data, out); err != nil {
		return 0, err
	}
	if cf, ok := out.(*campaignsFile); ok {
		return cf.FileVersion, nil
	}
	return 0, nil
}

// atomicWriteJSON writes JSON to path using a temp file, fsync, and rename.
// On Windows, if rename fails due to existing file, it removes the target and retries.
func atomicWriteJSON(path string, v any) error {
	tmp := path + ".tmp"

	f, err := os.OpenFile(tmp, os.O_CREATE|os.O_WRONLY|os.O_TRUNC, 0o644)
	if err != nil {
		return err
	}

	enc := json.NewEncoder(f)
	enc.SetEscapeHTML(false)
	enc.SetIndent("", "  ")
	if err := enc.Encode(v); err != nil {
		_ = f.Close()
		_ = os.Remove(tmp)
		return err
	}
	if err := f.Sync(); err != nil {
		_ = f.Close()
		_ = os.Remove(tmp)
		return err
	}
	if err := f.Close(); err != nil {
		_ = os.Remove(tmp)
		return err
	}

	if err := os.Rename(tmp, path); err != nil {
		// Windows fallback: replace existing file if needed
		_ = os.Remove(path)
		if err2 := os.Rename(tmp, path); err2 != nil {
			_ = os.Remove(tmp)
			return err2
		}
	}
	return nil
}

func nowRFC3339() string {
	return time.Now().UTC().Format(time.RFC3339)
}

// ValidateCampaignDraft performs minimal validation on a campaign.
// Optional helper (not used by Load/Save).
func ValidateCampaignDraft(c CampaignDraft) error {
	if strings.TrimSpace(c.ID) == "" {
		return errors.New("campaign id is required")
	}
	if strings.TrimSpace(c.Name) == "" {
		return errors.New("campaign name is required")
	}
	if strings.TrimSpace(c.Product) == "" {
		return errors.New("campaign product is required")
	}
	if c.Inputs.TermMonths < 0 {
		return errors.New("termMonths must be non-negative")
	}
	if c.Inputs.PriceExTaxTHB < 0 {
		return errors.New("priceExTaxTHB must be non-negative")
	}
	return nil
}
