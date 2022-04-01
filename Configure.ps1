$app = Register-PnPAzureADApp `
    -ApplicationName  "PoC.AzureADAppSitePermission" `
    -Tenant <tenant>.onmicrosoft.com `
    -CertificatePassword (ConvertTo-SecureString -String "<YourPasssword>" -AsPlainText -Force) `
    -OutPath "./Certificates" `
    -GraphApplicationPermissions User.Read.All `
    -SharePointApplicationPermissions User.Read.All `
    -DeviceLogin

# dotnet user-secrets set clientconfig:base64 $app.Base64Encoded
# dotnet user-secrets set clientconfig:password <YourPassword>

"PoC.AzureADAppSitePermission.pfx" -AsByteStream [System.Convert]::ToBase64String($fileContentBytes) | Out-File ‘PoC.AzureADAppSitePermission.base64’