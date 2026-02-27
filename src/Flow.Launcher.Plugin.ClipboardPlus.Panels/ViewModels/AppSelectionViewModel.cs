using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.ViewModels
{
    public partial class AppSelectionViewModel : ObservableObject
    {
        private CancellationTokenSource _searchCancellationTokenSource = new();
        private List<AppInfo> _allApps = [];
        private readonly HashSet<AppInfo> _existingAppsSet = [];

        public AppSelectionViewModel() : this(null)
        {
        }

        public AppSelectionViewModel(IEnumerable<AppInfo>? existingApps)
        {
            _existingAppsSet = existingApps != null ? [.. existingApps] : [];
            SearchQuery = string.Empty;
            LoadAppsAsync().ConfigureAwait(false);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotLoading))]
        private bool isLoading = true;

        public bool IsNotLoading => !IsLoading;

        [ObservableProperty]
        private string searchQuery;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAppSelected))]
        private AppInfo? selectedApp;

        public bool IsAppSelected => SelectedApp != null;

        public ObservableCollection<AppInfo> FilteredAppList { get; } = [];

        public AppInfo? Result { get; private set; }

        partial void OnSearchQueryChanged(string value)
        {
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();
            _searchCancellationTokenSource = new CancellationTokenSource();
            SearchAsync(value, _searchCancellationTokenSource.Token);
        }

        private async Task LoadAppsAsync()
        {
            try
            {
                IsLoading = true;

                // Load all apps from different sources
                var tasks = new List<Task<List<AppInfo>>>
                {
                    Task.Run(async () => (await ShortcutHelper.GetStartMenuAppsAsync()).Cast<AppInfo>().ToList())
                };

#pragma warning disable CA1416 // Validate platform compatibility
                if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
                {
                    tasks.Add(Task.Run(async () => (await UwpAppHelper.GetUwpAppsAsync()).Cast<AppInfo>().ToList()));
                }
#pragma warning restore CA1416 // Validate platform compatibility

                await Task.WhenAll(tasks);

                // Combine all apps and remove duplicates based on DefaultDisplayName
                var allApps = tasks.SelectMany(t => t.Result).ToList();
                _allApps = [.. allApps
                    .GroupBy(app => app.DefaultDisplayName)
                    .Select(group => group.First())
                    .Where(app => !_existingAppsSet.Contains(app))];

                // Initial display - show all apps and trigger icon loading
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FilteredAppList.Clear();
                    foreach (var app in _allApps.OrderBy(a => a.DisplayName))
                    {
                        // Trigger icon loading for each app
                        /*if (app.AppIcon.Task == null)
                        {
                            app.AppIcon.Reset();
                        }*/
                        FilteredAppList.Add(app);
                    }
                    IsLoading = false;
                });
            }
            catch (Exception)
            {
                IsLoading = false;
            }
        }

        private async void SearchAsync(string searchQuery, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(200, cancellationToken); // Debounce

                if (cancellationToken.IsCancellationRequested)
                    return;

                List<AppInfo> filteredAppList;
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    filteredAppList = [.. _allApps];
                }
                else
                {
                    filteredAppList = PerformSimpleSearch(searchQuery, _allApps);
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        FilteredAppList.Clear();
                        foreach (var app in filteredAppList.OrderBy(a => a.DisplayName))
                        {
                            // Trigger icon loading for each app
                            /*if (app.AppIcon.Task == null)
                            {
                                app.AppIcon.Reset();
                            }*/
                            FilteredAppList.Add(app);
                        }
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Expected when search is cancelled
            }
            catch (Exception)
            {
                // Handle other exceptions
            }
        }

        private static List<AppInfo> PerformSimpleSearch(string searchQuery, IEnumerable<AppInfo> apps)
        {
            List<AppInfo> filteredApps = [];
            
            foreach (AppInfo app in apps)
            {
                bool match = false;
                
                if (app.DefaultDisplayName.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                {
                    match = true;
                }
                else if (app.DisplayName.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                {
                    match = true;
                }

                if (match)
                {
                    filteredApps.Add(app);
                }
            }

            return filteredApps;
        }

        [RelayCommand]
        private void Select()
        {
            Result = SelectedApp;
        }

        [RelayCommand]
        private void Cancel()
        {
            Result = null;
        }
    }
}
