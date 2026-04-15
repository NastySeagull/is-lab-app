FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["prilog.csproj", "."]
RUN dotnet restore "prilog.csproj"

COPY . .
WORKDIR "/src"
RUN dotnet build "prilog.csproj" -c Release -o /app/build
RUN dotnet publish "prilog.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "prilog.dll"]
