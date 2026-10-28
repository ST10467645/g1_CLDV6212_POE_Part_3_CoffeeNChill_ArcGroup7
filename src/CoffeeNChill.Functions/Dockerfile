# Dockerfile
# Code Attribution:
# The multi-stage build pattern was adapted from Microsoft's Azure Functions custom container documentation and Docker's Dockerfile reference.
#
# Microsoft Learn (2026b). Work with containers and Azure Functions. [Online]
# Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-custom-container
# [Accessed 04 September 2026].
#
# Docker (2026h). Dockerfile reference. [Online]
# Available at: https://docs.docker.com/reference/dockerfile/
# [Accessed 09 October 2026].

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "CoffeeNChillFunctions.csproj" -c Release -o /home/site/wwwroot

# ---- Runtime stage ----
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY --from=build ["/home/site/wwwroot", "/home/site/wwwroot"]
EXPOSE 80
