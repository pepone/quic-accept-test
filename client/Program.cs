using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Quic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

[System.Runtime.Versioning.SupportedOSPlatform("macOS")]
[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public static class Program
{
    static async Task Main()
    {
        using var semaphore = new Semaphore(initialCount: 0, maximumCount: 1);
        var blockingClientAuthenticationOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
            {
                semaphore.WaitOne();
                return true;
            },
            ApplicationProtocols = new List<SslApplicationProtocol>
            {
                new SslApplicationProtocol("foo")
            }
        };

        var clientAuthenticationOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true,
            ApplicationProtocols = new List<SslApplicationProtocol>
            {
                new SslApplicationProtocol("foo")
            }
        };

        _ = QuicConnection.ConnectAsync(
            new QuicClientConnectionOptions
            {
                ClientAuthenticationOptions = blockingClientAuthenticationOptions,
                DefaultStreamErrorCode = 0,
                DefaultCloseErrorCode = 0,
                RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9090),
            },
            default);

        var connectTask = QuicConnection.ConnectAsync(
            new QuicClientConnectionOptions
            {
                ClientAuthenticationOptions = clientAuthenticationOptions,
                DefaultStreamErrorCode = 0,
                DefaultCloseErrorCode = 0,
                RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9090),
            });

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            semaphore.Release();
        });

        var stopWatch = new Stopwatch();
        stopWatch.Start();
        await connectTask;
        stopWatch.Stop();

        Console.WriteLine($"Elapsed: {stopWatch.Elapsed}");
    }
}
