namespace Zuijin.Server.Tests;

/// <summary>
/// Shares a single host and a single database seeding pass across every endpoint test class.
/// Without this, xUnit would build one factory per class and the parallel seeding passes
/// would race each other on the same test database.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ZuijinTestCollection : ICollectionFixture<ZuijinApplicationFactory>
{
    public const string Name = "Zuijin server";
}
