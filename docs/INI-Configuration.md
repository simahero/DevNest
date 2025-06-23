# DevNest INI Configuration

DevNest now uses INI format for configuration instead of JSON. The configuration file is located at `C:\DevNest\settings.ini`.

## Configuration File Structure

### [General] Section

Contains general application settings:

```ini
[General]
StartWithWindows = false          # Launch DevNest when Windows starts
MinimizeToSystemTray = false      # Minimize to system tray instead of taskbar
AutoVirtualHosts = true           # Automatically create virtual hosts for new sites
AutoCreateDatabase = false        # Automatically create databases for new projects
InstallDirectory = C:\DevNest     # Base installation directory for DevNest
```

### [Versions] Section

Tracks installed software versions:

```ini
[Versions]
Apache =
MySQL =
PHP =
Node =
Redis =
PostgreSQL =
Nginx =
```

### [SiteTypes] Section

Defines available site types for project creation:

```ini
[SiteTypes]
Blank.Name = Blank
Blank.InstallType = none

Wordpress.Name = Wordpress
Wordpress.InstallType = download
Wordpress.Url = https://wordpress.org/latest.zip
Wordpress.HasAdditionalDir = true

Laravel.Name = Laravel
Laravel.InstallType = command
Laravel.Command = composer create-project laravel/laravel %s --prefer-dist
```

## Site Type Properties

Each site type can have the following properties:

-   **Name**: Display name of the site type
-   **InstallType**: How to install the site type
    -   `none`: No installation required (blank project)
    -   `download`: Download and extract from URL
    -   `command`: Run a shell command to create the project
-   **Url**: Download URL (for `download` install type)
-   **Command**: Shell command to execute (for `command` install type)
    -   Use `%s` as placeholder for site name
-   **HasAdditionalDir**: Whether the downloaded archive contains an additional directory level

## Benefits of INI Format

1. **Human Readable**: Easy to read and edit manually
2. **Version Control Friendly**: Better diff support in Git
3. **Lightweight**: Smaller file size and faster parsing
4. **Standard Format**: Well-established configuration format
5. **Comment Support**: Can add comments with `;` or `#`

## Migration

The application automatically migrates from the old JSON format to INI format on first startup. The original JSON file is backed up as `settings.json.backup`.
