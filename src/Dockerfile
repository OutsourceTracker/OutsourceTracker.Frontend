FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION=0.0.1
ARG ASSEMBLY_VERSION=0.0.1.0
ARG FILE_VERSION=0.0.1.0
ARG INFORMATIONAL_VERSION=0.0.1+unknown
WORKDIR /src

COPY ["OutsourceTracker.Frontend.csproj", "."]
COPY ["nuget.config", "."]

RUN dotnet restore "OutsourceTracker.Frontend.csproj" --configfile "nuget.config"

COPY . .

RUN if [ -d "prebuilt-publish" ] && [ "$(ls -A prebuilt-publish)" ]; then \
      echo "Using downloaded publish artifact" && \
      mkdir -p /app/publish && \
      cp -r prebuilt-publish/* /app/publish/ && \
      if [ -f "wwwroot/version.json" ]; then cp wwwroot/version.json /app/publish/wwwroot/version.json 2>/dev/null || true; fi; \
    else \
      echo "Publishing from source (no prebuilt artifact found)" && \
      dotnet publish "OutsourceTracker.Frontend.csproj" \
        -c $BUILD_CONFIGURATION \
        -o /app/publish \
        --no-restore \
        /p:Version=${VERSION} \
        /p:AssemblyVersion=${ASSEMBLY_VERSION} \
        /p:FileVersion=${FILE_VERSION} \
        /p:InformationalVersion=${INFORMATIONAL_VERSION} \
        /p:AssemblyInformationalVersion=${INFORMATIONAL_VERSION} \
        /p:BlazorEnableCompression=false; \
    fi

# Verify that the critical Blazor WebAssembly loader exists (only checking webassembly, per requirements)
RUN if ! ls /app/publish/wwwroot/_framework/blazor.webassembly*.js 1>/dev/null 2>&1; then \
      echo "ERROR: blazor.webassembly.js (or fingerprinted version) was not found in /app/publish/wwwroot/_framework/"; \
      echo "The published artifact is incomplete. Docker build cannot continue."; \
      ls -la /app/publish/wwwroot/_framework/ 2>/dev/null || echo "No _framework directory at all"; \
      exit 1; \
    else \
      echo "✓ Verified blazor.webassembly*.js exists in published output"; \
    fi

FROM nginx:alpine AS final
WORKDIR /app
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]