# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy our source
COPY . .

# Create temp ASP.NET project and overwrite Program.cs
RUN dotnet new web -n api -o /tmp/api >/dev/null 2>&1 || true
COPY Program.cs /tmp/api/Program.cs

WORKDIR /tmp/api
# Ensure required packages are available
RUN dotnet add package Microsoft.Data.Sqlite --version 8.*

# Publish
RUN dotnet publish -c Release -o /out

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
COPY seed.json ./seed.json
VOLUME [ "/data" ]
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "api.dll"]
