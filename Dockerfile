FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
RUN apt-get update \
  && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/healthz || exit 1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src

COPY ["src/Raytha.Domain/Raytha.Domain.csproj", "src/Raytha.Domain/"]
COPY ["src/Raytha.Application/Raytha.Application.csproj", "src/Raytha.Application/"]
COPY ["src/Raytha.Infrastructure/Raytha.Infrastructure.csproj", "src/Raytha.Infrastructure/"]
COPY ["src/Raytha.Web/Raytha.Web.csproj", "src/Raytha.Web/"]
COPY ["Directory.Build.props", ""]
COPY ["Directory.Packages.props", ""]
COPY ["VERSION", ""]

ARG DOTNET_RESTORE_CLI_ARGS=
RUN dotnet restore "src/Raytha.Web/Raytha.Web.csproj" $DOTNET_RESTORE_CLI_ARGS

COPY . .
RUN dotnet publish "src/Raytha.Web/Raytha.Web.csproj" -c Release --no-restore -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
# The admin SPA is served from the bundle committed under src/Raytha.Web/wwwroot/raytha. The
# image has no src/admin or Node, so never try to start the Vite dev server (Development mode
# does by default).
ENV AdminSpa__AutoStart=false
ENTRYPOINT ["dotnet", "Raytha.Web.dll"]
