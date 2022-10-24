The client application creates two Quic connections the first connection install a certificate verification
callback that blocks the calling thread, the second connection install a certificate verification callback
that accepts all certificates, the ConnectAsync call for the second connection doesn't complete until the
certificate verification callback of the first connection returns.

Start the server

```
dotnet run --project server/server.csproj
```

Start the client

```
dotnet run --project client/client.csproj
Blocking remote certificate validation callback
Releasing remote certificate validation callback
Non blocking remote certificate validation callback
Elapsed: 00:00:28.1249768
```

Tested with .NET 7.0.100-rc.2.22477.23 & Ubuntu 22.04.1 LTS
