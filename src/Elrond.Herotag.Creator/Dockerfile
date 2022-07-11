#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Elrond.Herotag.Creator/Elrond.Herotag.Creator.Web.csproj", "Elrond.Herotag.Creator/"]
RUN dotnet restore "Elrond.Herotag.Creator/Elrond.Herotag.Creator.Web.csproj"
COPY . .
WORKDIR "/src/Elrond.Herotag.Creator"
RUN dotnet build "Elrond.Herotag.Creator.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Elrond.Herotag.Creator.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Elrond.Herotag.Creator.Web.dll"]