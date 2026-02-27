# Semantic Kernel Integration for CloudNimble.DotNetDocs.Plugins.AI

This document outlines the integration of Microsoft Semantic Kernel (SK) and optionally Kernel Memory into the CloudNimble.DotNetDocs ecosystem to provide flexible, AI-powered documentation augmentation.

## Overview

The `CloudNimble.DotNetDocs.Plugins.AI` project leverages Semantic Kernel to enable users to enhance their API documentation using AI models of their choice. This approach provides maximum flexibility, allowing developers to use OpenAI, Azure OpenAI, local models, or any SK-supported provider.

## Key Design Principles

1. **Model Agnostic**: Users choose their AI and embedding models
2. **Configuration-Driven**: Model selection and parameters via configuration
3. **Extensible**: Custom skills and planners can be added
4. **Privacy-Aware**: Support for local models and on-premise deployments
5. **Cost-Conscious**: Users control model usage and costs

## Architecture

### Core Components

#### 1. SemanticKernelEnricher
Primary orchestrator for AI-powered documentation enrichment.

```csharp
public class SemanticKernelEnricher : IDocEnricher
{
    private readonly IKernel _kernel;
    private readonly EnricherConfiguration _config;
    
    public SemanticKernelEnricher(EnricherConfiguration config)
    {
        _config = config;
        _kernel = BuildKernel(config);
    }
    
    public async Task EnrichAsync(DocEntity entity, EnrichmentContext context)
    {
        // Use configured AI model to enrich documentation
    }
}
```

#### 2. EnricherConfiguration
Flexible configuration for model selection and parameters.

```csharp
public class EnricherConfiguration
{
    /// <summary>
    /// AI model provider (OpenAI, AzureOpenAI, LocalModel, etc.)
    /// </summary>
    public ModelProvider Provider { get; set; }
    
    /// <summary>
    /// Model name/deployment (e.g., "gpt-4", "llama-2", custom deployment)
    /// </summary>
    public string ModelName { get; set; }
    
    /// <summary>
    /// API endpoint (for Azure or local models)
    /// </summary>
    public string? Endpoint { get; set; }
    
    /// <summary>
    /// API key or authentication token
    /// </summary>
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// Embedding model for semantic search (optional)
    /// </summary>
    public EmbeddingConfiguration? EmbeddingConfig { get; set; }
    
    /// <summary>
    /// Generation parameters
    /// </summary>
    public GenerationParameters Parameters { get; set; } = new();
}

public class GenerationParameters
{
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
    public double TopP { get; set; } = 0.95;
    public string? SystemPrompt { get; set; }
}
```

#### 3. DocumentationEnhancer
Specific skills for documentation enhancement.

```csharp
public class DocumentationEnhancer
{
    private readonly IKernel _kernel;
    
    /// <summary>
    /// Generate usage examples from method signatures
    /// </summary>
    public async Task<string> GenerateExamplesAsync(DocMember member)
    {
        var prompt = BuildExamplePrompt(member);
        return await _kernel.InvokeAsync<string>("GenerateExamples", prompt);
    }
    
    /// <summary>
    /// Enhance usage documentation with best practices
    /// </summary>
    public async Task<string> EnhanceUsageAsync(DocType type, string existingUsage)
    {
        var prompt = BuildUsagePrompt(type, existingUsage);
        return await _kernel.InvokeAsync<string>("EnhanceUsage", prompt);
    }
    
    /// <summary>
    /// Generate security/performance considerations
    /// </summary>
    public async Task<string> GenerateConsiderationsAsync(DocEntity entity)
    {
        var prompt = BuildConsiderationsPrompt(entity);
        return await _kernel.InvokeAsync<string>("GenerateConsiderations", prompt);
    }
}
```

#### 4. KernelMemoryIndexer (Optional)
Semantic search and similarity matching using Kernel Memory.

```csharp
public class KernelMemoryIndexer
{
    private readonly IKernelMemory _memory;
    
    /// <summary>
    /// Index existing documentation for semantic search
    /// </summary>
    public async Task IndexDocumentationAsync(DocAssembly assembly)
    {
        foreach (var ns in assembly.Namespaces)
        {
            foreach (var type in ns.Types)
            {
                await _memory.ImportTextAsync(
                    type.Usage + type.Examples,
                    documentId: type.Symbol.ToDisplayString()
                );
            }
        }
    }
    
    /// <summary>
    /// Find similar APIs based on semantic similarity
    /// </summary>
    public async Task<List<string>> FindSimilarApisAsync(DocEntity entity, int topK = 5)
    {
        var results = await _memory.SearchAsync(
            entity.Usage + entity.Examples,
            limit: topK
        );
        return results.Select(r => r.DocumentId).ToList();
    }
}
```

## Supported Model Providers

### 1. OpenAI
```json
{
  "Provider": "OpenAI",
  "ModelName": "gpt-4",
  "ApiKey": "sk-...",
  "Parameters": {
    "Temperature": 0.7,
    "MaxTokens": 1000
  }
}
```

### 2. Azure OpenAI
```json
{
  "Provider": "AzureOpenAI",
  "ModelName": "my-gpt4-deployment",
  "Endpoint": "https://myinstance.openai.azure.com/",
  "ApiKey": "...",
  "Parameters": {
    "Temperature": 0.7
  }
}
```

### 3. Local Models (via Ollama, LM Studio, etc.)
```json
{
  "Provider": "LocalModel",
  "ModelName": "llama2:13b",
  "Endpoint": "http://localhost:11434",
  "Parameters": {
    "Temperature": 0.8
  }
}
```

### 4. Hugging Face
```json
{
  "Provider": "HuggingFace",
  "ModelName": "codellama/CodeLlama-7b-Instruct-hf",
  "ApiKey": "hf_...",
  "Parameters": {
    "Temperature": 0.6
  }
}
```

## Usage Scenarios

### Scenario 1: Automated Example Generation
Generate code examples for methods that lack them:

```csharp
var config = new EnricherConfiguration
{
    Provider = ModelProvider.OpenAI,
    ModelName = "gpt-4",
    Parameters = new() { Temperature = 0.3 } // Lower temp for code
};

var enricher = new SemanticKernelEnricher(config);
await enricher.EnrichExamplesAsync(docMember);
```

### Scenario 2: Best Practices Enhancement
Enhance existing documentation with industry best practices:

```csharp
var enhancer = new DocumentationEnhancer(kernel);
docType.BestPractices = await enhancer.GenerateBestPracticesAsync(
    docType,
    context: "web-api-security"
);
```

### Scenario 3: Semantic API Discovery
Find and link related APIs using embeddings:

```csharp
var indexer = new KernelMemoryIndexer(memoryConfig);
await indexer.IndexDocumentationAsync(assembly);

docType.RelatedApis = await indexer.FindSimilarApisAsync(docType);
```

### Scenario 4: Offline/Air-gapped Documentation
Use local models for sensitive or air-gapped environments:

```csharp
var config = new EnricherConfiguration
{
    Provider = ModelProvider.LocalModel,
    ModelName = "mistral:7b",
    Endpoint = "http://internal-ai:8080"
};
```

## Prompt Engineering

### Customizable Prompt Templates
Users can customize prompts for their specific needs:

```csharp
public class PromptTemplates
{
    public string ExampleGenerationPrompt { get; set; } = @"
        Generate practical code examples for the following {0}:
        Signature: {1}
        Description: {2}
        
        Requirements:
        - Show typical usage patterns
        - Include error handling
        - Use meaningful variable names
        - Add brief comments
        
        Format as markdown code blocks.
    ";
    
    public string UsageEnhancementPrompt { get; set; } = @"
        Enhance the following usage documentation:
        Type: {0}
        Current: {1}
        
        Add:
        - Common use cases
        - Integration patterns
        - Performance tips
        
        Keep it concise and practical.
    ";
}
```

## Configuration Examples

### Via appsettings.json
```json
{
  "DotNetDocs": {
    "Plugins": {
      "AI": {
        "Provider": "AzureOpenAI",
        "ModelName": "gpt-4-turbo",
        "Endpoint": "${AZURE_OPENAI_ENDPOINT}",
        "ApiKey": "${AZURE_OPENAI_KEY}",
        "EmbeddingConfig": {
          "ModelName": "text-embedding-ada-002",
          "Dimensions": 1536
        },
        "Parameters": {
          "Temperature": 0.7,
          "MaxTokens": 2000,
          "SystemPrompt": "You are a technical documentation expert..."
        }
      }
    }
  }
}
```

### Via Environment Variables
```bash
DOTNETDOCS_AI_PROVIDER=OpenAI
DOTNETDOCS_AI_MODEL=gpt-4
DOTNETDOCS_AI_APIKEY=sk-...
DOTNETDOCS_AI_TEMPERATURE=0.7
```

### Via Code
```csharp
var config = new EnricherConfiguration
{
    Provider = ModelProvider.OpenAI,
    ModelName = Environment.GetEnvironmentVariable("AI_MODEL") ?? "gpt-3.5-turbo",
    ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
    Parameters = new()
    {
        Temperature = 0.7,
        MaxTokens = 1500,
        SystemPrompt = "Generate clear, concise API documentation."
    }
};
```

## Security Considerations

1. **API Key Management**: Use secure storage (Azure Key Vault, AWS Secrets Manager)
2. **Data Privacy**: Option to use local models for sensitive code
3. **Rate Limiting**: Built-in throttling to avoid API limits
4. **Content Filtering**: Optional filtering for sensitive information
5. **Audit Logging**: Track AI usage for compliance

## Performance Optimization

1. **Batch Processing**: Process multiple entities in parallel
2. **Caching**: Cache AI responses for identical inputs
3. **Incremental Updates**: Only process changed entities
4. **Model Selection**: Use smaller models for simple tasks
5. **Streaming**: Support streaming responses for large content

## Testing Strategy

1. **Mock AI Providers**: Test without real API calls
2. **Prompt Testing**: Validate prompt templates
3. **Response Validation**: Ensure generated content meets quality standards
4. **Cost Tracking**: Monitor and test token usage
5. **Fallback Handling**: Test behavior when AI is unavailable

## Future Enhancements

1. **Multi-Model Ensemble**: Use multiple models for better results
2. **Fine-Tuning Support**: Custom models for specific domains
3. **RAG Implementation**: Retrieval-augmented generation using codebase
4. **Interactive Mode**: Chat-based documentation refinement
5. **Version-Aware Docs**: Generate migration guides between versions

## Dependencies

- `Microsoft.SemanticKernel` (latest stable)
- `Microsoft.KernelMemory.Core` (optional, for semantic search)
- `Microsoft.Extensions.Configuration` (for config management)
- `Microsoft.Extensions.DependencyInjection` (for DI support)

## Summary

The Semantic Kernel integration provides a flexible, powerful foundation for AI-enhanced documentation that:
- Respects user choice of AI providers
- Supports both cloud and local deployments
- Enables cost-effective documentation generation
- Maintains security and privacy requirements
- Scales from simple enhancements to complex AI workflows

This design ensures CloudNimble.DotNetDocs remains at the forefront of AI-powered documentation while giving users complete control over their AI strategy.