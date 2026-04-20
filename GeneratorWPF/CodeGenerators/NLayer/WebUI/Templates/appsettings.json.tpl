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
  "CacheSettings": {
    "SlidingExpirationMinutes": 30,
    "AbsoluteExpirationMinutes": 120
  },
  "LocalizationSettings": {
    "DefaultLanguage": "tr-TR",
    "AvailableLanugages": [
      "tr-TR",
      "en-US"
    ]
  },
  "AllowedHosts": "*"
}
