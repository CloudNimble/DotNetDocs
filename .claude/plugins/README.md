# Claude Plugins for DotNetDocs

This directory contains Claude Plugins that provide specialized capabilities for working on the DotNetDocs project and .NET documentation in general.

## Available Plugins

### .NET Documentation Expert

**Location**: `dotnet-documentation-expert/`

**Purpose**: Expert guidance for writing high-quality .NET API documentation and conceptual content.

**Features**:
- XML documentation comment templates for all .NET member types
- Best practices for summaries, remarks, examples, and parameters
- DotNetDocs conceptual documentation structure (.mdz files)
- Project-specific standards from CLAUDE.md
- Mintlify-enhanced markdown features
- Code review and quality checking

**Usage**: The plugin activates automatically when working with .NET documentation or can be explicitly referenced.

**Architecture**: Uses progressive disclosure (3-level context loading) for efficient operation:
- Level 1: YAML frontmatter loaded at startup
- Level 2: Core guidance loaded when activated
- Level 3+: Detailed templates and best practices loaded as needed

See the [plugin README](dotnet-documentation-expert/README.md) for detailed information.

## Plugin vs Skill

**Plugins** are structured packages with:
- Versioning and changelog
- Comprehensive documentation (README, EXAMPLES, CHANGELOG)
- Distribution through marketplace (see `.claude-plugin/marketplace.json`)
- Activation contexts and permissions

**Skills** are simpler markdown files with expertise content.

Both can coexist - the skill in `.claude/skills/` provides the same core functionality as the plugin but without the marketplace distribution features.

## Marketplace

Plugins are registered in the marketplace at `.claude-plugin/marketplace.json` in the repository root. See the [marketplace documentation](../.claude-plugin/README.md) for details.

## Installing Plugins

### From This Repository

Plugins in this directory are automatically available when working in the DotNetDocs repository.

### Installing in Other Projects

To use these plugins in other projects:

```bash
# Copy the plugin directory
cp -r .claude/plugins/dotnet-documentation-expert /path/to/your/project/.claude/plugins/

# Or create a symlink
ln -s /path/to/DotNetDocs/.claude/plugins/dotnet-documentation-expert /path/to/your/project/.claude/plugins/
```

## Creating New Plugins

To create a new plugin:

1. **Create Plugin Directory**
   ```bash
   mkdir -p .claude/plugins/your-plugin-name
   ```

2. **Create marketplace.json**
   ```json
   {
     "name": "your-plugin-name",
     "displayName": "Your Plugin Display Name",
     "version": "1.0.0",
     "author": {
       "name": "Your Name",
       "url": "https://github.com/yourname"
     },
     "description": "Plugin description",
     "categories": ["category1", "category2"],
     "license": "MIT"
   }
   ```

3. **Create plugin.md**

   Write your plugin content with system prompt and capabilities.

4. **Create Documentation**
   - README.md: Plugin overview and usage
   - EXAMPLES.md: Comprehensive examples
   - CHANGELOG.md: Version history

5. **Test the Plugin**

   Use the plugin in conversations to verify functionality.

## Plugin Structure

```
.claude/plugins/
└── your-plugin-name/
    ├── marketplace.json          # Plugin metadata
    ├── plugin.md                 # Main plugin content
    ├── README.md                 # Plugin documentation
    ├── EXAMPLES.md               # Usage examples
    └── CHANGELOG.md              # Version history
```

## Distribution

Plugins can be:
- **Local**: Used only in this repository
- **Shared**: Copied to other projects manually
- **Marketplace**: Published to Claude plugin marketplace (future)

## Version Control

Plugins are tracked in git and versioned using semantic versioning:
- **MAJOR**: Breaking changes to plugin interface
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, documentation updates

## Best Practices

1. **Clear Scope**: Each plugin should have a focused purpose
2. **Good Documentation**: Provide comprehensive README and examples
3. **Versioning**: Use semantic versioning and maintain changelog
4. **Testing**: Test plugins with real-world scenarios
5. **Maintenance**: Keep plugins updated with project standards
6. **Progressive Disclosure**: Use YAML frontmatter and tiered content loading:
   - Level 1: Concise name and description in frontmatter
   - Level 2: Core guidance in main plugin.md
   - Level 3+: Detailed reference files loaded as needed
   - See [dotnet-documentation-expert](dotnet-documentation-expert/README.md#progressive-disclosure-architecture) for example

## Support

For questions or issues with plugins:
- Check the plugin's README.md
- Review EXAMPLES.md for usage patterns
- Open an issue in the repository
- Contribute improvements via pull request

## License

Plugins in this directory are licensed under the same MIT license as the DotNetDocs project.
