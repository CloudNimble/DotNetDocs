using System;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    [TestClass]
    public class DocParameterTests : TestBase
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
        public async Task Constructor_WithRequiredParameter_SetsProperties()
        {
            var compilation = await CreateCompilationAsync();
            var typeSymbol = compilation.GetTypeByMetadataName("CloudNimble.DotNetDocs.Tests.Shared.SampleClass");
            var methodSymbol = typeSymbol!.GetMembers("DoSomething").FirstOrDefault() as IMethodSymbol;
            var inputParam = methodSymbol!.Parameters.First(p => p.Name == "input");

            var docParam = new DocParameter(inputParam);

            docParam.Symbol.Should().Be(inputParam);
            docParam.IsOptional.Should().BeFalse();
            docParam.HasDefaultValue.Should().BeFalse();
            docParam.DefaultValue.Should().BeNull();
            docParam.IsParams.Should().BeFalse();
            docParam.Usage.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Constructor_WithOptionalParameter_SetsDefaultValue()
        {
            var compilation = await CreateCompilationAsync();
            var typeSymbol = compilation.GetTypeByMetadataName("CloudNimble.DotNetDocs.Tests.Shared.SampleClass");
            var methodSymbol = typeSymbol!.GetMembers("MethodWithOptional").FirstOrDefault() as IMethodSymbol;
            var optionalParam = methodSymbol!.Parameters.First(p => p.Name == "optional");

            var docParam = new DocParameter(optionalParam);

            docParam.Symbol.Should().Be(optionalParam);
            docParam.IsOptional.Should().BeTrue();
            docParam.HasDefaultValue.Should().BeTrue();
            docParam.DefaultValue.Should().Be("42");
            docParam.IsParams.Should().BeFalse();
        }

        [TestMethod]
        public async Task Constructor_WithParamsParameter_SetsIsParams()
        {
            var compilation = await CreateCompilationAsync();
            var typeSymbol = compilation.GetTypeByMetadataName("CloudNimble.DotNetDocs.Tests.Shared.SampleClass");
            var methodSymbol = typeSymbol!.GetMembers("MethodWithParams").FirstOrDefault() as IMethodSymbol;
            var itemsParam = methodSymbol!.Parameters.First();

            var docParam = new DocParameter(itemsParam);

            docParam.Symbol.Should().Be(itemsParam);
            docParam.IsParams.Should().BeTrue();
            docParam.IsOptional.Should().BeFalse();
            docParam.HasDefaultValue.Should().BeFalse();
        }

        #endregion

    }

}