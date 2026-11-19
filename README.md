# AE3OP Task 08 - Asynchronous programming

This .NET 10 Razor Pages application implements both tutorial flows:

- asynchronous image generation with the MAI image endpoint;
- asynchronous text generation with the Responses API;
- separate Razor Page handlers and bound form models;
- request/response JSON models;
- secrets loaded from .NET configuration rather than source control.

Configure local secrets before making real cloud requests:

```powershell
dotnet user-secrets set "Foundry:Endpoint" "<endpoint>"
dotnet user-secrets set "Foundry:ApiKey" "<api-key>"
dotnet user-secrets set "Foundry:ImageModel" "<image-model>"
dotnet user-secrets set "Foundry:TextModel" "<text-model>"
dotnet run
```

No credential is stored in this repository. Without secrets, the application starts normally and displays a clear configuration message instead of calling the cloud service.
