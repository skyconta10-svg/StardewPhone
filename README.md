# 📱 StardewPhone

Mod para Stardew Valley que adiciona um celular completo ao jogo.

## Funcionalidades

- 💬 **Chat** — mensagens em tempo real entre jogadores no multiplayer
- 💚 **Pix** — transferência de ouro entre jogadores com extrato
- 🔢 **Calculadora** — operações básicas
- 📋 **Lembretes** — to-do list com persistência no save
- 🌤 **Clima** — clima do Stardew + temperatura real via API (Open-Meteo, grátis)

## Como abrir o celular no jogo

Pressione **Tab** (PC) ou **botão de ombro esquerdo** (Mobile).

---

## Guia de Compilação via GitHub Actions (Termux)

### Passo 1 — Instalar o Termux e configurar git

No Termux:
```bash
pkg update && pkg upgrade
pkg install git
```

### Passo 2 — Configurar sua identidade no git

```bash
git config --global user.name "SeuNome"
git config --global user.email "seu@email.com"
```

### Passo 3 — Criar o repositório no GitHub

1. Acesse **github.com** no navegador do celular
2. Clique em **"New repository"**
3. Nome: `StardewPhone`
4. Marque **Public** (gratuito ilimitado) ou **Private** (2000 min/mês grátis)
5. Clique **Create repository**

### Passo 4 — Fazer o upload do código

No Termux:
```bash
# Clona o repositório vazio
git clone https://github.com/SEU_USUARIO/StardewPhone.git
cd StardewPhone

# Copia os arquivos do mod para dentro
# (coloque os arquivos que você recebeu aqui)
```

Estrutura esperada:
```
StardewPhone/
├── .github/
│   └── workflows/
│       └── build.yml
├── StardewPhone/
│   ├── ModEntry.cs
│   ├── manifest.json
│   ├── StardewPhone.csproj
│   ├── PhoneUI/
│   │   ├── PhoneOverlay.cs
│   │   ├── HomeScreen.cs
│   │   └── Apps/
│   │       ├── AppBase.cs
│   │       ├── ChatApp.cs
│   │       ├── PixApp.cs
│   │       ├── CalcApp.cs
│   │       ├── TodoApp.cs
│   │       └── WeatherApp.cs
│   ├── Notifications/
│   │   └── NotificationManager.cs
│   ├── Multiplayer/
│   │   └── PhoneMultiplayer.cs
│   └── assets/
│       └── sprites/
│           └── (coloque os PNGs aqui)
```

### Passo 5 — Enviar para o GitHub

```bash
git add .
git commit -m "Primeira versão do StardewPhone"
git push origin main
```

Se pedir senha, use um **Personal Access Token**:
- No GitHub: Settings → Developer Settings → Personal Access Tokens → Tokens (classic)
- Gere um token com permissão `repo`
- Use o token como senha no git

### Passo 6 — Baixar o mod compilado

1. Acesse seu repositório no GitHub
2. Clique na aba **Actions**
3. Clique no workflow mais recente
4. Ao final, em **Artifacts**, clique em **StardewPhone-mod**
5. Baixe o ZIP com o mod pronto!

### Passo 7 — Instalar o mod

1. Instale o **SMAPI** no seu dispositivo (smapi.io)
2. Extraia o ZIP baixado
3. Coloque a pasta `StardewPhone` dentro de:
   - **PC**: `Stardew Valley/Mods/`
   - **Mobile**: Pasta Mods do SMAPI no seu dispositivo

---

## Sprites necessários (crie você mesmo!)

Todos os arquivos ficam em `StardewPhone/assets/sprites/`:

| Arquivo | Tamanho | Descrição |
|---------|---------|-----------|
| `phone_body.png` | 64×112 | Corpo do celular |
| `phone_screen.png` | 48×80 | Tela interna |
| `icon_chat.png` | 16×16 | Ícone do chat |
| `icon_pix.png` | 16×16 | Ícone do Pix |
| `icon_calc.png` | 16×16 | Ícone da calculadora |
| `icon_todo.png` | 16×16 | Ícone dos lembretes |
| `icon_weather.png` | 16×16 | Ícone do clima |
| `notification_bubble.png` | 8×8 | Bolinha de notificação |
| `phone_item.png` | 16×16 | Item no inventário |

> 💡 Use **Piskel** (piskelapp.com) no navegador do celular — funciona perfeitamente!
> Estilo pixel art, paleta do Stardew Valley, sem anti-aliasing.

**Se não criar os sprites:** o mod funciona normalmente com fallback colorido (retângulos com letras).

---

## Fazendo alterações depois

Toda vez que quiser mudar algo:

```bash
# Edite os arquivos no Termux com nano
nano StardewPhone/PhoneUI/Apps/ChatApp.cs

# Suba as alterações
git add .
git commit -m "Descreva o que mudou"
git push

# Baixe o mod novo no GitHub Actions
```

---

## Dependências

- Stardew Valley 1.5.6+
- SMAPI 3.18+
- Nenhuma dependência extra (a WeatherApp usa HttpClient nativo do .NET)
