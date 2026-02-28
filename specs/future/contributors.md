# GitHub Contributors Feature

## Overview

Add contributor avatars with profile links to documentation pages using git + one GitHub API call.

## Approach (Hybrid)

1. One-time call to GitHub's `/repos/{owner}/{repo}/contributors` endpoint → cache email→user mapping
2. Use `git log` locally to get contributor emails per file
3. Match emails to cached GitHub usernames
4. Construct URLs: `https://github.com/{username}.png` (avatar), `https://github.com/{username}` (profile)

## Core Model

**New file: `src/CloudNimble.DotNetDocs.Core/Contributor.cs`**

```csharp
public class Contributor
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }
    public string? ProfileUrl { get; set; }
}
```

**Modify: `src/CloudNimble.DotNetDocs.Core/DocEntity.cs`**

```csharp
public List<Contributor>? Contributors { get; set; }
```

**Modify: `src/CloudNimble.DotNetDocs.Core/ProjectContext.cs`**

```csharp
public bool ContributorsEnabled { get; set; } = false;
```

## Implementation

### 1. GitHelper (in Core for reuse by future GitLab/AzureDevOps plugins)

**New file: `src/CloudNimble.DotNetDocs.Core/GitHelper.cs`**

```csharp
public static class GitHelper
{
    // git log --format="%an|%ae" -- {filePath} | sort -u
    public static List<(string Name, string Email)> GetFileContributors(string filePath);

    // git remote get-url origin
    public static string? GetRemoteUrl();

    // Parse "https://github.com/Owner/Repo.git" → (Provider, Owner, Repo)
    public static (string? Provider, string? Owner, string? Repo) ParseRemoteUrl(string url);
}
```

### 2. GitHubContributorEnricher (in GitHub plugin)

**New file: `src/CloudNimble.DotNetDocs.Plugins.GitHub/GitHubContributorEnricher.cs`**

Implements `IDocEnricher`:

```csharp
public class GitHubContributorEnricher : IDocEnricher
{
    private Dictionary<string, Contributor>? _contributorCache; // email → Contributor

    public async Task EnrichAsync(DocEntity entity)
    {
        if (entity is not DocAssembly assembly) return;

        // 1. Build cache once: GET /repos/{owner}/{repo}/contributors
        await BuildContributorCacheAsync();

        // 2. Walk entity graph recursively
        foreach (var ns in assembly.Namespaces)
            foreach (var type in ns.Types)
                EnrichType(type);
    }

    private void EnrichType(DocType type)
    {
        var contributors = new HashSet<Contributor>();

        // Source code file
        var sourcePath = type.Symbol?.Locations.FirstOrDefault()?.SourceTree?.FilePath;
        if (sourcePath is not null)
            AddContributorsFromFile(sourcePath, contributors);

        // Conceptual files (Usage.md, Examples.md, etc.)
        // ... get paths from ProjectContext.GetFullConceptualPath() + type path

        type.Contributors = contributors.ToList();
    }
}
```

### 3. Non-API Docs (Guides, etc.)

**TODO: Define rendering approach**

For non-generated pages (guides, tutorials, etc.), contributors need to be collected on-the-fly and rendered. Options:

- **Mintlify Component**: Create a `<Contributors />` React component
- **Markdown Injection**: Renderer injects contributor HTML directly into .mdx files

Example output:

```html
<div class="contributors">
  <a href="https://github.com/username"><img src="https://github.com/username.png" alt="username" /></a>
  ...
</div>
```

## Files to Create/Modify

| File | Action |
|------|--------|
| `src/CloudNimble.DotNetDocs.Core/Contributor.cs` | Create |
| `src/CloudNimble.DotNetDocs.Core/GitHelper.cs` | Create |
| `src/CloudNimble.DotNetDocs.Core/DocEntity.cs` | Add `Contributors` property |
| `src/CloudNimble.DotNetDocs.Core/ProjectContext.cs` | Add `ContributorsEnabled` flag |
| `src/CloudNimble.DotNetDocs.Plugins.GitHub/GitHubContributorEnricher.cs` | Create |

## Open Items

- [ ] Define Mintlify vs Markdown rendering approach
- [ ] Determine where contributor section appears in rendered output (top, bottom, sidebar?)
- [ ] Handle case where GitHub API is unavailable (graceful degradation to name/email only?)
- [ ] Consider caching the contributor cache to disk to avoid API calls on every build

## Verification

1. `dotnet build src -c Debug` - ensure it compiles
2. Run against a project with git history
3. Debug/inspect that `DocType.Contributors` is populated
4. Check rendered .mdx output includes contributor avatars
