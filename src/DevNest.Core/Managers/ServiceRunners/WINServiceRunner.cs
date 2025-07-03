using DevNest.Core.Enums;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using System.Diagnostics;

namespace DevNest.Core.Managers.ServiceRunners
{
    public class WINServiceRunner : IServiceRunner
    {

        private readonly ICommandExecutor _commandExecutor;
        private readonly IUIDispatcher _uiDispatcher;

        public WINServiceRunner(ICommandExecutor commandExecutor, IUIDispatcher uiDispatcher)
        {
            _commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
            _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        }

        public async Task<bool> StartServiceAsync(ServiceModel service)
        {
            if (service == null) return false;

            try
            {
                service.IsLoading = true;
                service.Status = ServiceStatus.Starting;

                var workingDirectory = service.WorkingDirectory ?? Environment.CurrentDirectory;
                var process = await _commandExecutor.StartProcessAsync(service.Command, workingDirectory, default);

                if (process != null)
                {
                    process.EnableRaisingEvents = true;

                    process.Exited += (sender, e) =>
                    {
                        _uiDispatcher.TryEnqueue(() =>
                        {
                            service.Status = ServiceStatus.Stopped;
                            service.Process = null;
                        });
                    };

                    service.Process = process;
                    service.Status = ServiceStatus.Running;
                    return true;
                }
                else
                {
                    service.Status = ServiceStatus.Stopped;
                    return false;
                }
            }
            catch (Exception)
            {
                service.Status = ServiceStatus.Stopped;
                return false;
            }
            finally
            {
                service.IsLoading = false;
            }
        }

        public async Task<bool> StopServiceAsync(ServiceModel service)
        {
            if (service == null) return false;

            try
            {
                service.IsLoading = true;
                service.Status = ServiceStatus.Stopping;

                if (service.Process != null)
                {
                    var killTree = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/PID {service.Process.Id} /T /F",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(killTree)?.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to kill process tree: {ex.Message}");
            }
            finally
            {
                if (service.Process != null)
                {
                    service.Process.Dispose();
                    service.Process = null;
                }

                service.IsLoading = false;
            }

            service.Status = ServiceStatus.Stopped;

            return true;
        }

        public async Task<bool> ToggleServiceAsync(ServiceModel service)
        {
            if (service == null)
            {
                return false;
            }

            if (service.IsRunning)
            {
                return await StopServiceAsync(service);
            }
            else
            {
                return await StartServiceAsync(service);
            }
        }
    }
}
