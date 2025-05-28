# Script de test pour le programme de transformation OpenAPI

Write-Host "🚀 Script de test pour la transformation OpenAPI" -ForegroundColor Green

# Charger les assemblies nécessaires pour le dialogue de fichier
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Créer et configurer le dialogue de sélection de fichier
$openFileDialog = New-Object System.Windows.Forms.OpenFileDialog
$openFileDialog.InitialDirectory = "C:\temp"
$openFileDialog.Filter = "Fichiers OpenAPI (*.yaml;*.yml;*.json)|*.yaml;*.yml;*.json|Tous les fichiers (*.*)|*.*"
$openFileDialog.FilterIndex = 1
$openFileDialog.Title = "Sélectionnez votre fichier OpenAPI"
$openFileDialog.Multiselect = $false

Write-Host "📁 Ouverture du dialogue de sélection de fichier..." -ForegroundColor Yellow
Write-Host "   Sélectionnez votre fichier OpenAPI (.yaml, .yml ou .json)" -ForegroundColor Cyan

# Afficher le dialogue et récupérer le résultat
$dialogResult = $openFileDialog.ShowDialog()

if ($dialogResult -eq [System.Windows.Forms.DialogResult]::OK) {
    $selectedFile = $openFileDialog.FileName
    Write-Host "✅ Fichier sélectionné: $selectedFile" -ForegroundColor Green
    
    # Vérifier si le fichier existe vraiment
    if (-not (Test-Path $selectedFile)) {
        Write-Host "❌ Erreur: Le fichier sélectionné n'existe pas!" -ForegroundColor Red
        exit 1
    }
    
    # Nettoyer les anciens fichiers de sortie dans le répertoire du fichier source
    $sourceDirectory = Split-Path $selectedFile -Parent
    $sourceFileName = [System.IO.Path]::GetFileNameWithoutExtension($selectedFile)
    $outputFile = Join-Path $sourceDirectory "$sourceFileName`_modified.yaml"
    
    if (Test-Path $outputFile) {
        Write-Host "🧹 Suppression de l'ancien fichier de sortie..." -ForegroundColor Yellow
        Remove-Item $outputFile -Force
    }
    
    # Naviguer vers le dossier du projet
    Set-Location ".\poc_openapi_transformation_apim"
    
    # Exécuter le programme avec le fichier sélectionné
    Write-Host "▶️ Exécution du programme de transformation..." -ForegroundColor Cyan
    Write-Host "   Fichier d'entrée: $selectedFile" -ForegroundColor Cyan
    Write-Host "   Fichier de sortie attendu: $outputFile" -ForegroundColor Cyan
    Write-Host "================================================" -ForegroundColor Cyan
    
    dotnet run -- "$selectedFile"
    
    Write-Host "================================================" -ForegroundColor Cyan
    
    # Revenir au dossier racine
    Set-Location ".."
    
    # Vérifier si le fichier de sortie a été créé
    if (Test-Path $outputFile) {
        Write-Host "✅ Transformation terminée! Fichier créé: $outputFile" -ForegroundColor Green
        
        # Proposer d'ouvrir le fichier de sortie dans l'explorateur
        Write-Host "📂 Voulez-vous ouvrir le dossier contenant le fichier transformé? (O/N)" -ForegroundColor Yellow
        $openFolder = Read-Host
        
        if ($openFolder -match "^[OoYy]") {
            Start-Process "explorer.exe" -ArgumentList "/select,`"$outputFile`""
            Write-Host "📁 Explorateur ouvert sur le fichier de sortie" -ForegroundColor Green
        }
        
        # Afficher un aperçu du fichier
        Write-Host "📄 Aperçu du fichier transformé (50 premières lignes):" -ForegroundColor Cyan
        Write-Host "================================================" -ForegroundColor Cyan
        Get-Content $outputFile | Select-Object -First 50
        Write-Host "================================================" -ForegroundColor Cyan
        
        Write-Host "💡 Pour voir le fichier complet: Get-Content '$outputFile'" -ForegroundColor Yellow
    } else {
        Write-Host "❌ Erreur: Le fichier de sortie n'a pas été créé!" -ForegroundColor Red
    }
    
} else {
    Write-Host "❌ Aucun fichier sélectionné. Opération annulée." -ForegroundColor Red
    exit 1
}

Write-Host "🎉 Test terminé!" -ForegroundColor Green 