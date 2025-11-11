using System;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for the DocumentationReference class.
    /// </summary>
    [TestClass]
    public class DocumentationReferenceTests : DotNetDocsTestBase
    {

        #region Constructor Tests

        [TestMethod]
        public void Constructor_Default_InitializesWithEmptyStringsAndTabsIntegrationType()
        {
            // Act
            var reference = new DocumentationReference();

            // Assert
            reference.DestinationPath.Should().BeEmpty();
            reference.DocumentationRoot.Should().BeEmpty();
            reference.DocumentationType.Should().Be(SupportedDocumentationType.Generic);
            reference.IntegrationType.Should().Be("Tabs");
            reference.NavigationFilePath.Should().BeEmpty();
            reference.ProjectPath.Should().BeEmpty();
        }

        [TestMethod]
        public void Constructor_WithValidProjectPath_SetsProjectPath()
        {
            // Arrange
            var projectPath = @"C:\repos\service\Service.docsproj";

            // Act
            var reference = new DocumentationReference(projectPath);

            // Assert
            reference.ProjectPath.Should().Be(projectPath);
            reference.DestinationPath.Should().BeEmpty();
            reference.DocumentationRoot.Should().BeEmpty();
            reference.DocumentationType.Should().Be(SupportedDocumentationType.Generic);
            reference.IntegrationType.Should().Be("Tabs");
            reference.NavigationFilePath.Should().BeEmpty();
        }

        [TestMethod]
        public void Constructor_WithRelativeProjectPath_SetsProjectPath()
        {
            // Arrange
            var projectPath = @"..\ServiceA\ServiceA.docsproj";

            // Act
            var reference = new DocumentationReference(projectPath);

            // Assert
            reference.ProjectPath.Should().Be(projectPath);
        }

        [TestMethod]
        public void Constructor_WithUnixStylePath_SetsProjectPath()
        {
            // Arrange
            var projectPath = "/repos/service/Service.docsproj";

            // Act
            var reference = new DocumentationReference(projectPath);

            // Assert
            reference.ProjectPath.Should().Be(projectPath);
        }

        [TestMethod]
        public void Constructor_WithNullProjectPath_ThrowsArgumentException()
        {
            // Act
            Action act = () => new DocumentationReference(null!);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Project path cannot be null or whitespace.*")
                .And.ParamName.Should().Be("projectPath");
        }

        [TestMethod]
        public void Constructor_WithEmptyProjectPath_ThrowsArgumentException()
        {
            // Act
            Action act = () => new DocumentationReference(string.Empty);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Project path cannot be null or whitespace.*")
                .And.ParamName.Should().Be("projectPath");
        }

        [TestMethod]
        public void Constructor_WithWhitespaceProjectPath_ThrowsArgumentException()
        {
            // Act
            Action act = () => new DocumentationReference("   ");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Project path cannot be null or whitespace.*")
                .And.ParamName.Should().Be("projectPath");
        }

        #endregion

        #region Property Tests

        [TestMethod]
        public void DestinationPath_CanBeSetAndRetrieved()
        {
            // Arrange
            var reference = new DocumentationReference();
            var destinationPath = "services/auth";

            // Act
            reference.DestinationPath = destinationPath;

            // Assert
            reference.DestinationPath.Should().Be(destinationPath);
        }

        [TestMethod]
        public void DestinationPath_CanBeSetToEmpty()
        {
            // Arrange
            var reference = new DocumentationReference
            {
                DestinationPath = "services/auth"
            };

            // Act
            reference.DestinationPath = string.Empty;

            // Assert
            reference.DestinationPath.Should().BeEmpty();
        }

        [TestMethod]
        public void DocumentationRoot_CanBeSetAndRetrieved()
        {
            // Arrange
            var reference = new DocumentationReference();
            var docRoot = @"C:\repos\auth-service\docs";

            // Act
            reference.DocumentationRoot = docRoot;

            // Assert
            reference.DocumentationRoot.Should().Be(docRoot);
        }

        [TestMethod]
        public void DocumentationRoot_CanBeSetToEmpty()
        {
            // Arrange
            var reference = new DocumentationReference
            {
                DocumentationRoot = @"C:\repos\auth-service\docs"
            };

            // Act
            reference.DocumentationRoot = string.Empty;

            // Assert
            reference.DocumentationRoot.Should().BeEmpty();
        }

        [TestMethod]
        public void DocumentationType_CanBeSetAndRetrieved()
        {
            // Arrange
            var reference = new DocumentationReference();
            var docType = SupportedDocumentationType.Mintlify;

            // Act
            reference.DocumentationType = docType;

            // Assert
            reference.DocumentationType.Should().Be(docType);
        }

        [TestMethod]
        public void DocumentationType_CanBeSetToDifferentValues()
        {
            // Arrange
            var reference = new DocumentationReference();
            var docTypes = new[] { SupportedDocumentationType.Mintlify, SupportedDocumentationType.DocFX, SupportedDocumentationType.MkDocs, SupportedDocumentationType.Jekyll, SupportedDocumentationType.Hugo, SupportedDocumentationType.Generic };

            foreach (var docType in docTypes)
            {
                // Act
                reference.DocumentationType = docType;

                // Assert
                reference.DocumentationType.Should().Be(docType);
            }
        }

        [TestMethod]
        public void IntegrationType_DefaultsToTabs()
        {
            // Arrange & Act
            var reference = new DocumentationReference();

            // Assert
            reference.IntegrationType.Should().Be("Tabs");
        }

        [TestMethod]
        public void IntegrationType_CanBeSetToProducts()
        {
            // Arrange
            var reference = new DocumentationReference();

            // Act
            reference.IntegrationType = "Products";

            // Assert
            reference.IntegrationType.Should().Be("Products");
        }

        [TestMethod]
        public void IntegrationType_CanBeSetToTabs()
        {
            // Arrange
            var reference = new DocumentationReference
            {
                IntegrationType = "Products"
            };

            // Act
            reference.IntegrationType = "Tabs";

            // Assert
            reference.IntegrationType.Should().Be("Tabs");
        }

        [TestMethod]
        public void IntegrationType_CanBeSetToCustomValue()
        {
            // Arrange
            var reference = new DocumentationReference();

            // Act
            reference.IntegrationType = "CustomIntegration";

            // Assert
            reference.IntegrationType.Should().Be("CustomIntegration");
        }

        [TestMethod]
        public void NavigationFilePath_CanBeSetAndRetrieved()
        {
            // Arrange
            var reference = new DocumentationReference();
            var navPath = @"C:\repos\auth-service\docs\docs.json";

            // Act
            reference.NavigationFilePath = navPath;

            // Assert
            reference.NavigationFilePath.Should().Be(navPath);
        }

        [TestMethod]
        public void NavigationFilePath_CanBeSetToEmpty()
        {
            // Arrange
            var reference = new DocumentationReference
            {
                NavigationFilePath = @"C:\repos\auth-service\docs\docs.json"
            };

            // Act
            reference.NavigationFilePath = string.Empty;

            // Assert
            reference.NavigationFilePath.Should().BeEmpty();
        }

        [TestMethod]
        public void ProjectPath_CanBeSetAndRetrieved()
        {
            // Arrange
            var reference = new DocumentationReference();
            var projectPath = @"C:\repos\service\Service.docsproj";

            // Act
            reference.ProjectPath = projectPath;

            // Assert
            reference.ProjectPath.Should().Be(projectPath);
        }

        [TestMethod]
        public void ProjectPath_CanBeSetToEmpty()
        {
            // Arrange
            var reference = new DocumentationReference(@"C:\repos\service\Service.docsproj");

            // Act
            reference.ProjectPath = string.Empty;

            // Assert
            reference.ProjectPath.Should().BeEmpty();
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void DocumentationReference_WithAllPropertiesSet_MaintainsAllValues()
        {
            // Arrange
            var projectPath = @"C:\repos\auth-service\AuthService.docsproj";
            var docRoot = @"C:\repos\auth-service\docs";
            var destinationPath = "services/auth";
            var docType = SupportedDocumentationType.Mintlify;
            var integrationType = "Products";
            var navPath = @"C:\repos\auth-service\docs\docs.json";

            // Act
            var reference = new DocumentationReference(projectPath)
            {
                DocumentationRoot = docRoot,
                DestinationPath = destinationPath,
                DocumentationType = docType,
                IntegrationType = integrationType,
                NavigationFilePath = navPath
            };

            // Assert
            reference.ProjectPath.Should().Be(projectPath);
            reference.DocumentationRoot.Should().Be(docRoot);
            reference.DestinationPath.Should().Be(destinationPath);
            reference.DocumentationType.Should().Be(docType);
            reference.IntegrationType.Should().Be(integrationType);
            reference.NavigationFilePath.Should().Be(navPath);
        }

        [TestMethod]
        public void DocumentationReference_WithObjectInitializer_SetsAllProperties()
        {
            // Arrange & Act
            var reference = new DocumentationReference
            {
                ProjectPath = @"..\ServiceA\ServiceA.docsproj",
                DocumentationRoot = @"..\ServiceA\docs",
                DestinationPath = "services/service-a",
                DocumentationType = SupportedDocumentationType.Mintlify,
                IntegrationType = "Tabs",
                NavigationFilePath = @"..\ServiceA\docs\docs.json"
            };

            // Assert
            reference.ProjectPath.Should().Be(@"..\ServiceA\ServiceA.docsproj");
            reference.DocumentationRoot.Should().Be(@"..\ServiceA\docs");
            reference.DestinationPath.Should().Be("services/service-a");
            reference.DocumentationType.Should().Be(SupportedDocumentationType.Mintlify);
            reference.IntegrationType.Should().Be("Tabs");
            reference.NavigationFilePath.Should().Be(@"..\ServiceA\docs\docs.json");
        }

        [TestMethod]
        public void DocumentationReference_CanModifyPropertiesAfterConstruction()
        {
            // Arrange
            var reference = new DocumentationReference(@"C:\original\path.docsproj");

            // Act
            reference.ProjectPath = @"C:\new\path.docsproj";
            reference.DocumentationRoot = @"C:\new\docs";
            reference.DestinationPath = "new/destination";
            reference.DocumentationType = SupportedDocumentationType.DocFX;
            reference.IntegrationType = "Products";
            reference.NavigationFilePath = @"C:\new\docs\toc.yml";

            // Assert
            reference.ProjectPath.Should().Be(@"C:\new\path.docsproj");
            reference.DocumentationRoot.Should().Be(@"C:\new\docs");
            reference.DestinationPath.Should().Be("new/destination");
            reference.DocumentationType.Should().Be(SupportedDocumentationType.DocFX);
            reference.IntegrationType.Should().Be("Products");
            reference.NavigationFilePath.Should().Be(@"C:\new\docs\toc.yml");
        }

        [TestMethod]
        public void DocumentationReference_WithMixedPathStyles_HandlesCorrectly()
        {
            // Arrange & Act
            var reference = new DocumentationReference
            {
                ProjectPath = "/repos/service/Service.docsproj",
                DocumentationRoot = @"C:\repos\service\docs",
                DestinationPath = "services/service",
                NavigationFilePath = "\\\\server\\share\\docs.json"
            };

            // Assert
            reference.ProjectPath.Should().Be("/repos/service/Service.docsproj");
            reference.DocumentationRoot.Should().Be(@"C:\repos\service\docs");
            reference.DestinationPath.Should().Be("services/service");
            reference.NavigationFilePath.Should().Be("\\\\server\\share\\docs.json");
        }

        [TestMethod]
        public void DocumentationReference_ForMicroservicesScenario_ConfiguresCorrectly()
        {
            // Arrange - Simulates a microservices documentation portal scenario
            var authReference = new DocumentationReference(@"..\services\AuthService\AuthService.docsproj")
            {
                DocumentationRoot = @"..\services\AuthService\docs",
                DestinationPath = "services/auth",
                DocumentationType = SupportedDocumentationType.Mintlify,
                IntegrationType = "Tabs",
                NavigationFilePath = @"..\services\AuthService\docs\docs.json"
            };

            var orderReference = new DocumentationReference(@"..\services\OrderService\OrderService.docsproj")
            {
                DocumentationRoot = @"..\services\OrderService\docs",
                DestinationPath = "services/orders",
                DocumentationType = SupportedDocumentationType.Mintlify,
                IntegrationType = "Tabs",
                NavigationFilePath = @"..\services\OrderService\docs\docs.json"
            };

            var paymentReference = new DocumentationReference(@"..\services\PaymentService\PaymentService.docsproj")
            {
                DocumentationRoot = @"..\services\PaymentService\docs",
                DestinationPath = "services/payments",
                DocumentationType = SupportedDocumentationType.Mintlify,
                IntegrationType = "Tabs",
                NavigationFilePath = @"..\services\PaymentService\docs\docs.json"
            };

            // Assert
            authReference.DestinationPath.Should().Be("services/auth");
            orderReference.DestinationPath.Should().Be("services/orders");
            paymentReference.DestinationPath.Should().Be("services/payments");

            authReference.IntegrationType.Should().Be("Tabs");
            orderReference.IntegrationType.Should().Be("Tabs");
            paymentReference.IntegrationType.Should().Be("Tabs");

            authReference.DocumentationType.Should().Be(SupportedDocumentationType.Mintlify);
            orderReference.DocumentationType.Should().Be(SupportedDocumentationType.Mintlify);
            paymentReference.DocumentationType.Should().Be(SupportedDocumentationType.Mintlify);
        }

        [TestMethod]
        public void DocumentationReference_ForProductPortalScenario_ConfiguresCorrectly()
        {
            // Arrange - Simulates an easyaf.dev style product portal scenario
            var coreReference = new DocumentationReference(@"..\..\EasyAF.Core\docs\EasyAF.Core.docsproj")
            {
                DocumentationRoot = @"..\..\EasyAF.Core\docs",
                DestinationPath = "core",
                DocumentationType = SupportedDocumentationType.Mintlify,
                IntegrationType = "Products",
                NavigationFilePath = @"..\..\EasyAF.Core\docs\docs.json"
            };

            var httpReference = new DocumentationReference(@"..\..\EasyAF.Http\docs\EasyAF.Http.docsproj")
            {
                DocumentationRoot = @"..\..\EasyAF.Http\docs",
                DestinationPath = "http",
                DocumentationType = SupportedDocumentationType.Mintlify,
                IntegrationType = "Products",
                NavigationFilePath = @"..\..\EasyAF.Http\docs\docs.json"
            };

            // Assert
            coreReference.IntegrationType.Should().Be("Products");
            httpReference.IntegrationType.Should().Be("Products");

            coreReference.DestinationPath.Should().Be("core");
            httpReference.DestinationPath.Should().Be("http");

            coreReference.DocumentationType.Should().Be(SupportedDocumentationType.Mintlify);
            httpReference.DocumentationType.Should().Be(SupportedDocumentationType.Mintlify);
        }

        [TestMethod]
        public void DocumentationReference_WithDifferentDocumentationTypes_SupportsMultipleFormats()
        {
            // Arrange - Different documentation system formats
            var mintlifyRef = new DocumentationReference
            {
                DocumentationType = SupportedDocumentationType.Mintlify,
                NavigationFilePath = "docs.json"
            };

            var docfxRef = new DocumentationReference
            {
                DocumentationType = SupportedDocumentationType.DocFX,
                NavigationFilePath = "toc.yml"
            };

            var mkdocsRef = new DocumentationReference
            {
                DocumentationType = SupportedDocumentationType.MkDocs,
                NavigationFilePath = "mkdocs.yml"
            };

            var jekyllRef = new DocumentationReference
            {
                DocumentationType = SupportedDocumentationType.Jekyll,
                NavigationFilePath = "_config.yml"
            };

            var hugoRef = new DocumentationReference
            {
                DocumentationType = SupportedDocumentationType.Hugo,
                NavigationFilePath = "hugo.toml"
            };

            // Assert
            mintlifyRef.DocumentationType.Should().Be(SupportedDocumentationType.Mintlify);
            docfxRef.DocumentationType.Should().Be(SupportedDocumentationType.DocFX);
            mkdocsRef.DocumentationType.Should().Be(SupportedDocumentationType.MkDocs);
            jekyllRef.DocumentationType.Should().Be(SupportedDocumentationType.Jekyll);
            hugoRef.DocumentationType.Should().Be(SupportedDocumentationType.Hugo);

            mintlifyRef.NavigationFilePath.Should().Be("docs.json");
            docfxRef.NavigationFilePath.Should().Be("toc.yml");
            mkdocsRef.NavigationFilePath.Should().Be("mkdocs.yml");
            jekyllRef.NavigationFilePath.Should().Be("_config.yml");
            hugoRef.NavigationFilePath.Should().Be("hugo.toml");
        }

        [TestMethod]
        public void DocumentationReference_WithGenericDocumentationType_WorksCorrectly()
        {
            var genericRef = new DocumentationReference
            {
                DocumentationType = SupportedDocumentationType.Generic,
                DocumentationRoot = @"C:
eposstom-docs",
                DestinationPath = "custom",
                NavigationFilePath = "custom-nav.json"
            };

            genericRef.DocumentationType.Should().Be(SupportedDocumentationType.Generic);
            genericRef.DocumentationRoot.Should().Be(@"C:
eposstom-docs");
            genericRef.DestinationPath.Should().Be("custom");
            genericRef.NavigationFilePath.Should().Be("custom-nav.json");
        }

        [TestMethod]
        public void DocumentationReference_CanSwitchFromKnownToGenericType()
        {
            var reference = new DocumentationReference
            {
                DocumentationType = SupportedDocumentationType.Mintlify
            };

            reference.DocumentationType = SupportedDocumentationType.Generic;

            reference.DocumentationType.Should().Be(SupportedDocumentationType.Generic);
        }

        [TestMethod]
        public void DocumentationReference_CanSwitchFromGenericToKnownType()
        {
            var reference = new DocumentationReference
            {
                DocumentationType = SupportedDocumentationType.Generic
            };

            reference.DocumentationType = SupportedDocumentationType.DocFX;

            reference.DocumentationType.Should().Be(SupportedDocumentationType.DocFX);
        }

        #endregion

    }

}
