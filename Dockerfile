# Etapa 1: Compilación (Build)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiar archivos de proyectos para restaurar dependencias
COPY ["src/MiCarroAlDia.Domain/MiCarroAlDia.Domain.csproj", "src/MiCarroAlDia.Domain/"]
COPY ["src/MiCarroAlDia.Application/MiCarroAlDia.Application.csproj", "src/MiCarroAlDia.Application/"]
COPY ["src/MiCarroAlDia.Infrastructure/MiCarroAlDia.Infrastructure.csproj", "src/MiCarroAlDia.Infrastructure/"]
COPY ["src/MiCarroAlDia.Web/MiCarroAlDia.Web.csproj", "src/MiCarroAlDia.Web/"]

RUN dotnet restore "src/MiCarroAlDia.Web/MiCarroAlDia.Web.csproj"

# Copiar el código fuente completo y compilar
COPY . .
WORKDIR "/app/src/MiCarroAlDia.Web"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Entorno de ejecución (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Exponer el puerto estándar de ASP.NET Core en contenedores
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MiCarroAlDia.Web.dll"]
