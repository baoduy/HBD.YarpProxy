FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# Work around for broken dotnet restore
ADD http://ftp.us.debian.org/debian/pool/main/c/ca-certificates/ca-certificates_20210119_all.deb .
RUN dpkg -i ca-certificates_20210119_all.deb

RUN apt-get update && apt-get install -y \
	ca-certificates \
	&& update-ca-certificates \
	&& rm -rf /var/lib/apt/lists/*
	
WORKDIR /app
# copy csproj and restore in distinct layer
COPY *.sln .
# COPY ./nuget.config ./nuget.config
COPY ./HBD.YarpProxy.csproj ./HBD.YarpProxy.csproj

RUN wget -qO- https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh | bash
ARG PAT
ENV NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED true
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS "{\"endpointCredentials\": [{\"endpoint\":\"https://nuget.pkg.github.com/baoduy/index.json\", \"password\":\"$PAT\"}]}"
RUN dotnet restore ./HBD.YarpProxy.csproj -s "https://api.nuget.org/v3/index.json" -s "https://nuget.pkg.github.com/baoduy/index.json"
# Copy everying else and build app
COPY . .
WORKDIR /app

# RUN dotnet publish HBD.YarpProxy.csproj -c Release -o out -r alpine-x64 -p:PublishTrimmed=true -p:TrimMode=Link --self-contained true
RUN dotnet publish HBD.YarpProxy.csproj -c Release -o out

# FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine-amd64 AS runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
# Fixed the Alpine image issue 
# https://andrewlock.net/dotnet-core-docker-and-cultures-solving-culture-issues-porting-a-net-core-app-from-windows-to-linux/
# Install cultures (same approach as Alpine SDK image)
# RUN apk add --no-cache icu-libs
# Disable the invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
# ENV ASPNETCORE_URLS=http://+:8080

WORKDIR /app
COPY --from=build /app/out ./

# Install the required packages
RUN apt-get --no-cache add ca-certificates
# Copy all the certificates from the certs folder
COPY certs/* /usr/local/share/ca-certificates/

EXPOSE 8080/tcp
ENTRYPOINT ["./HBD.YarpProxy"]
