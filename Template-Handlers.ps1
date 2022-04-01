Connect-PnPOnline -Url "https://<Tenant>.sharepoint.com/sites/site" -Tenant <Tenant>.onmicrosoft.com -ClientId "<ClientId>" -CertificatePassword (ConvertTo-SecureString -String "<YourPassword>" -AsPlainText -Force) -CertificateBase64Encoded "<Base64EncodedClientCertificate>"

@(
    "ApplicationLifecycleManagement",
    "AuditSettings",
    "ComposedLook",
    "ContentTypes",
    "CustomActions",
    "ExtensibilityProviders",
    "Features",
    "Fields",
    "Files",
    "ImageRenditions",
    "Lists",
    "Navigation",
    "None",
    "PageContents",
    "Pages",
    "PropertyBagEntries",
    "Publishing",
    "RegionalSettings",
    "SearchSettings",
    "SiteFooter",
    "SiteHeader",
    "SitePolicy",
    "SiteSecurity",
    "SiteSettings",
    "SupportedUILanguages",
    "SyntexModels",
    "Tenant",
    "TermGroups",
    "Theme",
    "WebApiPermissions",
    "WebSettings",
    "Workflows") | ForEach-Object { 
    Write-Host $_ 
    Get-PnPSiteTemplate -Out "Template-$_.xml" -Handlers $_ 
}