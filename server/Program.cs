using System.Net;
using System.Net.Security;
using System.Net.Quic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

[System.Runtime.Versioning.SupportedOSPlatform("macOS")]
[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public static class Program
{
    static async Task Main()
    {
        await using QuicListener listener = await QuicListener.ListenAsync(
            new QuicListenerOptions
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9090),
                ConnectionOptionsCallback = GetConnectionOptionsAsync,
                ApplicationProtocols = new List<SslApplicationProtocol>
                {
                    new SslApplicationProtocol("foo")
                }
            },
            CancellationToken.None);

        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        QuicConnection connection;
        while (!cts.Token.IsCancellationRequested)
        {
            var stopWatch = new Stopwatch();
            try
            {
                stopWatch.Start();
                Console.WriteLine("Accepting connection");
                connection = await listener.AcceptConnectionAsync(cts.Token).ConfigureAwait(false);
                Console.WriteLine("Connection accepted");
                _ = connection.CloseAsync(0, default);
            }
            catch (AuthenticationException ex)
            {
                // The connection was rejected due to an authentication exception.
                Console.WriteLine($"AuthenticationException retrying: {ex}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception retrying: {ex}");
            }
            finally
            {
                stopWatch.Stop();
                Console.WriteLine($"Accept connection took: {stopWatch.Elapsed}");
            }
        }
    }

    private static ValueTask<QuicServerConnectionOptions> GetConnectionOptionsAsync(
        QuicConnection connection,
        SslClientHelloInfo sslInfo,
        CancellationToken cancellationToken) =>
        new(new QuicServerConnectionOptions
        {
            ServerAuthenticationOptions = new SslServerAuthenticationOptions()
            {
                ServerCertificate = new X509Certificate2("certs/server.p12", "password"),
                ApplicationProtocols = new List<SslApplicationProtocol> // Mandatory with Quic
                {
                    new SslApplicationProtocol("foo")
                }
            },
            DefaultStreamErrorCode = 0,
            DefaultCloseErrorCode = 0,
        });
}
