using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for the ProjectContext class.
    /// </summary>
    [TestClass]
    public class ProjectContextTests : DotNetDocsTestBase
    {

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithDefaultParameters_InitializesWithPublicAccess()
        {
            // Act
            var context = new ProjectContext();

            // Assert
            context.IncludedMembers.Should().ContainSingle();
            context.IncludedMembers.Should().Contain(Accessibility.Public);
            context.References.Should().BeEmpty();
            context.ShowPlaceholders.Should().BeTrue();
            context.DocumentationRootPath.Should().Be("docs");
            context.ConceptualPath.Should().Be("conceptual");
            context.FileNamingOptions.Should().NotBeNull();
            context.FileNamingOptions.NamespaceMode.Should().Be(NamespaceMode.File);
            context.FileNamingOptions.NamespaceSeparator.Should().Be('-');
        }

        [TestMethod]
        public void Constructor_WithReferences_AddsReferencesToList()
        {
            // Arrange
            var ref1 = "ref1.dll";
            var ref2 = "ref2.dll";
            var ref3 = "ref3.dll";

            // Act
            var context = new ProjectContext(null, ref1, ref2, ref3);

            // Assert
            context.References.Should().HaveCount(3);
            context.References.Should().Contain(ref1);
            context.References.Should().Contain(ref2);
            context.References.Should().Contain(ref3);
        }

        [TestMethod]
        public void Constructor_WithCustomIncludedMembers_UsesProvidedAccessibilities()
        {
            // Arrange
            var includedMembers = new List<Accessibility> 
            { 
                Accessibility.Public, 
                Accessibility.Internal, 
                Accessibility.Protected 
            };

            // Act
            var context = new ProjectContext(includedMembers);

            // Assert
            context.IncludedMembers.Should().HaveCount(3);
            context.IncludedMembers.Should().Contain(Accessibility.Public);
            context.IncludedMembers.Should().Contain(Accessibility.Internal);
            context.IncludedMembers.Should().Contain(Accessibility.Protected);
        }

        #endregion

        #region GetNamespaceFolderPath Tests

        [TestMethod]
        public void GetNamespaceFolderPath_WithFileModeAndSimpleNamespace_ReturnsEmptyString()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File)
            };

            // Act
            var result = context.GetNamespaceFolderPath("System");

            // Assert
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetNamespaceFolderPath_WithFileModeAndNestedNamespace_ReturnsEmptyString()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File)
            };

            // Act
            var result = context.GetNamespaceFolderPath("System.Collections.Generic");

            // Assert
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetNamespaceFolderPath_WithFolderModeAndSimpleNamespace_ReturnsSingleFolder()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };

            // Act
            var result = context.GetNamespaceFolderPath("System");

            // Assert
            result.Should().Be("System");
        }

        [TestMethod]
        public void GetNamespaceFolderPath_WithFolderModeAndNestedNamespace_ReturnsNestedFolderPath()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };

            // Act
            var result = context.GetNamespaceFolderPath("System.Collections.Generic");

            // Assert
            result.Should().Be(Path.Combine("System", "Collections", "Generic"));
        }

        [TestMethod]
        public void GetNamespaceFolderPath_WithFolderModeAndGlobalKeyword_ReturnsGlobal()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };

            // Act
            var result = context.GetNamespaceFolderPath("global");

            // Assert
            result.Should().Be("global");
        }

        [TestMethod]
        public void GetNamespaceFolderPath_WithFolderModeAndEmptyString_ReturnsGlobal()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };

            // Act
            var result = context.GetNamespaceFolderPath("");

            // Assert
            result.Should().Be("global");
        }

        [TestMethod]
        public void GetNamespaceFolderPath_WithFolderModeAndWhitespace_ReturnsGlobal()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };

            // Act
            var result = context.GetNamespaceFolderPath("   ");

            // Assert
            result.Should().Be("global");
        }

        [TestMethod]
        public void GetNamespaceFolderPath_WithFolderModeAndDeepNesting_ReturnsCorrectPath()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };

            // Act
            var result = context.GetNamespaceFolderPath("CloudNimble.DotNetDocs.Core.Configuration.Internal");

            // Assert
            result.Should().Be(Path.Combine("CloudNimble", "DotNetDocs", "Core", "Configuration", "Internal"));
        }

        #endregion

        #region GetSafeNamespaceName Tests

        [TestMethod]
        public void GetSafeNamespaceName_WithGlobalNamespace_ReturnsGlobal()
        {
            // Arrange
            var context = new ProjectContext();
            var compilation = CSharpCompilation.Create("Test");
            var globalNamespace = compilation.GlobalNamespace;

            // Act
            var result = context.GetSafeNamespaceName(globalNamespace);

            // Assert
            result.Should().Be("global");
        }

        [TestMethod]
        public void GetSafeNamespaceName_WithNormalNamespace_ReturnsNamespaceName()
        {
            // Arrange
            var context = new ProjectContext();
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
                namespace TestNamespace
                {
                    public class TestClass { }
                }");
            var compilation = CSharpCompilation.Create("Test", new[] { syntaxTree });
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var namespaceDeclaration = syntaxTree.GetRoot().DescendantNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .First();
            var namespaceSymbol = semanticModel.GetDeclaredSymbol(namespaceDeclaration);

            // Act
            var result = context.GetSafeNamespaceName(namespaceSymbol!);

            // Assert
            result.Should().Be("TestNamespace");
        }

        [TestMethod]
        public void GetSafeNamespaceName_WithNestedNamespace_ReturnsFullName()
        {
            // Arrange
            var context = new ProjectContext();
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
                namespace CloudNimble.DotNetDocs.Core
                {
                    public class TestClass { }
                }");
            var compilation = CSharpCompilation.Create("Test", new[] { syntaxTree });
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var namespaceDeclaration = syntaxTree.GetRoot().DescendantNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .First();
            var namespaceSymbol = semanticModel.GetDeclaredSymbol(namespaceDeclaration);

            // Act
            var result = context.GetSafeNamespaceName(namespaceSymbol!);

            // Assert
            result.Should().Be("CloudNimble.DotNetDocs.Core");
        }

        #endregion

        #region Property Initialization Tests

        [TestMethod]
        public void Properties_CanBeInitialized_WithCustomValues()
        {
            // Arrange & Act
            var context = new ProjectContext
            {
                ConceptualPath = "/custom/conceptual",
                DocumentationRootPath = "/custom/output",
                ShowPlaceholders = false,
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '_')
            };

            // Assert
            context.ConceptualPath.Should().Be("/custom/conceptual");
            context.DocumentationRootPath.Should().Be("/custom/output");
            context.ShowPlaceholders.Should().BeFalse();
            context.FileNamingOptions.NamespaceMode.Should().Be(NamespaceMode.Folder);
            context.FileNamingOptions.NamespaceSeparator.Should().Be('_');
        }

        [TestMethod]
        public void References_CanBeModified_AfterInitialization()
        {
            // Arrange
            var context = new ProjectContext();

            // Act
            context.References.Add("new-ref.dll");
            context.References.Add("another-ref.dll");

            // Assert
            context.References.Should().HaveCount(2);
            context.References.Should().Contain("new-ref.dll");
            context.References.Should().Contain("another-ref.dll");
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void GetNamespaceFolderPath_WithDifferentSeparators_IgnoresSeparatorInFolderMode()
        {
            // Arrange
            var separators = new[] { '-', '_', '.', '~' };
            var namespaceName = "CloudNimble.DotNetDocs.Core";
            var expectedPath = Path.Combine("CloudNimble", "DotNetDocs", "Core");

            foreach (var separator in separators)
            {
                var context = new ProjectContext
                {
                    FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, separator)
                };

                // Act
                var result = context.GetNamespaceFolderPath(namespaceName);

                // Assert
                result.Should().Be(expectedPath, 
                    $"Separator '{separator}' should be ignored in Folder mode");
            }
        }

        #endregion

        #region GetTypeFilePath Tests

        [TestMethod]
        public void GetTypeFilePath_WithFolderMode_CreatesCorrectFolderStructure()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };

            // Act
            var result = context.GetTypeFilePath("System.Text.Json.JsonSerializer", "md");

            // Assert
            result.Should().Be(Path.Combine("System", "Text", "Json", "JsonSerializer.md"));
        }

        [TestMethod]
        public void GetTypeFilePath_WithFileMode_CreatesFlatFileWithSeparator()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '_')
            };

            // Act
            var result = context.GetTypeFilePath("System.Text.Json.JsonSerializer", "md");

            // Assert
            result.Should().Be("System_Text_Json_JsonSerializer.md");
        }

        [TestMethod]
        public void GetTypeFilePath_WithFileModeHyphenSeparator_UsesHyphen()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-')
            };

            // Act
            var result = context.GetTypeFilePath("CloudNimble.DotNetDocs.Core.ProjectContext", "yaml");

            // Assert
            result.Should().Be("CloudNimble-DotNetDocs-Core-ProjectContext.yaml");
        }

        [TestMethod]
        public void GetTypeFilePath_WithGlobalNamespaceType_HandlesCorrectly()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };

            // Act
            var result = context.GetTypeFilePath("GlobalClass", "md");

            // Assert
            result.Should().Be(Path.Combine("global", "GlobalClass.md"));
        }

        [TestMethod]
        public void GetTypeFilePath_WithNullTypeName_ThrowsArgumentException()
        {
            // Arrange
            var context = new ProjectContext();

            // Act
            Action act = () => context.GetTypeFilePath(null!, "md");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Type name cannot be null or whitespace.*");
        }

        [TestMethod]
        public void GetTypeFilePath_WithEmptyTypeName_ThrowsArgumentException()
        {
            // Arrange
            var context = new ProjectContext();

            // Act
            Action act = () => context.GetTypeFilePath("", "md");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Type name cannot be null or whitespace.*");
        }

        [TestMethod]
        public void GetTypeFilePath_WithWhitespaceTypeName_ThrowsArgumentException()
        {
            // Arrange
            var context = new ProjectContext();

            // Act
            Action act = () => context.GetTypeFilePath("   ", "md");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Type name cannot be null or whitespace.*");
        }

        #endregion

        #region EnsureOutputDirectoryStructure Tests

        [TestMethod]
        public void EnsureOutputDirectoryStructure_WithFileMode_OnlyCreatesOutputDirectory()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File)
            };
            var tempPath = Path.Combine(Path.GetTempPath(), $"EnsureTest_{Guid.NewGuid()}");
            
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
                namespace TestNamespace.SubNamespace
                {
                    public class TestClass { }
                }");
            var compilation = CSharpCompilation.Create("TestAssembly", new[] { syntaxTree });
            var assemblySymbol = compilation.Assembly;
            var namespaceSymbol = compilation.GlobalNamespace.GetNamespaceMembers()
                .First(n => n.Name == "TestNamespace");
            
            var assembly = new DocAssembly(assemblySymbol);
            assembly.Namespaces.Add(new DocNamespace(namespaceSymbol));

            try
            {
                // Act
                context.EnsureOutputDirectoryStructure(assembly, tempPath);

                // Assert
                Directory.Exists(tempPath).Should().BeTrue();
                // Should not create namespace folders in File mode
                Directory.Exists(Path.Combine(tempPath, "TestNamespace")).Should().BeFalse();
                Directory.Exists(Path.Combine(tempPath, "TestNamespace", "SubNamespace")).Should().BeFalse();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [TestMethod]
        public void EnsureOutputDirectoryStructure_WithFolderMode_CreatesNamespaceFolders()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };
            var tempPath = Path.Combine(Path.GetTempPath(), $"EnsureTest_{Guid.NewGuid()}");
            
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
                namespace TestNamespace.SubNamespace
                {
                    public class TestClass { }
                }
                namespace AnotherNamespace
                {
                    public class AnotherClass { }
                }");
            var compilation = CSharpCompilation.Create("TestAssembly", new[] { syntaxTree });
            var assemblySymbol = compilation.Assembly;
            
            var assembly = new DocAssembly(assemblySymbol);
            
            // Add TestNamespace.SubNamespace
            var testNamespace = compilation.GlobalNamespace.GetNamespaceMembers()
                .First(n => n.Name == "TestNamespace");
            var subNamespace = testNamespace.GetNamespaceMembers()
                .First(n => n.Name == "SubNamespace");
            assembly.Namespaces.Add(new DocNamespace(subNamespace));
            
            // Add AnotherNamespace
            var anotherNamespace = compilation.GlobalNamespace.GetNamespaceMembers()
                .First(n => n.Name == "AnotherNamespace");
            assembly.Namespaces.Add(new DocNamespace(anotherNamespace));

            try
            {
                // Act
                context.EnsureOutputDirectoryStructure(assembly, tempPath);

                // Assert
                Directory.Exists(tempPath).Should().BeTrue();
                Directory.Exists(Path.Combine(tempPath, "TestNamespace")).Should().BeTrue();
                Directory.Exists(Path.Combine(tempPath, "TestNamespace", "SubNamespace")).Should().BeTrue();
                Directory.Exists(Path.Combine(tempPath, "AnotherNamespace")).Should().BeTrue();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [TestMethod]
        public void EnsureOutputDirectoryStructure_WithGlobalNamespace_CreatesGlobalFolder()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };
            var tempPath = Path.Combine(Path.GetTempPath(), $"EnsureTest_{Guid.NewGuid()}");
            
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
                public class GlobalClass { }");
            var compilation = CSharpCompilation.Create("TestAssembly", new[] { syntaxTree });
            var assemblySymbol = compilation.Assembly;
            
            var assembly = new DocAssembly(assemblySymbol);
            assembly.Namespaces.Add(new DocNamespace(compilation.GlobalNamespace));

            try
            {
                // Act
                context.EnsureOutputDirectoryStructure(assembly, tempPath);

                // Assert
                Directory.Exists(tempPath).Should().BeTrue();
                Directory.Exists(Path.Combine(tempPath, "global")).Should().BeTrue();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [TestMethod]
        public void EnsureOutputDirectoryStructure_WithNullAssembly_ThrowsArgumentNullException()
        {
            // Arrange
            var context = new ProjectContext();

            // Act
            Action act = () => context.EnsureOutputDirectoryStructure(null!, "output");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void EnsureOutputDirectoryStructure_WithNullOutputPath_ThrowsArgumentException()
        {
            // Arrange
            var context = new ProjectContext();
            var compilation = CSharpCompilation.Create("Test");
            var assembly = new DocAssembly(compilation.Assembly);

            // Act
            Action act = () => context.EnsureOutputDirectoryStructure(assembly, null!);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void EnsureOutputDirectoryStructure_WithEmptyOutputPath_ThrowsArgumentException()
        {
            // Arrange
            var context = new ProjectContext();
            var compilation = CSharpCompilation.Create("Test");
            var assembly = new DocAssembly(compilation.Assembly);

            // Act
            Action act = () => context.EnsureOutputDirectoryStructure(assembly, "");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        #endregion

    }

}