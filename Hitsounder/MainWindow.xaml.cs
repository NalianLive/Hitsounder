using System;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Path = System.IO.Path;
using System.Windows.Input;
using System.Windows.Controls;

namespace Hitsounder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        // for hastebin error reporting
        private static HttpClient _httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            txtSkinFolder.Text = Properties.Settings.Default.lastSkin;
        }

        private void btnSelectMap_Click(object sender, RoutedEventArgs e)
        {
            using (var ofd = new CommonOpenFileDialog())
            {
                ofd.Multiselect = false;
                ofd.EnsurePathExists = true;
                ofd.Title = "Select osu! Beatmap";
                var filter = new CommonFileDialogFilter
                {
                    DisplayName = "osu! beatmap",
                    ShowExtensions = true
                };
                filter.Extensions.Add("osu");
                ofd.Filters.Add(filter);

                if (ofd.ShowDialog() != CommonFileDialogResult.Ok) return;
                txtMap.Text = ofd.FileName;
            }
        }

        private void btnSelectSkin_Click(object sender, RoutedEventArgs e)
        {
            using (var ofd = new CommonOpenFileDialog())
            {
                ofd.IsFolderPicker = true;
                ofd.Multiselect = false;
                ofd.EnsurePathExists = true;
                ofd.Title = "Select osu! Skin";

                ofd.FileOk += (s, param) =>
                {
                    var dialog = s as CommonOpenFileDialog;
                    var files = new Collection<string>();
                    typeof(CommonFileDialog)
                        .GetMethod("PopulateWithFileNames", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.Invoke(dialog, new object[] { files });

                    if (File.Exists(Path.Combine(files[0], "skin.ini"))) return;
                    param.Cancel = true;
                    MessageBox.Show(
                        "This does not appear to be a valid skin. Please select a valid skin and try again.", 
                        "Hitsounder", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error
                        );
                };

                if (ofd.ShowDialog() != CommonFileDialogResult.Ok) return;
                txtSkinFolder.Text = ofd.FileName;
            }
        }

        private void btnHitsound_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = 
                (txtMap.Text.Length > 0 && File.Exists(txtMap.Text)) &&
                (txtSkinFolder.Text.Length <= 0 || File.Exists(Path.Combine(txtSkinFolder.Text, "skin.ini")));
        }

        private async void btnHitsound_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                btnHitsound.IsEnabled = false;
                var map = txtMap.Text;
                var skin = txtSkinFolder.Text.Length > 0 ? txtSkinFolder.Text : null;
                await Task.Run(() =>
                {
                    MapProcessor.Hitsound(map, skin);
                });
                btnHitsound.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // something went badly wrong, break out and upload error to hastebin
                var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://hastebin.com/documents"))
                {
                    Content = new StringContent(ex.ToString())
                };
                var response = await _httpClient.SendAsync(request);
                var key = await response.Content.ReadAsStringAsync();
                key = key.Substring(8, key.Length - 10);

                MessageBox.Show(
                    $"An error has occurred whilst trying to process the map. An error report has already been uploaded to https://hastebin.com/{key}. Please screenshot this message and report it on GitHub.",
                    "Hitsounder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
            }
        }

        private void txtSkinFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.lastSkin = txtSkinFolder.Text;
            Properties.Settings.Default.Save();
        }
    }
}
