package parameters

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"runtime"
	"sort"
	"strings"
	"sync"
	"time"
)

// Storage handles local file storage for parameter sets
type Storage struct {
	basePath string
	mu       sync.RWMutex
}

// NewStorage creates a new storage instance
func NewStorage() (*Storage, error) {
	basePath, err := getStoragePath()
	if err != nil {
		return nil, fmt.Errorf("failed to get storage path: %w", err)
	}

	// Create directory if it doesn't exist
	if err := os.MkdirAll(basePath, 0755); err != nil {
		return nil, fmt.Errorf("failed to create storage directory: %w", err)
	}

	return &Storage{
		basePath: basePath,
	}, nil
}

// getStoragePath returns the appropriate storage path based on the OS
func getStoragePath() (string, error) {
	var basePath string

	switch runtime.GOOS {
	case "windows":
		// Use %APPDATA%/FinancialCalculator/parameters
		appData := os.Getenv("APPDATA")
		if appData == "" {
			// Fallback to %USERPROFILE%/AppData/Roaming
			userProfile := os.Getenv("USERPROFILE")
			if userProfile == "" {
				return "", fmt.Errorf("cannot determine Windows app data directory")
			}
			appData = filepath.Join(userProfile, "AppData", "Roaming")
		}
		basePath = filepath.Join(appData, "FinancialCalculator", "parameters")

	case "darwin": // macOS
		// Use ~/Library/Application Support/FinancialCalculator/parameters
		home, err := os.UserHomeDir()
		if err != nil {
			return "", err
		}
		basePath = filepath.Join(home, "Library", "Application Support", "FinancialCalculator", "parameters")

	case "linux":
		// Use ~/.config/FinancialCalculator/parameters
		home, err := os.UserHomeDir()
		if err != nil {
			return "", err
		}
		configDir := os.Getenv("XDG_CONFIG_HOME")
		if configDir == "" {
			configDir = filepath.Join(home, ".config")
		}
		basePath = filepath.Join(configDir, "FinancialCalculator", "parameters")

	default:
		// Fallback to home directory
		home, err := os.UserHomeDir()
		if err != nil {
			return "", err
		}
		basePath = filepath.Join(home, ".financial-calculator", "parameters")
	}

	return basePath, nil
}

// Save stores a parameter set with checksum validation
func (s *Storage) Save(params *ParameterSet) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	if params == nil {
		return fmt.Errorf("parameter set is nil")
	}

	// Validate parameters
	if errors := params.Validate(); len(errors) > 0 {
		return fmt.Errorf("parameter validation failed: %v", errors)
	}

	// Generate filename
	filename := fmt.Sprintf("params_%s.json", params.ID)
	filepath := filepath.Join(s.basePath, filename)
	checksumPath := filepath + ".sha256"

	// Marshal to JSON with indentation
	data, err := json.MarshalIndent(params, "", "  ")
	if err != nil {
		return fmt.Errorf("failed to marshal parameters: %w", err)
	}

	// Calculate checksum
	hash := sha256.Sum256(data)
	checksum := hex.EncodeToString(hash[:])

	// Write atomically using temp file
	tempFile := filepath + ".tmp"
	if err := os.WriteFile(tempFile, data, 0644); err != nil {
		return fmt.Errorf("failed to write temp file: %w", err)
	}

	// Verify temp file is complete
	tempData, err := os.ReadFile(tempFile)
	if err != nil {
		os.Remove(tempFile)
		return fmt.Errorf("failed to verify temp file: %w", err)
	}

	if len(tempData) != len(data) {
		os.Remove(tempFile)
		return fmt.Errorf("temp file size mismatch")
	}

	// Write checksum file
	if err := os.WriteFile(checksumPath, []byte(checksum), 0644); err != nil {
		os.Remove(tempFile)
		return fmt.Errorf("failed to write checksum: %w", err)
	}

	// Atomic rename
	if err := os.Rename(tempFile, filepath); err != nil {
		os.Remove(tempFile)
		return fmt.Errorf("failed to rename temp file: %w", err)
	}

	// Update latest symlink/reference
	if err := s.updateLatest(params.ID); err != nil {
		// Non-critical error, log but don't fail
		fmt.Printf("Warning: failed to update latest reference: %v\n", err)
	}

	return nil
}

// Load reads a parameter set by version ID
func (s *Storage) Load(versionID string) (*ParameterSet, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	filename := fmt.Sprintf("params_%s.json", versionID)
	filepath := filepath.Join(s.basePath, filename)
	checksumPath := filepath + ".sha256"

	// Read the JSON file
	data, err := os.ReadFile(filepath)
	if err != nil {
		if os.IsNotExist(err) {
			return nil, fmt.Errorf("parameter set %s not found", versionID)
		}
		return nil, fmt.Errorf("failed to read parameter file: %w", err)
	}

	// Read and verify checksum if it exists
	if checksumData, err := os.ReadFile(checksumPath); err == nil {
		expectedChecksum := strings.TrimSpace(string(checksumData))
		actualHash := sha256.Sum256(data)
		actualChecksum := hex.EncodeToString(actualHash[:])

		if expectedChecksum != actualChecksum {
			return nil, fmt.Errorf("checksum validation failed for %s", versionID)
		}
	}

	// Unmarshal the parameters
	var params ParameterSet
	if err := json.Unmarshal(data, &params); err != nil {
		return nil, fmt.Errorf("failed to unmarshal parameters: %w", err)
	}

	// Validate loaded parameters
	if errors := params.Validate(); len(errors) > 0 {
		return nil, fmt.Errorf("loaded parameters are invalid: %v", errors)
	}

	return &params, nil
}

// LoadLatest loads the most recent parameter set
func (s *Storage) LoadLatest() (*ParameterSet, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	// First try to read the latest reference file
	latestPath := filepath.Join(s.basePath, "params_latest.json")
	if latestData, err := os.ReadFile(latestPath); err == nil {
		// Try to unmarshal as ParameterSet first
		var params ParameterSet
		if err := json.Unmarshal(latestData, &params); err == nil {
			return &params, nil
		}

		// Otherwise treat as reference to version ID
		versionID := strings.TrimSpace(string(latestData))
		if versionID != "" {
			return s.Load(versionID)
		}
	}

	// Fallback: find the most recent parameter file
	versions, err := s.GetAvailableVersions()
	if err != nil {
		return nil, err
	}

	if len(versions) == 0 {
		return nil, fmt.Errorf("no parameter sets available")
	}

	// Sort versions to get the most recent
	sort.Slice(versions, func(i, j int) bool {
		return versions[i].CreatedAt.After(versions[j].CreatedAt)
	})

	return s.Load(versions[0].ID)
}

// GetAvailableVersions returns metadata about all cached parameter sets
func (s *Storage) GetAvailableVersions() ([]VersionInfo, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	entries, err := os.ReadDir(s.basePath)
	if err != nil {
		return nil, fmt.Errorf("failed to read storage directory: %w", err)
	}

	var versions []VersionInfo

	for _, entry := range entries {
		if entry.IsDir() {
			continue
		}

		name := entry.Name()
		if !strings.HasPrefix(name, "params_") || !strings.HasSuffix(name, ".json") {
			continue
		}

		// Skip checksum files and latest reference
		if strings.Contains(name, ".sha256") || name == "params_latest.json" {
			continue
		}

		// Extract version ID
		versionID := strings.TrimPrefix(name, "params_")
		versionID = strings.TrimSuffix(versionID, ".json")

		// Get file info
		info, err := entry.Info()
		if err != nil {
			continue
		}

		// Try to load the parameter set to get metadata
		params, err := s.Load(versionID)
		if err != nil {
			// If load fails, still include basic info
			versions = append(versions, VersionInfo{
				ID:         versionID,
				ModifiedAt: info.ModTime(),
				Size:       info.Size(),
				Valid:      false,
			})
			continue
		}

		versions = append(versions, VersionInfo{
			ID:            params.ID,
			EffectiveDate: params.EffectiveDate,
			CreatedAt:     params.CreatedAt,
			CreatedBy:     params.CreatedBy,
			Description:   params.Description,
			ModifiedAt:    info.ModTime(),
			Size:          info.Size(),
			Valid:         true,
			IsDefault:     params.IsDefault,
		})
	}

	return versions, nil
}

// Delete removes a parameter set and its checksum
func (s *Storage) Delete(versionID string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	filename := fmt.Sprintf("params_%s.json", versionID)
	filepath := filepath.Join(s.basePath, filename)
	checksumPath := filepath + ".sha256"

	// Remove main file
	if err := os.Remove(filepath); err != nil && !os.IsNotExist(err) {
		return fmt.Errorf("failed to delete parameter file: %w", err)
	}

	// Remove checksum file
	if err := os.Remove(checksumPath); err != nil && !os.IsNotExist(err) {
		// Non-critical, just log
		fmt.Printf("Warning: failed to delete checksum file: %v\n", err)
	}

	return nil
}

// updateLatest updates the latest reference to point to the given version
func (s *Storage) updateLatest(versionID string) error {
	latestPath := filepath.Join(s.basePath, "params_latest.json")
	tempPath := latestPath + ".tmp"

	// Write version ID as reference
	if err := os.WriteFile(tempPath, []byte(versionID), 0644); err != nil {
		return err
	}

	// Atomic rename
	return os.Rename(tempPath, latestPath)
}

// RecoverFromCorruption attempts to recover from corrupted files
func (s *Storage) RecoverFromCorruption(versionID string) (*ParameterSet, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	// Try to find backup files
	backupPatterns := []string{
		fmt.Sprintf("params_%s.json.backup", versionID),
		fmt.Sprintf("params_%s.json.bak", versionID),
		fmt.Sprintf("params_%s.json.old", versionID),
	}

	for _, pattern := range backupPatterns {
		backupPath := filepath.Join(s.basePath, pattern)
		if data, err := os.ReadFile(backupPath); err == nil {
			var params ParameterSet
			if err := json.Unmarshal(data, &params); err == nil {
				// Validate recovered parameters
				if errors := params.Validate(); len(errors) == 0 {
					// Save the recovered parameters
					if err := s.Save(&params); err == nil {
						return &params, nil
					}
				}
			}
		}
	}

	return nil, fmt.Errorf("unable to recover parameter set %s", versionID)
}

// CreateBackup creates a backup of the specified parameter set
func (s *Storage) CreateBackup(versionID string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	sourcePath := filepath.Join(s.basePath, fmt.Sprintf("params_%s.json", versionID))
	backupPath := filepath.Join(s.basePath, fmt.Sprintf("params_%s.json.backup", versionID))

	// Read source file
	data, err := os.ReadFile(sourcePath)
	if err != nil {
		return fmt.Errorf("failed to read source file: %w", err)
	}

	// Write backup
	if err := os.WriteFile(backupPath, data, 0644); err != nil {
		return fmt.Errorf("failed to write backup: %w", err)
	}

	return nil
}

// VerifyIntegrity checks the integrity of all stored parameter sets
func (s *Storage) VerifyIntegrity() ([]IntegrityResult, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	versions, err := s.GetAvailableVersions()
	if err != nil {
		return nil, err
	}

	var results []IntegrityResult

	for _, version := range versions {
		result := IntegrityResult{
			VersionID: version.ID,
			Valid:     true,
		}

		// Try to load and validate
		params, err := s.Load(version.ID)
		if err != nil {
			result.Valid = false
			result.Error = err.Error()
		} else if errors := params.Validate(); len(errors) > 0 {
			result.Valid = false
			result.Error = fmt.Sprintf("validation errors: %v", errors)
		}

		// Check checksum
		filepath := filepath.Join(s.basePath, fmt.Sprintf("params_%s.json", version.ID))
		checksumPath := filepath + ".sha256"

		if data, err := os.ReadFile(filepath); err == nil {
			if checksumData, err := os.ReadFile(checksumPath); err == nil {
				expectedChecksum := strings.TrimSpace(string(checksumData))
				actualHash := sha256.Sum256(data)
				actualChecksum := hex.EncodeToString(actualHash[:])

				if expectedChecksum != actualChecksum {
					result.Valid = false
					result.ChecksumMismatch = true
					result.Error = "checksum mismatch"
				}
			} else {
				result.ChecksumMissing = true
			}
		}

		results = append(results, result)
	}

	return results, nil
}

// VersionInfo contains metadata about a stored parameter set
type VersionInfo struct {
	ID            string    `json:"id"`
	EffectiveDate time.Time `json:"effective_date"`
	CreatedAt     time.Time `json:"created_at"`
	CreatedBy     string    `json:"created_by"`
	Description   string    `json:"description"`
	ModifiedAt    time.Time `json:"modified_at"`
	Size          int64     `json:"size"`
	Valid         bool      `json:"valid"`
	IsDefault     bool      `json:"is_default"`
}

// IntegrityResult represents the result of an integrity check
type IntegrityResult struct {
	VersionID        string `json:"version_id"`
	Valid            bool   `json:"valid"`
	ChecksumMismatch bool   `json:"checksum_mismatch"`
	ChecksumMissing  bool   `json:"checksum_missing"`
	Error            string `json:"error,omitempty"`
}

// Export exports a parameter set to a writer
func (s *Storage) Export(versionID string, w io.Writer) error {
	params, err := s.Load(versionID)
	if err != nil {
		return err
	}

	encoder := json.NewEncoder(w)
	encoder.SetIndent("", "  ")
	return encoder.Encode(params)
}

// Import imports a parameter set from a reader
func (s *Storage) Import(r io.Reader) (*ParameterSet, error) {
	decoder := json.NewDecoder(r)
	var params ParameterSet

	if err := decoder.Decode(&params); err != nil {
		return nil, fmt.Errorf("failed to decode parameters: %w", err)
	}

	// Validate before saving
	if errors := params.Validate(); len(errors) > 0 {
		return nil, fmt.Errorf("imported parameters are invalid: %v", errors)
	}

	// Save the imported parameters
	if err := s.Save(&params); err != nil {
		return nil, fmt.Errorf("failed to save imported parameters: %w", err)
	}

	return &params, nil
}
