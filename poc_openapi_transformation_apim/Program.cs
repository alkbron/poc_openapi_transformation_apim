using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;
using System.Text;

namespace OpenApiTransformation
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Début de la transformation OpenAPI...");

                // 1. Vérifier les arguments
                string inputPath;
                if (args.Length > 0)
                {
                    inputPath = args[0];
                    Console.WriteLine($"Fichier spécifié en paramètre : {inputPath}");
                }
                else
                {
                    // Fallback vers le chemin par défaut si aucun argument
                    inputPath = @"C:\temp\API Weather Forecast.openapi.yaml";
                    Console.WriteLine($"Aucun fichier spécifié, utilisation du chemin par défaut : {inputPath}");
                }

                if (!File.Exists(inputPath))
                {
                    Console.WriteLine($"Erreur : Le fichier {inputPath} n'existe pas.");
                    Console.WriteLine("Usage : dotnet run <chemin_vers_fichier_openapi>");
                    return;
                }

                string openApiContent = await File.ReadAllTextAsync(inputPath);
                var reader = new OpenApiStringReader();
                var openApiDocument = reader.Read(openApiContent, out var diagnostic);

                if (diagnostic.Errors.Count > 0)
                {
                    Console.WriteLine("Erreurs lors de la lecture du fichier OpenAPI :");
                    foreach (var error in diagnostic.Errors)
                    {
                        Console.WriteLine($"- {error}");
                    }
                    return;
                }

                Console.WriteLine("Fichier OpenAPI lu avec succès.");

                // 2. Extraire les security schemes existants et les transformer en paramètres
                var existingSecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>();
                if (openApiDocument.Components?.SecuritySchemes != null)
                {
                    foreach (var scheme in openApiDocument.Components.SecuritySchemes)
                    {
                        if (scheme.Value.Type == SecuritySchemeType.ApiKey)
                        {
                            existingSecuritySchemes.Add(scheme.Key, scheme.Value);
                        }
                    }
                }

                // 3. Ajouter ces paramètres à chaque opération
                foreach (var pathItem in openApiDocument.Paths.Values)
                {
                    var operations = new List<OpenApiOperation>();

                    // Collecter toutes les opérations existantes
                    if (pathItem.Operations.TryGetValue(OperationType.Get, out var getOp) && getOp != null)
                        operations.Add(getOp);
                    if (pathItem.Operations.TryGetValue(OperationType.Post, out var postOp) && postOp != null)
                        operations.Add(postOp);
                    if (pathItem.Operations.TryGetValue(OperationType.Put, out var putOp) && putOp != null)
                        operations.Add(putOp);
                    if (pathItem.Operations.TryGetValue(OperationType.Delete, out var deleteOp) && deleteOp != null)
                        operations.Add(deleteOp);
                    if (pathItem.Operations.TryGetValue(OperationType.Patch, out var patchOp) && patchOp != null)
                        operations.Add(patchOp);
                    if (pathItem.Operations.TryGetValue(OperationType.Head, out var headOp) && headOp != null)
                        operations.Add(headOp);
                    if (pathItem.Operations.TryGetValue(OperationType.Options, out var optionsOp) && optionsOp != null)
                        operations.Add(optionsOp);
                    if (pathItem.Operations.TryGetValue(OperationType.Trace, out var traceOp) && traceOp != null)
                        operations.Add(traceOp);

                    foreach (var operation in operations)
                    {
                        if (operation.Parameters == null)
                            operation.Parameters = new List<OpenApiParameter>();

                        // Ajouter les paramètres apiKey comme paramètres facultatifs
                        foreach (var securityScheme in existingSecuritySchemes)
                        {
                            var parameterLocation = securityScheme.Value.In == ParameterLocation.Header
                                ? ParameterLocation.Header
                                : ParameterLocation.Query;

                            // Vérifier si le paramètre n'existe pas déjà
                            bool parameterExists = operation.Parameters.Any(p =>
                                p.Name == securityScheme.Value.Name && p.In == parameterLocation);

                            if (!parameterExists)
                            {
                                var parameter = new OpenApiParameter
                                {
                                    Name = securityScheme.Value.Name,
                                    In = parameterLocation,
                                    Required = false, // Facultatif
                                    Description = $"Clé de souscription API ({securityScheme.Key})",
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "string"
                                    }
                                };

                                operation.Parameters.Add(parameter);
                            }
                        }
                    }
                }

                Console.WriteLine("Paramètres API Key ajoutés aux opérations.");

                // 4. Supprimer les anciens security schemes et security requirements
                if (openApiDocument.Components?.SecuritySchemes != null)
                {
                    var apiKeySchemes = openApiDocument.Components.SecuritySchemes
                        .Where(s => s.Value.Type == SecuritySchemeType.ApiKey)
                        .Select(s => s.Key)
                        .ToList();

                    foreach (var scheme in apiKeySchemes)
                    {
                        openApiDocument.Components.SecuritySchemes.Remove(scheme);
                    }
                }

                // Supprimer les security requirements liés aux API Keys
                if (openApiDocument.SecurityRequirements != null)
                {
                    var requirementsToRemove = openApiDocument.SecurityRequirements
                        .Where(req => req.Keys.Any(key => key.Reference?.Id != null && existingSecuritySchemes.ContainsKey(key.Reference.Id)))
                        .ToList();

                    foreach (var req in requirementsToRemove)
                    {
                        openApiDocument.SecurityRequirements.Remove(req);
                    }
                }

                // 5. Ajouter le nouveau security scheme OAuth2 Bearer Token
                if (openApiDocument.Components == null)
                    openApiDocument.Components = new OpenApiComponents();

                if (openApiDocument.Components.SecuritySchemes == null)
                    openApiDocument.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>();

                var oauth2SecurityScheme = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri("https://inte-oauth.cegid.com/authorize"),
                            TokenUrl = new Uri("https://inte-oauth.cegid.com/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                { "sampleapp.weather.read", "Accès aux APIs" }
                            }
                        }
                    },
                    Description = "OAuth2 Bearer Token"
                };

                openApiDocument.Components.SecuritySchemes["oauth2"] = oauth2SecurityScheme;

                // Ajouter le security requirement OAuth2 au niveau global
                if (openApiDocument.SecurityRequirements == null)
                    openApiDocument.SecurityRequirements = new List<OpenApiSecurityRequirement>();

                var oauth2Requirement = new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "oauth2"
                            }
                        },
                        new List<string> { "sampleapp.weather.read" }
                    }
                };

                openApiDocument.SecurityRequirements.Add(oauth2Requirement);

                Console.WriteLine("Security scheme OAuth2 ajouté.");

                // 6. Sauvegarder le fichier modifié
                var inputFileName = Path.GetFileNameWithoutExtension(inputPath);
                var inputDirectory = Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory();
                string outputPath = Path.Combine(inputDirectory, $"{inputFileName}_modified.yaml");

                using var outputStream = new FileStream(outputPath, FileMode.Create);
                using var writer = new StreamWriter(outputStream);
                var yamlWriter = new OpenApiYamlWriter(writer);
                openApiDocument.SerializeAsV3(yamlWriter);

                Console.WriteLine($"Fichier OpenAPI modifié sauvegardé dans : {outputPath}");
                Console.WriteLine("Transformation terminée avec succès !");

                // Afficher un résumé des changements
                Console.WriteLine("\nRésumé des modifications :");
                Console.WriteLine($"- {existingSecuritySchemes.Count} security schemes API Key transformés en paramètres facultatifs");
                Console.WriteLine("- Security scheme OAuth2 Bearer Token ajouté");
                Console.WriteLine("- Security requirements API Key supprimés");
                Console.WriteLine("- Security requirement OAuth2 ajouté au niveau global");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la transformation : {ex.Message}");
                Console.WriteLine($"Stack trace : {ex.StackTrace}");
            }
        }
    }
}
