using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;



#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Xml.Linq;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Mintlify;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mintlify.Core;
using Mintlify.Core.Models;
using Mintlify.Core.Models.Integrations;
#endif

namespace CloudNimble.DotNetDocs.Sdk.Tasks
{

    /// <summary>
    /// MSBuild task that generates documentation using DocumentationManager directly within the MSBuild process.
    /// </summary>
    public class GenerateDocumentationTask : Task
    {

        #region Properties

        /// <summary>
        /// Gets or sets the assemblies to generate documentation for.
        /// </summary>
        [Required]
        public ITaskItem[] Assemblies { get; set; } = [];

        /// <summary>
        /// Gets or sets the output path for the generated documentation.
        /// </summary>
        [Required]
        public string OutputPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API reference path relative to the output path.
        /// </summary>
        public string ApiReferencePath { get; set; } = "api-reference";

        /// <summary>
        /// Gets or sets the namespace mode for organizing documentation files.
        /// </summary>
        public string NamespaceMode { get; set; } = "Folder";

        /// <summary>
        /// Gets or sets the documentation type to generate.
        /// </summary>
        public string DocumentationType { get; set; } = "Mintlify";

        /// <summary>
        /// Gets or sets the conceptual content path.
        /// </summary>
        public string? ConceptualPath { get; set; }

        /// <summary>
        /// Gets or sets whether to generate placeholders for missing documentation.
        /// </summary>
        public bool GeneratePlaceholders { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of generated files (output).
        /// </summary>
        [Output]
        public ITaskItem[] GeneratedFiles { get; set; } = [];

        /// <summary>
        /// Gets or sets the navigation mode for Mintlify documentation.
        /// </summary>
        public string? MintlifyNavigationMode { get; set; }

        /// <summary>
        /// Gets or sets the unified group name for Mintlify documentation.
        /// </summary>
        public string? MintlifyUnifiedGroupName { get; set; }

        /// <summary>
        /// Gets or sets the path to an external docs.json template file.
        /// </summary>
        public string? DocsJsonTemplatePath { get; set; }

        /// <summary>
        /// Gets or sets the inline Mintlify template XML.
        /// </summary>
        public string? MintlifyTemplate { get; set; }

        /// <summary>
        /// Gets or sets the solution name for use as a default in templates.
        /// </summary>
        public string? SolutionName { get; set; }

        /// <summary>
        /// Gets or sets whether conceptual documentation features are enabled.
        /// </summary>
        public bool ConceptualDocsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show placeholder content in the documentation.
        /// </summary>
        public bool ShowPlaceholders { get; set; } = true;

        /// <summary>
        /// Gets or sets the resolved documentation references to combine with this documentation project.
        /// </summary>
        public ITaskItem[]? ResolvedDocumentationReferences { get; set; }

        /// <summary>
        /// Gets or sets whether a MintlifyTemplate is defined (inline or file-based).
        /// </summary>
        /// <value>
        /// When true, always generate docs.json regardless of assemblies.
        /// The presence of a template is an explicit signal that the user wants documentation output.
        /// </value>
        public bool HasMintlifyTemplate { get; set; } = false;

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes the task to generate documentation.
        /// </summary>
        /// <returns>true if the task executed successfully; otherwise, false.</returns>
        public override bool Execute()
        {
#if NET8_0_OR_GREATER
            try
            {
                Log.LogMessage(MessageImportance.High, $"🚀 Generating {DocumentationType} documentation...");
                // Validate DocumentationType (renderer type)
                var validRendererTypes = new[] { RendererType.Mintlify, RendererType.Markdown, RendererType.Json, RendererType.Yaml };
                if (!validRendererTypes.Contains(DocumentationType, StringComparer.OrdinalIgnoreCase))
                {
                    var validValues = string.Join(", ", validRendererTypes);
                    Log.LogError($"Invalid DocumentationType '{DocumentationType}'. Valid values are: {validValues}");
                    return false;
                }


                // Validate DocumentationReferences before building service container
                var documentationReferences = new List<DocumentationReference>();
                if (ResolvedDocumentationReferences is not null && ResolvedDocumentationReferences.Length > 0)
                {
                    Log.LogMessage(MessageImportance.Normal, $"   📚 Processing {ResolvedDocumentationReferences.Length} documentation reference(s)");

                    foreach (var item in ResolvedDocumentationReferences)
                    {
                        var name = item.GetMetadata("Name");
                        var docTypeString = item.GetMetadata("DocumentationType");

                        if (!Enum.TryParse<SupportedDocumentationType>(docTypeString, true, out var docType))
                        {
                            var validValues = string.Join(", ", Enum.GetNames<SupportedDocumentationType>());
                            Log.LogError($"Invalid DocumentationType '{docTypeString}' for reference '{item.GetMetadata("ProjectPath")}'. Valid values are: {validValues}");
                            return false;
                        }

                        var reference = new DocumentationReference
                        {
                            ProjectPath = item.GetMetadata("ProjectPath"),
                            DocumentationRoot = item.GetMetadata("DocumentationRoot"),
                            DestinationPath = item.GetMetadata("DestinationPath"),
                            IntegrationType = item.GetMetadata("IntegrationType"),
                            DocumentationType = docType,
                            NavigationFilePath = item.GetMetadata("NavigationFilePath"),
                            Name = !string.IsNullOrWhiteSpace(name) ? name : null
                        };

                        documentationReferences.Add(reference);
                        var displayName = !string.IsNullOrWhiteSpace(reference.Name) ? $"{reference.Name} ({Path.GetFileName(reference.ProjectPath)})" : Path.GetFileName(reference.ProjectPath);
                        Log.LogMessage(MessageImportance.Normal, $"      Added reference: {displayName} → {reference.DestinationPath}");
                    }
                }

                // Set up dependency injection
                var services = new ServiceCollection();

                // Configure the project context
                services.AddDotNetDocsCore(context =>
                {
                    context.DocumentationRootPath = OutputPath;
                    context.ApiReferencePath = ApiReferencePath;
                    context.ConceptualPath = ConceptualPath ?? Path.Combine(OutputPath, "conceptual");
                    context.FileNamingOptions.NamespaceMode = Enum.TryParse<NamespaceMode>(NamespaceMode, true, out var mode) ? mode : Core.Configuration.NamespaceMode.Folder;
                    context.ConceptualDocsEnabled = ConceptualDocsEnabled;
                    context.ShowPlaceholders = ShowPlaceholders;
                    context.HasMintlifyTemplate = HasMintlifyTemplate;

                    // Add the validated documentation references
                    foreach (var reference in documentationReferences)
                    {
                        context.DocumentationReferences.Add(reference);
                    }

                    // NamespaceFileMode will be set via the NamespaceMode property
                    // This is handled internally by the Core library
                });

                // Add the appropriate renderer based on documentation type
                switch (DocumentationType)
                {
                    case var _ when DocumentationType.Equals(RendererType.Mintlify, StringComparison.OrdinalIgnoreCase):
                        services.AddMintlifyServices(options =>
                        {
                            options.GenerateDocsJson = true;
                            options.GenerateNamespaceIndex = true;
                            options.IncludeIcons = true;

                            // Parse navigation mode from property (legacy support)
                            if (!string.IsNullOrWhiteSpace(MintlifyNavigationMode))
                            {
                                if (Enum.TryParse<NavigationMode>(MintlifyNavigationMode, true, out var navMode))
                                {
                                    options.Navigation.Mode = navMode;
                                    Log.LogMessage(MessageImportance.Normal, $"   Using navigation mode: {navMode}");
                                }
                                else
                                {
                                    Log.LogWarning($"Invalid MintlifyNavigationMode value: {MintlifyNavigationMode}. Using default.");
                                }
                            }

                            // Set unified group name
                            if (!string.IsNullOrWhiteSpace(MintlifyUnifiedGroupName))
                            {
                                options.UnifiedGroupName = MintlifyUnifiedGroupName;
                                Log.LogMessage(MessageImportance.Normal, $"   Using unified group name: {MintlifyUnifiedGroupName}");
                            }

                            // Load template from XML or file
                            DocsJsonConfig? template = null;
                            DocsNavigationConfig? docsNavConfig = null;

                            // First try inline XML template
                            if (!string.IsNullOrWhiteSpace(MintlifyTemplate))
                            {
                                var (parsedTemplate, parsedNavConfig) = ParseMintlifyTemplate(MintlifyTemplate);
                                template = parsedTemplate;
                                docsNavConfig = parsedNavConfig;
                                if (template is not null)
                                {
                                    Log.LogMessage(MessageImportance.Normal, "   Loaded Mintlify template from inline XML");
                                }
                            }
                            // Fall back to external file
                            else if (!string.IsNullOrWhiteSpace(DocsJsonTemplatePath) && File.Exists(DocsJsonTemplatePath))
                            {
                                try
                                {
                                    var json = File.ReadAllText(DocsJsonTemplatePath);
                                    template = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);
                                    Log.LogMessage(MessageImportance.Normal, $"   Loaded Mintlify template from: {DocsJsonTemplatePath}");
                                }
                                catch (Exception ex)
                                {
                                    Log.LogWarning($"Failed to load Mintlify template from {DocsJsonTemplatePath}: {ex.Message}");
                                }
                            }

                            // If no template was provided, create a default with solution name
                            if (template is null && !string.IsNullOrWhiteSpace(SolutionName))
                            {
                                template = new DocsJsonConfig
                                {
                                    Name = SolutionName,
                                    Theme = "mint",
                                    Colors = new ColorsConfig { Primary = "#0D9373" }
                                };
                                Log.LogMessage(MessageImportance.Normal, $"   Using default template with solution name: {SolutionName}");
                            }
                            
                            if (template is not null)
                            {
                                options.Template = template;
                            }

                            // Apply DocsNavigationConfig if parsed from template
                            if (docsNavConfig is not null)
                            {
                                options.Navigation = docsNavConfig;
                                Log.LogMessage(MessageImportance.Normal, $"   Using navigation config: Mode={docsNavConfig.Mode}, Type={docsNavConfig.Type}");
                            }
                        });
                        break;
                    case var _ when DocumentationType.Equals(RendererType.Markdown, StringComparison.OrdinalIgnoreCase):
                        services.AddMarkdownRenderer();
                        break;
                    case var _ when DocumentationType.Equals(RendererType.Json, StringComparison.OrdinalIgnoreCase):
                        services.AddJsonRenderer();
                        break;
                    case var _ when DocumentationType.Equals(RendererType.Yaml, StringComparison.OrdinalIgnoreCase):
                        services.AddYamlRenderer();
                        break;
                    default:
                        // This should never happen due to validation above, but keeping for safety
                        var validValues = string.Join(", ", new[] { RendererType.Mintlify, RendererType.Markdown, RendererType.Json, RendererType.Yaml });
                        Log.LogError($"Unknown documentation type: {DocumentationType}. Valid values are: {validValues}");
                        return false;
                }

                var serviceProvider = services.BuildServiceProvider();
                var manager = serviceProvider.GetRequiredService<DocumentationManager>();

                // Check if we have DocumentationReferences
                var hasReferences = documentationReferences.Count > 0;

                // Collect all valid assembly paths, skipping malformed paths from empty MSBuild batching
                var assemblyPairs = new List<(string assemblyPath, string xmlPath)>();
                foreach (var assembly in Assemblies)
                {
                    var assemblyPath = assembly.ItemSpec;

                    // Skip malformed paths from empty ItemGroup batching (e.g., "bin\Debug\\.dll")
                    if (string.IsNullOrWhiteSpace(assemblyPath) ||
                        assemblyPath.EndsWith("\\.dll", StringComparison.OrdinalIgnoreCase) ||
                        assemblyPath.EndsWith("/.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.LogMessage(MessageImportance.Low, $"Skipping malformed assembly path: {assemblyPath}");
                        continue;
                    }

                    var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

                    if (!File.Exists(assemblyPath))
                    {
                        Log.LogWarning($"Assembly not found: {assemblyPath}");
                        continue;
                    }

                    Log.LogMessage(MessageImportance.High, $"   📖 Found {Path.GetFileName(assemblyPath)}");
                    assemblyPairs.Add((assemblyPath, xmlPath));
                }

                // Handle different scenarios based on what we have
                if (assemblyPairs.Count == 0)
                {
                    if (hasReferences)
                    {
                        Log.LogMessage(MessageImportance.High, "📚 Documentation-only mode: Processing DocumentationReferences without local assemblies");
                    }
                    else if (HasMintlifyTemplate)
                    {
                        Log.LogMessage(MessageImportance.High, "📄 Template mode: Generating docs.json from MintlifyTemplate without assemblies");
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.High, "No assemblies, documentation references, or template found. Nothing to process.");
                        return true;
                    }
                }

                // Process all assemblies together to properly merge navigation
                Log.LogMessage(MessageImportance.High, $"   📚 Processing {assemblyPairs.Count} assemblies together for merged output...");
                
                try
                {
                    manager.ProcessAsync(assemblyPairs).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Log.LogError($"Failed to process assemblies: {ex.Message}");
                    return false;
                }

                // Collect statistics after processing
                var generatedFiles = new List<string>();

                if (DocumentationType.Equals(RendererType.Mintlify, StringComparison.OrdinalIgnoreCase))
                {
                    var mdFiles = Directory.GetFiles(OutputPath, "*.mdx", SearchOption.AllDirectories);
                    generatedFiles.AddRange(mdFiles);
                }
                else if (DocumentationType.Equals(RendererType.Markdown, StringComparison.OrdinalIgnoreCase))
                {
                    var mdFiles = Directory.GetFiles(OutputPath, "*.md", SearchOption.AllDirectories);
                    generatedFiles.AddRange(mdFiles);
                }
                else if (DocumentationType.Equals(RendererType.Json, StringComparison.OrdinalIgnoreCase))
                {
                    var jsonFiles = Directory.GetFiles(OutputPath, "*.json", SearchOption.AllDirectories);
                    generatedFiles.AddRange(jsonFiles);
                }
                else if (DocumentationType.Equals(RendererType.Yaml, StringComparison.OrdinalIgnoreCase))
                {
                    var yamlFiles = Directory.GetFiles(OutputPath, "*.yaml", SearchOption.AllDirectories);
                    generatedFiles.AddRange(yamlFiles);
                }

                // Log statistics
                Log.LogMessage(MessageImportance.High, "📊 Documentation Statistics:");
                Log.LogMessage(MessageImportance.High, $"   📄 Documentation type: {DocumentationType}");
                Log.LogMessage(MessageImportance.High, $"   📦 Assemblies processed: {assemblyPairs.Count}");
                
                if (generatedFiles.Count > 0)
                {
                    Log.LogMessage(MessageImportance.High, $"   📝 Files generated: {generatedFiles.Distinct().Count()}");
                }

                // Return generated files as output
                GeneratedFiles = [.. generatedFiles.Distinct().Select(f => new TaskItem(f))];

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Documentation generation failed: {ex.Message}");
                Log.LogMessage(MessageImportance.Low, ex.StackTrace);
                return false;
            }
#else
            // For netstandard2.0, we can't use the documentation generation
            // This would typically shell out to a tool instead
            Log.LogWarning("Documentation generation is not supported on .NET Framework. Please use the dotnet tool instead.");
            return true;
#endif
        }

        #endregion

#if NET8_0_OR_GREATER
        #region Internal Methods

        /// <summary>
        /// Parses the Mintlify template from XML string.
        /// </summary>
        /// <param name="xmlTemplate">The XML template string.</param>
        /// <returns>A DocsJsonConfig instance, or null if parsing fails.</returns>
        internal (DocsJsonConfig? config, DocsNavigationConfig? navConfig) ParseMintlifyTemplate(string xmlTemplate)
        {
            try
            {
                var doc = XDocument.Parse($"<root>{xmlTemplate}</root>");
                var root = doc.Root;

                if (root is null)
                {
                    Log.LogWarning("Failed to parse MintlifyTemplate XML: root element is null");
                    return (null, null);
                }

                // Get the name with fallback to solution name
                var nameValue = root.Element(nameof(DocsJsonConfig.Name))?.Value;
                if (string.IsNullOrWhiteSpace(nameValue) && !string.IsNullOrWhiteSpace(SolutionName))
                {
                    nameValue = SolutionName;
                }

                var config = new DocsJsonConfig
                {
                    Name = nameValue ?? "API Documentation",
                    Description = root.Element(nameof(DocsJsonConfig.Description))?.Value,
                    Theme = root.Element(nameof(DocsJsonConfig.Theme))?.Value ?? "mint"
                };

                // Parse DocsNavigationConfig from Navigation element attributes and legacy elements
                var docsNavConfig = ParseDocsNavigationConfig(root);

                // Parse Colors
                var colorsElement = root.Element(nameof(DocsJsonConfig.Colors));
                if (colorsElement is not null)
                {
                    config.Colors = new ColorsConfig
                    {
                        Primary = colorsElement.Element(nameof(ColorsConfig.Primary))?.Value ?? "#000000",
                        Light = colorsElement.Element(nameof(ColorsConfig.Light))?.Value,
                        Dark = colorsElement.Element(nameof(ColorsConfig.Dark))?.Value
                    };
                }

                // Parse Logo
                var logoElement = root.Element(nameof(DocsJsonConfig.Logo));
                if (logoElement is not null)
                {
                    config.Logo = new LogoConfig
                    {
                        Light = logoElement.Element(nameof(LogoConfig.Light))?.Value,
                        Dark = logoElement.Element(nameof(LogoConfig.Dark))?.Value,
                        Href = logoElement.Element(nameof(LogoConfig.Href))?.Value
                    };
                }

                // Parse Favicon
                var faviconElement = root.Element(nameof(DocsJsonConfig.Favicon));
                if (faviconElement is not null)
                {
                    // Check if it has Light/Dark sub-elements
                    var lightFavicon = faviconElement.Element(nameof(FaviconConfig.Light))?.Value;
                    var darkFavicon = faviconElement.Element(nameof(FaviconConfig.Dark))?.Value;

                    if (lightFavicon is not null || darkFavicon is not null)
                    {
                        config.Favicon = new FaviconConfig
                        {
                            Light = lightFavicon,
                            Dark = darkFavicon
                        };
                    }
                    else if (!string.IsNullOrWhiteSpace(faviconElement.Value))
                    {
                        // Simple string format - both light and dark use same icon
                        config.Favicon = new FaviconConfig
                        {
                            Light = faviconElement.Value,
                            Dark = faviconElement.Value
                        };
                    }
                }

                // Parse Navigation structure
                var navigationElement = root.Element(nameof(DocsJsonConfig.Navigation));
                if (navigationElement is not null)
                {
                    config.Navigation = ParseNavigationConfig(navigationElement);
                }

                // Parse Styling
                var stylingElement = root.Element(nameof(DocsJsonConfig.Styling));
                if (stylingElement is not null)
                {
                    config.Styling = ParseStylingConfig(stylingElement);
                }

                // Parse Appearance
                var appearanceElement = root.Element(nameof(DocsJsonConfig.Appearance));
                if (appearanceElement is not null)
                {
                    config.Appearance = ParseAppearanceConfig(appearanceElement);
                }

                // Parse Integrations
                var integrationsElement = root.Element(nameof(DocsJsonConfig.Integrations));
                if (integrationsElement is not null)
                {
                    config.Integrations = ParseIntegrationsConfig(integrationsElement);
                }

                // Parse Interaction
                var interactionElement = root.Element(nameof(DocsJsonConfig.Interaction));
                if (interactionElement is not null)
                {
                    config.Interaction = ParseInteractionConfig(interactionElement);
                }

                // Parse Api
                var apiElement = root.Element(nameof(DocsJsonConfig.Api));
                if (apiElement is not null)
                {
                    config.Api = ParseApiConfig(apiElement);
                }

                // Parse Contextual
                var contextualElement = root.Element(nameof(DocsJsonConfig.Contextual));
                if (contextualElement is not null)
                {
                    config.Contextual = ParseContextualConfig(contextualElement);
                }

                // Parse Fonts
                var fontsElement = root.Element(nameof(DocsJsonConfig.Fonts));
                if (fontsElement is not null)
                {
                    config.Fonts = ParseFontsConfig(fontsElement);
                }

                // Parse Thumbnails
                var thumbnailsElement = root.Element(nameof(DocsJsonConfig.Thumbnails));
                if (thumbnailsElement is not null)
                {
                    config.Thumbnails = ParseThumbnailsConfig(thumbnailsElement);
                }

                // Parse Metadata
                var metadataElement = root.Element(nameof(DocsJsonConfig.Metadata));
                if (metadataElement is not null)
                {
                    config.Metadata = ParseMetadataConfig(metadataElement);
                }

                // Parse Errors
                var errorsElement = root.Element(nameof(DocsJsonConfig.Errors));
                if (errorsElement is not null)
                {
                    config.Errors = ParseErrorsConfig(errorsElement);
                }

                return (config, docsNavConfig);
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to parse MintlifyTemplate XML: {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// Parses the DocsNavigationConfig from the root element.
        /// Supports both new attribute-based format and legacy element-based format.
        /// </summary>
        /// <param name="root">The root XML element.</param>
        /// <returns>A DocsNavigationConfig instance.</returns>
        internal DocsNavigationConfig ParseDocsNavigationConfig(XElement root)
        {
            var navConfig = new DocsNavigationConfig();
            var navigationElement = root.Element(nameof(DocsJsonConfig.Navigation));

            // Try new attribute-based format first
            if (navigationElement is not null)
            {
                // Parse Mode attribute
                var modeAttr = navigationElement.Attribute("Mode")?.Value;
                if (!string.IsNullOrWhiteSpace(modeAttr))
                {
                    if (Enum.TryParse<NavigationMode>(modeAttr, true, out var mode))
                    {
                        navConfig.Mode = mode;
                    }
                }

                // Parse Type attribute
                var typeAttr = navigationElement.Attribute("Type")?.Value;
                if (!string.IsNullOrWhiteSpace(typeAttr))
                {
                    if (Enum.TryParse<NavigationType>(typeAttr, true, out var type))
                    {
                        navConfig.Type = type;
                    }
                }

                // Parse Name attribute
                var nameAttr = navigationElement.Attribute("Name")?.Value;
                if (!string.IsNullOrWhiteSpace(nameAttr))
                {
                    navConfig.Name = nameAttr;
                }
            }

            // Fall back to legacy element-based format for backward compatibility
            if (navigationElement is null || (navigationElement.Attribute("Mode") is null &&
                                              navigationElement.Attribute("Type") is null &&
                                              navigationElement.Attribute("Name") is null))
            {
                // Try NavigationType legacy element
                var navTypeElement = root.Element("NavigationType");
                if (navTypeElement is not null && !string.IsNullOrWhiteSpace(navTypeElement.Value))
                {
                    if (Enum.TryParse<NavigationType>(navTypeElement.Value, true, out var type))
                    {
                        navConfig.Type = type;
                    }
                }

                // Try NavigationName legacy element
                var navNameElement = root.Element("NavigationName");
                if (navNameElement is not null && !string.IsNullOrWhiteSpace(navNameElement.Value))
                {
                    navConfig.Name = navNameElement.Value;
                }

                // Note: NavigationMode was previously handled separately via MintlifyNavigationMode property,
                // so we don't need a legacy element for it here
            }

            return navConfig;
        }

        /// <summary>
        /// Parses the Navigation element from the MintlifyTemplate XML.
        /// </summary>
        /// <param name="navigationElement">The navigation XML element.</param>
        /// <returns>A NavigationConfig instance.</returns>
        internal NavigationConfig ParseNavigationConfig(XElement navigationElement)
        {
            var navConfig = new NavigationConfig();

            // Parse Tabs
            var tabsElement = navigationElement.Element("Tabs");
            if (tabsElement is not null)
            {
                navConfig.Tabs = [];
                foreach (var tabElement in tabsElement.Elements("Tab"))
                {
                    var tab = ParseTabConfig(tabElement);
                    if (tab is not null)
                    {
                        navConfig.Tabs.Add(tab);
                    }
                }
            }

            // Parse Anchors
            var anchorsElement = navigationElement.Element("Anchors");
            if (anchorsElement is not null)
            {
                navConfig.Anchors = [];
                foreach (var anchorElement in anchorsElement.Elements("Anchor"))
                {
                    var anchor = ParseAnchorConfig(anchorElement);
                    if (anchor is not null)
                    {
                        navConfig.Anchors.Add(anchor);
                    }
                }
            }

            // Parse Dropdowns
            var dropdownsElement = navigationElement.Element("Dropdowns");
            if (dropdownsElement is not null)
            {
                navConfig.Dropdowns = [];
                foreach (var dropdownElement in dropdownsElement.Elements("Dropdown"))
                {
                    var dropdown = ParseDropdownConfig(dropdownElement);
                    if (dropdown is not null)
                    {
                        navConfig.Dropdowns.Add(dropdown);
                    }
                }
            }

            // Parse Products
            var productsElement = navigationElement.Element("Products");
            if (productsElement is not null)
            {
                navConfig.Products = [];
                foreach (var productElement in productsElement.Elements("Product"))
                {
                    var product = ParseProductConfig(productElement);
                    if (product is not null)
                    {
                        navConfig.Products.Add(product);
                    }
                }
            }

            // Parse Pages (only if explicitly provided alongside or instead of structured section types)
            var pagesElement = navigationElement.Element(nameof(NavigationConfig.Pages));
            if (pagesElement is not null)
            {
                navConfig.Pages = [];
                var groupsElement = pagesElement.Element("Groups");
                if (groupsElement is not null)
                {
                    foreach (var groupElement in groupsElement.Elements("Group"))
                    {
                        var group = ParseGroupConfig(groupElement);
                        if (group is not null)
                        {
                            navConfig.Pages.Add(group);
                        }
                    }
                }

                foreach (var pageElement in pagesElement.Elements("Page"))
                {
                    var pageName = pageElement.Value?.Trim();
                    if (!string.IsNullOrWhiteSpace(pageName))
                    {
                        navConfig.Pages.Add(pageName);
                    }
                }
            }

            return navConfig;
        }

        /// <summary>
        /// Parses a Group element from the navigation XML.
        /// </summary>
        /// <param name="groupElement">The group XML element.</param>
        /// <returns>A GroupConfig instance, or null if parsing fails.</returns>
        internal GroupConfig? ParseGroupConfig(XElement groupElement)
        {
            var groupName = groupElement.Attribute("Name")?.Value;
            if (string.IsNullOrWhiteSpace(groupName))
            {
                Log.LogWarning("Group element missing Name attribute, skipping");
                return null;
            }

            var group = new GroupConfig
            {
                Group = groupName,
                Icon = groupElement.Attribute("Icon")?.Value,
                Tag = groupElement.Attribute("Tag")?.Value,
                Pages = []
            };

            // Parse direct pages (semicolon-separated list)
            var pagesElement = groupElement.Element(nameof(GroupConfig.Pages));
            if (pagesElement is not null && !string.IsNullOrWhiteSpace(pagesElement.Value))
            {
                var pageList = pagesElement.Value.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var page in pageList)
                {
                    var trimmedPage = page.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedPage))
                    {
                        group.Pages.Add(trimmedPage);
                    }
                }
            }

            // Parse nested groups - check both <Groups> wrapper and direct <Group> children
            var nestedGroupsElement = groupElement.Element("Groups");
            if (nestedGroupsElement is not null)
            {
                foreach (var nestedGroupElement in nestedGroupsElement.Elements("Group"))
                {
                    var nestedGroup = ParseGroupConfig(nestedGroupElement);
                    if (nestedGroup is not null)
                    {
                        group.Pages.Add(nestedGroup);
                    }
                }
            }
            else
            {
                // Also check for direct Group children (when Groups wrapper is not used)
                foreach (var directGroupElement in groupElement.Elements("Group"))
                {
                    var nestedGroup = ParseGroupConfig(directGroupElement);
                    if (nestedGroup is not null)
                    {
                        group.Pages.Add(nestedGroup);
                    }
                }
            }

            return group;
        }

        /// <summary>
        /// Parses an Anchor element from the navigation XML.
        /// </summary>
        /// <param name="anchorElement">The anchor XML element.</param>
        /// <returns>An <see cref="AnchorConfig"/> instance, or <see langword="null"/> if parsing fails.</returns>
        /// <remarks>
        /// Expects a <c>Name</c> attribute on the element. Optional attributes include <c>Href</c> and <c>Icon</c>.
        /// Child <c>&lt;Pages&gt;</c> and <c>&lt;Tabs&gt;</c> elements are also parsed if present.
        /// </remarks>
        /// <example>
        /// <code>
        /// &lt;Anchor Name="API Reference" Href="/api" Icon="code"&gt;
        ///     &lt;Pages&gt;
        ///         &lt;Groups&gt;
        ///             &lt;Group Name="Endpoints" Icon="bolt"&gt;
        ///                 &lt;Pages&gt;api/index;api/auth&lt;/Pages&gt;
        ///             &lt;/Group&gt;
        ///         &lt;/Groups&gt;
        ///     &lt;/Pages&gt;
        /// &lt;/Anchor&gt;
        /// </code>
        /// </example>
        internal AnchorConfig? ParseAnchorConfig(XElement anchorElement)
        {
            var anchorName = anchorElement.Attribute("Name")?.Value;
            if (string.IsNullOrWhiteSpace(anchorName))
            {
                Log.LogWarning("Anchor element missing Name attribute, skipping");
                return null;
            }

            var anchor = new AnchorConfig
            {
                Anchor = anchorName,
                Href = anchorElement.Attribute("Href")?.Value,
                Icon = anchorElement.Attribute("Icon")?.Value,
                Pages = []
            };

            ParseNavigationSectionPages(anchorElement, anchor.Pages);

            // Parse nested Tabs within Anchor
            var tabsElement = anchorElement.Element("Tabs");
            if (tabsElement is not null)
            {
                anchor.Tabs = [];
                foreach (var tabElement in tabsElement.Elements("Tab"))
                {
                    var tab = ParseTabConfig(tabElement);
                    if (tab is not null)
                    {
                        anchor.Tabs.Add(tab);
                    }
                }
            }

            return anchor;
        }

        /// <summary>
        /// Parses a Dropdown element from the navigation XML.
        /// </summary>
        /// <param name="dropdownElement">The dropdown XML element.</param>
        /// <returns>A <see cref="DropdownConfig"/> instance, or <see langword="null"/> if parsing fails.</returns>
        /// <remarks>
        /// Expects a <c>Name</c> attribute on the element. Optional attributes include <c>Href</c> and <c>Icon</c>.
        /// Child <c>&lt;Pages&gt;</c>, <c>&lt;Tabs&gt;</c>, and <c>&lt;Anchors&gt;</c> elements are also parsed if present.
        /// </remarks>
        /// <example>
        /// <code>
        /// &lt;Dropdown Name="Products" Icon="grid"&gt;
        ///     &lt;Anchors&gt;
        ///         &lt;Anchor Name="Core" Icon="star"&gt;
        ///             &lt;Pages&gt;&lt;Groups&gt;&lt;Group Name="Basics"&gt;&lt;Pages&gt;core/index&lt;/Pages&gt;&lt;/Group&gt;&lt;/Groups&gt;&lt;/Pages&gt;
        ///         &lt;/Anchor&gt;
        ///     &lt;/Anchors&gt;
        /// &lt;/Dropdown&gt;
        /// </code>
        /// </example>
        internal DropdownConfig? ParseDropdownConfig(XElement dropdownElement)
        {
            var dropdownName = dropdownElement.Attribute("Name")?.Value;
            if (string.IsNullOrWhiteSpace(dropdownName))
            {
                Log.LogWarning("Dropdown element missing Name attribute, skipping");
                return null;
            }

            var dropdown = new DropdownConfig
            {
                Dropdown = dropdownName,
                Href = dropdownElement.Attribute("Href")?.Value,
                Icon = dropdownElement.Attribute("Icon")?.Value,
                Pages = []
            };

            ParseNavigationSectionPages(dropdownElement, dropdown.Pages);

            // Parse nested Tabs within Dropdown
            var tabsElement = dropdownElement.Element("Tabs");
            if (tabsElement is not null)
            {
                dropdown.Tabs = [];
                foreach (var tabElement in tabsElement.Elements("Tab"))
                {
                    var tab = ParseTabConfig(tabElement);
                    if (tab is not null)
                    {
                        dropdown.Tabs.Add(tab);
                    }
                }
            }

            // Parse nested Anchors within Dropdown
            var anchorsElement = dropdownElement.Element("Anchors");
            if (anchorsElement is not null)
            {
                dropdown.Anchors = [];
                foreach (var anchorElement in anchorsElement.Elements("Anchor"))
                {
                    var anchor = ParseAnchorConfig(anchorElement);
                    if (anchor is not null)
                    {
                        dropdown.Anchors.Add(anchor);
                    }
                }
            }

            return dropdown;
        }

        /// <summary>
        /// Parses a Product element from the navigation XML.
        /// </summary>
        /// <param name="productElement">The product XML element.</param>
        /// <returns>A <see cref="ProductConfig"/> instance, or <see langword="null"/> if parsing fails.</returns>
        /// <remarks>
        /// Expects a <c>Name</c> attribute on the element. Optional attributes include <c>Href</c>, <c>Icon</c>, and <c>Description</c>.
        /// Child <c>&lt;Pages&gt;</c> elements are also parsed if present.
        /// </remarks>
        /// <example>
        /// <code>
        /// &lt;Product Name="CloudNimble Core" Href="/core" Icon="box" Description="Core platform features"&gt;
        ///     &lt;Pages&gt;
        ///         &lt;Groups&gt;
        ///             &lt;Group Name="Getting Started" Icon="stars"&gt;
        ///                 &lt;Pages&gt;core/index;core/quickstart&lt;/Pages&gt;
        ///             &lt;/Group&gt;
        ///         &lt;/Groups&gt;
        ///     &lt;/Pages&gt;
        /// &lt;/Product&gt;
        /// </code>
        /// </example>
        internal ProductConfig? ParseProductConfig(XElement productElement)
        {
            var productName = productElement.Attribute("Name")?.Value;
            if (string.IsNullOrWhiteSpace(productName))
            {
                Log.LogWarning("Product element missing Name attribute, skipping");
                return null;
            }

            var product = new ProductConfig
            {
                Product = productName,
                Href = productElement.Attribute("Href")?.Value,
                Icon = productElement.Attribute("Icon")?.Value,
                Description = productElement.Attribute("Description")?.Value,
                Pages = []
            };

            ParseNavigationSectionPages(productElement, product.Pages);

            return product;
        }

        /// <summary>
        /// Parses a Tab element from the navigation XML.
        /// </summary>
        /// <param name="tabElement">The tab XML element.</param>
        /// <returns>A <see cref="TabConfig"/> instance, or <see langword="null"/> if parsing fails.</returns>
        /// <remarks>
        /// Expects a <c>Name</c> attribute on the element. Optional attributes include <c>Href</c> and <c>Icon</c>.
        /// Child <c>&lt;Pages&gt;</c> elements are also parsed if present.
        /// </remarks>
        /// <example>
        /// <code>
        /// &lt;Tab Name="Guides" Href="/guides" Icon="book"&gt;
        ///     &lt;Pages&gt;
        ///         &lt;Groups&gt;
        ///             &lt;Group Name="Getting Started" Icon="stars"&gt;
        ///                 &lt;Pages&gt;guides/index;guides/quickstart&lt;/Pages&gt;
        ///             &lt;/Group&gt;
        ///         &lt;/Groups&gt;
        ///     &lt;/Pages&gt;
        /// &lt;/Tab&gt;
        /// </code>
        /// </example>
        internal TabConfig? ParseTabConfig(XElement tabElement)
        {
            var tabName = tabElement.Attribute("Name")?.Value;
            if (string.IsNullOrWhiteSpace(tabName))
            {
                Log.LogWarning("Tab element missing Name attribute, skipping");
                return null;
            }

            var tab = new TabConfig
            {
                Tab = tabName,
                Href = tabElement.Attribute("Href")?.Value,
                Icon = tabElement.Attribute("Icon")?.Value,
                Pages = []
            };

            ParseNavigationSectionPages(tabElement, tab.Pages);

            return tab;
        }

        /// <summary>
        /// Parses the <c>&lt;Pages&gt;</c> child element of a navigation section element and populates the provided
        /// <paramref name="pages"/> list with page strings and nested <see cref="GroupConfig"/> objects.
        /// </summary>
        /// <param name="parentElement">The parent XML element that may contain a <c>&lt;Pages&gt;</c> child.</param>
        /// <param name="pages">The list to populate with parsed page entries.</param>
        private void ParseNavigationSectionPages(XElement parentElement, List<object> pages)
        {
            var pagesElement = parentElement.Element("Pages");
            if (pagesElement is null)
            {
                return;
            }

            var groupsElement = pagesElement.Element("Groups");
            if (groupsElement is not null)
            {
                foreach (var groupElement in groupsElement.Elements("Group"))
                {
                    var group = ParseGroupConfig(groupElement);
                    if (group is not null)
                    {
                        pages.Add(group);
                    }
                }
            }

            foreach (var pageElement in pagesElement.Elements("Page"))
            {
                var pageName = pageElement.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(pageName))
                {
                    pages.Add(pageName);
                }
            }
        }

        /// <summary>
        /// Parses the Integrations element from the MintlifyTemplate XML using attributes.
        /// </summary>
        /// <param name="integrationsElement">The integrations XML element.</param>
        /// <returns>An IntegrationsConfig instance.</returns>
        internal IntegrationsConfig ParseIntegrationsConfig(XElement integrationsElement)
        {
            var config = new IntegrationsConfig();

            // Parse Amplitude
            var amplitudeElement = integrationsElement.Element(nameof(IntegrationsConfig.Amplitude));
            if (amplitudeElement is not null)
            {
                config.Amplitude = new AmplitudeConfig
                {
                    ApiKey = amplitudeElement.Attribute(nameof(AmplitudeConfig.ApiKey))?.Value
                };
            }

            // Parse Clearbit
            var clearbitElement = integrationsElement.Element(nameof(IntegrationsConfig.Clearbit));
            if (clearbitElement is not null)
            {
                config.Clearbit = new ClearbitConfig
                {
                    PublicApiKey = clearbitElement.Attribute(nameof(ClearbitConfig.PublicApiKey))?.Value
                };
            }

            // Parse Fathom
            var fathomElement = integrationsElement.Element(nameof(IntegrationsConfig.Fathom));
            if (fathomElement is not null)
            {
                config.Fathom = new FathomConfig
                {
                    SiteId = fathomElement.Attribute(nameof(FathomConfig.SiteId))?.Value
                };
            }

            // Parse Google Analytics 4
            var ga4Element = integrationsElement.Element(nameof(IntegrationsConfig.GoogleAnalytics4));
            if (ga4Element is not null)
            {
                config.GoogleAnalytics4 = new GoogleAnalytics4Config
                {
                    MeasurementId = ga4Element.Attribute(nameof(GoogleAnalytics4Config.MeasurementId))?.Value
                };
            }

            // Parse Google Tag Manager
            var gtmElement = integrationsElement.Element(nameof(IntegrationsConfig.Gtm));
            if (gtmElement is not null)
            {
                config.Gtm = new GtmConfig
                {
                    TagId = gtmElement.Attribute(nameof(GtmConfig.TagId))?.Value
                };
            }

            // Parse Heap
            var heapElement = integrationsElement.Element(nameof(IntegrationsConfig.Heap));
            if (heapElement is not null)
            {
                config.Heap = new HeapConfig
                {
                    AppId = heapElement.Attribute(nameof(HeapConfig.AppId))?.Value
                };
            }

            // Parse Hightouch
            var hightouchElement = integrationsElement.Element(nameof(IntegrationsConfig.Hightouch));
            if (hightouchElement is not null)
            {
                config.Hightouch = new HightouchConfig
                {
                    ApiKey = hightouchElement.Attribute(nameof(HightouchConfig.ApiKey))?.Value
                };
            }

            // Parse Hotjar
            var hotjarElement = integrationsElement.Element(nameof(IntegrationsConfig.Hotjar));
            if (hotjarElement is not null)
            {
                config.Hotjar = new HotjarConfig
                {
                    Hjid = hotjarElement.Attribute(nameof(HotjarConfig.Hjid))?.Value,
                    Hjsv = hotjarElement.Attribute(nameof(HotjarConfig.Hjsv))?.Value
                };
            }

            // Parse LogRocket
            var logrocketElement = integrationsElement.Element(nameof(IntegrationsConfig.LogRocket));
            if (logrocketElement is not null)
            {
                config.LogRocket = new LogRocketConfig
                {
                    AppId = logrocketElement.Attribute(nameof(LogRocketConfig.AppId))?.Value
                };
            }

            // Parse Mixpanel
            var mixpanelElement = integrationsElement.Element(nameof(IntegrationsConfig.Mixpanel));
            if (mixpanelElement is not null)
            {
                config.Mixpanel = new MixpanelConfig
                {
                    ProjectToken = mixpanelElement.Attribute(nameof(MixpanelConfig.ProjectToken))?.Value
                };
            }

            // Parse Pirsch
            var pirschElement = integrationsElement.Element(nameof(IntegrationsConfig.Pirsch));
            if (pirschElement is not null)
            {
                config.Pirsch = new PirschConfig
                {
                    Id = pirschElement.Attribute(nameof(PirschConfig.Id))?.Value
                };
            }

            // Parse Plausible
            var plausibleElement = integrationsElement.Element(nameof(IntegrationsConfig.Plausible));
            if (plausibleElement is not null)
            {
                config.Plausible = new PlausibleConfig
                {
                    Domain = plausibleElement.Attribute(nameof(PlausibleConfig.Domain))?.Value,
                    Server = plausibleElement.Attribute(nameof(PlausibleConfig.Server))?.Value
                };
            }

            // Parse PostHog
            var posthogElement = integrationsElement.Element(nameof(IntegrationsConfig.PostHog));
            if (posthogElement is not null)
            {
                config.PostHog = new PostHogConfig
                {
                    ApiKey = posthogElement.Attribute(nameof(PostHogConfig.ApiKey))?.Value,
                    ApiHost = posthogElement.Attribute(nameof(PostHogConfig.ApiHost))?.Value
                };

                var sessionRecordingAttr = posthogElement.Attribute(nameof(PostHogConfig.SessionRecording))?.Value;
                if (!string.IsNullOrWhiteSpace(sessionRecordingAttr) && bool.TryParse(sessionRecordingAttr, out var sessionRecording))
                {
                    config.PostHog.SessionRecording = sessionRecording;
                }
            }

            // Parse Adobe
            var adobeElement = integrationsElement.Element(nameof(IntegrationsConfig.Adobe));
            if (adobeElement is not null)
            {
                config.Adobe = new AdobeConfig
                {
                    LaunchUrl = adobeElement.Attribute(nameof(AdobeConfig.LaunchUrl))?.Value
                };
            }

            // Parse Clarity
            var clarityElement = integrationsElement.Element(nameof(IntegrationsConfig.Clarity));
            if (clarityElement is not null)
            {
                config.Clarity = new ClarityConfig
                {
                    ProjectId = clarityElement.Attribute(nameof(ClarityConfig.ProjectId))?.Value
                };
            }

            // Parse Cookies
            var cookiesElement = integrationsElement.Element(nameof(IntegrationsConfig.Cookies));
            if (cookiesElement is not null)
            {
                config.Cookies = new CookiesConfig
                {
                    Key = cookiesElement.Attribute(nameof(CookiesConfig.Key))?.Value,
                    Value = cookiesElement.Attribute(nameof(CookiesConfig.Value))?.Value
                };
            }

            // Parse FrontChat
            var frontChatElement = integrationsElement.Element(nameof(IntegrationsConfig.FrontChat));
            if (frontChatElement is not null)
            {
                config.FrontChat = new FrontChatConfig
                {
                    SnippetId = frontChatElement.Attribute(nameof(FrontChatConfig.SnippetId))?.Value
                };
            }

            // Parse Intercom
            var intercomElement = integrationsElement.Element(nameof(IntegrationsConfig.Intercom));
            if (intercomElement is not null)
            {
                config.Intercom = new IntercomConfig
                {
                    AppId = intercomElement.Attribute(nameof(IntercomConfig.AppId))?.Value
                };
            }

            // Parse Koala
            var koalaElement = integrationsElement.Element(nameof(IntegrationsConfig.Koala));
            if (koalaElement is not null)
            {
                config.Koala = new KoalaConfig
                {
                    PublicApiKey = koalaElement.Attribute(nameof(KoalaConfig.PublicApiKey))?.Value
                };
            }

            // Parse Telemetry
            var telemetryElement = integrationsElement.Element(nameof(IntegrationsConfig.Telemetry));
            if (telemetryElement is not null)
            {
                config.Telemetry = new TelemetryConfig();

                var enabledAttr = telemetryElement.Attribute(nameof(TelemetryConfig.Enabled))?.Value;
                if (!string.IsNullOrWhiteSpace(enabledAttr) && bool.TryParse(enabledAttr, out var telemetryEnabled))
                {
                    config.Telemetry.Enabled = telemetryEnabled;
                }
            }

            // Parse Segment
            var segmentElement = integrationsElement.Element(nameof(IntegrationsConfig.Segment));
            if (segmentElement is not null)
            {
                config.Segment = new SegmentConfig
                {
                    Key = segmentElement.Attribute(nameof(SegmentConfig.Key))?.Value
                };
            }

            return config;
        }

        /// <summary>
        /// Parses the Styling element from the MintlifyTemplate XML.
        /// </summary>
        /// <param name="stylingElement">The styling XML element.</param>
        /// <returns>A StylingConfig instance.</returns>
        internal StylingConfig ParseStylingConfig(XElement stylingElement)
        {
            var config = new StylingConfig();

            // Parse Codeblocks
            var codeBlocksElement = stylingElement.Element(nameof(StylingConfig.Codeblocks));
            if (codeBlocksElement is not null && !string.IsNullOrWhiteSpace(codeBlocksElement.Value))
            {
                config.Codeblocks = codeBlocksElement.Value.Trim();
            }

            // Parse Eyebrows
            var eyebrowsElement = stylingElement.Element(nameof(StylingConfig.Eyebrows));
            if (eyebrowsElement is not null && !string.IsNullOrWhiteSpace(eyebrowsElement.Value))
            {
                config.Eyebrows = eyebrowsElement.Value.Trim();
            }

            // Parse Latex
            var latexElement = stylingElement.Element(nameof(StylingConfig.Latex));
            if (latexElement is not null && !string.IsNullOrWhiteSpace(latexElement.Value))
            {
                if (bool.TryParse(latexElement.Value.Trim(), out var latex))
                {
                    config.Latex = latex;
                }
            }

            return config;
        }

        /// <summary>
        /// Parses the Appearance element from the MintlifyTemplate XML.
        /// </summary>
        /// <param name="appearanceElement">The appearance XML element.</param>
        /// <returns>An AppearanceConfig instance.</returns>
        internal AppearanceConfig ParseAppearanceConfig(XElement appearanceElement)
        {
            var config = new AppearanceConfig();

            // Parse Default
            var defaultElement = appearanceElement.Element(nameof(AppearanceConfig.Default));
            if (defaultElement is not null && !string.IsNullOrWhiteSpace(defaultElement.Value))
            {
                config.Default = defaultElement.Value.Trim();
            }

            // Parse Strict
            var strictElement = appearanceElement.Element(nameof(AppearanceConfig.Strict));
            if (strictElement is not null && !string.IsNullOrWhiteSpace(strictElement.Value))
            {
                if (bool.TryParse(strictElement.Value.Trim(), out var strict))
                {
                    config.Strict = strict;
                }
            }

            return config;
        }

        /// <summary>
        /// Parses the Interaction element from the MintlifyTemplate XML.
        /// </summary>
        /// <param name="interactionElement">The interaction XML element.</param>
        /// <returns>An InteractionConfig instance.</returns>
        internal InteractionConfig ParseInteractionConfig(XElement interactionElement)
        {
            var config = new InteractionConfig();

            // Parse Drilldown
            var drilldownElement = interactionElement.Element(nameof(InteractionConfig.Drilldown));
            if (drilldownElement is not null && !string.IsNullOrWhiteSpace(drilldownElement.Value))
            {
                if (bool.TryParse(drilldownElement.Value.Trim(), out var drilldown))
                {
                    config.Drilldown = drilldown;
                }
            }

            return config;
        }

        /// <summary>
        /// Parses the Api element from the MintlifyTemplate XML.
        /// </summary>
        /// <param name="apiElement">The API XML element.</param>
        /// <returns>An ApiConfig instance.</returns>
        internal ApiConfig ParseApiConfig(XElement apiElement)
        {
            var config = new ApiConfig();

            // Parse Url
            var urlElement = apiElement.Element(nameof(ApiConfig.Url));
            if (urlElement is not null && !string.IsNullOrWhiteSpace(urlElement.Value))
            {
                config.Url = urlElement.Value.Trim();
            }

            // Parse Proxy
            var proxyElement = apiElement.Element(nameof(ApiConfig.Proxy));
            if (proxyElement is not null && !string.IsNullOrWhiteSpace(proxyElement.Value))
            {
                if (bool.TryParse(proxyElement.Value.Trim(), out var proxy))
                {
                    config.Proxy = proxy;
                }
            }

            // Parse OpenApi
            var openApiElement = apiElement.Element(nameof(ApiConfig.OpenApi));
            if (openApiElement is not null && !string.IsNullOrWhiteSpace(openApiElement.Value))
            {
                config.OpenApi = new ApiSpecConfig { Source = openApiElement.Value.Trim() };
            }

            // Parse AsyncApi
            var asyncApiElement = apiElement.Element(nameof(ApiConfig.AsyncApi));
            if (asyncApiElement is not null && !string.IsNullOrWhiteSpace(asyncApiElement.Value))
            {
                config.AsyncApi = new ApiSpecConfig { Source = asyncApiElement.Value.Trim() };
            }

            // Parse Playground
            var playgroundElement = apiElement.Element(nameof(ApiConfig.Playground));
            if (playgroundElement is not null)
            {
                config.Playground = new ApiPlaygroundConfig();

                var displayElement = playgroundElement.Element(nameof(ApiPlaygroundConfig.Display));
                if (displayElement is not null && !string.IsNullOrWhiteSpace(displayElement.Value))
                {
                    config.Playground.Display = displayElement.Value.Trim();
                }

                var playgroundProxyElement = playgroundElement.Element(nameof(ApiPlaygroundConfig.Proxy));
                if (playgroundProxyElement is not null && !string.IsNullOrWhiteSpace(playgroundProxyElement.Value))
                {
                    if (bool.TryParse(playgroundProxyElement.Value.Trim(), out var playgroundProxy))
                    {
                        config.Playground.Proxy = playgroundProxy;
                    }
                }
            }

            // Parse Params
            var paramsElement = apiElement.Element(nameof(ApiConfig.Params));
            if (paramsElement is not null)
            {
                config.Params = new ApiParamsConfig();

                var expandedElement = paramsElement.Element(nameof(ApiParamsConfig.Expanded));
                if (expandedElement is not null && !string.IsNullOrWhiteSpace(expandedElement.Value))
                {
                    if (bool.TryParse(expandedElement.Value.Trim(), out var expanded))
                    {
                        config.Params.Expanded = expanded;
                    }
                }
            }

            // Parse Examples
            var examplesElement = apiElement.Element(nameof(ApiConfig.Examples));
            if (examplesElement is not null)
            {
                config.Examples = new ApiExamplesConfig();

                var defaultsElement = examplesElement.Element(nameof(ApiExamplesConfig.Defaults));
                if (defaultsElement is not null && !string.IsNullOrWhiteSpace(defaultsElement.Value))
                {
                    config.Examples.Defaults = defaultsElement.Value.Trim();
                }

                var prefillElement = examplesElement.Element(nameof(ApiExamplesConfig.Prefill));
                if (prefillElement is not null && !string.IsNullOrWhiteSpace(prefillElement.Value))
                {
                    if (bool.TryParse(prefillElement.Value.Trim(), out var prefill))
                    {
                        config.Examples.Prefill = prefill;
                    }
                }

                var autogenerateElement = examplesElement.Element(nameof(ApiExamplesConfig.Autogenerate));
                if (autogenerateElement is not null && !string.IsNullOrWhiteSpace(autogenerateElement.Value))
                {
                    if (bool.TryParse(autogenerateElement.Value.Trim(), out var autogenerate))
                    {
                        config.Examples.Autogenerate = autogenerate;
                    }
                }

                var languagesElement = examplesElement.Element(nameof(ApiExamplesConfig.Languages));
                if (languagesElement is not null && !string.IsNullOrWhiteSpace(languagesElement.Value))
                {
                    config.Examples.Languages = languagesElement.Value
                        .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                }
            }

            // Parse Mdx
            var mdxElement = apiElement.Element(nameof(ApiConfig.Mdx));
            if (mdxElement is not null)
            {
                config.Mdx = new MdxConfig();

                var serverElement = mdxElement.Element(nameof(MdxConfig.Server));
                if (serverElement is not null && !string.IsNullOrWhiteSpace(serverElement.Value))
                {
                    config.Mdx.Server = new ServerConfig { Url = serverElement.Value.Trim() };
                }

                var authElement = mdxElement.Element(nameof(MdxConfig.Auth));
                if (authElement is not null)
                {
                    config.Mdx.Auth = new MdxAuthConfig();

                    var methodElement = authElement.Element(nameof(MdxAuthConfig.Method));
                    if (methodElement is not null && !string.IsNullOrWhiteSpace(methodElement.Value))
                    {
                        config.Mdx.Auth.Method = methodElement.Value.Trim();
                    }

                    var nameElement = authElement.Element(nameof(MdxAuthConfig.Name));
                    if (nameElement is not null && !string.IsNullOrWhiteSpace(nameElement.Value))
                    {
                        config.Mdx.Auth.Name = nameElement.Value.Trim();
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Parses the Contextual element from the MintlifyTemplate XML.
        /// </summary>
        /// <param name="contextualElement">The contextual XML element.</param>
        /// <returns>A ContextualConfig instance.</returns>
        internal ContextualConfig ParseContextualConfig(XElement contextualElement)
        {
            var config = new ContextualConfig();

            // Parse Options
            var optionsElement = contextualElement.Element(nameof(ContextualConfig.Options));
            if (optionsElement is not null && !string.IsNullOrWhiteSpace(optionsElement.Value))
            {
                config.Options = optionsElement.Value
                    .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(o => o.Trim())
                    .Where(o => !string.IsNullOrWhiteSpace(o))
                    .ToList();
            }

            // Parse Display
            var displayElement = contextualElement.Element(nameof(ContextualConfig.Display));
            if (displayElement is not null && !string.IsNullOrWhiteSpace(displayElement.Value))
            {
                config.Display = displayElement.Value.Trim();
            }

            return config;
        }

        /// <summary>
        /// Parses the Fonts element from the MintlifyTemplate XML.
        /// </summary>
        /// <param name="fontsElement">The fonts XML element.</param>
        /// <returns>A FontsConfig instance.</returns>
        internal FontsConfig ParseFontsConfig(XElement fontsElement)
        {
            var config = new FontsConfig();

            // Parse top-level font properties
            var familyElement = fontsElement.Element(nameof(FontsConfig.Family));
            if (familyElement is not null && !string.IsNullOrWhiteSpace(familyElement.Value))
            {
                config.Family = familyElement.Value.Trim();
            }

            var formatElement = fontsElement.Element(nameof(FontsConfig.Format));
            if (formatElement is not null && !string.IsNullOrWhiteSpace(formatElement.Value))
            {
                config.Format = formatElement.Value.Trim();
            }

            var sourceElement = fontsElement.Element(nameof(FontsConfig.Source));
            if (sourceElement is not null && !string.IsNullOrWhiteSpace(sourceElement.Value))
            {
                config.Source = sourceElement.Value.Trim();
            }

            var weightElement = fontsElement.Element(nameof(FontsConfig.Weight));
            if (weightElement is not null && !string.IsNullOrWhiteSpace(weightElement.Value))
            {
                if (int.TryParse(weightElement.Value.Trim(), out var weight))
                {
                    config.Weight = weight;
                }
            }

            // Parse Heading
            var headingElement = fontsElement.Element(nameof(FontsConfig.Heading));
            if (headingElement is not null)
            {
                config.Heading = ParseFontConfig(headingElement);
            }

            // Parse Body
            var bodyElement = fontsElement.Element(nameof(FontsConfig.Body));
            if (bodyElement is not null)
            {
                config.Body = ParseFontConfig(bodyElement);
            }

            return config;
        }

        /// <summary>
        /// Parses a FontConfig element from the MintlifyTemplate XML.
        /// </summary>
        /// <param name="fontElement">The font XML element.</param>
        /// <returns>A FontConfig instance.</returns>
        internal FontConfig ParseFontConfig(XElement fontElement)
        {
            var config = new FontConfig();

            var familyElement = fontElement.Element(nameof(FontConfig.Family));
            if (familyElement is not null && !string.IsNullOrWhiteSpace(familyElement.Value))
            {
                config.Family = familyElement.Value.Trim();
            }

            var weightElement = fontElement.Element(nameof(FontConfig.Weight));
            if (weightElement is not null && !string.IsNullOrWhiteSpace(weightElement.Value))
            {
                if (int.TryParse(weightElement.Value.Trim(), out var weight))
                {
                    config.Weight = weight;
                }
            }

            var sourceElement = fontElement.Element(nameof(FontConfig.Source));
            if (sourceElement is not null && !string.IsNullOrWhiteSpace(sourceElement.Value))
            {
                config.Source = sourceElement.Value.Trim();
            }

            var formatElement = fontElement.Element(nameof(FontConfig.Format));
            if (formatElement is not null && !string.IsNullOrWhiteSpace(formatElement.Value))
            {
                config.Format = formatElement.Value.Trim();
            }

            return config;
        }

        /// <summary>
        /// Parses the Thumbnails element from the MintlifyTemplate XML.
        /// </summary>
        /// <param name="thumbnailsElement">The thumbnails XML element.</param>
        /// <returns>A ThumbnailsConfig instance.</returns>
        internal ThumbnailsConfig ParseThumbnailsConfig(XElement thumbnailsElement)
        {
            var config = new ThumbnailsConfig();

            var appearanceElement = thumbnailsElement.Element(nameof(ThumbnailsConfig.Appearance));
            if (appearanceElement is not null && !string.IsNullOrWhiteSpace(appearanceElement.Value))
            {
                config.Appearance = appearanceElement.Value.Trim();
            }

            var backgroundElement = thumbnailsElement.Element(nameof(ThumbnailsConfig.Background));
            if (backgroundElement is not null && !string.IsNullOrWhiteSpace(backgroundElement.Value))
            {
                config.Background = backgroundElement.Value.Trim();
            }

            var fontsElement = thumbnailsElement.Element(nameof(ThumbnailsConfig.Fonts));
            if (fontsElement is not null)
            {
                config.Fonts = new ThumbnailFontsConfig();

                var familyElement = fontsElement.Element(nameof(ThumbnailFontsConfig.Family));
                if (familyElement is not null && !string.IsNullOrWhiteSpace(familyElement.Value))
                {
                    config.Fonts.Family = familyElement.Value.Trim();
                }
            }

            return config;
        }

        /// <summary>
        /// Parses the Metadata element from the MintlifyTemplate XML.
        /// </summary>
        /// <param name="metadataElement">The metadata XML element.</param>
        /// <returns>A MetadataConfig instance.</returns>
        internal MetadataConfig ParseMetadataConfig(XElement metadataElement)
        {
            var config = new MetadataConfig();

            var timestampElement = metadataElement.Element(nameof(MetadataConfig.Timestamp));
            if (timestampElement is not null && !string.IsNullOrWhiteSpace(timestampElement.Value))
            {
                if (bool.TryParse(timestampElement.Value.Trim(), out var timestamp))
                {
                    config.Timestamp = timestamp;
                }
            }

            return config;
        }

        /// <summary>
        /// Parses the Errors element from the MintlifyTemplate XML.
        /// </summary>
        /// <param name="errorsElement">The errors XML element.</param>
        /// <returns>An ErrorsConfig instance.</returns>
        internal ErrorsConfig ParseErrorsConfig(XElement errorsElement)
        {
            var config = new ErrorsConfig();

            // The XML element name is "NotFound" but serializes as "404"
            var notFoundElement = errorsElement.Element(nameof(ErrorsConfig.NotFound));
            if (notFoundElement is not null)
            {
                config.NotFound = new Error404Config();

                var redirectElement = notFoundElement.Element(nameof(Error404Config.Redirect));
                if (redirectElement is not null && !string.IsNullOrWhiteSpace(redirectElement.Value))
                {
                    if (bool.TryParse(redirectElement.Value.Trim(), out var redirect))
                    {
                        config.NotFound.Redirect = redirect;
                    }
                }

                var titleElement = notFoundElement.Element(nameof(Error404Config.Title));
                if (titleElement is not null && !string.IsNullOrWhiteSpace(titleElement.Value))
                {
                    config.NotFound.Title = titleElement.Value.Trim();
                }

                var descriptionElement = notFoundElement.Element(nameof(Error404Config.Description));
                if (descriptionElement is not null && !string.IsNullOrWhiteSpace(descriptionElement.Value))
                {
                    config.NotFound.Description = descriptionElement.Value.Trim();
                }
            }

            return config;
        }

        #endregion
#endif

    }

}