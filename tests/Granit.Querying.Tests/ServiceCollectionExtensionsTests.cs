using Granit.Querying.SavedViews;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Querying.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitQuerying_registers_null_saved_view_store()
    {
        ServiceCollection services = new();

        services.AddGranitQuerying();

        services.ShouldContain(d =>
            d.ServiceType == typeof(ISavedViewStore) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitQuerying_does_not_replace_existing_store()
    {
        ServiceCollection services = new();
        services.AddScoped<ISavedViewStore, FakeSavedViewStore>();

        services.AddGranitQuerying();

        ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        ISavedViewStore store = scope.ServiceProvider.GetRequiredService<ISavedViewStore>();
        store.ShouldBeOfType<FakeSavedViewStore>();
    }

    [Fact]
    public void AddGranitQuerying_returns_service_collection_for_chaining()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddGranitQuerying();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddQueryDefinition_registers_definition_as_singleton()
    {
        ServiceCollection services = new();

        services.AddQueryDefinition<TestEntity, TestQueryDef>();

        services.ShouldContain(d =>
            d.ServiceType == typeof(QueryDefinition<TestEntity>) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddQueryDefinition_registers_descriptor_as_singleton()
    {
        ServiceCollection services = new();

        services.AddQueryDefinition<TestEntity, TestQueryDef>();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IQueryDefinitionDescriptor) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddQueryDefinition_descriptor_resolves_to_same_instance()
    {
        ServiceCollection services = new();
        services.AddQueryDefinition<TestEntity, TestQueryDef>();
        ServiceProvider provider = services.BuildServiceProvider();

        QueryDefinition<TestEntity> definition = provider.GetRequiredService<QueryDefinition<TestEntity>>();
        IQueryDefinitionDescriptor descriptor = provider.GetRequiredService<IQueryDefinitionDescriptor>();

        descriptor.ShouldBeSameAs(definition);
    }

    [Fact]
    public void AddQueryDefinition_returns_service_collection_for_chaining()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddQueryDefinition<TestEntity, TestQueryDef>();

        result.ShouldBeSameAs(services);
    }

    private sealed class TestQueryDef : QueryDefinition<TestEntity>
    {
        public override string Name => "Test.Entities";

        protected override void Configure(QueryDefinitionBuilder<TestEntity> builder) =>
            builder.Column(e => e.Name);
    }

    private sealed class FakeSavedViewStore : ISavedViewStore
    {
        public Task<IReadOnlyList<SavedView>> GetListAsync(
            string entityType, string userId, Guid? tenantId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SavedView>>([]);

        public Task<SavedView?> GetAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<SavedView?>(null);

        public Task CreateAsync(SavedView view, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(SavedView view, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task SetDefaultAsync(Guid id, string userId, string entityType, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
