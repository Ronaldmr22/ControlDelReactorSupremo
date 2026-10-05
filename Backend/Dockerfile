# ==============================
# ETAPA 1: COMPILAR EL BACKEND
# ==============================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copiamos los archivos de los proyectos
COPY ["ControlReactor.Api/ControlReactor.Api.csproj", "ControlReactor.Api/"]
COPY ["ControlReactor.Negocio/ControlReactor.Negocio.csproj", "ControlReactor.Negocio/"]
COPY ["ControlReactor.Datos/ControlReactor.Datos.csproj", "ControlReactor.Datos/"]
COPY ["ControlReactor.Modelos/ControlReactor.Modelos.csproj", "ControlReactor.Modelos/"]

# Restauramos las dependencias
RUN dotnet restore "ControlReactor.Api/ControlReactor.Api.csproj"

# Copiamos el resto del código
COPY . .

# Publicamos la API
RUN dotnet publish "ControlReactor.Api/ControlReactor.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore


# ==============================
# ETAPA 2: EJECUTAR EL BACKEND
# ==============================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ControlReactor.Api.dll"]