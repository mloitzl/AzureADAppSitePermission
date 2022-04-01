using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PnP.Core.Auth.Services.Builder.Configuration;
using PnP.Core.Services;
using PnP.Core.Services.Builder.Configuration;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        var clientConfig = new ClientConfig();
        context.Configuration.GetSection("ClientConfig").Bind(clientConfig);
        services.AddSingleton(clientConfig);

        var certFile = new X509Certificate2(
            Convert.FromBase64String(
                clientConfig.Base64),
            clientConfig.Password);

        var cert =
            new PnPCoreAuthenticationX509CertificateOptions
            {
                Certificate = certFile
            };

        services.AddPnPCore(options =>
        {
            options.Sites.Add("SiteToWorkWith", new PnPCoreSiteOptions
            {
                SiteUrl = clientConfig.SiteUrl
            });
        });

        services.AddPnPCoreAuthentication(
            options =>
            {
                options.Credentials.Configurations.Add("x509certificate",
                    new PnPCoreAuthenticationCredentialConfigurationOptions
                    {
                        ClientId = clientConfig.ClientId,
                        TenantId = clientConfig.TenantId,
                        X509Certificate = cert
                    });

                options.Credentials.DefaultConfiguration = "x509certificate";

                options.Sites.Add("SiteToWorkWith",
                    new PnPCoreAuthenticationSiteOptions
                    {
                        AuthenticationProviderName = "x509certificate"
                    });
            });
    })
    .ConfigureLogging(builder => builder.AddConsole())
    .UseConsoleLifetime()
    .Build();

await host.StartAsync();

using var scope = host.Services.CreateScope();

var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

var uri = new Uri("https://loitzl.sharepoint.com/sites/11dfa94d");

var writePermission = new[] { "Write" };
var fullControlPermission = new[] { "FullControl" };

var appId = "72c73f9e-a789-44ba-b731-d0971c04df44"; // PoC.SiteCollectionCreationPermissions

var pnpContextFactory = scope.ServiceProvider.GetRequiredService<IPnPContextFactory>();
using var context = await pnpContextFactory.CreateAsync("SiteToWorkWith");

///
/// Get appDisplayName by appId
/// App Permission: Application.Read.All
/// 
var displayNameRequest = new ApiRequest(ApiRequestType.Graph, $"applications/?$filter=appId eq '{appId}'");
var displayNameResult = await context.Team.ExecuteRequestAsync(displayNameRequest);
var appDisplayName = JsonSerializer.Deserialize<JsonElement>(displayNameResult.Response).GetProperty("value")[0]
    .GetProperty("displayName").GetString(); // todo: several NullReferenceExceptions hidden in this line

var siteId = Guid.Empty;
///
/// Get SiteId by Url (from: SitePipeBind.GetSiteIdThroughGraph)
/// App Permission: ???
///
var idRequest = new ApiRequest(ApiRequestType.Graph, $"sites/{uri.Host}:{uri.LocalPath}");
var idResult = await context.Team.ExecuteRequestAsync(idRequest);

if (!string.IsNullOrEmpty(idResult.Response))
{
    var resultElement = JsonSerializer.Deserialize<JsonElement>(idResult.Response);
    if (resultElement.TryGetProperty("id", out var idProperty))
    {
        var idValue = idProperty.GetString();
        siteId = Guid.Parse(idValue.Split(',')[1]);
    }
}

if (siteId == Guid.Empty) Environment.Exit(1);

///
/// Grant SiteId with permissions (from: GrantPnPAzureADAppSitePermission.ExecuteCmdlet)
/// App Permission: https://graph.microsoft.com/Sites.FullControl.All               😬
/// App Permission: https://microsoft.sharepoint-df.com/Sites.FullControl.All       😬
/// 
var postPayload = new
{
    roles = writePermission.Select(p => p.ToLower()).ToArray(),
    grantedToIdentities = new[]
    {
        new
        {
            application = new
            {
                id = appId,
                displayName = appDisplayName
            }
        }
    }
};

var grantPostRequest = new ApiRequest(
    HttpMethod.Post,
    ApiRequestType.Graph,
    $"sites/{siteId}/permissions",
    JsonSerializer.Serialize(
        postPayload,
        new JsonSerializerOptions
        {
            IgnoreNullValues = true
        }));

var grantPostResult = await context.Team.ExecuteRequestAsync(grantPostRequest);

var permissionId = string.Empty;

///
/// Get permissions
///
if (!string.IsNullOrEmpty(grantPostResult.Response))
{
    Console.WriteLine(IndentJson(grantPostResult.Response));
    
    permissionId = JsonSerializer.Deserialize<JsonElement>(grantPostResult.Response).GetProperty("id").GetString();
    
    var grantGetResult =
        await context.Team.ExecuteRequestAsync(new ApiRequest(ApiRequestType.Graph, $"sites/{siteId}/permissions"));

    Console.WriteLine(IndentJson(grantGetResult.Response));
}

if(string.IsNullOrWhiteSpace(permissionId)) Environment.Exit(1);

///
/// Grant SiteId with permissions (from: SetPnPAzureADAppSitePermission.ExecuteCmdlet)
///

var putPayload = new
{
    roles = fullControlPermission.Select(p => p.ToLower()).ToArray()
};

var grantPatchRequest = new ApiRequest(
    HttpMethod.Patch,
    ApiRequestType.Graph,
    $"sites/{siteId}/permissions/{permissionId}",
    JsonSerializer.Serialize(
        putPayload,
        new JsonSerializerOptions
        {
            IgnoreNullValues = true
        }));

var grantPatchResult = await context.Team.ExecuteRequestAsync(grantPatchRequest);

///
/// Get permissions
///

if (!string.IsNullOrEmpty(grantPatchResult.Response))
{
    Console.WriteLine(IndentJson(grantPatchResult.Response));
    
    var grantGetResult =
        await context.Team.ExecuteRequestAsync(new ApiRequest(ApiRequestType.Graph, $"sites/{siteId}/permissions"));

    Console.WriteLine(IndentJson(grantGetResult.Response));
}



#region [ Helper ]

string IndentJson(string @string)
{
    return JsonSerializer
        .Serialize(
            JsonSerializer.Deserialize<JsonElement>(@string),
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
}

#endregion