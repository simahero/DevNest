using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;

namespace DevNest.UI.ViewModels
{
    public partial class DumpsViewModel : BaseViewModel
    {
        private readonly IDumpService _dumpService;

        public ObservableCollection<DumpFile> DumpFiles { get; } = new();

        public DumpsViewModel(IDumpService dumpService)
        {
            _dumpService = dumpService;
            Title = "Database Dumps";
            LoadDumpsCommand = new AsyncRelayCommand(LoadDumpsAsync);
            CreateDumpCommand = new AsyncRelayCommand<(string DatabaseName, string OutputPath)>(CreateDumpAsync);
            ImportDumpCommand = new AsyncRelayCommand<(DumpFile DumpFile, string TargetDatabase)>(ImportDumpAsync);
            DeleteDumpCommand = new AsyncRelayCommand<DumpFile>(DeleteDumpAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshDumpsAsync);
        }

        public IAsyncRelayCommand LoadDumpsCommand { get; }
        public IAsyncRelayCommand<(string DatabaseName, string OutputPath)> CreateDumpCommand { get; }
        public IAsyncRelayCommand<(DumpFile DumpFile, string TargetDatabase)> ImportDumpCommand { get; }
        public IAsyncRelayCommand<DumpFile> DeleteDumpCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }

        private async Task LoadDumpsAsync()
        {
            IsLoading = true;
            try
            {
                var dumpFiles = await _dumpService.GetDumpFilesAsync();
                DumpFiles.Clear();
                foreach (var dumpFile in dumpFiles)
                {
                    DumpFiles.Add(dumpFile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dump files: {ex.Message}");
                // TODO: Show error to user
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CreateDumpAsync((string DatabaseName, string OutputPath) dumpInfo)
        {
            if (string.IsNullOrWhiteSpace(dumpInfo.DatabaseName) || string.IsNullOrWhiteSpace(dumpInfo.OutputPath))
                return;

            try
            {
                var dumpFile = await _dumpService.CreateDumpAsync(dumpInfo.DatabaseName, dumpInfo.OutputPath);
                DumpFiles.Insert(0, dumpFile); // Add to top of list
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating dump: {ex.Message}");
                // TODO: Show error to user
            }
        }

        private async Task ImportDumpAsync((DumpFile DumpFile, string TargetDatabase) importInfo)
        {
            if (importInfo.DumpFile == null || string.IsNullOrWhiteSpace(importInfo.TargetDatabase))
                return;

            try
            {
                await _dumpService.ImportDumpAsync(importInfo.DumpFile.Path, importInfo.TargetDatabase);
                // TODO: Show success message to user
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing dump: {ex.Message}");
                // TODO: Show error to user
            }
        }

        private async Task DeleteDumpAsync(DumpFile? dumpFile)
        {
            if (dumpFile == null) return;

            try
            {
                await _dumpService.DeleteDumpAsync(dumpFile.Path);
                DumpFiles.Remove(dumpFile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting dump: {ex.Message}");
                // TODO: Show error to user
            }
        }

        private async Task RefreshDumpsAsync()
        {
            await LoadDumpsAsync();
        }

        protected override async Task OnLoadedAsync()
        {
            await LoadDumpsCommand.ExecuteAsync(null);
        }
    }
}
