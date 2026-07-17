# FROM ghcr.io/anomalyco/opencode <- TODO: replace by noser opencode image
FROM ghcr.io/anomalyco/opencode

USER root
ENV HOME=/home/root
ENV PIPX_HOME=/opt/pipx
ENV PIPX_BIN_DIR=/usr/local/bin
WORKDIR ${HOME}

RUN apk --no-cache add \
        bash \
        tmux \
        nodejs \
        npm \
        grep \
        curl \
        wget \
        git \
        python3 \
        pipx \
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

ENV RTK_INSTALL_DIR=/usr/local/bin
ENV PATH="${RTK_INSTALL_DIR}:${PATH}"
RUN curl -fsSL https://raw.githubusercontent.com/rtk-ai/rtk/refs/heads/master/install.sh | sh

RUN set -eu; \
    arch="$(uname -m)"; \
    case "$arch" in x86_64) gk_arch=amd64 ;; aarch64|arm64) gk_arch=arm64 ;; i386|i686) gk_arch=386 ;; *) echo "Unsupported arch: $arch" >&2; exit 1 ;; esac; \
    gk_version="$(curl -fsSL https://api.github.com/repos/gitkraken/gk-cli/releases/latest | grep '"tag_name":' | sed -E 's/.*"v?([^"]+)".*/\1/')"; \
    curl -fsSL "https://github.com/gitkraken/gk-cli/releases/download/v${gk_version}/gk_${gk_version}_linux_${gk_arch}.zip" -o /tmp/gk.zip; \
    unzip /tmp/gk.zip -d /tmp/gk; \
    install -m 0755 /tmp/gk/gk /usr/local/bin/gk; \
    rm -rf /tmp/gk /tmp/gk.zip; \
    gk version

EXPOSE 4098

ENTRYPOINT [ "sh", "-c", \
    "set -eu; \
    echo \"$(which opencode) $(opencode --version)\"; \
    exec opencode serve --hostname 0.0.0.0 --port 4098" \
]
