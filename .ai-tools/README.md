# AI-Container

## Setup

### Einmalig, vor dem ersten Start

1. Erstelle Docker Named Volume:

```bash
docker volume create opencode-home
```

2. Erstelle zwei leere Files (oder kopiere aus bestehender OpenCode-Installation):

```bash
${HOME}/.local/share/opencode/account.json
${HOME}/.local/share/opencode/auth.json
```

3. Passwort als Umgebungsvariable setzen (beliebiger Wert):

```bash
OPENCHAMBER_UI_PASSWORD
```

4. Leeren Worktree-Ordner erstellen:

```
├── <repo>
└── <repo>.worktrees
```

## Use

### Run

```bash
cd .ai-tools
./run-opencode.sh
```

### OpenChamber UI öffnen

[personal.localhost:3001](http://personal.localhost:3001)

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

- Set tags in ``compose-opencode.yml`` to match updated opencode or openchamber versions (triggers image pull and rebuild)
- ``./run-opencode.sh``
