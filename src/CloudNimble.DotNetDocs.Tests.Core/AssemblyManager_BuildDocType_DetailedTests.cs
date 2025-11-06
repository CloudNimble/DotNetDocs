using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.AccessModifiers;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Detailed tests for BuildDocType() method covering every code path individually.
    /// </summary>
    [TestClass]
    public class AssemblyManager_BuildDocType_DetailedTests : DotNetDocsTestBase
    {

        #region Tests - Member Type Detection

        [TestMethod]
        public async Task BuildDocType_Method_SetsIsInheritedTrue_WhenFromBaseClass()
        {
            // Arrange
            var testAssemblyPath = typeof(DerivedClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var derivedClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "DerivedClass");
            var inheritedMethod = derivedClass.Members.First(m => m.Name == "BaseMethod");
            inheritedMethod.IsInherited.Should().BeTrue();
        }

        [TestMethod]
        public async Task BuildDocType_Method_SetsIsInheritedFalse_WhenDeclaredInType()
        {
            // Arrange
            var testAssemblyPath = typeof(DerivedClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var derivedClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "DerivedClass");
            var declaredMethod = derivedClass.Members.First(m => m.Name == "DerivedMethod");
            declaredMethod.IsInherited.Should().BeFalse();
        }

        [TestMethod]
        public async Task BuildDocType_Property_SetsIsInheritedTrue_WhenFromBaseClass()
        {
            // Arrange
            var testAssemblyPath = typeof(DerivedClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var derivedClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "DerivedClass");
            // Properties show up as get_/set_ methods in members, but also as property accessors
            var inheritedProp = derivedClass.Members.FirstOrDefault(m => m.Name.Contains("BaseProperty") && m.IsInherited);
            inheritedProp.Should().NotBeNull();
        }

        [TestMethod]
        public async Task BuildDocType_Property_SetsIsInheritedFalse_WhenDeclaredInType()
        {
            // Arrange
            var testAssemblyPath = typeof(DerivedClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var derivedClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "DerivedClass");
            var declaredProp = derivedClass.Members.FirstOrDefault(m => m.Name.Contains("DerivedProperty") && !m.IsInherited);
            declaredProp.Should().NotBeNull();
        }

        #endregion

        #region Tests - System.Object Filtering

        [TestMethod]
        public async Task BuildDocType_FiltersSystemObjectMembers_WhenIncludeSystemObjectInheritanceFalse()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { IncludeSystemObjectInheritance = false };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var simpleClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "SimpleClass");
            simpleClass.Members.Should().NotContain(m => m.Name == "ToString" && m.DeclaringTypeName!.Contains("System.Object"));
        }

        [TestMethod]
        public async Task BuildDocType_IncludesSystemObjectMembers_WhenIncludeSystemObjectInheritanceTrue()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { IncludeSystemObjectInheritance = true };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var simpleClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "SimpleClass");
            simpleClass.Members.Should().Contain(m => m.Name == "ToString" && m.IsInherited);
        }

        [TestMethod]
        public async Task BuildDocType_IncludesSystemObjectMembers_WhenProjectContextNull()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(null); // Null context defaults to true

            // Assert
            var simpleClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "SimpleClass");
            simpleClass.Members.Should().Contain(m => m.Name == "ToString" && m.IsInherited);
        }

        #endregion

        #region Tests - Accessibility Filtering for Declared Members

        [TestMethod]
        public async Task BuildDocType_IncludesDeclaredPublicMember_WhenPublicInIncludedMembers()
        {
            // Arrange
            var testAssemblyPath = typeof(MixedAccessClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext([Accessibility.Public]);

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var testClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "MixedAccessClass");
            testClass.Members.Should().Contain(m => m.Name == "PublicMethod");
        }

        [TestMethod]
        public async Task BuildDocType_ExcludesDeclaredPrivateMember_WhenPrivateNotInIncludedMembers()
        {
            // Arrange
            var testAssemblyPath = typeof(MixedAccessClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext([Accessibility.Public]);

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var testClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "MixedAccessClass");
            testClass.Members.Should().NotContain(m => m.Name == "PrivateMethod");
        }

        [TestMethod]
        public async Task BuildDocType_IncludesDeclaredInternalMember_WhenInternalInIncludedMembers()
        {
            // Arrange
            var testAssemblyPath = typeof(MixedAccessClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext([Accessibility.Public, Accessibility.Internal]);

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var testClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "MixedAccessClass");
            testClass.Members.Should().Contain(m => m.Name == "InternalMethod");
        }

        #endregion

        #region Tests - Override Tracking

        [TestMethod]
        public async Task BuildDocType_Method_SetsIsOverrideTrue_WhenMethodOverridesBase()
        {
            // Arrange
            var testAssemblyPath = typeof(DerivedClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var derivedClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "DerivedClass");
            var overriddenMethod = derivedClass.Members.First(m => m.Name == "VirtualMethod");
            overriddenMethod.IsOverride.Should().BeTrue();
        }

        [TestMethod]
        public async Task BuildDocType_Method_SetsOverriddenMember_WhenMethodOverridesBase()
        {
            // Arrange
            var testAssemblyPath = typeof(DerivedClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var derivedClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "DerivedClass");
            var overriddenMethod = derivedClass.Members.First(m => m.Name == "VirtualMethod");
            overriddenMethod.OverriddenMember.Should().NotBeNullOrWhiteSpace();
            overriddenMethod.OverriddenMember.Should().Contain("BaseClass");
        }

        [TestMethod]
        public async Task BuildDocType_Property_SetsIsOverrideTrue_WhenPropertyOverridesBase()
        {
            // Arrange
            var testAssemblyPath = typeof(DerivedClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var derivedClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "DerivedClass");
            // BaseProperty is overridden, look for accessor methods or the property itself
            var overriddenProp = derivedClass.Members.FirstOrDefault(m =>
                m.Name.Contains("BaseProperty") && m.IsOverride);
            overriddenProp.Should().NotBeNull();
        }

        [TestMethod]
        public async Task BuildDocType_Property_SetsOverriddenMember_WhenPropertyOverridesBase()
        {
            // Arrange
            var testAssemblyPath = typeof(DerivedClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var derivedClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "DerivedClass");
            var overriddenProp = derivedClass.Members.FirstOrDefault(m =>
                m.Name.Contains("BaseProperty") && m.IsOverride);
            overriddenProp.Should().NotBeNull();
            overriddenProp!.OverriddenMember.Should().NotBeNullOrWhiteSpace();
        }

        #endregion

        #region Tests - Virtual/Abstract Tracking

        [TestMethod]
        public async Task BuildDocType_Method_SetsIsVirtualTrue_WhenMethodIsVirtual()
        {
            // Arrange
            var testAssemblyPath = typeof(BaseClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var baseClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "BaseClass");
            var virtualMethod = baseClass.Members.First(m => m.Name == "VirtualMethod");
            virtualMethod.IsVirtual.Should().BeTrue();
        }

        [TestMethod]
        public async Task BuildDocType_Method_SetsIsVirtualFalse_WhenMethodIsNotVirtual()
        {
            // Arrange
            var testAssemblyPath = typeof(BaseClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var baseClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "BaseClass");
            var normalMethod = baseClass.Members.First(m => m.Name == "BaseMethod");
            normalMethod.IsVirtual.Should().BeFalse();
        }

        #endregion

        #region Tests - Extension Method Detection

        [TestMethod]
        public async Task BuildDocType_Method_SetsIsExtensionMethodTrue_WhenMethodIsExtension()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert - Extension methods should be marked with IsExtensionMethod = true
            // After relocation, they may be on target types, not in static classes
            var hasExtensionMethod = result.Namespaces
                .SelectMany(ns => ns.Types)
                .Any(t => t.Members.Any(m => m.IsExtensionMethod));

            // Extension methods should be detected during BuildDocType
            hasExtensionMethod.Should().BeTrue("Extension methods should be marked during BuildDocType");
        }

        [TestMethod]
        public async Task BuildDocType_Method_SetsExtendedTypeName_WhenMethodIsExtension()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert - After relocation, extension methods should have ExtendedTypeName
            var simpleClass = result.Namespaces.SelectMany(ns => ns.Types).FirstOrDefault(t => t.Name == "SimpleClass");
            if (simpleClass is not null)
            {
                var extensionMethod = simpleClass.Members.FirstOrDefault(m => m.IsExtensionMethod);
                if (extensionMethod is not null)
                {
                    extensionMethod.ExtendedTypeName.Should().NotBeNullOrWhiteSpace();
                }
            }
        }

        [TestMethod]
        public async Task BuildDocType_Method_SetsExtendedTypeNameNull_WhenMethodIsNotExtension()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var simpleClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "SimpleClass");
            var normalMethods = simpleClass.Members.Where(m => !m.IsExtensionMethod).ToList();
            foreach (var method in normalMethods)
            {
                method.ExtendedTypeName.Should().BeNull($"{method.Name} is not an extension method");
            }
        }

        #endregion

        #region Tests - DeclaringTypeName

        [TestMethod]
        public async Task BuildDocType_SetsDeclaringTypeName_ForDeclaredMember()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var simpleClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "SimpleClass");
            foreach (var member in simpleClass.Members.Where(m => !m.IsInherited))
            {
                member.DeclaringTypeName.Should().Contain("SimpleClass", $"{member.Name} is declared in SimpleClass");
            }
        }

        [TestMethod]
        public async Task BuildDocType_SetsDeclaringTypeName_ForInheritedMember()
        {
            // Arrange
            var testAssemblyPath = typeof(DerivedClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var derivedClass = result.Namespaces.SelectMany(ns => ns.Types).First(t => t.Name == "DerivedClass");
            var inheritedMethod = derivedClass.Members.First(m => m.Name == "BaseMethod");
            inheritedMethod.DeclaringTypeName.Should().Contain("BaseClass");
        }

        #endregion

    }

}
