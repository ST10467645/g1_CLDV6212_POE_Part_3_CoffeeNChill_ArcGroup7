# Dockerfile for the CoffeeNChill gateway (ASP.NET Core 9 Web API).
#
# Code Attribution:
# The multi-stage build (big SDK image to build, small ASP.NET runtime image to run) was adapted from the
# lecturer-provided Docker examples and my Part 2 Functions Dockerfile (IIE, 2026), together with Microsoft's
# .NET container tutorial and Docker's multi-stage build documentation.
#
# Microsoft Learn (2026aj). Tutorial: Containerize a .NET app. [Online]
# Available at: https://learn.microsoft.com/en-us/dotnet/core/docker/build-container
# [Accessed 20 October 2026].
#
# Docker (2026h). Dockerfile reference. [Online]
# Available at: https://docs.docker.com/reference/dockerfile/
# [Accessed 09 October 2026].
#
# Docker (2026j). Multi-stage builds. [Online]
# Available at: https://docs.docker.com/build/building/multi-stage/
# [Accessed 20 October 2026].

# ---- Build stage: the big SDK image compiles and publishes the app ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only the project file first and restore the packages. Docker keeps this step in its cache,
# so a rebuild after a small code change is much faster.
COPY CoffeeNChill.Gateway.csproj ./
RUN dotnet restore CoffeeNChill.Gateway.csproj

# Now copy the rest of the code and publish a Release build (it also copies the XML file Swagger needs).
COPY . .
RUN dotnet publish CoffeeNChill.Gateway.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage: the small image that only runs the app ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The .NET 9 image does not run as root, so it cannot use port 80. The app listens on 8080 inside the container.
# docker-compose maps the public port 5000 on the VPS to this 8080.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CoffeeNChill.Gateway.dll"]