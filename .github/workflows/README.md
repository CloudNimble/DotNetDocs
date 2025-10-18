# GitHub Actions Workflows

This directory contains the GitHub Actions workflows for the CloudNimble.Build.Documentation project.

## Workflows

### 🚀 Build and Deploy (`build-and-deploy.yml`)

Handles building, testing, and deploying packages to NuGet using .NET 10 preview.

**Triggers:**
- Push to `main` or `dev` branches
- Pull requests to `main` or `dev` (build only, no deployment)

**Platform:** Windows (all jobs run on `windows-latest`)

**Versioning:**
- **Main branch**: Standard SemVer (e.g., `1.0.42`)
- **Dev branch**: Preview versioning (e.g., `1.0.42-preview.1`)
- **Feature/other branches**: CI versioning (e.g., `1.0.0-CI-20250716-193214`)

**Features:**
- Automatic .NET 10 preview installation via PowerShell
- NuGet package deployment to NuGet.org
- GitHub release creation for main branch builds
- Artifact upload with 7-day retention

### ✅ PR Validation (`pr-validation.yml`)

Validates pull requests with comprehensive testing.

**Triggers:**
- Pull requests to `main` or `dev` branches

**Platform:** Windows only (for .NET 10 preview support)

**Testing:**
- .NET: 10.0.x (preview)
- Package validation
- Test result upload

## Configuration

### Repository Variables

Set these in your repository settings under Settings → Secrets and variables → Actions → Variables:

| Variable | Description | Default | Example |
|----------|-------------|---------|---------|
| `VERSION_MAJOR` | Major version number | `1` | `2` |
| `VERSION_MINOR` | Minor version number | `0` | `1` |
| `VERSION_PREVIEW_SUFFIX` | Preview build counter (auto-updated by workflow) | `0` | `5` |

### Repository Secrets

Set these in your repository settings under Settings → Secrets and variables → Actions → Secrets:

| Secret | Description | Required |
|--------|-------------|----------|
| `NUGET_API_KEY` | NuGet.org API key for package publishing | Yes |

## Version Number Format

### Main Branch (Production)
```
{VERSION_MAJOR}.{VERSION_MINOR}.{PATCH_VERSION}
```
**Example:** `1.0.5`

**Usage:** Production releases with patch versions determined by examining Git tags

### Dev Branch (Preview Builds)
```
{VERSION_MAJOR}.{VERSION_MINOR}.0-preview.{VERSION_PREVIEW_SUFFIX}
```
**Example:** `1.0.0-preview.3`

**Usage:** Development previews for testing and staging

### Feature/Other Branches (CI Builds)
```
{VERSION_MAJOR}.{VERSION_MINOR}.0-CI-{YYYYMMDD}-{HHMMSS}
```
**Example:** `1.0.0-CI-20250716-193214`

**Usage:** CI builds for feature branches and pull requests

### Variable Definitions
- `PATCH_VERSION` = For main branch, determined by examining existing Git tags and incrementing
- `VERSION_PREVIEW_SUFFIX` = Auto-incrementing preview counter, updated after successful dev deployments
- `YYYYMMDD` = UTC date (e.g., `20250716`)
- `HHMMSS` = UTC time in 24-hour format (e.g., `193214` = 7:32:14 PM)

## .NET 10 Preview Setup

The workflows automatically install .NET 10 preview using:

```powershell
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "dotnet-install.ps1"
./dotnet-install.ps1 -Channel 10.0 -Quality preview -InstallDir "$env:ProgramFiles\dotnet"
```

**Benefits:**
- Latest .NET features and performance improvements
- C# 13 support
- Enhanced SDK capabilities

## Deployment Strategy

### Automatic Deployment
- **Main branch pushes:** Deploy to NuGet with production version
- **Dev branch pushes:** Deploy to NuGet with preview version
- **Feature branches:** Build only, no deployment

### Release Creation
- Automatic GitHub releases for main branch deployments
- Includes NuGet package as release asset
- Generated release notes with installation instructions

## Manual Version Management

### Bumping Versions

1. **Major Version:** Update `VERSION_MAJOR` repository variable (resets preview suffix when used)
2. **Minor Version:** Update `VERSION_MINOR` repository variable (resets preview suffix when used)
3. **Preview Builds:** Automatically handled by `VERSION_PREVIEW_SUFFIX` variable (auto-updated after successful dev deployments)
4. **Patch Versions:** Automatically handled by examining Git tags on main branch

### Version Strategy Examples

**Version Flow Example:**
```
Main: 1.0.0 → 1.0.1 → 1.0.2 → 1.1.0 → 1.1.1 → 2.0.0
Dev:  1.0.0-preview.1 → 1.0.0-preview.2 → 1.0.0-preview.3
```

Given `VERSION_MAJOR=1`, `VERSION_MINOR=0`, `VERSION_PREVIEW_SUFFIX=2`, and existing main tag `v1.0.1`:

| Branch Type | Next Build Result | When to Use |
|-------------|------------------|-------------|
| `main` | `1.0.2` | Production releases (increments from existing tags) |
| `dev` | `1.0.0-preview.3` | Development testing (increments preview suffix) |
| `feature/new-feature` | `1.0.0-CI-20250716-193214` | Feature development |

### Automatic Variable Updates

- **`VERSION_PREVIEW_SUFFIX`**: Automatically incremented and updated after successful dev branch deployments
- **Git Tags**: Created automatically for main branch releases (e.g., `v1.0.2`)
- **Error Handling**: Deployment failures (bad API key, version conflicts) are detected and reported

## Workflow Environment Variables

Both workflows use consistent environment variables:

```yaml
env:
  DOTNET_VERSION: '10.0.x'
  SOLUTION_FILE: 'src/CloudNimble.Build.Documentation.slnx'
  PROJECT_FILE: 'src/CloudNimble.Build.Documentation/CloudNimble.Build.Documentation.csproj'
```

## Troubleshooting

### .NET Installation Issues
If .NET 10 preview installation fails:
1. Check if Channel 10.0 with Quality preview is available
2. Verify the installation script URL is accessible
3. Try using a specific version number with `-Version` parameter if needed

### Package Already Exists
The workflow uses `--skip-duplicate` to handle cases where a package version already exists on NuGet. This is treated as a warning, not a failure.

### Deployment Error Detection
The workflow automatically detects and handles various deployment failures:

- **Authentication Errors (403/Unauthorized)**: Bad API key or expired token
- **Version Conflicts (409)**: Package version already exists (treated as warning with `--skip-duplicate`)
- **General Errors**: Other push failures are captured and reported

### Missing Secrets
If deployment fails with authentication errors:
1. Verify `NUGET_API_KEY` secret is configured
2. Ensure the API key has package publishing permissions
3. Check if the API key has expired

### Preview Suffix Issues
If `VERSION_PREVIEW_SUFFIX` updates fail:
1. Check that `GITHUB_TOKEN` has repository write permissions
2. Verify the variable exists in repository settings
3. The workflow continues even if this update fails (non-critical)

### Version Conflicts
If you need to force a new version:
1. Update the repository variables (`VERSION_MAJOR`/`VERSION_MINOR`)
2. For preview builds, manually reset `VERSION_PREVIEW_SUFFIX` to `0`
3. For CI builds, the timestamp ensures automatic uniqueness

### Windows-Specific Issues
Since all jobs run on Windows:
1. PowerShell syntax is used throughout
2. Path separators use backslashes in some contexts
3. File operations use PowerShell cmdlets

## Adding New Workflows

When creating additional workflows:

1. **Use consistent environment variables** from existing workflows
2. **Include .NET 10 preview installation** step
3. **Use PowerShell** for scripting consistency
4. **Run on Windows** for .NET preview compatibility
5. **Follow naming convention:** `{purpose}.yml`
6. **Update this README** with new workflow details

## Dependencies

### Dependabot Configuration
The repository includes automatic dependency updates via `.github/dependabot.yml`:

- **GitHub Actions:** Weekly updates on Mondays
- **NuGet packages:** Weekly updates on Mondays
- **Intelligent ignoring:** Skips major/minor updates for pinned SDK dependencies

### Action Dependencies
- `actions/checkout@v4` - Repository checkout
- `actions/upload-artifact@v4` - Artifact management
- `actions/download-artifact@v4` - Artifact retrieval
- `softprops/action-gh-release@v1` - GitHub release creation