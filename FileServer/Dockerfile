FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER root
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FileServer/FileServer.csproj", "FileServer/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "FileServer/FileServer.csproj"
COPY . .
WORKDIR "/src/FileServer"
RUN dotnet build "FileServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FileServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 55555/udp
EXPOSE 6969/tcp
ENTRYPOINT ["sudo dotnet", "FileServer.dll"]
