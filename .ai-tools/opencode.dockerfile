ARG OPENCODE_TAG=1.18.4
# FROM ghcr.io/anomalyco/opencode <- TODO: replace by noser opencode image
FROM ghcr.io/anomalyco/opencode:${OPENCODE_TAG}

USER root
ENV HOME=/home/root
ENV PIPX_HOME=/opt/pipx
ENV PIPX_BIN_DIR=/usr/local/bin
ENV GIT_ASKPASS=/usr/local/bin/azure-devops-askpass.sh
ENV GIT_TERMINAL_PROMPT=0
WORKDIR ${HOME}

RUN apk --no-cache add \
        bash \
        tmux \
        unzip \
        nodejs \
        npm \
        grep \
        curl \
        wget \
        gcompat \
        git \
        python3 \
        pipx \
        dotnet10-sdk \
        chromium-headless-shell \
        firefox \
    && pipx install graphifyy --pip-args="--no-cache-dir" \
    && rm -rf "${HOME}/.cache/pip" "${HOME}/.cache/pipx" /var/cache/apk/*

ENV CHROME_BIN=/usr/bin/chromium-headless-shell
ENV PUPPETEER_SKIP_DOWNLOAD=true
ENV PUPPETEER_EXECUTABLE_PATH="${CHROME_BIN}"

ARG PNPM_VERSION=11
ENV PNPM_HOME="/pnpm"
ENV PATH="${PNPM_HOME}/bin:${PATH}"

RUN npm install -g corepack \
    && corepack enable pnpm \
    && corepack use pnpm@latest-${PNPM_VERSION}

ENV PATH="${PIPX_BIN_DIR}:${PATH}"
RUN "${PIPX_BIN_DIR}/graphify" install --platform opencode

COPY --chmod=0755 scripts/ /usr/local/bin/

ENV RTK_INSTALL_DIR=/usr/local/bin
ENV PATH="${RTK_INSTALL_DIR}:${PATH}"
ARG RTK_VERSION=0.43.0
RUN set -eu; \
    case "$(uname -m)" in \
        x86_64) rtk_archive="rtk-x86_64-unknown-linux-musl.tar.gz" ;; \
        aarch64|arm64) rtk_archive="rtk-aarch64-unknown-linux-gnu.tar.gz" ;; \
        *) echo "Unsupported RTK architecture: $(uname -m)" >&2; exit 1 ;; \
    esac; \
    rtk_base_url="https://github.com/rtk-ai/rtk/releases/download/v${RTK_VERSION}"; \
    curl -fsSL "${rtk_base_url}/checksums.txt" -o /tmp/rtk-checksums.txt; \
    curl -fsSL "${rtk_base_url}/${rtk_archive}" -o "/tmp/${rtk_archive}"; \
    grep -F "  ${rtk_archive}" /tmp/rtk-checksums.txt > /tmp/rtk-archive.sha256; \
    cd /tmp; sha256sum -c rtk-archive.sha256; \
    tar -xzf "/tmp/${rtk_archive}" -C /tmp; \
    install -m 0755 /tmp/rtk "${RTK_INSTALL_DIR}/rtk"; \
    rtk --version; \
    rm -f "/tmp/${rtk_archive}" /tmp/rtk-checksums.txt /tmp/rtk-archive.sha256 /tmp/rtk

ARG GK_VERSION=3.1.70
RUN set -eu; \
    arch="$(uname -m)"; \
    case "$arch" in x86_64) gk_arch=amd64 ;; aarch64|arm64) gk_arch=arm64 ;; i386|i686) gk_arch=386 ;; *) echo "Unsupported arch: $arch" >&2; exit 1 ;; esac; \
    gk_archive="gk_${GK_VERSION}_linux_${gk_arch}.zip"; \
    gk_base_url="https://github.com/gitkraken/gk-cli/releases/download/v${GK_VERSION}"; \
    curl -fsSL "${gk_base_url}/gk_checksums.txt" -o /tmp/gk-checksums.txt; \
    curl -fsSL "${gk_base_url}/${gk_archive}" -o "/tmp/${gk_archive}"; \
    grep -F "  ${gk_archive}" /tmp/gk-checksums.txt > /tmp/gk-archive.sha256; \
    cd /tmp; sha256sum -c gk-archive.sha256; \
    unzip "/tmp/${gk_archive}" -d /tmp/gk; \
    install -m 0755 /tmp/gk/gk /usr/local/bin/gk; \
    rm -rf /tmp/gk "/tmp/${gk_archive}" /tmp/gk-checksums.txt /tmp/gk-archive.sha256; \
    gk version

EXPOSE 4098

ENTRYPOINT [ "sh", "-c", \
    "set -eu; \
    echo \"$(which opencode) $(opencode --version)\"; \
    exec opencode serve --hostname 0.0.0.0 --port 4098" \
]
