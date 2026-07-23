# AI-Container

## Setup

### Einmalig, vor dem ersten Start

1. Erstelle Docker Named Volume:

```bash
docker volume create opencode-home
```

2. Erstelle zwei leere Files (oder kopiere aus bestehender OpenCode-Installation):

```bash
mkdir -p "${HOME}/.local/share/opencode"
touch "${HOME}/.local/share/opencode/account.json" \
      "${HOME}/.local/share/opencode/auth.json"
```

3. Ein zufälliges Passwort als Umgebungsvariable setzen:

```bash
export OPENCHAMBER_UI_PASSWORD="$(openssl rand -base64 32)"
```

4. Azure-DevOps- und GitKraken-PATs als Umgebungsvariablen setzen. Beide Variablen sind für die Compose-Secrets erforderlich. `AZURE_DEVOPS_PAT` enthält den unveränderten rohen Azure-DevOps-PAT, den MCP und Git verwenden; er ersetzt den bisherigen Base64-kodierten Wert `username:PAT`:

```bash
export AZURE_DEVOPS_PAT='<Azure-DevOps-PAT>'
export GITKRAKEN_PAT='<GitKraken-PAT>'
```

5. Leeren Worktree-Ordner erstellen:

```
├── <repo>
└── <repo>.worktrees
```

5. Optional: Lokale Website-Zugangsdaten aktivieren. Beide ignorierten Dateien erstellen und mit den jeweiligen Werten fuellen:

```bash
touch .ai-tools/user.txt .ai-tools/password.txt
```

   Die Zugangsdaten werden nur eingehaengt, wenn beide Dateien vorhanden sind.

## Use

### Run

```bash
cd .ai-tools
./run-opencode.sh
```

### OpenChamber UI öffnen

[personal.localhost:3001](http://personal.localhost:3001)

Die UI ist nur über den lokalen Host erreichbar. Für externen Zugriff muss die
Port-Bindung bewusst angepasst werden.

## Documentation

### Worktrees

#### Host

```bash
..\..\<repo>.worktrees
```

#### Im AI-Container

```bash
~/.local/share/opencode/worktree
```

### Config

#### OpenCode

- Persönliche Einstellungen werden im ``opencode-home``-Volume vorgenommen.
- Projektbezogene Einstellungen im ``.opencode``-Verzeichnis im Projekt.

#### Volumes

Docker-Volumes:

- Nutzerspezifisch
  - opencode-home
- Projektspezifisch
  - cache
  - cache-bun
  - cache-npm
  - openchamber-config
  - opencode-agents
  - opencode-local

Files:

- Nutzerspezifisch
  - ${HOME}/.local/share/opencode/account.json
  - ${HOME}/.local/share/opencode/auth.json

## Maintenance

``run-opencode.sh`` löst Runtime-Tags in dieser Reihenfolge auf: explizite Umgebungsvariablen, neuestes GitHub-Release und zuletzt den im lokalen Image verfügbaren Tag.

Für einen reproduzierbaren Start Tags als Umgebungsvariablen übergeben:

```bash
OPENCODE_TAG=1.18.4 OPENCHAMBER_TAG=1.16.3 ./run-opencode.sh
```

Die Compose-Dateien müssen für Tag-Updates nicht bearbeitet werden.
