﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using WindowPlacementUtil;

namespace ThwargLauncher
{
    /// <summary>
    /// Interaction logic for Help.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        private HelpWindowViewModel _viewModel;
        public HelpWindow(HelpWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            chkShowStartup.IsChecked = Properties.Settings.Default.ShowHelpAtStart;
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var assemblyTitle = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            this.Title = string.Format("Help - {0} {1}", assemblyTitle, version);
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            LoadWindowSettings();
        }
        private void LoadWindowSettings()
        {
            this.SetPlacement(Properties.Settings.Default.HelpWindowPlacement);
        }
        private void SaveWindowSettings()
        {
            Properties.Settings.Default.HelpWindowPlacement = this.GetPlacement();
            Properties.Settings.Default.Save();
        }

        private void btnDefaultPreferences_Click(object sender, RoutedEventArgs e)
        {
            string pathtoPreferences = Configuration.UserPreferencesFile;

            if (File.Exists(pathtoPreferences))
            {
                Process.Start("notepad.exe", pathtoPreferences);
            }
            else
            {
                MessageBox.Show("Your UserPreferences file is not in the default location.", "File not found.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void btnDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new DiagnosticsWindow(_viewModel.GetDiagnosticWindowViewModel());
            dlg.Show();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.ShowHelpAtStart = chkShowStartup.IsChecked.Value;
            Properties.Settings.Default.Save();
            SaveWindowSettings();
        }
    }
}