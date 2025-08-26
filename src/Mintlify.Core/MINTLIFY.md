# Mintlify Specification Gaps and Undocumented Features

This document captures features and specifications found in the actual Mintlify implementation that are not clearly documented in the official documentation.

## API Configuration Properties

### MDX Configuration (`api.mdx`)
The official docs mention MDX API configuration but don't fully document the structure:

```json
"api": {
  "mdx": {
    "server": "https://api.example.com",  // Can also be string[]
    "auth": {
      "method": "bearer",  // Options: "bearer", "basic", "key", "cobo"
      "name": "x-api-key"  // Required for "key" method
    }
  }
}
```

### API Playground Configuration (`api.playground`)
Not fully documented in the main settings page:

```json
"api": {
  "playground": {
    "display": "interactive",  // Options: "interactive", "simple", "none"
    "proxy": true  // Whether to proxy requests through Mintlify servers
  }
}
```

### API Examples Configuration (`api.examples`)
Controls code example generation:

```json
"api": {
  "examples": {
    "languages": ["javascript", "python", "curl"],
    "defaults": "all"  // Options: "all", "required"
  }
}
```

### API Parameters Configuration (`api.params`)
Controls parameter display:

```json
"api": {
  "params": {
    "expanded": false  // Whether to expand parameters by default
  }
}
```

### Legacy Proxy Property (`api.proxy`)
A top-level proxy property exists for backward compatibility:

```json
"api": {
  "proxy": true  // Legacy, superseded by playground.proxy
}
```

## Navigation Properties

### Navigation Pages Polymorphism
Pages in navigation can be either strings OR nested GroupConfig objects:

```json
"navigation": {
  "pages": [
    "simple-page",
    {
      "group": "Nested Group",
      "pages": ["nested-page-1", "nested-page-2"]
    }
  ]
}
```

### Group-Level API Specifications
Groups can have their own OpenAPI/AsyncAPI specifications:

```json
"groups": [
  {
    "group": "API Reference",
    "openapi": "/path/to/openapi.json",
    "asyncapi": "/path/to/asyncapi.json",
    "pages": ["endpoint1", "endpoint2"]
  }
]
```

## Thumbnails Configuration
Used for social media previews but not documented in main settings:

```json
"thumbnails": {
  "background": "/images/thumbnail/background.svg",
  "logo": "/images/thumbnail/logo.png"
}
```

## Contextual Options
The `contextual` configuration for copy buttons and AI integrations:

```json
"contextual": {
  "options": ["copy", "chatgpt", "claude", "cursor", "vscode"]
}
```

## Validation Rules (Not Explicitly Documented)

### Color Format
- Must be valid hex colors: `#RRGGBB` or `#RGB`
- Regex pattern: `^#([a-fA-F0-9]{6}|[a-fA-F0-9]{3})$`

### Theme Values
Valid themes: `mint`, `maple`, `palm`, `willow`, `linden`, `almond`, `aspen`

### Icon Libraries
Valid libraries: `fontawesome`, `lucide`

### Appearance Modes
Valid modes: `system`, `light`, `dark`

### SEO Indexing
Valid values: `navigable`, `all`

### Authentication Methods
Valid methods for MDX API: `bearer`, `basic`, `key`, `cobo`

### Playground Display Modes
Valid modes: `interactive`, `simple`, `none`

### Examples Defaults
Valid values: `all`, `required`

## Type Flexibility

### API Configuration Values
OpenAPI, AsyncAPI, and MDX server configurations can be:
- String (single URL)
- Array of strings (multiple URLs)
- Object with `source` and `directory` properties

### Icon Configuration
Icons can be specified as:
- String (icon name)
- Object with additional properties

### Background Configuration
Background images can be specified as:
- String (image path)
- Object with style properties

## Navigation Hierarchy

The actual navigation structure supports more nesting than documented:
- Dropdowns can contain: languages, versions, tabs, anchors, groups, pages
- Tabs can contain: groups, pages with full nesting
- Groups can contain: nested groups via pages array

## Global Navigation
The `navigation.global` property allows adding items that appear across all sections:

```json
"navigation": {
  "global": {
    "anchors": [...],
    "tabs": [...],
    "dropdowns": [...],
    "languages": [...],
    "versions": [...]
  }
}
```

## Notes

- The schema URL `https://mintlify.com/docs.json` provides autocomplete in editors
- The actual schema at `https://leaves.mintlify.com/schema/docs.json` contains the full specification
- Many properties are optional but have defaults when not specified
- The navigation structure is highly flexible and supports deep nesting