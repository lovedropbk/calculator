# Campaign Designer: Fluent v2 alignment, tokens, a11y, virtualization

Scope: This document describes tokens, layout, accessibility, high-contrast (HC), and performance recipes for the Campaign Designer in FinancialCalculator.WinUI3.

Locations
- Styles: [FluentTheme.xaml](winui3-mvp/FinancialCalculator.WinUI3/Styles/FluentTheme.xaml)
- Designer tokens: [DesignerStyles.xaml](winui3-mvp/FinancialCalculator.WinUI3/Styles/DesignerStyles.xaml)
- Default controls: [DefaultStyles.xaml](winui3-mvp/FinancialCalculator.WinUI3/Styles/DefaultStyles.xaml)
- App resources: [App.xaml](winui3-mvp/FinancialCalculator.WinUI3/App.xaml)
- Designer header: [CampaignDesignerHeaderView.xaml](winui3-mvp/FinancialCalculator.WinUI3/Controls/CampaignDesignerHeaderView.xaml)
- Designer tiles host: [CampaignDesignerTilesView.xaml](winui3-mvp/FinancialCalculator.WinUI3/Controls/CampaignDesignerTilesView.xaml)
- Designer tiles host code-behind: [CampaignDesignerTilesView.xaml.cs](winui3-mvp/FinancialCalculator.WinUI3/Controls/CampaignDesignerTilesView.xaml.cs)
- Tile control: [CampaignDesignerTileView.xaml](winui3-mvp/FinancialCalculator.WinUI3/Controls/CampaignDesignerTileView.xaml)
- Tile control code-behind: [CampaignDesignerTileView.xaml.cs](winui3-mvp/FinancialCalculator.WinUI3/Controls/CampaignDesignerTileView.xaml.cs)
- Main host page: [MainWindow.xaml](winui3-mvp/FinancialCalculator.WinUI3/MainWindow.xaml)

Virtualization and layout strategy
- Default path uses ListView + ItemsWrapGrid for virtualized wrapping.
- Preferred path adds ItemsRepeater + UniformGridLayout for dense virtualization and predictable spacing.
- Switch via dependency property on CampaignDesignerTilesView: set LayoutStrategy="ItemsRepeaterUniformGrid" to enable ItemsRepeater, or "ListViewWrap" to use ListView.
- Spacing tokens for tiles:
  - DesignerTilesGutter (column/row spacing in ItemsRepeater layout)
  - DesignerTilesItemMargin (container margin for ListView items)
- Tile sizing tokens:
  - DesignerTileWidth, DesignerTileMinWidth, DesignerTileMaxWidth

Density and tokenization (Fluent v2)
- All controls use Dense* styles from Fluent tokens:
  - DenseTextBoxStyle, DenseComboBoxStyle, DenseNumberBoxStyle, DenseButtonStyle
- Typography tokens:
  - TokenSectionHeaderTextStyle (section headings)
  - TokenLabelTextStyle (labels)
  - TokenCaptionTextStyle (caption/aux)
- Card surfaces use TokenCardStyle / CardContainerStyle; do not inline padding, margins, or opacities.
- Designer thickness helpers:
  - MarginTopXS, MarginTopS, MarginBottomXS, MarginVertXS, InlineRightSmallMargin
- Remove ad‑hoc styling and literal colors. Always bind to theme resources (e.g., ControlStrokeColorDefaultBrush) and tokenized thickness/spacing.

Dense ToggleSwitch template
- DenseToggleSwitchStyle defines:
  - Compact track/thumb sizing via DenseToggleTrackWidth/DenseToggleTrackHeight/DenseToggleThumbDiameter/DenseToggleThumbMargin.
  - VisualStates: ToggleStates (Off/On), CommonStates (Normal/PointerOver/Pressed/Disabled).
  - Focus visuals preserved (UseSystemFocusVisuals=True).
- Applied in:
  - Header: [CampaignDesignerHeaderView.xaml](winui3-mvp/FinancialCalculator.WinUI3/Controls/CampaignDesignerHeaderView.xaml)
  - Tile header: [CampaignDesignerTileView.xaml](winui3-mvp/FinancialCalculator.WinUI3/Controls/CampaignDesignerTileView.xaml)

Tri‑state tile detail mode (+ Reset)
- Global detail state is supplied by the parent (Comparison.IsDesignerDetailed) into each tile via GlobalIsDetailed.
- TileDetailMode supports Global, Compact, Detailed; effective IsDetailed is computed automatically.
- User toggling the tile switches from Global into a local override (Compact or Detailed).
- “Reset to global” mini-button restores TileDetailMode=Global.

Accessibility requirements
- AccessKeys:
  - Clear All tiles: Alt+C in [CampaignDesignerHeaderView.xaml](winui3-mvp/FinancialCalculator.WinUI3/Controls/CampaignDesignerHeaderView.xaml)
  - Designer details toggle: Alt+D in [CampaignDesignerHeaderView.xaml](winui3-mvp/FinancialCalculator.WinUI3/Controls/CampaignDesignerHeaderView.xaml)
  - Export XLSX: Alt+E in [MainWindow.xaml](winui3-mvp/FinancialCalculator.WinUI3/MainWindow.xaml)
- AutomationProperties.Name:
  - Tile root announces “Campaign tile for {Title}” and actions announce context.
- Narrator:
  - Ensure captions/labels use tokenized text styles; avoid using opacity to convey meaning.
- Focus:
  - Do not suppress focus visuals. Default focus outlines must remain visible.

High Contrast (HC)
- Budget* brushes are overridden in App.xaml HighContrast theme dictionary to system colors for contrast.
- DataGrid theme mapping:
  - Foreground and AlternatingRowBackground set to token brushes in [DefaultStyles.xaml](winui3-mvp/FinancialCalculator.WinUI3/Styles/DefaultStyles.xaml).
- Verify with Accessibility Insights: no contrast violations for text or key visuals in Designer surfaces.

Localization
- All user-facing strings in Designer should move to resources and be applied via x:Uid on elements.
- Recommended approach:
  - Create a Resources.resw for the view or a shared resources file.
  - Add x:Uid to elements (ToggleSwitch, Buttons, ToolTips, TextBlocks) and use resource keys for Content/Text/ToolTipService.ToolTip/AutomationProperties.Name.
  - Keep AutomationProperties.Name localized consistently with visible text (include Title context when appropriate).

Performance guidance
- 500+ tiles should remain smooth (target 60fps) when virtualized:
  - Prefer ItemsRepeater for dense grids or when ListView’s item chrome isn’t needed.
  - Avoid heavy animations in item templates; favor transitions defined in container styles.
- Keep tiles self-contained; avoid cross-tile bindings that prevent virtualization from recycling.

Implementation checklist (do/don’t)
- Do:
  - Use spacing tokens: DesignerTilesGutter, DesignerTilesItemMargin, MarginTopXS/MarginTopS, MarginBottomXS, MarginVertXS.
  - Use TokenCaptionTextStyle for captions and TokenSectionHeaderTextStyle for section headers.
  - Use Dense* styles for input density.
  - Use theme resources for colors/strokes.
- Don’t:
  - Hardcode margins, opacities, or colors in Designer area.
  - Disable focus visuals or hijack pointer events that affect keyboard navigation.

Extensibility
- New Designer tiles or fields should:
  - Reuse existing Dense* control styles and Token* text styles.
  - Expose state via dependency properties (bindable from host).
  - Keep per-file length under 500 lines and functions under ~30–40 lines; split into helpers when needed.

Testing recipes
- Accessibility Insights:
  - Tab through Designer actions; verify focus visibility and announced names with Title context.
  - Run contrast tests in Light, Dark, and High Contrast.
- Performance:
  - Load 500+ tiles and scroll quickly; watch for hitching and memory ballooning.
- Visual:
  - Compare Designer spacing to tokens (no literal spacing in code review).

Change log (this refactor)
- Added ItemsRepeater path and LayoutStrategy on CampaignDesignerTilesView.
- Added DenseToggleSwitchStyle with compact template and VisualStates.
- Added tri‑state TileDetailMode with “Reset to global” in tile UI.
- Tokenized spacing (DesignerTilesGutter/DesignerTilesItemMargin, thickness helpers).
- Added AccessKeys and AutomationProperties for primary Designer actions.
- Added HC overrides for Budget* brushes and DataGrid token mapping.
- Removed obsolete OnDesignerRemoveClick from MainWindow.xaml.cs.

Ownership
- UI contracts: WinUI layer (FinancialCalculator.WinUI3).
- Business logic/calculations: Engine layer (no changes in this refactor).

End.