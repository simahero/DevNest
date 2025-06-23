using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser;
using IniParser.Model;
using IniParser.Parser;

namespace DevNest.Services
{
    public class SitesReader : ISitesReaderService
    {
        private static readonly string SitesFilePath = @"C:\DevNest\sites.ini";
        private readonly IFileSystemService _fileSystemService;
        private readonly FileIniDataParser _parser;

        public SitesReader(IFileSystemService fileSystemService)
        {
            _fileSystemService = fileSystemService;
            _parser = new FileIniDataParser();
        }

        public async Task<IEnumerable<SiteType>> LoadSiteTypesAsync()
        {
            try
            {
                if (!await _fileSystemService.FileExistsAsync(SitesFilePath))
                {
                    return GetDefaultSiteTypes();
                }

                var content = await _fileSystemService.ReadAllTextAsync(SitesFilePath);
                var iniData = new IniDataParser().Parse(content);
                var siteTypes = ParseIniToSiteTypes(iniData);

                return siteTypes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load site types: {ex.Message}");
                return GetDefaultSiteTypes();
            }
        }

        public async Task SaveSiteTypesAsync(IEnumerable<SiteType> siteTypes)
        {
            try
            {
                var directory = System.IO.Path.GetDirectoryName(SitesFilePath);
                if (!string.IsNullOrEmpty(directory) && !await _fileSystemService.DirectoryExistsAsync(directory))
                {
                    await _fileSystemService.CreateDirectoryAsync(directory);
                }

                var iniData = ConvertSiteTypesToIni(siteTypes);
                var content = iniData.ToString();

                await _fileSystemService.WriteAllTextAsync(SitesFilePath, content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save site types: {ex.Message}");
                throw;
            }
        }

        private IEnumerable<SiteType> ParseIniToSiteTypes(IniData iniData)
        {
            var siteTypes = new List<SiteType>();

            if (iniData.Sections.ContainsSection("SiteTypes"))
            {
                var siteTypesSection = iniData.Sections["SiteTypes"];
                var siteTypeGroups = siteTypesSection.GroupBy(key => key.KeyName.Split('.')[0]);

                foreach (var group in siteTypeGroups)
                {
                    var siteType = new SiteType();
                    foreach (var key in group)
                    {
                        var property = key.KeyName.Split('.')[1];
                        switch (property)
                        {
                            case "Name":
                                siteType.Name = key.Value ?? string.Empty;
                                siteType.DisplayName = key.Value ?? string.Empty;
                                break;
                            case "InstallType":
                                siteType.InstallType = key.Value ?? string.Empty;
                                break;
                            case "Url":
                                siteType.Url = key.Value;
                                break;
                            case "Command":
                                siteType.Command = key.Value;
                                break;
                            case "HasAdditionalDir":
                                siteType.HasAdditionalDir = bool.Parse(key.Value ?? "false");
                                break;
                        }
                    }
                    siteType.IsEnabled = true;
                    siteTypes.Add(siteType);
                }
            }

            return siteTypes;
        }

        private IniData ConvertSiteTypesToIni(IEnumerable<SiteType> siteTypes)
        {
            var iniData = new IniData();

            iniData.Sections.AddSection("SiteTypes");
            var siteTypesSection = iniData.Sections["SiteTypes"];

            foreach (var siteType in siteTypes)
            {
                var prefix = siteType.Name.Replace(" ", "_");
                siteTypesSection.AddKey($"{prefix}.Name", siteType.DisplayName ?? siteType.Name);
                siteTypesSection.AddKey($"{prefix}.InstallType", siteType.InstallType);

                if (!string.IsNullOrEmpty(siteType.Url))
                    siteTypesSection.AddKey($"{prefix}.Url", siteType.Url);

                if (!string.IsNullOrEmpty(siteType.Command))
                    siteTypesSection.AddKey($"{prefix}.Command", siteType.Command);

                if (siteType.HasAdditionalDir)
                    siteTypesSection.AddKey($"{prefix}.HasAdditionalDir", siteType.HasAdditionalDir.ToString().ToLower());
            }

            return iniData;
        }

        private IEnumerable<SiteType> GetDefaultSiteTypes()
        {
            return new List<SiteType>
            {
                new SiteType
                {
                    Name = "Blank",
                    DisplayName = "Blank",
                    InstallType = "none",
                    IsEnabled = true
                },
                new SiteType
                {
                    Name = "Wordpress",
                    DisplayName = "WordPress",
                    InstallType = "download",
                    Url = "https://wordpress.org/latest.zip",
                    HasAdditionalDir = true,
                    IsEnabled = true
                },
                new SiteType
                {
                    Name = "Laravel",
                    DisplayName = "Laravel",
                    InstallType = "command",
                    Command = "composer create-project laravel/laravel %s --prefer-dist",
                    IsEnabled = true
                },
                new SiteType
                {
                    Name = "Laravel_CLI",
                    DisplayName = "Laravel CLI",
                    InstallType = "command",
                    Command = "laravel new %s",
                    IsEnabled = true
                },
                new SiteType
                {
                    Name = "CakePHP",
                    DisplayName = "CakePHP",
                    InstallType = "command",
                    Command = "composer create-project --prefer-dist cakephp/app %s",
                    IsEnabled = true
                },
                new SiteType
                {
                    Name = "Symfony",
                    DisplayName = "Symfony",
                    InstallType = "command",
                    Command = "composer create-project symfony/website-skeleton %s",
                    IsEnabled = true
                }
            };
        }
    }
}
