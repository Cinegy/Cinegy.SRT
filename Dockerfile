#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Cinegy.Srt.Recv/Cinegy.Srt.Recv.csproj", "Cinegy.Srt.Recv/"]
COPY ["nuget.config",""]
RUN dotnet restore "Cinegy.Srt.Recv/Cinegy.Srt.Recv.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "Cinegy.Srt.Recv/Cinegy.Srt.Recv.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Cinegy.Srt.Recv/Cinegy.Srt.Recv.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cinegy.Srt.Recv.dll"]