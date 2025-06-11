FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Receptoria.API.csproj", "."]
RUN dotnet restore "./Receptoria.API.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Receptoria.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Receptoria.API.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Receptoria.API.dll"]
