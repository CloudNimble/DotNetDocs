namespace CloudNimble.DotNetDocs.Tests.Shared.EdgeCases
{
    // This class intentionally has no XML documentation
    public class ClassWithNoDocs
    {

        public string UndocumentedProperty { get; set; } = string.Empty;

        public void UndocumentedMethod()
        {
            // No documentation
        }

    }

}