### Periodic Maintenance 

1. To avoid updates of `sdk-catalog.json` file a web service should be established to grep actual versions from pages below:

   a. [All](https://dotnet.microsoft.com/download/dotnet-core)
   b. [.NET Core 3.1.x](https://dotnet.microsoft.com/download/dotnet-core/3.1)
   c. [.NET Core 6.0x](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

`sdk-catalog.json` has to be actual at release date. If the web service isn't available the `sdk-catalog.json` gets taken from debugger.
