# Setup Guide

## Configurarea API Key-urilor

Acest proiect utilizeaz? Google AI API. Pentru a rula proiectul local, trebuie s? configurezi propriul API key.

### Metoda 1: Folosind appsettings.Development.json (Recomandat pentru development)

1. Deschide fi?ierul `appsettings.Development.json`
2. Înlocuie?te `"Your key here"` cu cheia ta Google AI API:
   ```json
   {
     "GoogleAI": {
       "ApiKey": "YOUR_ACTUAL_API_KEY_HERE"
     }
   }
   ```

**Not?:** Fi?ierul `appsettings.Development.json` este deja în `.gitignore` ?i nu va fi commit-at pe GitHub.

### Metoda 2: Folosind User Secrets (Recomandat pentru produc?ie)

Pentru securitate maxim?, po?i folosi User Secrets:

```bash
cd OnlineShopProject_dNet
dotnet user-secrets init
dotnet user-secrets set "GoogleAI:ApiKey" "YOUR_ACTUAL_API_KEY_HERE"
```

### Ob?inerea unui Google AI API Key

1. Acceseaz? [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Conecteaz?-te cu contul t?u Google
3. Creeaz? un nou API key
4. Copiaz? cheia ?i configureaz?-o folosind una din metodele de mai sus

### Fi?iere importante

- **appsettings.json** - Configurare de baz? (f?r? secret-uri)
- **appsettings.Development.json** - Configurare pentru development local (nu se commit-?)
- **appsettings.example.json** - Template pentru al?i dezvoltatori

## Connection String

De asemenea, va trebui s? configurezi connection string-ul pentru baza de date local? în `appsettings.Development.json` dac? este necesar.
