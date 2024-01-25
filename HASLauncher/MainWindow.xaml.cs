using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace HASLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static string _username = "Steve";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Main_Initialized(object sender, EventArgs e)
        {
            foreach (var version in HASLibrary.VersionManager.GetAllVersions())
            {
                VersionSelectorDropdownBox.Items.Add(new SplitButtonItem
                {
                    Text = version
                });
            }
        }

        private void VersionSelectorSplitButton_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VersionSelectorDropdownBox.Text = ((SplitButtonItem)VersionSelectorDropdownBox.SelectedItem).Text;
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            var version = ((SplitButtonItem)VersionSelectorDropdownBox.SelectedItem).Text;
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HASLauncher");
            HASLibrary.VersionManager.DownloadVersion(version, path);
            HASLibrary.VersionManager.StartGame(version, path, _username);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VersionSelectorDropdownBox.Items.Clear();
            
            foreach (var version in HASLibrary.VersionManager.GetAllVersions((HASLibrary.VersionManager.VersionListType)((ComboBox)sender).SelectedIndex))
            {
                VersionSelectorDropdownBox.Items.Add(new SplitButtonItem
                {
                    Text = version
                });
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _username = ((TextBox)sender).Text;
        }
    }
    public class SplitButtonItem
    {
        public string? Text { get; set; }
    }
}