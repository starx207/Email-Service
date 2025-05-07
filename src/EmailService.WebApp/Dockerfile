# Use the official .NET runtime image as a runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Expose the ports the application runs on
EXPOSE 80

# Use the official .NET SDK image as a build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build -c Release


FROM build AS publish
WORKDIR /src/src/EmailService.WebApp
RUN dotnet publish --no-build -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
# Set the entry point for the container
ENTRYPOINT ["dotnet", "EmailService.WebApp.dll"]