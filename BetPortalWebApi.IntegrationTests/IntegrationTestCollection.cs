using Xunit;

[CollectionDefinition("API integration tests")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
}