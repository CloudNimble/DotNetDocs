# Documentation Mapping Specification

## Overview

This specification defines how .NET XML documentation comments are mapped to DocEntity properties in the DotNetDocs system. It provides clear, unambiguous rules for both AI and human developers to understand the documentation pipeline.

## Core Principles

### 1. Semantic Clarity

• Summary = What something IS (from `<summary>`)
• Usage = How to USE something (from conceptual content)
• Remarks = Additional notes and details (from `<remarks>`)

### 2. Source Separation

• XML Documentation = Code comments (`<summary>`, `<returns>`, etc.)
• Conceptual Documentation = External content (Usage, BestPractices, etc.)

### 3. Null Handling

• Empty XML tags result in null properties
• Missing XML documentation results in null properties
• Clean JSON/YAML output with no empty strings

## XML Tag Mappings

### Required Tags (Always Extracted)

#### `<summary>` → Summary Property

Purpose: Brief description of what the API element IS
Type: string? (nullable)
Example:

```csharp
/// <summary>Gets or sets the name of the person.</summary>
public string Name { get; set; }
```

Result: Summary = "Gets or sets the name of the person."

#### `<remarks>` → Remarks Property

Purpose: Additional detailed information
Type: string? (nullable)
Example:

```csharp
/// <summary>Gets or sets the name.</summary>
/// <remarks>This property supports Unicode characters.</remarks>
public string Name { get; set; }
```

Result: Remarks = "This property supports Unicode characters."

#### `<example>` → Examples Collection

Purpose: Code usage examples
Type: ICollection<string>? (nullable)
Example:

```csharp
/// <summary>Calculates the sum of two numbers.</summary>
/// <example>
/// <code>
/// int result = calculator.Add(5, 3);
/// Console.WriteLine(result); // Output: 8
/// </code>
/// </example>
public int Add(int a, int b) { return a + b; }
```

Result: Examples = ["<code>\nint result = calculator.Add(5, 3);\nConsole.WriteLine(result); // Output: 8\n</code>"]

### Method-Specific Tags

#### `<returns>` → Returns Property

Purpose: Description of what the method returns
Type: string? (nullable)
Applies To: Methods only
Example:

```csharp
/// <summary>Calculates the sum of two numbers.</summary>
/// <param name="a">The first number.</param>
/// <param name="b">The second number.</param>
/// <returns>The sum of the two numbers.</returns>
public int Add(int a, int b) { return a + b; }
```

Result: Returns = "The sum of the two numbers."

#### `<exception>` → Exceptions Collection

Purpose: Exceptions that can be thrown
Type: ICollection<DocException>? (nullable)
Applies To: Methods only
Example:

```csharp
/// <summary>Parses a string to an integer.</summary>
/// <param name="input">The string to parse.</param>
/// <returns>The parsed integer.</returns>
/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
/// <exception cref="FormatException">Thrown when input is not a valid number.</exception>
public int ParseToInt(string input) { ... }
```

Result: Exceptions = [{Type: "ArgumentNullException", Description: "Thrown when input is null."}, {Type: "FormatException", Description: "Thrown when input is not a valid number."}]

### Generic Type Tags

#### `<typeparam>` → TypeParameters Collection

Purpose: Description of generic type parameters
Type: ICollection<DocTypeParameter>? (nullable)
Applies To: Generic types and methods
Example:

```csharp
/// <summary>A generic container class.</summary>
/// <typeparam name="T">The type of elements in the container.</typeparam>
public class Container<T> { ... }
```

Result: TypeParameters = [{Name: "T", Description: "The type of elements in the container."}]

### Property-Specific Tags

#### `<value>` → Value Property

Purpose: Description of what the property represents
Type: string? (nullable)
Applies To: Properties only
Example:

```csharp
/// <summary>Gets or sets the person's age.</summary>
/// <value>The age in years.</value>
public int Age { get; set; }
```

Result: Value = "The age in years."

### Parameter Tags

#### `<param>` → Usage Property (on DocParameter)

Purpose: Description of method parameters
Type: string? (nullable)
Applies To: Method parameters
Example:

```csharp
/// <summary>Calculates the sum of two numbers.</summary>
/// <param name="a">The first number to add.</param>
/// <param name="b">The second number to add.</param>
public int Add(int a, int b) { return a + b; }
```

Result: Parameter a has Usage = "The first number to add."

### Cross-Reference Tags

#### `<seealso>` → SeeAlso Collection

Purpose: Related APIs or concepts
Type: ICollection<string>? (nullable)
Example:

```csharp
/// <summary>Represents a collection of items.</summary>
/// <seealso cref="List{T}"/>
/// <seealso cref="Dictionary{TKey,TValue}"/>
public interface ICollection<T> { ... }
```

Result: SeeAlso = ["List{T}", "Dictionary{TKey,TValue}"]

## Conceptual Documentation Properties

### Usage Property

Purpose: How to use the API element
Type: string? (nullable)
Source: External conceptual content files
Example: Tutorial-style usage instructions

### BestPractices Property

Purpose: Recommended usage patterns
Type: string? (nullable)
Source: External conceptual content files

### Patterns Property

Purpose: Common usage patterns
Type: string? (nullable)
Source: External conceptual content files

### Considerations Property

Purpose: Important things to consider
Type: string? (nullable)
Source: External conceptual content files

### RelatedApis Property

Purpose: Related API elements
Type: ICollection<string>? (nullable)
Source: External conceptual content files

## Decision Tree: When to Use Each Property

```
Does it describe WHAT something IS?
├── Yes → Use Summary (from <summary>)
└── No
    └── Does it describe HOW to use something?
        ├── Yes → Use Usage (from conceptual content)
        └── No
            └── Is it additional details about the API?
                ├── Yes → Use Remarks (from <remarks>)
                └── No
                    └── Is it a usage example?
                        ├── Yes → Use Examples (from <example>)
                        └── No
                            └── Is it method-specific?
                                ├── Yes
                                │   ├── Return value → Use Returns (from <returns>)
                                │   ├── Exceptions → Use Exceptions (from <exception>)
                                │   └── Generic parameters → Use TypeParameters (from <typeparam>)
                                └── No
                                    └── Is it property-specific?
                                        ├── Yes → Use Value (from <value>)
                                        └── No
                                            └── Is it parameter-specific?
                                                ├── Yes → Use Usage on DocParameter (from <param>)
                                                └── No
                                                    └── Is it cross-references?
                                                        ├── Yes → Use SeeAlso (from <seealso>)
                                                        └── No → Use appropriate conceptual property
```

## Pipeline Flow

```
Source Code with XML Comments
          │
          ▼
   AssemblyManager Extraction
          │
          ▼
   DocEntity Objects (mutable)
          │
          ▼
   Conceptual Content Loading
          │
          ▼
   Transformation Pipeline
          │
          ▼
   Renderer Output (JSON/YAML/Markdown)
```

## Data Types

### DocException

```csharp
public class DocException
{
    public string? Type { get; set; }        // Exception type (e.g., "ArgumentNullException")
    public string? Description { get; set; } // Description of when it's thrown
}
```

### DocTypeParameter

```csharp
public class DocTypeParameter
{
    public string? Name { get; set; }        // Parameter name (e.g., "T")
    public string? Description { get; set; } // Description of the parameter
}
```

## Error Handling

### Missing XML Documentation

• All properties default to null
• No errors thrown - graceful degradation
• Clean output with no empty strings

### Malformed XML

• Invalid XML tags are ignored
• Valid tags are still processed
• Logging for debugging purposes

### Encoding Issues

• UTF-8 encoding assumed
• Invalid characters are escaped
• No data loss from encoding problems

## Examples

### Complete Method Example

```csharp
/// <summary>Parses a string to an integer with validation.</summary>
/// <param name="input">The string to parse. Must not be null or empty.</param>
/// <typeparam name="TResult">The result type. Must be a numeric type.</typeparam>
/// <returns>The parsed integer value.</returns>
/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
/// <exception cref="FormatException">Thrown when input format is invalid.</exception>
/// <example>
/// <code>
/// var parser = new StringParser();
/// int result = parser.Parse<int>("123");
/// </code>
/// </example>
/// <remarks>This method uses invariant culture for parsing.</remarks>
public TResult Parse<TResult>(string input) where TResult : struct
{
    // Implementation
}
```

Result DocEntity:

```json
{
  "summary": "Parses a string to an integer with validation.",
  "returns": "The parsed integer value.",
  "remarks": "This method uses invariant culture for parsing.",
  "examples": ["<code>\nvar parser = new StringParser();\nint result = parser.Parse<int>(\"123\");\n</code>"],
  "exceptions": [
    {"type": "ArgumentNullException", "description": "Thrown when input is null."},
    {"type": "FormatException", "description": "Thrown when input format is invalid."}
  ],
  "typeParameters": [
    {"name": "TResult", "description": "The result type. Must be a numeric type."}
  ],
  "parameters": [
    {"name": "input", "usage": "The string to parse. Must not be null or empty."}
  ]
}
```

### Complete Property Example

```csharp
/// <summary>Gets or sets the person's full name.</summary>
/// <value>The person's full name including first, middle, and last names.</value>
public string FullName { get; set; }
```

Result DocEntity:

```json
{
  "summary": "Gets or sets the person's full name.",
  "value": "The person's full name including first, middle, and last names."
}
```

## Troubleshooting

### Problem: Empty strings in JSON output

Solution: Ensure all string properties are nullable and set to null when empty

### Problem: Missing documentation in output

Solution: Check that XML comments are properly formatted and extraction methods are called

### Problem: Wrong property getting populated

Solution: Refer to the decision tree above to verify correct property mapping

### Problem: Conceptual content overwriting XML content

Solution: Ensure conceptual loading happens after XML extraction, and use IsFromXml flags if needed

### Problem: Performance issues with large assemblies

Solution: Use lazy loading for large collections and consider parallel processing for extraction