# WinUI 3 SDK and Runtime Issues - Lessons Learned

## Problem Summary
WinUI 3 apps have strict runtime dependencies that cause deployment friction:
1. Apps require exact matching Windows App Runtime installed on target machine
2. Different SDK versions in CI vs. developer machines cause compatibility issues
3. Users without admin rights cannot install runtimes

## Issues We Encountered

### Issue #1: PRI Packaging Task Failure in CI (Build #1-10)
**Error:**
```
error MSB4062: The "Microsoft.Build.Packaging.Pri.Tasks.ExpandPriContent" task could not be loaded
Could not load file or assembly 'C:\Program Files\dotnet\sdk\9.0.305\Microsoft\VisualStudio\v17.0\AppxPackage\Microsoft.Build.Packaging.Pri.Tasks.dll'
```

**Root Cause:**
- WindowsAppSDK 1.6.x has a bug where the PRI (resource) packaging task looks for Visual Studio MSBuild components in the .NET SDK path
- The .NET SDK doesn't include these components; they exist only in Visual Studio installations
- GitHub issue: https://github.com/microsoft/WindowsAppSDK/issues/4889

**Solutions Attempted:**
1. ❌ Install maui-windows workload → Helped with XAML compiler but not PRI task
2. ❌ Pin .NET SDK to 8.0 instead of 9.0 → Same error persisted
3. ❌ Disable PRI generation with `<EnableDefaultPriFiles>false</EnableDefaultPriFiles>` → Task still invoked
4. ❌ Switch from `dotnet build` to MSBuild → Same error (MSBuild also uses dotnet SDK for WinUI)
5. ✅ **Upgrade to WindowsAppSDK 1.8-preview** (Build #11) → Fixed the bug
6. ✅ **Use `<EnableMsixTooling>true</EnableMsixTooling>` with SDK 1.6** (Build #12) → Workaround for stable SDK

**Final Solution (Build #12):**
```xml
<PropertyGroup>
  <EnableMsixTooling>true</EnableMsixTooling>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.241106002" />
</ItemGroup>
```

**Why This Works:**
- `EnableMsixTooling=true` tells the build system to use a different code path for resource packaging
- This code path doesn't try to load the missing Visual Studio MSBuild task DLL
- Confirmed working by multiple users in the GitHub issue thread

---

### Issue #2: Runtime Version Mismatch (Post-Build #11)
**Error on user machine:**
```
Application requires Windows App SDK runtime 1.8.1 which is not installed
```

**Root Cause:**
- Build #11 used WindowsAppSDK 1.8-preview (to fix PRI bug)
- Preview runtimes are not widely installed on user machines
- Each Windows App SDK version requires its matching runtime to be installed

**Solution:**
- Reverted to stable WindowsAppSDK 1.6.241106002 (Build #12)
- Applied the `EnableMsixTooling=true` workaround instead
- Most Windows 11 22H2+ machines have SDK 1.6 runtime pre-installed

---

### Issue #3: Runtime Not Installed (Post-Build #12)
**Error on user machine:**
```
Windows App SDK 1.6 runtime not available
```

**Root Cause:**
- WinUI 3 apps are "framework-dependent" by default
- They require Windows App Runtime to be installed on the target machine
- Users without admin rights cannot install the runtime

**Solutions Available:**

#### Option A: Manual Runtime Installation (requires admin or policy)
- Download: https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads
- Install Windows App Runtime 1.6.x
- Some corporate environments allow per-user installs; others require IT/admin

#### Option B: Self-Contained Deployment (NO admin needed)
- Bundle the Windows App Runtime DLLs with the app
- Larger package size (~50MB instead of ~10MB)
- App runs on any Windows 10 1809+ machine without runtime install
- **This is what we implemented in Build #13+**

**How to enable self-contained:**
```xml
<PropertyGroup>
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
</PropertyGroup>
```

Or in publish command:
```bash
dotnet publish -p:WindowsAppSDKSelfContained=true
```

#### Option C: MSIX Package with Runtime (requires sideloading policy)
- Create MSIX package that bundles the runtime
- Can be installed per-user if sideloading is enabled
- Requires "Developer Mode" or enterprise sideloading policy
- Best for enterprise distribution via Intune

---

## Best Practices for WinUI 3 CI/CD

### 1. SDK Version Selection
- **Stable SDK (1.6.x):** Use for production; most users have the runtime
- **Preview SDK (1.8+):** Use only if you need bleeding-edge features; requires users to install preview runtime
- **Always test deployment on a clean VM** to catch runtime dependency issues

### 2. Build Configuration
```xml
<PropertyGroup>
  <!-- Use stable SDK -->
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.241106002" />
  
  <!-- Workaround for PRI task bug in dotnet build -->
  <EnableMsixTooling>true</EnableMsixTooling>
  
  <!-- For adminless deployment: bundle runtime with app -->
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
</PropertyGroup>
```

### 3. CI Environment
- Pin .NET SDK to 8.0.x in workflows (use global.json)
- Install maui-windows workload for XAML compiler
- Use `dotnet publish` instead of `dotnet build` for release artifacts
- Test artifacts on a machine without dev tools installed

### 4. Deployment Strategy Decision Tree

**Can users install software?**
- YES, with admin → Distribute installer + runtime installer
- YES, per-user (sideloading enabled) → MSIX package
- NO, locked-down environment → Self-contained deployment

**Self-contained is the safest for MVP/testing:**
- No runtime installation required
- Works on any supported Windows version
- Slightly larger download size is acceptable for ease of use

---

## References
- PRI task bug: https://github.com/microsoft/WindowsAppSDK/issues/4889
- Windows App SDK downloads: https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads
- Self-contained deployment: https://learn.microsoft.com/windows/apps/windows-app-sdk/deploy-packaged-apps
- MSIX packaging: https://learn.microsoft.com/windows/msix/

---

## Quick Troubleshooting

**"Windows App SDK runtime not found"**
→ Switch to self-contained deployment or install matching runtime

**"PRI task could not be loaded" in CI**
→ Add `<EnableMsixTooling>true</EnableMsixTooling>` to project file

**"XAML compiler failed" in CI**
→ Install maui-windows workload: `dotnet workload install maui-windows`

**App won't start, no error message**
→ Check Event Viewer → Windows Logs → Application for detailed error

**Large artifact size after self-contained**
→ Expected; runtime DLLs add ~40MB; can trim with PublishTrimmed if needed
