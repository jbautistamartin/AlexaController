<#
.SYNOPSIS
    Compila las tres piezas de AlexaController, arma dist/ y publica el release en GitHub.

.DESCRIPTION
    Una sola version manda en todo: la del csproj. El script la lee de ahi y comprueba que
    la aplicacion Android declare la misma, en lugar de aceptar un numero por parametro,
    porque un release cuyos artefactos no coinciden entre si no hay forma de rastrearlo
    despues.

    Artefactos que deja en dist/:
      AlexaController-<version>-win-x64.zip   API autocontenida, ejecutable unico
      gamecontroller-<version>.apk            aplicacion Android firmada
      lambda-alexacontroller-<version>.zip    manejador del skill para AWS Lambda
      intents-<version>.json                  modelo de interaccion de Alexa

.PARAMETER SoloCompilar
    Compila y arma dist/ sin tocar git ni GitHub. Para comprobar que todo sale bien antes
    de etiquetar nada.

.PARAMETER Borrador
    Crea el release en GitHub como borrador, para revisar las notas antes de publicarlo.

.EXAMPLE
    .\release.ps1 -SoloCompilar
    .\release.ps1
#>
[CmdletBinding()]
param(
    [switch]$SoloCompilar,
    [switch]$Borrador
)

$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot
$dist = Join-Path $raiz 'dist'

function Paso($texto) { Write-Host "`n=== $texto ===" -ForegroundColor Cyan }
function Aviso($texto) { Write-Host "  ! $texto" -ForegroundColor Yellow }
function Bien($texto) { Write-Host "  + $texto" -ForegroundColor Green }

# --------------------------------------------------------------------------------------
# Version: el csproj manda, el resto tiene que coincidir
# --------------------------------------------------------------------------------------
Paso 'Version'

$csproj = Join-Path $raiz 'AlexaController\AlexaController.csproj'
$version = ([xml](Get-Content $csproj)).Project.PropertyGroup.Version | Where-Object { $_ }
if (-not $version) { throw "No se encuentra <Version> en $csproj" }

$gradle = Get-Content (Join-Path $raiz 'GameController\app\build.gradle.kts') -Raw
if ($gradle -notmatch 'versionName\s*=\s*"([^"]+)"') { throw 'No se encuentra versionName en build.gradle.kts' }
$versionAndroid = $Matches[1]
if ($versionAndroid -ne $version) {
    throw "Las versiones no coinciden: csproj dice $version y la aplicacion Android $versionAndroid"
}

$tag = "v$version"
Bien "version $version (tag $tag)"

# --------------------------------------------------------------------------------------
# Comprobaciones previas: mejor fallar aqui que a medio release
# --------------------------------------------------------------------------------------
if (-not $SoloCompilar) {
    Paso 'Comprobaciones previas'

    if (git status --porcelain) { throw 'Hay cambios sin commitear. El tag debe apuntar a un arbol limpio.' }
    if (git tag --list $tag) { throw "El tag $tag ya existe." }

    gh auth status *> $null
    if ($LASTEXITCODE -ne 0) { throw 'gh no esta autenticado. Ejecuta: gh auth login' }
    Bien 'arbol limpio, tag libre, gh autenticado'
}

# El almacen de claves vive fuera del repositorio y lo comparten las demas aplicaciones:
# sin el no hay APK firmado, y conviene saberlo antes de compilar la API durante un minuto.
$localProps = Join-Path $raiz 'GameController\local.properties'
if (-not (Test-Path $localProps)) { throw "Falta $localProps con las claves de firma." }
$rutaAlmacen = (Select-String -Path $localProps -Pattern '^KEYSTORE_PATH=(.+)$').Matches.Groups[1].Value
# KEYSTORE_PATH se escribe relativa al modulo app/, que es donde la resuelve el build.
$almacen = Join-Path $raiz "GameController\app\$rutaAlmacen"
if (-not (Test-Path $almacen)) { throw "No se encuentra el almacen de claves: $almacen" }
Bien "almacen de claves localizado"

New-Item -ItemType Directory -Force -Path $dist | Out-Null
Get-ChildItem $dist -File | Remove-Item -Force

# --------------------------------------------------------------------------------------
# 1. API .NET
# --------------------------------------------------------------------------------------
Paso '1/4  API .NET (win-x64, autocontenida, ReadyToRun)'

$pubApi = Join-Path $dist 'api'
# ReadyToRun precompila a codigo nativo: abulta mas, pero el arranque deja de pagar el JIT.
# En una API que se lanza al iniciar sesion y recibe la primera orden por voz poco despues,
# ese primer arranque es justo el que se nota.
dotnet publish $csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    --output $pubApi `
    --nologo
if ($LASTEXITCODE -ne 0) { throw 'Fallo la publicacion de la API' }

# Fuera lo que no le sirve a quien instala esto: simbolos de depuracion, el web.config (solo
# aplica bajo IIS, que aqui no hay), la configuracion de Development y el manifiesto de
# herramientas de dotnet. El SDK Web los arrastra al publicar por su glob de appsettings*.json.
Get-ChildItem $pubApi -Recurse -Include '*.pdb', 'web.config', 'appsettings.Development.json', 'dotnet-tools.json' |
    Remove-Item -Force
Copy-Item (Join-Path $raiz 'LICENSE') $pubApi -Force

$zipApi = Join-Path $dist "AlexaController-$version-win-x64.zip"
Compress-Archive -Path "$pubApi\*" -DestinationPath $zipApi -Force
Remove-Item $pubApi -Recurse -Force
Bien "$(Split-Path $zipApi -Leaf)  ($([math]::Round((Get-Item $zipApi).Length / 1MB, 1)) MB)"

# --------------------------------------------------------------------------------------
# 2. Aplicacion Android
# --------------------------------------------------------------------------------------
Paso '2/4  Aplicacion Android (APK firmado)'

Push-Location (Join-Path $raiz 'GameController')
try {
    # Gradle necesita un JDK para arrancar, no un JRE. En esta maquina JAVA_HOME apunta a un
    # JRE, asi que el JDK va fijado en ~/.gradle/gradle.properties (org.gradle.java.home).
    & '.\gradlew.bat' assembleRelease --console=plain
    if ($LASTEXITCODE -ne 0) { throw 'Fallo assembleRelease' }
} finally { Pop-Location }

$apk = Join-Path $raiz "GameController\app\build\outputs\apk\release\gamecontroller-$version.apk"
if (-not (Test-Path $apk)) { throw "No se genero el APK esperado: $apk" }
Copy-Item $apk $dist -Force
Bien "$(Split-Path $apk -Leaf)  ($([math]::Round((Get-Item $apk).Length / 1MB, 1)) MB)"

# --------------------------------------------------------------------------------------
# 3. Lambda
# --------------------------------------------------------------------------------------
Paso '3/4  Funcion Lambda'

# El contenido de src/ va en la raiz del zip: AWS busca lambda_function.lambda_handler ahi.
# No hace falta instalar dependencias, el manejador solo usa la biblioteca estandar.
$zipLambda = Join-Path $dist "lambda-alexacontroller-$version.zip"
Compress-Archive -Path (Join-Path $raiz 'lambda\AlexaController\src\*') -DestinationPath $zipLambda -Force
Bien "$(Split-Path $zipLambda -Leaf)  ($([math]::Round((Get-Item $zipLambda).Length / 1KB, 1)) KB)"

# --------------------------------------------------------------------------------------
# 4. Modelo del skill
# --------------------------------------------------------------------------------------
Paso '4/4  Modelo del skill'

# Se copia tal cual: el esquema del modelo de interaccion solo admite la clave
# interactionModel, de modo que la version va en el nombre del fichero y no dentro.
$jsonSkill = Join-Path $dist "intents-$version.json"
Copy-Item (Join-Path $raiz 'AlexaControllerSkill\intents.json') $jsonSkill -Force
Bien "$(Split-Path $jsonSkill -Leaf)"

# --------------------------------------------------------------------------------------
# Publicacion
# --------------------------------------------------------------------------------------
if ($SoloCompilar) {
    Paso 'Listo (-SoloCompilar: no se ha tocado git ni GitHub)'
    Get-ChildItem $dist -File | Format-Table Name, @{N = 'MB'; E = { [math]::Round($_.Length / 1MB, 2) } }
    return
}

Paso "Publicando $tag en GitHub"

$notas = Join-Path $raiz 'docs\NOTAS_RELEASE.md'
if (-not (Test-Path $notas)) { throw "Faltan las notas del release: $notas" }

git tag -a $tag -m "AlexaController $version"
if ($LASTEXITCODE -ne 0) { throw "No se pudo crear el tag $tag" }
git push origin $tag
if ($LASTEXITCODE -ne 0) { throw "No se pudo subir el tag $tag" }

$argumentos = @(
    'release', 'create', $tag,
    '--title', "AlexaController $version",
    '--notes-file', $notas
)
if ($Borrador) { $argumentos += '--draft' }
$argumentos += (Get-ChildItem $dist -File | ForEach-Object { $_.FullName })

gh @argumentos
if ($LASTEXITCODE -ne 0) { throw 'Fallo gh release create' }

Bien "release $tag publicado"
gh release view $tag --json url --jq '.url'
