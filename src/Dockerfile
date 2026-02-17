FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["OutsourceTracker.Frontend.csproj", "."]
COPY ["nuget.config", "."]

RUN dotnet restore "OutsourceTracker.Frontend.csproj" \
    --configfile "nuget.config"

COPY . .

RUN dotnet publish "OutsourceTracker.Frontend.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore

FROM nginx:alpine AS final
WORKDIR /app
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]