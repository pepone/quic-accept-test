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
        int numConnections = 0;
        while (true)
        {
            Console.Write("Number of blocking connections:" );
            if (Console.ReadLine() is string numConnectionsStr)
            {
                if (int.TryParse(numConnectionsStr, out numConnections))
                {
                    break;
                }
            }
        }

        using var semaphore = new Semaphore(initialCount: 0, maximumCount: 1);
        var blockingClientAuthenticationOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
            {
                Console.WriteLine("Blocking remote certificate validation callback");
                semaphore.WaitOne();
                Console.WriteLine("Releasing remote certificate validation callback");
                return true;
            },
            ApplicationProtocols = new List<SslApplicationProtocol>
            {
                new SslApplicationProtocol("foo")
            }
        };

        var clientAuthenticationOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
            {
                Console.WriteLine("Non blocking remote certificate validation callback");
                return true;
            },
            ApplicationProtocols = new List<SslApplicationProtocol>
            {
                new SslApplicationProtocol("foo")
            }
        };

        for (int i = 0; i < numConnections; ++i)
        {
            _ = Task.Run(() => QuicConnection.ConnectAsync(
                             new QuicClientConnectionOptions
                             {
                                 ClientAuthenticationOptions = blockingClientAuthenticationOptions,
                                 DefaultStreamErrorCode = 0,
                                 DefaultCloseErrorCode = 0,
                                 RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9090),
                             },
                             default));
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            for (int i = 0; i < numConnections; ++i)
            {
                semaphore.Release();
            }
        });

        // Small delay to ensure the connection with the blocking certificate verification callback is accepted first
        await Task.Delay(TimeSpan.FromSeconds(2));

        var task2 = Task.Run(async () =>
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            await QuicConnection.ConnectAsync(
                new QuicClientConnectionOptions
                {
                    ClientAuthenticationOptions = clientAuthenticationOptions,
                    DefaultStreamErrorCode = 0,
                    DefaultCloseErrorCode = 0,
                    RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9090),
                });
            stopWatch.Stop();
            Console.WriteLine($"Elapsed: {stopWatch.Elapsed}");
        });

        await task2;
    }
}
