# Keycloak with the HealthyFac login theme. The build context is the keycloak-theme folder. The frontend folder is a
# named extra context (additional_contexts), because the theme imports the preset and the language flags from it.
#   docker build -f docker/keycloak/Dockerfile --build-context frontend=frontend keycloak-theme

# ---------- theme build ----------
# The stage must not be named "frontend": COPY --from=frontend would then read the stage, not the extra context.
FROM node:24.15.0-alpine3.22 AS theme-build

# keycloakify build packs the theme jar with Maven.
RUN apk add --no-cache maven openjdk21-jdk

ENV npm_config_update_notifier=false \
    npm_config_fund=false

WORKDIR /build/keycloak-theme
COPY package.json package-lock.json ./
RUN npm ci

# The theme imports these three files of the frontend (tsconfig paths ../frontend/...). The node_modules link lets
# them find the packages of the theme.
COPY --from=frontend src/app/shared/theme/healthyfac-preset.ts /build/frontend/src/app/shared/theme/
COPY --from=frontend src/app/shared/i18n/i18n.constants.ts src/app/shared/i18n/language-flags.ts /build/frontend/src/app/shared/i18n/
RUN ln -s /build/keycloak-theme/node_modules /build/node_modules

COPY . ./
RUN npm run build

# ---------- final ----------
FROM quay.io/keycloak/keycloak:26.8.0

COPY --from=theme-build --chown=keycloak:root \
    /build/keycloak-theme/build_keycloak/keycloak-theme-for-kc-all-other-versions.jar \
    /opt/keycloak/providers/healthyfac-theme.jar

USER 1000
