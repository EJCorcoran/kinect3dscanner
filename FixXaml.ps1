# Fix XAML Compilation Issues
# This script cleans and rebuilds the WPF project to resolve InitializeComponent errors

param(
    [string]$Configuration = "Debug"
)

Write-Host "Fixing XAML Compilation Issues..." -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

$ProjectPath = "src/Scanner.UI/Scanner.UI.csproj"

try {
    Write-Host "1. Cleaning project..." -ForegroundColor Yellow
    dotnet clean $ProjectPath --configuration $Configuration

    Write-Host "2. Removing obj and bin directories..." -ForegroundColor Yellow
    $objPath = "src/Scanner.UI/obj"
    $binPath = "src/Scanner.UI/bin"
    
    if (Test-Path $objPath) { 
        Remove-Item $objPath -Recurse -Force 
        Write-Host "   Removed $objPath" -ForegroundColor Gray
    }
    if (Test-Path $binPath) { 
        Remove-Item $binPath -Recurse -Force 
        Write-Host "   Removed $binPath" -ForegroundColor Gray
    }

    Write-Host "3. Restoring packages..." -ForegroundColor Yellow
    dotnet restore $ProjectPath

    Write-Host "4. Building project..." -ForegroundColor Yellow
    dotnet build $ProjectPath --configuration $Configuration --verbosity minimal

    Write-Host "XAML compilation fix completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "If you're still seeing InitializeComponent errors in VS Code:" -ForegroundColor Cyan
    Write-Host "1. Restart VS Code (Ctrl+Shift+P -> 'Developer: Reload Window')" -ForegroundColor White
    Write-Host "2. Wait for C# extension to reload project files" -ForegroundColor White

} catch {
    Write-Host "Fix failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
