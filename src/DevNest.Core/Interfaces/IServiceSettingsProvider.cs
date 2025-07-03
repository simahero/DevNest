using DevNest.Core.Enums;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Core.Interfaces
{
    public interface IServiceSettingsProvider
    {
        string ServiceName { get; }
        ServiceType Type { get; }
        void ParseFromIni(IniData iniData, Model serviceSettings);
        void SaveToIni(IniData iniData, Model serviceSettings);
    }

    public interface IServiceSettingsProvider<T> : IServiceSettingsProvider
    {
        T GetDefaultConfiguration();
    }

}
