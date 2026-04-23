# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HoneyDo/HoneyDo.csproj", "HoneyDo/"]
RUN dotnet restore "./HoneyDo/HoneyDo.csproj"
COPY . .
WORKDIR "/src/HoneyDo"
RUN dotnet build "./HoneyDo.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HoneyDo.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# Create the data directory for the SQLite database and give the app user
# write access before switching back to the non-root user.
# Without this, Docker creates the volume mount point owned by root and the
# app cannot open the database file (SQLite error 14).
USER root
RUN mkdir -p /app/data && chown -R $APP_UID /app/data
USER $APP_UID

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HoneyDo.dll"]