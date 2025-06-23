using DevNest.UI.Views;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace DevNest.UI.Services
{
    public interface INavigationService
    {
        void NavigateTo<T>() where T : Page;
        void NavigateTo(Type pageType);
        void SetFrame(Frame frame);
    }

    public class NavigationService : INavigationService
    {
        private Frame? _frame;
        private readonly Dictionary<Type, Type> _pageViewModelMap = new();

        public NavigationService()
        {
            // Map pages to their ViewModels
            _pageViewModelMap[typeof(DashboardPage)] = typeof(ViewModels.DashboardViewModel);
            _pageViewModelMap[typeof(ServicesPage)] = typeof(ViewModels.ServicesViewModel);
            _pageViewModelMap[typeof(SitesPage)] = typeof(ViewModels.SitesViewModel);
            _pageViewModelMap[typeof(SettingsPage)] = typeof(ViewModels.SettingsViewModel);
            _pageViewModelMap[typeof(DumpsPage)] = typeof(ViewModels.DumpsViewModel);
        }

        public void SetFrame(Frame frame)
        {
            _frame = frame;
        }

        public void NavigateTo<T>() where T : Page
        {
            NavigateTo(typeof(T));
        }

        public void NavigateTo(Type pageType)
        {
            if (_frame == null)
                throw new InvalidOperationException("Frame not set. Call SetFrame first.");

            // Get the page instance from DI
            var page = ServiceLocator.GetService(pageType) as Page;
            if (page == null)
                throw new InvalidOperationException($"Could not create page of type {pageType.Name}");

            // Set up the ViewModel if the page has a SetViewModel method
            if (_pageViewModelMap.TryGetValue(pageType, out var viewModelType))
            {
                var viewModel = ServiceLocator.GetService(viewModelType);
                SetViewModel(page, viewModel);
            }

            _frame.Content = page;
        }

        private static void SetViewModel(Page page, object viewModel)
        {
            // Use reflection to call SetViewModel if it exists
            var setViewModelMethod = page.GetType().GetMethod("SetViewModel");
            if (setViewModelMethod != null)
            {
                setViewModelMethod.Invoke(page, new[] { viewModel });
            }
        }
    }
}
