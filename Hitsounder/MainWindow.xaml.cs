using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using Path = System.IO.Path;

namespace Hitsounder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
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
                Properties.Settings.Default.lastSkin = ofd.FileName;
                Properties.Settings.Default.Save();
            }
        }
    }
}
