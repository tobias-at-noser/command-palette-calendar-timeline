ARG NODE_VERSION=lts

# image: ghcr.io/openchamber/openchamber <- TODO: use official image when published
FROM node:${NODE_VERSION}-alpine

USER root

ENV HOME=/home/root
WORKDIR ${HOME}

ARG OPENCHAMBER_TAG
RUN apk --no-cache add \
    bash \
    git \
    && apk --no-cache add --virtual .openchamber-build-deps \
        build-base \
        curl \
        python3 \
    # echo version to trigger cache invalidations on update
    && echo "Installing ${OPENCHAMBER_TAG}..." \
    && curl -fsSL https://raw.githubusercontent.com/btriapitsyn/openchamber/main/scripts/install.sh | sh \
    && apk del .openchamber-build-deps \
    && rm -rf /var/cache/apk/*

EXPOSE 3003

# trick openchamber into believing that opencode is installed
# as it's not needed in our setup but can't start without
ENV OPENCODE_BINARY=/usr/local/bin/openchamber
ENV OPENCODE_SKIP_START=true

ENTRYPOINT [ "sh", "-c", \
    "rm -f $HOME/.config/openchamber/run/openchamber-3003.* \
    && echo $(which openchamber && openchamber --version) \
    && exec openchamber serve --port 3003 --host 0.0.0.0 --foreground" ]
