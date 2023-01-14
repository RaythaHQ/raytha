FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS publish
WORKDIR /src

RUN apt install -y curl
RUN curl -fsSL https://deb.nodesource.com/setup_16.x | bash -
RUN apt-get install -y nodejs

COPY ["src/Raytha.Domain/Raytha.Domain.csproj", "src/Raytha.Domain/"]
COPY ["src/Raytha.Application/Raytha.Application.csproj", "src/Raytha.Application/"]
COPY ["src/Raytha.Infrastructure/Raytha.Infrastructure.csproj", "src/Raytha.Infrastructure/"]
COPY ["src/Raytha.Web/Raytha.Web.csproj", "src/Raytha.Web/"]
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
