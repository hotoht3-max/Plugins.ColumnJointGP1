using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Tekla.Structures;
using Tekla.Structures.Dialog;

namespace RAM.Plugins.ColumnJointGP1.UIControls
{
    public partial class HeaderSystemControl : UserControl
    {
        private const string PLUGIN_EXTENSION = "RAM_ColumnJointGP1";
        private const string PLUGIN_SUFFIX = ".RAM.Plugins.ColumnJointGP1.MainWindow.xml";

        public HeaderSystemControl()
        {
            InitializeComponent();
            Loaded += (s, e) => RefreshPresets();
        }

        private PluginWindowBase GetWindow() => Window.GetWindow(this) as PluginWindowBase;

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (PresetsCombo.SelectedItem is string selectedFile)
            {
                GetWindow()?.LoadValues(selectedFile);
                SaveNameBox.Text = selectedFile;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (PresetsCombo.SelectedItem is string selectedFile)
            {
                GetWindow()?.SaveValues(selectedFile);
            }
            else
            {
                MessageBox.Show("Введите имя файла.", "RAM BIM", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            string fileName = SaveNameBox.Text.Trim();
            if (string.IsNullOrEmpty(fileName)) return;
            GetWindow()?.SaveValues(fileName);
            RefreshPresets();
            PresetsCombo.SelectedItem = fileName;
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Плагин узла колонны RAM BIM.\nВерсия: Alpha 1.0", "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshPresets()
        {
            // Исправлено: IDE0019
            string currentSelection = PresetsCombo.SelectedItem?.ToString();
            var uniqueFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                string modelPath = new Tekla.Structures.Model.Model().GetInfo().ModelPath;
                if (!string.IsNullOrEmpty(modelPath))
                {
                    AddFilesFromDirectory(Path.Combine(modelPath, "attributes"), uniqueFiles);
                }
            }
            catch { }

            AddFilesFromEnvironment("XS_PROJECT", uniqueFiles);
            AddFilesFromEnvironment("XS_FIRM", uniqueFiles);
            AddFilesFromEnvironment("XS_SYSTEM", uniqueFiles);

            var sortedList = uniqueFiles.ToList();
            sortedList.Sort();
            PresetsCombo.ItemsSource = sortedList;

            if (currentSelection != null && sortedList.Contains(currentSelection))
                PresetsCombo.SelectedItem = currentSelection;
            else if (sortedList.Contains("standard"))
                PresetsCombo.SelectedItem = "standard";
            else if (sortedList.Count > 0)
                PresetsCombo.SelectedIndex = 0;
        }

        private void AddFilesFromEnvironment(string envVarName, HashSet<string> fileSet)
        {
            try
            {
                string paths = string.Empty;
                TeklaStructuresSettings.GetAdvancedOption(envVarName, ref paths);
                if (string.IsNullOrWhiteSpace(paths)) return;
                foreach (var path in paths.Split(';'))
                {
                    AddFilesFromDirectory(path.Trim(), fileSet);
                }
            }
            catch { }
        }

        private void AddFilesFromDirectory(string directory, HashSet<string> fileSet)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    var files = Directory.GetFiles(directory, $"*{PLUGIN_SUFFIX}");
                    foreach (var file in files)
                    {
                        string cleanName = Path.GetFileName(file).Replace(PLUGIN_SUFFIX, "");
                        if (!string.IsNullOrWhiteSpace(cleanName))
                        {
                            fileSet.Add(cleanName);
                        }
                    }
                }
            }
            catch { }
        }
    }
}