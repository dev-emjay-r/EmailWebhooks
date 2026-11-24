sc.exe create EmailWebhooks binPath= "C:\Services\EmailWebhooks\EmailWebhooks.exe" start= auto
sc.exe description EmailWebhooks "Email Webhooks ASP.NET Core API"
sc.exe start EmailWebhooks