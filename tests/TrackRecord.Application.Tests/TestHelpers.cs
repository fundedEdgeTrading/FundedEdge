using TrackRecord.Application.Abstractions;

namespace TrackRecord.Application.Tests;

/// <summary>ICurrentUserAccessor de prueba: siempre resuelve al mismo usuario fijo (o ninguno).</summary>
public sealed class FakeCurrentUserAccessor(string? userId) : ICurrentUserAccessor
{
    public Task<string?> GetUserIdAsync() => Task.FromResult(userId);
}
