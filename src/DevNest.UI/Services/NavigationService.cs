using DevNest.UI.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;

namespace DevNest.UI.Services
{
    public interface INavigationService
    {
        void NavigateTo<T>() where T : Page;
        void NavigateTo<T>(NavigationTransitionInfo? transitionInfo) where T : Page;
        void NavigateTo(Type pageType);
        void NavigateTo(Type pageType, NavigationTransitionInfo? transitionInfo);
        void NavigateTo(Type pageType, object? parameter);
        void NavigateTo(Type pageType, object? parameter, NavigationTransitionInfo? transitionInfo);
        void SetFrame(Frame frame);
        bool CanGoBack { get; }
        void GoBack();
    }

    public class NavigationService : INavigationService
    {
        private Frame? _frame;

        private readonly Dictionary<Type, Type> _pageViewModelMap = new(); public NavigationService()
        {
            _pageViewModelMap[typeof(DashboardPage)] = typeof(ViewModels.DashboardViewModel);
            _pageViewModelMap[typeof(ServicesPage)] = typeof(ViewModels.ServicesViewModel);
            _pageViewModelMap[typeof(SitesPage)] = typeof(ViewModels.SitesViewModel);
            _pageViewModelMap[typeof(EnvironmentsPage)] = typeof(ViewModels.EnvironmentsViewModel);
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
        public void NavigateTo<T>(NavigationTransitionInfo? transitionInfo) where T : Page
        {
            NavigateTo(typeof(T), transitionInfo);
        }

        public void NavigateTo(Type pageType)
        {
            NavigateTo(pageType, null, null);
        }

        public void NavigateTo(Type pageType, NavigationTransitionInfo? transitionInfo)
        {
            NavigateTo(pageType, null, transitionInfo);
        }

        public void NavigateTo(Type pageType, object? parameter)
        {
            NavigateTo(pageType, parameter, null);
        }

        public void NavigateTo(Type pageType, object? parameter, NavigationTransitionInfo? transitionInfo)
        {
            if (_frame == null)
            {
                var errorMessage = "Frame not set. Call SetFrame first.";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _frame.Navigate(pageType, parameter, transitionInfo);

            if (_frame.Content is Page page && _pageViewModelMap.TryGetValue(pageType, out var viewModelType))
            {
                var viewModel = ServiceLocator.GetService(viewModelType);
                SetViewModel(page, viewModel);
            }
        }

        public bool CanGoBack => _frame?.CanGoBack ?? false;

        public void GoBack()
        {
            if (_frame?.CanGoBack == true)
            {
                _frame.GoBack();
            }
        }

        private static void SetViewModel(Page page, object viewModel)
        {
            var setViewModelMethod = page.GetType().GetMethod("SetViewModel");
            if (setViewModelMethod != null)
            {
                setViewModelMethod.Invoke(page, new[] { viewModel });
            }
        }
    }
}
