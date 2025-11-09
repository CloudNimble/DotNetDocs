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

                    // Populate DocumentationReferences from resolved items
                    if (ResolvedDocumentationReferences is not null && ResolvedDocumentationReferences.Length > 0)
                    {
                        Log.LogMessage(MessageImportance.Normal, $"   📚 Processing {ResolvedDocumentationReferences.Length} documentation reference(s)");

                        foreach (var item in ResolvedDocumentationReferences)
                        {
                            var name = item.GetMetadata("Name");
                            var reference = new DocumentationReference
                            {
                                ProjectPath = item.GetMetadata("ProjectPath"),
                                DocumentationRoot = item.GetMetadata("DocumentationRoot"),
                                DestinationPath = item.GetMetadata("DestinationPath"),
                                IntegrationType = item.GetMetadata("IntegrationType"),
                                DocumentationType = item.GetMetadata("DocumentationType"),
                                NavigationFilePath = item.GetMetadata("NavigationFilePath"),
                                Name = !string.IsNullOrWhiteSpace(name) ? name : null
                            };

                            context.DocumentationReferences.Add(reference);
                            var displayName = !string.IsNullOrWhiteSpace(reference.Name) ? $"{reference.Name} ({Path.GetFileName(reference.ProjectPath)})" : Path.GetFileName(reference.ProjectPath);
                            Log.LogMessage(MessageImportance.Normal, $"      Added reference: {displayName} → {reference.DestinationPath}");
                        }
                    }

                    // NamespaceFileMode will be set via the NamespaceMode property
                    // This is handled internally by the Core library
                });

                // Add the appropriate renderer based on documentation type
                switch (DocumentationType.ToLowerInvariant())
                {
                    case "mintlify":
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
                    case "markdown":
                        services.AddMarkdownRenderer();
                        break;
                    case "json":
                        services.AddJsonRenderer();
                        break;
                    case "yaml":
                        services.AddYamlRenderer();
                        break;
                    default:
                        Log.LogError($"Unknown documentation type: {DocumentationType}");
                        return false;
                }

                var serviceProvider = services.BuildServiceProvider();
                var manager = serviceProvider.GetRequiredService<DocumentationManager>();

                // Collect all valid assembly paths
                var assemblyPairs = new List<(string assemblyPath, string xmlPath)>();
                foreach (var assembly in Assemblies)
                {
                    var assemblyPath = assembly.ItemSpec;
                    var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

                    if (!File.Exists(assemblyPath))
                    {
                        Log.LogWarning($"Assembly not found: {assemblyPath}");
                        continue;
                    }

                    Log.LogMessage(MessageImportance.High, $"   📖 Found {Path.GetFileName(assemblyPath)}");
                    assemblyPairs.Add((assemblyPath, xmlPath));
                }

                if (assemblyPairs.Count == 0)
                {
                    Log.LogWarning("No valid assemblies found to process");
                    return true;
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
                
                if (DocumentationType.Equals("Mintlify", StringComparison.OrdinalIgnoreCase))
                {
                    var mdFiles = Directory.GetFiles(OutputPath, "*.mdx", SearchOption.AllDirectories);
                    generatedFiles.AddRange(mdFiles);
                }
                else if (DocumentationType.Equals("Markdown", StringComparison.OrdinalIgnoreCase))
                {
                    var mdFiles = Directory.GetFiles(OutputPath, "*.md", SearchOption.AllDirectories);
                    generatedFiles.AddRange(mdFiles);
                }
                else if (DocumentationType.Equals("Json", StringComparison.OrdinalIgnoreCase))
                {
                    var jsonFiles = Directory.GetFiles(OutputPath, "*.json", SearchOption.AllDirectories);
                    generatedFiles.AddRange(jsonFiles);
                }
                else if (DocumentationType.Equals("Yaml", StringComparison.OrdinalIgnoreCase))
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
                var nameValue = root.Element("Name")?.Value;
                if (string.IsNullOrWhiteSpace(nameValue) && !string.IsNullOrWhiteSpace(SolutionName))
                {
                    nameValue = SolutionName;
                }

                var config = new DocsJsonConfig
                {
                    Name = nameValue ?? "API Documentation",
                    Description = root.Element("Description")?.Value,
                    Theme = root.Element("Theme")?.Value ?? "mint"
                };

                // Parse DocsNavigationConfig from Navigation element attributes and legacy elements
                var docsNavConfig = ParseDocsNavigationConfig(root);

                // Parse Colors
                var colorsElement = root.Element("Colors");
                if (colorsElement is not null)
                {
                    config.Colors = new ColorsConfig
                    {
                        Primary = colorsElement.Element("Primary")?.Value ?? "#000000",
                        Light = colorsElement.Element("Light")?.Value,
                        Dark = colorsElement.Element("Dark")?.Value
                    };
                }

                // Parse Logo
                var logoElement = root.Element("Logo");
                if (logoElement is not null)
                {
                    config.Logo = new LogoConfig
                    {
                        Light = logoElement.Element("Light")?.Value,
                        Dark = logoElement.Element("Dark")?.Value,
                        Href = logoElement.Element("Href")?.Value
                    };
                }

                // Parse Favicon
                var faviconElement = root.Element("Favicon");
                if (faviconElement is not null)
                {
                    // Check if it has Light/Dark sub-elements
                    var lightFavicon = faviconElement.Element("Light")?.Value;
                    var darkFavicon = faviconElement.Element("Dark")?.Value;

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
                var navigationElement = root.Element("Navigation");
                if (navigationElement is not null)
                {
                    config.Navigation = ParseNavigationConfig(navigationElement);
                }

                // Parse Styling
                var stylingElement = root.Element("Styling");
                if (stylingElement is not null)
                {
                    config.Styling = ParseStylingConfig(stylingElement);
                }

                // Parse Appearance
                var appearanceElement = root.Element("Appearance");
                if (appearanceElement is not null)
                {
                    config.Appearance = ParseAppearanceConfig(appearanceElement);
                }

                // Parse Integrations
                var integrationsElement = root.Element("Integrations");
                if (integrationsElement is not null)
                {
                    config.Integrations = ParseIntegrationsConfig(integrationsElement);
                }

                // Parse Interaction
                var interactionElement = root.Element("Interaction");
                if (interactionElement is not null)
                {
                    config.Interaction = ParseInteractionConfig(interactionElement);
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
            var navigationElement = root.Element("Navigation");

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
            var navConfig = new NavigationConfig
            {
                Pages = []
            };

            var pagesElement = navigationElement.Element("Pages");
            if (pagesElement is not null)
            {
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

                // Also parse any direct page references
                var directPages = pagesElement.Elements("Page");
                foreach (var pageElement in directPages)
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
            var pagesElement = groupElement.Element("Pages");
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
        /// Parses the Integrations element from the MintlifyTemplate XML using attributes.
        /// </summary>
        /// <param name="integrationsElement">The integrations XML element.</param>
        /// <returns>An IntegrationsConfig instance.</returns>
        internal IntegrationsConfig ParseIntegrationsConfig(XElement integrationsElement)
        {
            var config = new IntegrationsConfig();

            // Parse Amplitude
            var amplitudeElement = integrationsElement.Element("Amplitude");
            if (amplitudeElement is not null)
            {
                config.Amplitude = new AmplitudeConfig
                {
                    ApiKey = amplitudeElement.Attribute("ApiKey")?.Value
                };
            }

            // Parse Clearbit
            var clearbitElement = integrationsElement.Element("Clearbit");
            if (clearbitElement is not null)
            {
                config.Clearbit = new ClearbitConfig
                {
                    PublicApiKey = clearbitElement.Attribute("PublicApiKey")?.Value
                };
            }

            // Parse Fathom
            var fathomElement = integrationsElement.Element("Fathom");
            if (fathomElement is not null)
            {
                config.Fathom = new FathomConfig
                {
                    SiteId = fathomElement.Attribute("SiteId")?.Value
                };
            }

            // Parse Google Analytics 4
            var ga4Element = integrationsElement.Element("GoogleAnalytics4");
            if (ga4Element is not null)
            {
                config.GoogleAnalytics4 = new GoogleAnalytics4Config
                {
                    MeasurementId = ga4Element.Attribute("MeasurementId")?.Value
                };
            }

            // Parse Google Tag Manager
            var gtmElement = integrationsElement.Element("Gtm");
            if (gtmElement is not null)
            {
                config.Gtm = new GtmConfig
                {
                    TagId = gtmElement.Attribute("TagId")?.Value
                };
            }

            // Parse Heap
            var heapElement = integrationsElement.Element("Heap");
            if (heapElement is not null)
            {
                config.Heap = new HeapConfig
                {
                    AppId = heapElement.Attribute("AppId")?.Value
                };
            }

            // Parse Hightouch
            var hightouchElement = integrationsElement.Element("Hightouch");
            if (hightouchElement is not null)
            {
                config.Hightouch = new HightouchConfig
                {
                    ApiKey = hightouchElement.Attribute("ApiKey")?.Value
                };
            }

            // Parse Hotjar
            var hotjarElement = integrationsElement.Element("Hotjar");
            if (hotjarElement is not null)
            {
                config.Hotjar = new HotjarConfig
                {
                    Hjid = hotjarElement.Attribute("Hjid")?.Value,
                    Hjsv = hotjarElement.Attribute("Hjsv")?.Value
                };
            }

            // Parse LogRocket
            var logrocketElement = integrationsElement.Element("LogRocket");
            if (logrocketElement is not null)
            {
                config.LogRocket = new LogRocketConfig
                {
                    AppId = logrocketElement.Attribute("AppId")?.Value
                };
            }

            // Parse Mixpanel
            var mixpanelElement = integrationsElement.Element("Mixpanel");
            if (mixpanelElement is not null)
            {
                config.Mixpanel = new MixpanelConfig
                {
                    ProjectToken = mixpanelElement.Attribute("ProjectToken")?.Value
                };
            }

            // Parse Pirsch
            var pirschElement = integrationsElement.Element("Pirsch");
            if (pirschElement is not null)
            {
                config.Pirsch = new PirschConfig
                {
                    Id = pirschElement.Attribute("Id")?.Value
                };
            }

            // Parse Plausible
            var plausibleElement = integrationsElement.Element("Plausible");
            if (plausibleElement is not null)
            {
                config.Plausible = new PlausibleConfig
                {
                    Domain = plausibleElement.Attribute("Domain")?.Value,
                    Server = plausibleElement.Attribute("Server")?.Value
                };
            }

            // Parse PostHog
            var posthogElement = integrationsElement.Element("PostHog");
            if (posthogElement is not null)
            {
                config.PostHog = new PostHogConfig
                {
                    ApiKey = posthogElement.Attribute("ApiKey")?.Value,
                    ApiHost = posthogElement.Attribute("ApiHost")?.Value
                };
            }

            // Parse Segment
            var segmentElement = integrationsElement.Element("Segment");
            if (segmentElement is not null)
            {
                config.Segment = new SegmentConfig
                {
                    Key = segmentElement.Attribute("Key")?.Value
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

            // Parse CodeBlocks
            var codeBlocksElement = stylingElement.Element("CodeBlocks");
            if (codeBlocksElement is not null && !string.IsNullOrWhiteSpace(codeBlocksElement.Value))
            {
                config.Codeblocks = codeBlocksElement.Value.Trim();
            }

            // Parse Eyebrows
            var eyebrowsElement = stylingElement.Element("Eyebrows");
            if (eyebrowsElement is not null && !string.IsNullOrWhiteSpace(eyebrowsElement.Value))
            {
                config.Eyebrows = eyebrowsElement.Value.Trim();
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
            var defaultElement = appearanceElement.Element("Default");
            if (defaultElement is not null && !string.IsNullOrWhiteSpace(defaultElement.Value))
            {
                config.Default = defaultElement.Value.Trim();
            }

            // Parse Strict
            var strictElement = appearanceElement.Element("Strict");
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
            var drilldownElement = interactionElement.Element("Drilldown");
            if (drilldownElement is not null && !string.IsNullOrWhiteSpace(drilldownElement.Value))
            {
                if (bool.TryParse(drilldownElement.Value.Trim(), out var drilldown))
                {
                    config.Drilldown = drilldown;
                }
            }

            return config;
        }

        #endregion
#endif

    }

}