using DevNest.Core.Interfaces;

namespace DevNest.Core.Interfaces
{
    public interface IPlatformServiceFactory
    {
        IServiceRunner GetServiceRunner();
        IServiceInstaller GetServiceInstaller();
        ICommandExecutor GetCommandExecutor();
        ICommandManager GetCommandManager();
    }
}
