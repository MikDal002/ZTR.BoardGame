namespace ZtrBoardGame.Testing.ToxiProxy;

/// <summary>
/// Constants and helpers for ToxiProxy test environments.
/// </summary>
public static class ToxiProxyHelper
{
    /// <summary>
    /// The hostname that resolves to the host machine from inside a Testcontainers-managed container.
    /// Used as upstream address when tests run services directly on the host.
    /// </summary>
    public const string HostAddress = "host.docker.internal";
}
