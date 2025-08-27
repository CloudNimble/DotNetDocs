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
    public class DocMemberTests : TestBase
    {

        #region Public Methods

        [TestMethod]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            Action act = () => new DocMember(null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("symbol");
        }

        [TestMethod]
        public async Task Constructor_WithMethodSymbol_SetsProperties()
        {
            var compilation = await CreateCompilationAsync();
            var typeSymbol = compilation.GetTypeByMetadataName("CloudNimble.DotNetDocs.Tests.Shared.SampleClass");
            var methodSymbol = typeSymbol!.GetMembers("DoSomething").FirstOrDefault() as IMethodSymbol;
            methodSymbol.Should().NotBeNull();

            var docMember = new DocMember(methodSymbol!);

            docMember.Symbol.Should().Be(methodSymbol);
            docMember.Kind.Should().Be(SymbolKind.Method);
            docMember.Parameters.Should().BeEmpty();
            docMember.ReturnType.Should().BeNull();
            docMember.Usage.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Constructor_WithPropertySymbol_SetsProperties()
        {
            var compilation = await CreateCompilationAsync();
            var typeSymbol = compilation.GetTypeByMetadataName("CloudNimble.DotNetDocs.Tests.Shared.SampleClass");
            var propertySymbol = typeSymbol!.GetMembers("Name").FirstOrDefault() as IPropertySymbol;
            propertySymbol.Should().NotBeNull();

            var docMember = new DocMember(propertySymbol!);

            docMember.Symbol.Should().Be(propertySymbol);
            docMember.Kind.Should().Be(SymbolKind.Property);
            docMember.Parameters.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Parameters_CanBeAdded()
        {
            var compilation = await CreateCompilationAsync();
            var typeSymbol = compilation.GetTypeByMetadataName("CloudNimble.DotNetDocs.Tests.Shared.SampleClass");
            var methodSymbol = typeSymbol!.GetMembers("DoSomething").FirstOrDefault() as IMethodSymbol;

            var docMember = new DocMember(methodSymbol!);
            var paramSymbol = methodSymbol!.Parameters.First();
            var docParam = new DocParameter(paramSymbol);

            docMember.Parameters.Add(docParam);

            docMember.Parameters.Should().HaveCount(1);
            docMember.Parameters.Should().Contain(docParam);
        }

        #endregion

    }

}