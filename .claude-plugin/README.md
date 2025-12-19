# DotNetDocs Plugin Marketplace

This directory contains the marketplace configuration for distributing Claude plugins from the DotNetDocs repository.

## Structure

```
.claude-plugin/
└── marketplace.json    # Marketplace metadata and plugin registry
```

## About This Marketplace

**Name**: `dotnetdocs-plugins`
**Owner**: CloudNimble
**Purpose**: Distribute official Claude plugins for .NET documentation

This marketplace provides access to plugins that enhance Claude's capabilities for writing, reviewing, and maintaining .NET documentation.

## Available Plugins

### .NET Documentation Expert

**Location**: `.claude/plugins/dotnet-documentation-expert`
**Version**: 1.0.0

Expert guidance for writing high-quality .NET API documentation and conceptual content.

**Features**:
- XML documentation comment templates for all .NET member types
- Best practices following Microsoft's documentation standards
- DotNetDocs conceptual documentation (.mdz files)
- Mintlify-enhanced markdown features
- Project-specific standards compliance
- Code review and quality checking
- Progressive disclosure for efficient context loading

See the [plugin documentation](../.claude/plugins/dotnet-documentation-expert/README.md) for detailed information.

## Marketplace Configuration

The `marketplace.json` file defines:

- **Marketplace Identity**: Name and owner information
- **Plugin Registry**: List of available plugins with metadata
- **Distribution Settings**: Plugin sources and locations
- **Documentation Links**: References to plugin documentation

## Using Plugins from This Marketplace

### In This Repository

Plugins are automatically available when working in the DotNetDocs repository.

### In Other Projects

#### Option 1: Install Individual Plugin

Copy a specific plugin to your project:

```bash
# Copy the plugin directory
cp -r .claude/plugins/dotnet-documentation-expert /path/to/your/project/.claude/plugins/

# The plugin will be automatically available
```

#### Option 2: Reference This Marketplace

Configure your project to reference this marketplace (feature availability depends on Claude Code version):

```json
{
  "marketplaces": [
    {
      "name": "dotnetdocs-plugins",
      "source": "https://github.com/CloudNimble/DotNetDocs.git"
    }
  ]
}
```

## Adding New Plugins

To add a new plugin to this marketplace:

1. **Create the Plugin**

   Create your plugin in `.claude/plugins/your-plugin-name/`:
   ```
   .claude/plugins/your-plugin-name/
   ├── plugin.md           # Main plugin content
   ├── README.md          # Documentation
   ├── EXAMPLES.md        # Usage examples
   └── CHANGELOG.md       # Version history
   ```

2. **Register in Marketplace**

   Add an entry to the `plugins` array in `marketplace.json`:
   ```json
   {
     "name": "your-plugin-name",
     "source": {
       "type": "relative",
       "path": ".claude/plugins/your-plugin-name"
     },
     "displayName": "Your Plugin Display Name",
     "description": "Plugin description",
     "version": "1.0.0",
     "author": {
       "name": "Author Name",
       "url": "https://github.com/author"
     },
     "license": "MIT",
     "categories": ["category1", "category2"]
   }
   ```

3. **Update Documentation**

   Update this README with information about the new plugin.

4. **Test the Plugin**

   Verify the plugin works correctly before committing.

## Marketplace Structure

The marketplace.json follows this schema:

```json
{
  "name": "marketplace-identifier",
  "owner": {
    "name": "Maintainer Name",
    "url": "https://...",
    "email": "contact@..."
  },
  "metadata": {
    "description": "Marketplace description",
    "version": "1.0.0",
    "homepage": "https://...",
    "repository": { ... },
    "pluginRoot": ".claude/plugins"
  },
  "plugins": [
    {
      "name": "plugin-identifier",
      "source": { ... },
      "displayName": "Display Name",
      "description": "...",
      "version": "1.0.0",
      ...
    }
  ]
}
```

### Required Fields

- `name`: Marketplace identifier (kebab-case)
- `owner`: Maintainer information
- `plugins`: Array of available plugins

### Plugin Entry Fields

Each plugin entry should include:
- `name`: Plugin identifier (kebab-case)
- `source`: Where to fetch the plugin from
- `version`: Semantic version
- `description`: Brief description
- `author`: Author information
- `license`: License identifier

## Version Management

- **Marketplace Version**: Tracked in `metadata.version`
- **Plugin Versions**: Tracked individually in each plugin entry
- Use semantic versioning (MAJOR.MINOR.PATCH)
- Update CHANGELOG.md files when incrementing versions

## Distribution

This marketplace can be:
- **Local**: Used within this repository
- **Shared**: Referenced by other projects via git URL
- **Published**: Potentially published to official marketplace (future)

## Support

For questions or issues:
- **Plugin Issues**: See individual plugin documentation
- **Marketplace Issues**: Open an issue in the [DotNetDocs repository](https://github.com/CloudNimble/DotNetDocs/issues)
- **Contributions**: Submit pull requests to add or improve plugins

## License

All plugins in this marketplace are licensed under the MIT license unless otherwise specified.

---

**Documentation**: [Plugin Marketplaces Guide](https://code.claude.com/docs/en/plugin-marketplaces)
**Repository**: [CloudNimble/DotNetDocs](https://github.com/CloudNimble/DotNetDocs)
