# Getting started

## Register App

```powershell
$app = Register-PnPAzureADApp `
    -ApplicationName  "PoC.SiteCollectionCreationPermissions" `
    -Tenant <YourTenant>.onmicrosoft.com `
    -CertificatePassword (ConvertTo-SecureString -String "<YourPassword>" -AsPlainText -Force) `
    -OutPath "./Certificates" `
    -GraphApplicationPermissions User.Read.All,Application.Read.All,Sites.FullControl.All `
    -SharePointApplicationPermissions User.Read.All,Sites.FullControl.All `
    -DeviceLogin
```

## Add User Secrets

```sh
$ dotnet user-secrets set clientconfig:base64 $app.Base64Encoded
$ dotnet user-secrets set clientconfig:password <YourPassword>
```

## Adjust appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ClientConfig": {
    "TenantId": "<TenantId>",
    "ClientId": "<ClientId>",
    "SiteUrl": "https://<tenant>.sharepoint.com/sites/<whatever>",
  }
}
```

## Certificate Handling

- Convert pfx to base 64 (PS Core)

```ps
$pfx_cert = get-content .\Some.pfx -AsByteStream
$base64 = [System.Convert]::ToBase64String($pfx_cert)
```

```ps
[System.Convert]::ToBase64String((get-content .\Some.pfx -AsByteStream))
```