﻿using ARK_Server_Manager.Lib;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Threading;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for GlobalSettings.xaml
    /// </summary>
    public partial class GlobalSettings : UserControl
    {
        private GlobalizedApplication _globalizedApplication = GlobalizedApplication.Instance;

        public string Version
        {
            get;
            set;
        }

        public Config CurrentConfig
        {
            get;
            set;
        }

        public bool IsAdministrator
        {
            get { return (bool)GetValue(IsAdministratorProperty); }
            set { SetValue(IsAdministratorProperty, value); }
        }

        public static readonly DependencyProperty IsAdministratorProperty = DependencyProperty.Register(nameof(IsAdministrator), typeof(bool), typeof(GlobalSettings), new PropertyMetadata(false));
        
        public GlobalSettings()
        {
            this.Version = GetDeplployedVersion();

            this.CurrentConfig = Config.Default;
            this.DataContext = this;

            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            this.IsAdministrator = SecurityUtils.IsAdministrator();
        }

        private string GetDeplployedVersion()
        {
            XmlDocument xmlDoc = new XmlDocument();
            Assembly asmCurrent = System.Reflection.Assembly.GetExecutingAssembly();
            string executePath = new Uri(asmCurrent.GetName().CodeBase).LocalPath;

            xmlDoc.Load(executePath + ".manifest");
            XmlNamespaceManager ns = new XmlNamespaceManager(xmlDoc.NameTable);
            ns.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");
            string xPath = "/asmv1:assembly/asmv1:assemblyIdentity/@version";
            XmlNode node = xmlDoc.SelectSingleNode(xPath, ns);
            string version = node.Value;
            return version;
        }

        public void SetDataDir_Click(object sender, RoutedEventArgs args)
        {
            var optionResult = MessageBox.Show(_globalizedApplication.GetResourceString("GlobalSettings_DataDirectoryChange_ConfirmLabel"), _globalizedApplication.GetResourceString("GlobalSettings_DataDirectoryChange_ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (optionResult == MessageBoxResult.Yes)
            {
                var dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                dialog.Title = _globalizedApplication.GetResourceString("Application_DataDirectoryTitle");
                dialog.InitialDirectory = Config.Default.DataDir;
                var result = dialog.ShowDialog();

                if (result == CommonFileDialogResult.Ok)
                {
                    if (!String.Equals(dialog.FileName, Config.Default.DataDir))
                    {
                        try
                        {
                            // Set up the destination directories
                            string newConfigDirectory = Path.Combine(dialog.FileName, Config.Default.ProfilesDir);
                            string oldSteamDirectory = Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir);
                            string newSteamDirectory = Path.Combine(dialog.FileName, Config.Default.SteamCmdDir);

                            Directory.CreateDirectory(newConfigDirectory);
                            Directory.CreateDirectory(newSteamDirectory);

                            // Copy the Profiles
                            foreach (var file in Directory.EnumerateFiles(Config.Default.ConfigDirectory, "*.*", SearchOption.AllDirectories))
                            {
                                string sourceWithoutRoot = file.Substring(Config.Default.ConfigDirectory.Length + 1);
                                string destination = Path.Combine(newConfigDirectory, sourceWithoutRoot);
                                if (!File.Exists(destination))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                                    File.Copy(file, destination);
                                }
                            }

                            // Copy the SteamCMD files
                            foreach (var file in Directory.EnumerateFiles(oldSteamDirectory, "*.*", SearchOption.AllDirectories))
                            {
                                string sourceWithoutRoot = file.Substring(oldSteamDirectory.Length + 1);
                                string destination = Path.Combine(newSteamDirectory, sourceWithoutRoot);
                                if (!File.Exists(destination))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                                    File.Copy(file, destination);
                                }
                            }

                            // Remove the old directories
                            Directory.Delete(Config.Default.ConfigDirectory, true);
                            Directory.Delete(oldSteamDirectory, true);

                            // Update the config
                            Config.Default.DataDir = dialog.FileName;
                            Config.Default.ConfigDirectory = newConfigDirectory;
                            App.ReconfigureLogging();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(String.Format(_globalizedApplication.GetResourceString("GlobalSettings_DataDirectoryChange_FailedLabel"), ex.Message), _globalizedApplication.GetResourceString("GlobalSettings_DataDirectoryChange_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }

                    }
                }
            }
        }

        private void SetCacheDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = _globalizedApplication.GetResourceString("GlobalSettings_CacheDirectoryTitle");
            dialog.InitialDirectory = Config.Default.DataDir;
            var result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                if (!String.Equals(dialog.FileName, Config.Default.ServerCacheDir))
                {
                    Config.Default.ServerCacheDir = dialog.FileName;
                }
            }
        }

        private void LanguageSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentConfig.CultureName = Thread.CurrentThread.CurrentCulture.Name;
        }
    }
}
