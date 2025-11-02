using System;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for the <see cref="AssemblyManager"/> class focusing on inherited member functionality.
    /// </summary>
    [TestClass]
    public class AssemblyManager_InheritanceTests : DotNetDocsTestBase
    {

        #region Tests - Inherited Members

        [TestMethod]
        public async Task BuildDocType_IncludesInheritedMembers_FromBaseClass()
        {
            // Arrange
            var testAssemblyPath = typeof(DerivedClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext();

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var derivedClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "DerivedClass");

            derivedClass.Should().NotBeNull();

            // Should include inherited method from BaseClass
            var baseMethod = derivedClass!.Members.FirstOrDefault(m => m.Name == "BaseMethod");
            baseMethod.Should().NotBeNull("BaseMethod should be inherited from BaseClass");
            baseMethod!.IsInherited.Should().BeTrue();
            baseMethod.DeclaringTypeName.Should().Contain("BaseClass");

            // Should include declared method
            var derivedMethod = derivedClass.Members.FirstOrDefault(m => m.Name == "DerivedMethod");
            derivedMethod.Should().NotBeNull();
            derivedMethod!.IsInherited.Should().BeFalse();
            derivedMethod.DeclaringTypeName.Should().Contain("DerivedClass");
        }

        [TestMethod]
        public async Task BuildDocType_TracksOverriddenMembers()
        {
            // Arrange
            var testAssemblyPath = typeof(DerivedClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext();

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var derivedClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "DerivedClass");

            derivedClass.Should().NotBeNull();

            // Check overridden method
            var virtualMethod = derivedClass!.Members.FirstOrDefault(m => m.Name == "VirtualMethod");
            virtualMethod.Should().NotBeNull();
            virtualMethod!.IsOverride.Should().BeTrue("VirtualMethod is marked with override");
            virtualMethod.IsInherited.Should().BeFalse("Override is declared in DerivedClass");
            virtualMethod.OverriddenMember.Should().NotBeNullOrWhiteSpace();
            virtualMethod.OverriddenMember.Should().Contain("BaseClass");
        }

        [TestMethod]
        public async Task BuildDocType_TracksVirtualMembers()
        {
            // Arrange
            var testAssemblyPath = typeof(BaseClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext();

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var baseClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "BaseClass");

            baseClass.Should().NotBeNull();

            var virtualMethod = baseClass!.Members.FirstOrDefault(m => m.Name == "VirtualMethod");
            virtualMethod.Should().NotBeNull();
            virtualMethod!.IsVirtual.Should().BeTrue("VirtualMethod is marked as virtual");
            virtualMethod.IsOverride.Should().BeFalse();
        }

        [TestMethod]
        public async Task BuildDocType_WithSystemObjectInheritance_Enabled_IncludesObjectMembers()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext
            {
                IncludeSystemObjectInheritance = true
            };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var simpleClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "SimpleClass");

            simpleClass.Should().NotBeNull();

            // Should include System.Object members
            simpleClass!.Members.Should().Contain(m => m.Name == "ToString" && m.IsInherited);
            simpleClass.Members.Should().Contain(m => m.Name == "GetHashCode" && m.IsInherited);
            simpleClass.Members.Should().Contain(m => m.Name == "Equals" && m.IsInherited);
            simpleClass.Members.Should().Contain(m => m.Name == "GetType" && m.IsInherited);
        }

        [TestMethod]
        public async Task BuildDocType_WithSystemObjectInheritance_Disabled_ExcludesObjectMembers()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext
            {
                IncludeSystemObjectInheritance = false
            };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var simpleClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "SimpleClass");

            simpleClass.Should().NotBeNull();

            // Should NOT include System.Object members
            simpleClass!.Members.Should().NotContain(m => m.Name == "ToString" && m.DeclaringTypeName == "System.Object");
            simpleClass.Members.Should().NotContain(m => m.Name == "GetHashCode" && m.DeclaringTypeName == "System.Object");
            simpleClass.Members.Should().NotContain(m => m.Name == "Equals" && m.DeclaringTypeName == "System.Object");
            simpleClass.Members.Should().NotContain(m => m.Name == "GetType" && m.DeclaringTypeName == "System.Object");
        }

        [TestMethod]
        public async Task BuildDocType_IncludesInheritedMembers_FromInterfaces()
        {
            // Arrange
            var testAssemblyPath = typeof(TestImplementation).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext();

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var implementation = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "TestImplementation");

            implementation.Should().NotBeNull();

            // The TestMethod and TestValue from ITestInterface should be present but not marked as inherited
            // because they are implemented in the class itself
            var testMethod = implementation!.Members.FirstOrDefault(m => m.Name == "TestMethod");
            testMethod.Should().NotBeNull();
            // Interface members implemented directly are not marked as inherited
            testMethod!.IsInherited.Should().BeFalse("TestMethod is declared in TestImplementation");

            // Should have its own method
            var additionalMethod = implementation.Members.FirstOrDefault(m => m.Name == "AdditionalMethod");
            additionalMethod.Should().NotBeNull();
            additionalMethod!.IsInherited.Should().BeFalse();
        }

        [TestMethod]
        public async Task BuildDocType_SetsDeclaringTypeName_ForAllMembers()
        {
            // Arrange
            var testAssemblyPath = typeof(DerivedClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext();

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var derivedClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "DerivedClass");

            derivedClass.Should().NotBeNull();

            // All members should have DeclaringTypeName set
            foreach (var member in derivedClass!.Members)
            {
                member.DeclaringTypeName.Should().NotBeNullOrWhiteSpace($"{member.Name} should have DeclaringTypeName set");
            }

            // Inherited members should have BaseClass as declaring type
            var baseMethod = derivedClass.Members.FirstOrDefault(m => m.Name == "BaseMethod");
            baseMethod!.DeclaringTypeName.Should().Contain("BaseClass");

            // Declared members should have DerivedClass as declaring type
            var derivedMethod = derivedClass.Members.FirstOrDefault(m => m.Name == "DerivedMethod");
            derivedMethod!.DeclaringTypeName.Should().Contain("DerivedClass");
        }

        #endregion

    }

}
