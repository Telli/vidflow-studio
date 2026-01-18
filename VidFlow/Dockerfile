FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY VidFlow.Api/*.csproj ./VidFlow.Api/
RUN dotnet restore VidFlow.Api/VidFlow.Api.csproj

COPY VidFlow.Api/ ./VidFlow.Api/
WORKDIR /src/VidFlow.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "VidFlow.Api.dll"]
