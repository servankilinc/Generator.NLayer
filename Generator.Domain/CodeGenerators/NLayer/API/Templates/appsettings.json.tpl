{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "Database": "{{ db_connection_name }}"
  },
  "TokenSettings": {
    "Audience": "{{ project_name }}.com",
    "Issuer": "{{ project_name }}.com",
    "AccessTokenExpiration": 1440,
    "RefreshTokenExpiration": 10080,
    "SecurityKey": "{{ securityKey }}",
    "RefreshTokenTTL": 7
  },
  "CacheSettings": {
    "SlidingExpirationMinutes": 30,
    "AbsoluteExpirationMinutes": 120
  },
  "LocalizationSettings": {
    "DefaultLanguage": "tr-TR",
    "AvailableLanguages": [
      "tr-TR",
      "en-US"
    ]
  },
  "AllowedHosts": "*"
}
