FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
ARG TARGETARCH
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["HBD.YarpProxy.csproj", "./"]
RUN dotnet restore "HBD.YarpProxy.csproj"
COPY . .
WORKDIR "/src/"

RUN dotnet build "HBD.YarpProxy.csproj" -c $BUILD_CONFIGURATION -o /app/build -a $TARGETARCH

FROM --platform=$BUILDPLATFORM build AS publish
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "HBD.YarpProxy.csproj" --no-restore -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -a $TARGETARCH

FROM --platform=$BUILDPLATFORM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "HBD.YarpProxy.dll"]
