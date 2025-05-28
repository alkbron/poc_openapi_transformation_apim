# Programme de transformation OpenAPI

Ce programme C# transforme un fichier OpenAPI en effectuant les opérations suivantes :

## Fonctionnalités

1. **Lecture du fichier OpenAPI** : Lit un fichier OpenAPI (YAML ou JSON) depuis le chemin spécifié en paramètre

2. **Transformation des security schemes** :

   - Extrait les security schemes de type `apiKey` (header et query)
   - Les supprime de la section `securitySchemes`
   - Les ajoute comme paramètres facultatifs à chaque opération

3. **Ajout d'OAuth2** :

   - Ajoute un nouveau security scheme OAuth2 Bearer Token
   - Configure le flow client credentials
   - Ajoute le requirement OAuth2 au niveau global

4. **Sauvegarde** : Sauvegarde le fichier modifié dans le même répertoire que le fichier source sous le nom `{nom_original}_modified.yaml`

## Utilisation

### Prérequis

- .NET 9.0 ou supérieur
- Un fichier OpenAPI (format YAML ou JSON)

### Méthode 1 : Avec le script de test (recommandé)

Le script PowerShell `test.ps1` ouvre un dialogue de sélection de fichier pour faciliter l'utilisation :

```powershell
.\test.ps1
```

Le script va :

1. Ouvrir un dialogue de sélection de fichier
2. Permettre de choisir votre fichier OpenAPI (.yaml, .yml, .json)
3. Exécuter automatiquement la transformation
4. Proposer d'ouvrir l'explorateur sur le fichier de sortie
5. Afficher un aperçu du résultat

### Méthode 2 : Exécution directe

```bash
cd poc_openapi_transformation_apim
dotnet run "C:\chemin\vers\votre\fichier.yaml"
```

ou

```bash
dotnet build
./bin/Debug/net9.0/poc_openapi_transformation_apim.exe "C:\chemin\vers\votre\fichier.yaml"
```

## Exemple de transformation

### Avant (exemple de structure attendue)

```yaml
components:
  securitySchemes:
    apiKeyHeader:
      type: apiKey
      name: Ocp-Apim-Subscription-Key
      in: header
    apiKeyQuery:
      type: apiKey
      name: subscription-key
      in: query
security:
  - apiKeyHeader: []
  - apiKeyQuery: []
```

### Après

```yaml
components:
  securitySchemes:
    oauth2:
      type: oauth2
      description: OAuth2 Bearer Token
      flows:
        clientCredentials:
          tokenUrl: https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token
          scopes:
            https://graph.microsoft.com/.default: Accès aux APIs

security:
  - oauth2:
      - https://graph.microsoft.com/.default

paths:
  /exemple:
    get:
      parameters:
        - name: Ocp-Apim-Subscription-Key
          in: header
          required: false
          description: Clé de souscription API (apiKeyHeader)
          schema:
            type: string
        - name: subscription-key
          in: query
          required: false
          description: Clé de souscription API (apiKeyQuery)
          schema:
            type: string
```

## Formats supportés

- **Entrée** : YAML (.yaml, .yml) ou JSON (.json)
- **Sortie** : Toujours en format YAML avec le suffixe `_modified`

## Packages utilisés

- `Microsoft.OpenApi` v1.6.24
- `Microsoft.OpenApi.Readers` v1.6.24

## Structure du projet

```
poc_openapi_transformation_apim/
├── Program.cs                          # Code principal
├── poc_openapi_transformation_apim.csproj # Configuration du projet
├── test.ps1                            # Script de test avec dialogue de fichier
├── exemple_openapi.yaml                # Fichier d'exemple
└── README.md                           # Documentation
```

## Notes importantes

- Le fichier de sortie est créé dans le **même répertoire** que le fichier d'entrée
- Les fichiers de sortie existants sont automatiquement écrasés
- Le programme supporte les chemins avec espaces (encadrez-les de guillemets)
- En cas d'erreur, des messages détaillés sont affichés pour faciliter le débogage
