
Start the server and client applications from separate command windows


dotnet run --project server/server.csproj

dotnet run --project client/client.csproj


The client application creates two Quic connections the first connection install a certificate verification
callback that blocks the calling thread, the second connection install a certificate verification callback
that accepts all certificates, the ConnectAsync call for the second connection doesn't complete until the
certificate verification callback of the first connection returns.


```
Blocking remote certificate validation callback
Releasing remote certificate validation callback
Non blocking remote certificate validation callback
Elapsed: 00:00:28.1249768
```



