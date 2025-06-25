# DevNest - Development Environment Manager

DevNest is a WinUI3 application designed to help developers manage their local development environment, including services, sites, and database dumps.

## Project Structure

The solution follows Clean Architecture principles with clear separation of concerns:

```
DevNest/
├── src/
│   ├── DevNest.Core/                    # Core business logic and models
│   │   ├── Models/                      # Domain models (Service, Site, DumpFile, AppSettings)
│   │   ├── Interfaces/                  # Service interfaces
│   │   └── Exceptions/                  # Custom exceptions
│   │
│   ├── DevNest.Services/               # Business services implementation
│   │   ├── ServiceManager.cs           # Service management
│   │   ├── SiteService.cs              # Site installation/management
│   │   ├── FileSystemService.cs        # File system operations
│   │   └── SettingsManager.cs       # Application settings
│   │
│   ├── DevNest.Data/                   # Data access layer
│   │   ├── Repositories/               # Data repositories
│   │   └── Context/                    # Data context
│   │
│   └── DevNest.UI/                     # WinUI3 presentation layer
│       ├── Views/                      # XAML pages
│       ├── ViewModels/                 # MVVM ViewModels
│       ├── Controls/                   # Custom user controls
│       ├── Converters/                 # Value converters
│       ├── Helpers/                    # UI helpers
│       ├── Assets/                     # Images and icons
│       └── Themes/                     # Styling resources
│
├── tests/                              # Unit and integration tests
│   ├── DevNest.Core.Tests/
│   ├── DevNest.Services.Tests/
│   └── DevNest.UI.Tests/
│
└── docs/                               # Documentation
```

## Architecture Patterns Used

### 1. **Clean Architecture**

-   **Core Layer**: Contains business entities and interfaces
-   **Services Layer**: Business logic implementation
-   **Data Layer**: Data access and persistence
-   **UI Layer**: User interface and presentation logic

### 2. **MVVM (Model-View-ViewModel)**

-   Views are XAML files
-   ViewModels handle UI logic and data binding
-   Models represent business entities
-   Command pattern for user actions

### 3. **Dependency Injection**

-   Services are registered in the DI container
-   ViewModels receive dependencies through constructor injection
-   Loose coupling between layers

### 4. **Repository Pattern**

-   Data access abstraction
-   Easy to test and mock
-   Consistent data access interface

## Key Features

-   **Service Management**: Start/stop development services (Apache, MySQL, etc.)
-   **Site Installation**: Install and manage development sites
-   **Database Dumps**: Create, import, and manage database dumps
-   **Settings Management**: Configure application preferences
-   **System Tray Integration**: Minimize to system tray

## Dependencies

-   .NET 8.0
-   WinUI 3
-   CommunityToolkit.Mvvm
-   Microsoft.Extensions.Hosting
-   Microsoft.Extensions.DependencyInjection
-   WinUIEx

## Getting Started

1. Open `DevNest-New.sln` in Visual Studio 2022
2. Set `DevNest.UI` as the startup project
3. Build and run the application

## Project Benefits

-   **Maintainability**: Clear separation of concerns
-   **Testability**: Easy to unit test with dependency injection
-   **Scalability**: Modular architecture allows easy feature additions
-   **Reusability**: Core logic separated from UI
-   **Best Practices**: Follows modern .NET and WinUI3 patterns

## Migration Notes

This restructure moves from a single-project solution to a multi-project solution following Clean Architecture principles. The original functionality is preserved while improving code organization and maintainability.
