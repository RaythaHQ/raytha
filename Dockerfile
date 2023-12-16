FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src

RUN set -uex; \
    apt-get update; \
    apt-get install -y ca-certificates curl gnupg; \
    mkdir -p /etc/apt/keyrings; \
    curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key \
     | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg; \
    NODE_MAJOR=18; \
    echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_$NODE_MAJOR.x nodistro main" \
     > /etc/apt/sources.list.d/nodesource.list; \
    apt-get -qy update; \
    apt-get -qy install nodejs;

COPY ["src/Raytha.Domain/Raytha.Domain.csproj", "src/Raytha.Domain/"]
COPY ["src/Raytha.Application/Raytha.Application.csproj", "src/Raytha.Application/"]
COPY ["src/Raytha.Infrastructure/Raytha.Infrastructure.csproj", "src/Raytha.Infrastructure/"]
COPY ["src/Raytha.Web/Raytha.Web.csproj", "src/Raytha.Web/"]
COPY ["tests/Raytha.Domain.UnitTests/Raytha.Domain.UnitTests.csproj", "tests/Raytha.Domain.UnitTests/"]
COPY ["Raytha.sln", ""]

ARG DOTNET_RESTORE_CLI_ARGS=
RUN dotnet restore "Raytha.sln" $DOTNET_RESTORE_CLI_ARGS

COPY . .
RUN dotnet build "Raytha.sln" -c Release --no-restore

RUN dotnet publish -c Release --no-build -o /app "src/Raytha.Web/Raytha.Web.csproj"

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ARG BUILD_NUMBER=
ENV BUILD_NUMBER=$BUILD_NUMBER

ENTRYPOINT ["dotnet", "Raytha.Web.dll"]