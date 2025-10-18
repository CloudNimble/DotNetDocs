using System;
using System.Linq;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    [TestClass]
    public class DocParameterTests : DotNetDocsTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            Action act = () => new DocParameter(null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("symbol");
        }

        [TestMethod]
        public void Constructor_WithRequiredParameter_SetsProperties()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            type.Should().NotBeNull("SampleClass should exist in test assembly");
            
            var methodMember = type!.Members
                .FirstOrDefault(m => m.Symbol.Name == "DoSomething");
            
            methodMember.Should().NotBeNull("DoSomething method should exist in SampleClass");
            
            var methodSymbol = methodMember!.Symbol as IMethodSymbol;
            methodSymbol.Should().NotBeNull("Symbol should be an IMethodSymbol");
            
            var inputParam = methodSymbol!.Parameters.FirstOrDefault(p => p.Name == "input");
            
            inputParam.Should().NotBeNull("'input' parameter should exist in DoSomething method");

            var docParam = new DocParameter(inputParam!);

            docParam.Symbol.Should().Be(inputParam);
            docParam.IsOptional.Should().BeFalse();
            docParam.HasDefaultValue.Should().BeFalse();
            docParam.DefaultValue.Should().BeNull();
            docParam.IsParams.Should().BeFalse();
            docParam.Usage.Should().BeNull();
        }

        [TestMethod]
        public void Constructor_WithOptionalParameter_SetsDefaultValue()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            type.Should().NotBeNull("SampleClass should exist in test assembly");
            
            var methodMember = type!.Members
                .FirstOrDefault(m => m.Symbol.Name == "MethodWithOptional");
            
            methodMember.Should().NotBeNull("MethodWithOptional method should exist in SampleClass");
            
            var methodSymbol = methodMember!.Symbol as IMethodSymbol;
            methodSymbol.Should().NotBeNull("Symbol should be an IMethodSymbol");
            
            var optionalParam = methodSymbol!.Parameters.FirstOrDefault(p => p.Name == "optional");
            
            optionalParam.Should().NotBeNull("'optional' parameter should exist in MethodWithOptional method");

            var docParam = new DocParameter(optionalParam!);

            docParam.Symbol.Should().Be(optionalParam);
            docParam.IsOptional.Should().BeTrue();
            docParam.HasDefaultValue.Should().BeTrue();
            docParam.DefaultValue.Should().Be("42");
            docParam.IsParams.Should().BeFalse();
        }

        [TestMethod]
        public void Constructor_WithParamsParameter_SetsIsParams()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            type.Should().NotBeNull("SampleClass should exist in test assembly");
            
            var methodMember = type!.Members
                .FirstOrDefault(m => m.Symbol.Name == "MethodWithParams");
            
            methodMember.Should().NotBeNull("MethodWithParams method should exist in SampleClass");
            
            var methodSymbol = methodMember!.Symbol as IMethodSymbol;
            methodSymbol.Should().NotBeNull("Symbol should be an IMethodSymbol");
            
            methodSymbol!.Parameters.Should().NotBeEmpty("MethodWithParams should have parameters");
            
            var itemsParam = methodSymbol.Parameters.First();

            var docParam = new DocParameter(itemsParam);

            docParam.Symbol.Should().Be(itemsParam);
            docParam.IsParams.Should().BeTrue();
            docParam.IsOptional.Should().BeFalse();
            docParam.HasDefaultValue.Should().BeFalse();
        }

        #endregion

    }

}