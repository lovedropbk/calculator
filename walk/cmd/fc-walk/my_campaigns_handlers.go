package main

import "fmt"

// MyCampaignsDeps carries minimal dependencies for My Campaigns handlers to avoid long parameter lists.
type MyCampaignsDeps struct {
	Model *MyCampaignsTableModel
	Save  func([]CampaignDraft) error
	Load  func() ([]CampaignDraft, int, error)
	Clear func() error

	// Optional callbacks for lifecycle events
	OnSaved   func([]CampaignDraft)      // after successful Save
	OnLoaded  func([]CampaignDraft, int) // after successful Load (with file version)
	OnCleared func()                     // after successful Clear

	SelectMyCampaign func(id string)
	ExitEditMode     func()
	SetDirty         func(bool)
	SeedBlank        func() CampaignDraft
	SeedCopy         func() (CampaignDraft, error)
	SelectedID       func() string
}

// HandleMyCampaignNewBlank creates a blank draft and selects it.
func HandleMyCampaignNewBlank(deps MyCampaignsDeps) error {
	if deps.Model == nil || deps.SeedBlank == nil {
		return fmt.Errorf("invalid deps: Model/SeedBlank")
	}
	d := deps.SeedBlank()
	deps.Model.AddDraft(d)
	if deps.SelectMyCampaign != nil {
		deps.SelectMyCampaign(d.ID)
	}
	if deps.SetDirty != nil {
		deps.SetDirty(true)
	}
	return nil
}

// HandleMyCampaignCopySelected copies from current inputs into a new draft and selects it.
func HandleMyCampaignCopySelected(deps MyCampaignsDeps) error {
	if deps.Model == nil || deps.SeedCopy == nil {
		return fmt.Errorf("invalid deps: Model/SeedCopy")
	}
	d, err := deps.SeedCopy()
	if err != nil {
		return err
	}
	deps.Model.AddDraft(d)
	if deps.SelectMyCampaign != nil {
		deps.SelectMyCampaign(d.ID)
	}
	if deps.SetDirty != nil {
		deps.SetDirty(true)
	}
	return nil
}

// HandleMyCampaignDelete deletes the selected draft (if any) and exits edit mode.
func HandleMyCampaignDelete(deps MyCampaignsDeps) error {
	if deps.Model == nil || deps.SelectedID == nil {
		return fmt.Errorf("invalid deps: Model/SelectedID")
	}
	id := deps.SelectedID()
	if id != "" {
		removed := deps.Model.RemoveByID(id)
		if removed {
			if deps.ExitEditMode != nil {
				deps.ExitEditMode()
			}
			if deps.SetDirty != nil {
				deps.SetDirty(true)
			}
		}
	}
	return nil
}

// HandleMyCampaignSaveAll persists current drafts and clears dirty state.
func HandleMyCampaignSaveAll(deps MyCampaignsDeps) error {
	if deps.Model == nil || deps.Save == nil || deps.SetDirty == nil {
		return fmt.Errorf("invalid deps: Model/Save/SetDirty")
	}
	drafts := deps.Model.ToDrafts()
	if err := deps.Save(drafts); err != nil {
		return err
	}
	deps.SetDirty(false)
	if deps.OnSaved != nil {
		deps.OnSaved(drafts)
	}
	return nil
}

// HandleMyCampaignLoad loads drafts, replaces model, exits edit mode, and clears dirty state.
func HandleMyCampaignLoad(deps MyCampaignsDeps) error {
	if deps.Model == nil || deps.Load == nil {
		return fmt.Errorf("invalid deps: Model/Load")
	}
	drafts, ver, err := deps.Load()
	if err != nil {
		return err
	}
	deps.Model.ReplaceFromDrafts(drafts)
	if deps.ExitEditMode != nil {
		deps.ExitEditMode()
	}
	if deps.SetDirty != nil {
		deps.SetDirty(false)
	}
	if deps.OnLoaded != nil {
		deps.OnLoaded(drafts, ver)
	}
	return nil
}

// HandleMyCampaignClear clears persisted drafts, resets model, exits edit mode, and clears dirty state.
func HandleMyCampaignClear(deps MyCampaignsDeps) error {
	if deps.Model == nil || deps.Clear == nil {
		return fmt.Errorf("invalid deps: Model/Clear")
	}
	if err := deps.Clear(); err != nil {
		return err
	}
	deps.Model.ReplaceFromDrafts(nil)
	if deps.ExitEditMode != nil {
		deps.ExitEditMode()
	}
	if deps.SetDirty != nil {
		deps.SetDirty(false)
	}
	if deps.OnCleared != nil {
		deps.OnCleared()
	}
	return nil
}
