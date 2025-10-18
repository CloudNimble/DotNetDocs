using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for the <see cref="DocTypeParameter"/> class.
    /// </summary>
    [TestClass]
    public class DocTypeParameterTests : DotNetDocsTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Constructor_CreatesInstanceWithNullProperties()
        {
            var typeParam = new DocTypeParameter();

            typeParam.Name.Should().BeNull();
            typeParam.Description.Should().BeNull();
        }

        [TestMethod]
        public void Properties_CanBeSetAndRetrieved()
        {
            var typeParam = new DocTypeParameter
            {
                Name = "T",
                Description = "The type of elements in the collection."
            };

            typeParam.Name.Should().Be("T");
            typeParam.Description.Should().Be("The type of elements in the collection.");
        }

        [TestMethod]
        public void Properties_CanBeSetToNull()
        {
            var typeParam = new DocTypeParameter
            {
                Name = "TKey",
                Description = "The type of the key"
            };

            typeParam.Name = null;
            typeParam.Description = null;

            typeParam.Name.Should().BeNull();
            typeParam.Description.Should().BeNull();
        }

        [TestMethod]
        public void Name_HandlesCommonTypeParameterNames()
        {
            var names = new[] { "T", "TKey", "TValue", "TResult", "TSource", "TEntity", "TService" };

            foreach (var name in names)
            {
                var typeParam = new DocTypeParameter { Name = name };
                typeParam.Name.Should().Be(name);
            }
        }

        [TestMethod]
        public void Name_HandlesConstrainedTypeParameterNames()
        {
            var typeParam = new DocTypeParameter
            {
                Name = "TEntity",
                Description = "The entity type that must implement IEntity."
            };

            typeParam.Name.Should().Be("TEntity");
            typeParam.Description.Should().Contain("IEntity");
        }


        [TestMethod]
        public void Description_HandlesComplexConstraintDescriptions()
        {
            var complexDescription = @"The type parameter that must satisfy the following constraints:
- Must implement IComparable<T>
- Must have a parameterless constructor
- Must be a reference type";

            var typeParam = new DocTypeParameter
            {
                Name = "T",
                Description = complexDescription
            };

            typeParam.Description.Should().Be(complexDescription);
            typeParam.Description.Should().Contain("IComparable<T>");
            typeParam.Description.Should().Contain("parameterless constructor");
            typeParam.Description.Should().Contain("reference type");
        }

        [TestMethod]
        public void Description_HandlesGenericNotation()
        {
            var typeParam = new DocTypeParameter
            {
                Name = "TDelegate",
                Description = "The delegate type, typically Func<T, TResult> or Action<T>."
            };

            typeParam.Description.Should().Contain("Func<T, TResult>");
            typeParam.Description.Should().Contain("Action<T>");
        }

        [TestMethod]
        public void Multiple_TypeParameters_CanBeCreated()
        {
            var keyParam = new DocTypeParameter
            {
                Name = "TKey",
                Description = "The type of the dictionary key."
            };

            var valueParam = new DocTypeParameter
            {
                Name = "TValue",
                Description = "The type of the dictionary value."
            };

            keyParam.Name.Should().Be("TKey");
            valueParam.Name.Should().Be("TValue");
            keyParam.Description.Should().NotBe(valueParam.Description);
        }

        #endregion

    }

}