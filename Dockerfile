FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY ["EducationPlatform.sln", "./"]
COPY ["src/EducationPlatform.Domain/EducationPlatform.Domain.csproj", "src/EducationPlatform.Domain/"]
COPY ["src/EducationPlatform.Application/EducationPlatform.Application.csproj", "src/EducationPlatform.Application/"]
COPY ["src/EducationPlatform.Infrastructure/EducationPlatform.Infrastructure.csproj", "src/EducationPlatform.Infrastructure/"]
COPY ["src/EducationPlatform.Api/EducationPlatform.Api.csproj", "src/EducationPlatform.Api/"]
RUN dotnet restore src/EducationPlatform.Api/EducationPlatform.Api.csproj
COPY src/ src/
RUN dotnet publish src/EducationPlatform.Api/EducationPlatform.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN addgroup -S appgroup && adduser -S appuser -G appgroup && mkdir -p /app/uploads && chown -R appuser:appgroup /app
USER appuser
ENTRYPOINT ["dotnet", "EducationPlatform.Api.dll"]
