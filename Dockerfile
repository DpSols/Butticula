FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["FileExchanger.csproj", "./"]
RUN dotnet restore "FileExchanger.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "FileExchanger.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileExchanger.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileExchanger.dll"]
