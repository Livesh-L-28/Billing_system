# Setup Script for BillingSoftware .NET 8 WPF Solution
# Execute this in PowerShell from the solution root: c:\Users\katana\Downloads\BILLING

# Exit on error
$ErrorActionPreference = "Stop"

Write-Host "Creating solution..."
dotnet new sln -n BillingSoftware

Write-Host "Creating projects..."
# Class libraries
dotnet new classlib -n BillingSoftware.Core -f net8.0
dotnet new classlib -n BillingSoftware.Application -f net8.0
dotnet new classlib -n BillingSoftware.Infrastructure -f net8.0
dotnet new classlib -n BillingSoftware.Database -f net8.0
dotnet new classlib -n BillingSoftware.Reporting -f net8.0
dotnet new classlib -n BillingSoftware.Printing -f net8.0
dotnet new classlib -n BillingSoftware.Backup -f net8.0
dotnet new classlib -n BillingSoftware.Common -f net8.0

# WPF App
dotnet new wpf -n BillingSoftware.UI -f net8.0-windows

# XUnit Tests
dotnet new xunit -n BillingSoftware.Tests -f net8.0

Write-Host "Adding projects to solution..."
dotnet sln BillingSoftware.sln add BillingSoftware.Core/BillingSoftware.Core.csproj
dotnet sln BillingSoftware.sln add BillingSoftware.Application/BillingSoftware.Application.csproj
dotnet sln BillingSoftware.sln add BillingSoftware.Infrastructure/BillingSoftware.Infrastructure.csproj
dotnet sln BillingSoftware.sln add BillingSoftware.Database/BillingSoftware.Database.csproj
dotnet sln BillingSoftware.sln add BillingSoftware.Reporting/BillingSoftware.Reporting.csproj
dotnet sln BillingSoftware.sln add BillingSoftware.Printing/BillingSoftware.Printing.csproj
dotnet sln BillingSoftware.sln add BillingSoftware.Backup/BillingSoftware.Backup.csproj
dotnet sln BillingSoftware.sln add BillingSoftware.Common/BillingSoftware.Common.csproj
dotnet sln BillingSoftware.sln add BillingSoftware.UI/BillingSoftware.UI.csproj
dotnet sln BillingSoftware.sln add BillingSoftware.Tests/BillingSoftware.Tests.csproj

Write-Host "Setting up project references according to Clean Architecture..."
# Core references Common
dotnet add BillingSoftware.Core/BillingSoftware.Core.csproj reference BillingSoftware.Common/BillingSoftware.Common.csproj

# Application references Core and Common
dotnet add BillingSoftware.Application/BillingSoftware.Application.csproj reference BillingSoftware.Core/BillingSoftware.Core.csproj
dotnet add BillingSoftware.Application/BillingSoftware.Application.csproj reference BillingSoftware.Common/BillingSoftware.Common.csproj

# Database references Core, Common
dotnet add BillingSoftware.Database/BillingSoftware.Database.csproj reference BillingSoftware.Core/BillingSoftware.Core.csproj
dotnet add BillingSoftware.Database/BillingSoftware.Database.csproj reference BillingSoftware.Common/BillingSoftware.Common.csproj

# Infrastructure references Application, Core, Database, Common
dotnet add BillingSoftware.Infrastructure/BillingSoftware.Infrastructure.csproj reference BillingSoftware.Application/BillingSoftware.Application.csproj
dotnet add BillingSoftware.Infrastructure/BillingSoftware.Infrastructure.csproj reference BillingSoftware.Core/BillingSoftware.Core.csproj
dotnet add BillingSoftware.Infrastructure/BillingSoftware.Infrastructure.csproj reference BillingSoftware.Database/BillingSoftware.Database.csproj
dotnet add BillingSoftware.Infrastructure/BillingSoftware.Infrastructure.csproj reference BillingSoftware.Common/BillingSoftware.Common.csproj

# Reporting references Application, Core, Common
dotnet add BillingSoftware.Reporting/BillingSoftware.Reporting.csproj reference BillingSoftware.Application/BillingSoftware.Application.csproj
dotnet add BillingSoftware.Reporting/BillingSoftware.Reporting.csproj reference BillingSoftware.Core/BillingSoftware.Core.csproj
dotnet add BillingSoftware.Reporting/BillingSoftware.Reporting.csproj reference BillingSoftware.Common/BillingSoftware.Common.csproj

# Printing references Application, Core, Common
dotnet add BillingSoftware.Printing/BillingSoftware.Printing.csproj reference BillingSoftware.Application/BillingSoftware.Application.csproj
dotnet add BillingSoftware.Printing/BillingSoftware.Printing.csproj reference BillingSoftware.Core/BillingSoftware.Core.csproj
dotnet add BillingSoftware.Printing/BillingSoftware.Printing.csproj reference BillingSoftware.Common/BillingSoftware.Common.csproj

# Backup references Application, Core, Common
dotnet add BillingSoftware.Backup/BillingSoftware.Backup.csproj reference BillingSoftware.Application/BillingSoftware.Application.csproj
dotnet add BillingSoftware.Backup/BillingSoftware.Backup.csproj reference BillingSoftware.Core/BillingSoftware.Core.csproj
dotnet add BillingSoftware.Backup/BillingSoftware.Backup.csproj reference BillingSoftware.Common/BillingSoftware.Common.csproj

# UI references Infrastructure, Application, Core, Database, Reporting, Printing, Backup, Common
dotnet add BillingSoftware.UI/BillingSoftware.UI.csproj reference BillingSoftware.Infrastructure/BillingSoftware.Infrastructure.csproj
dotnet add BillingSoftware.UI/BillingSoftware.UI.csproj reference BillingSoftware.Application/BillingSoftware.Application.csproj
dotnet add BillingSoftware.UI/BillingSoftware.UI.csproj reference BillingSoftware.Core/BillingSoftware.Core.csproj
dotnet add BillingSoftware.UI/BillingSoftware.UI.csproj reference BillingSoftware.Database/BillingSoftware.Database.csproj
dotnet add BillingSoftware.UI/BillingSoftware.UI.csproj reference BillingSoftware.Reporting/BillingSoftware.Reporting.csproj
dotnet add BillingSoftware.UI/BillingSoftware.UI.csproj reference BillingSoftware.Printing/BillingSoftware.Printing.csproj
dotnet add BillingSoftware.UI/BillingSoftware.UI.csproj reference BillingSoftware.Backup/BillingSoftware.Backup.csproj
dotnet add BillingSoftware.UI/BillingSoftware.UI.csproj reference BillingSoftware.Common/BillingSoftware.Common.csproj

# Tests references UI, Application, Core, Infrastructure, Database, Reporting, Common
dotnet add BillingSoftware.Tests/BillingSoftware.Tests.csproj reference BillingSoftware.UI/BillingSoftware.UI.csproj
dotnet add BillingSoftware.Tests/BillingSoftware.Tests.csproj reference BillingSoftware.Application/BillingSoftware.Application.csproj
dotnet add BillingSoftware.Tests/BillingSoftware.Tests.csproj reference BillingSoftware.Core/BillingSoftware.Core.csproj
dotnet add BillingSoftware.Tests/BillingSoftware.Tests.csproj reference BillingSoftware.Infrastructure/BillingSoftware.Infrastructure.csproj
dotnet add BillingSoftware.Tests/BillingSoftware.Tests.csproj reference BillingSoftware.Database/BillingSoftware.Database.csproj
dotnet add BillingSoftware.Tests/BillingSoftware.Tests.csproj reference BillingSoftware.Common/BillingSoftware.Common.csproj

Write-Host "Installing NuGet packages..."
# CommunityToolkit.Mvvm for UI and Application
dotnet add BillingSoftware.UI/BillingSoftware.UI.csproj package CommunityToolkit.Mvvm --version 8.2.2
dotnet add BillingSoftware.Application/BillingSoftware.Application.csproj package CommunityToolkit.Mvvm --version 8.2.2

# Microsoft.Extensions.DependencyInjection for Dependency Injection in UI
dotnet add BillingSoftware.UI/BillingSoftware.UI.csproj package Microsoft.Extensions.DependencyInjection --version 8.0.0

# EF Core SQLite in Database and Infrastructure
dotnet add BillingSoftware.Database/BillingSoftware.Database.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.4
dotnet add BillingSoftware.Infrastructure/BillingSoftware.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.4
dotnet add BillingSoftware.Database/BillingSoftware.Database.csproj package Microsoft.EntityFrameworkCore.Design --version 8.0.4
dotnet add BillingSoftware.UI/BillingSoftware.UI.csproj package Microsoft.EntityFrameworkCore.Design --version 8.0.4

# QuestPDF for Printing
dotnet add BillingSoftware.Printing/BillingSoftware.Printing.csproj package QuestPDF --version 2024.3.3

# EPPlus for Excel import/export in Reporting and Application
dotnet add BillingSoftware.Reporting/BillingSoftware.Reporting.csproj package EPPlus --version 7.1.1
dotnet add BillingSoftware.Application/BillingSoftware.Application.csproj package EPPlus --version 7.1.1

# Serilog for Logging in Infrastructure
dotnet add BillingSoftware.Infrastructure/BillingSoftware.Infrastructure.csproj package Serilog --version 4.0.0
dotnet add BillingSoftware.Infrastructure/BillingSoftware.Infrastructure.csproj package Serilog.Sinks.File --version 5.0.0
dotnet add BillingSoftware.Infrastructure/BillingSoftware.Infrastructure.csproj package Serilog.Sinks.Console --version 5.0.1

Write-Host "Verification: building solution..."
dotnet build BillingSoftware.sln

Write-Host "Success!"
