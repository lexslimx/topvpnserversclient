﻿using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SurfVpnClientTest1.Models;
using SurfVpnClientTest1.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Text.Json;
using SurfVpnClientTest1.Views; // Make sure this namespace matches where HomeView.xaml is

namespace SurfVpnClientTest1.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private UserControl _currentView;
        public UserControl CurrentView
        {
            get => _currentView;
            set
            {
                if (_currentView != value)
                {
                    _currentView = value;
                    OnPropertyChanged(nameof(CurrentView));
                }
            }
        }

        private readonly HomeView _homeView = new HomeView();

        public ICommand ShowSettingsCommand => new RelayCommand(() => CurrentView = new SubscriptionView());
        public ICommand ShowHomeViewCommand => new RelayCommand(() => CurrentView = _homeView);

        public MainWindowViewModel()
        {
            CurrentView = _homeView; // Set the initial view to HomeView
        }
    }        
}
