FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/healthz || exit 1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src

COPY ["src/Raytha.Domain/Raytha.Domain.csproj", "src/Raytha.Domain/"]
COPY ["src/Raytha.Application/Raytha.Application.csproj", "src/Raytha.Application/"]
COPY ["src/Raytha.Infrastructure/Raytha.Infrastructure.csproj", "src/Raytha.Infrastructure/"]
COPY ["src/Raytha.Web/Raytha.Web.csproj", "src/Raytha.Web/"]
COPY ["tests/Raytha.Domain.UnitTests/Raytha.Domain.UnitTests.csproj", "tests/Raytha.Domain.UnitTests/"]
COPY ["tests/Raytha.Application.UnitTests/Raytha.Application.UnitTests.csproj", "tests/Raytha.Application.UnitTests/"]
COPY ["tests/Raytha.Infrastructure.UnitTests/Raytha.Infrastructure.UnitTests.csproj", "tests/Raytha.Infrastructure.UnitTests/"]
COPY ["Directory.Build.props", ""]
COPY ["Directory.Packages.props", ""]
COPY ["VERSION", ""]
COPY ["Raytha.sln", ""]

ARG DOTNET_RESTORE_CLI_ARGS=
RUN dotnet restore "Raytha.sln" $DOTNET_RESTORE_CLI_ARGS

COPY . .
RUN dotnet build "Raytha.sln" -c Release --no-restore
RUN dotnet publish -c Release --no-build -o /app "src/Raytha.Web/Raytha.Web.csproj"

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Raytha.Web.dll"]
