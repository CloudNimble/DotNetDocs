# Placeholder Content Handling Plan

## Overview
This document outlines the approach for handling placeholder content in the DotNetDocs documentation system to prevent sample text from accidentally making it into production documentation.

## Core Implementation

### 1. Update Placeholder Generation
Add an HTML comment marker at the very beginning of each placeholder file to clearly identify it as placeholder content:

```markdown
<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Usage
Describe how to use `TypeName` here.
```

This marker will be:
- Easy to spot visually when editing files
- Simple to detect programmatically
- Clear instruction to developers

### 2. Add ProjectContext Property
Add a new property to control placeholder visibility:
- Property: `bool ShowPlaceholders` (default: `true`)
- When `true`: Load all conceptual content including placeholders
- When `false`: Skip loading files that contain the TODO marker

This ensures:
- Default behavior shows everything (developers can see gaps)
- Production builds can hide placeholders if needed
- No breaking changes to existing behavior

### 3. Update LoadConceptualFileAsync Method
Enhance the loading logic to:
- Check for the TODO comment marker at the beginning of files
- If `ShowPlaceholders` is `false` and marker is found, skip loading
- Optionally log/store warnings about skipped placeholder content

### 4. Future Enhancements (Not Implemented Yet)
These features can be added later as needed:
- Create `ValidationReportRenderer` to analyze documentation completeness
- Generate reports showing which files are placeholders vs. customized
- Store warnings/errors for console output during processing
- Add "release mode" processing for production builds

## Implementation Benefits

This approach:
- **Simple**: Minimal changes to existing code
- **Non-breaking**: Default behavior remains the same
- **Visible**: Users can see documentation gaps by default
- **Flexible**: Easy to hide placeholders when needed
- **Extensible**: Sets foundation for future validation/reporting features

## Usage Example

```csharp
// Development mode - see all content including placeholders
var context = new ProjectContext 
{ 
    ShowPlaceholders = true  // default
};

// Production mode - hide placeholder content
var context = new ProjectContext 
{ 
    ShowPlaceholders = false 
};
```

## Testing Strategy

1. Generate placeholder files with TODO markers
2. Verify placeholders are shown by default
3. Test that setting `ShowPlaceholders = false` hides them
4. Ensure customized content (without markers) always shows