FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS publish
WORKDIR /src

RUN apt update
RUN apt install -y curl ca-certificates curl gnupg
RUN mkdir -p /etc/apt/keyrings
RUN curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg
RUN echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_16.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list
RUN apt update
RUN apt install -y nodejs

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
