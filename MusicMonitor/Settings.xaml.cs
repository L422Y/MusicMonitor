using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MusicMonitor.Properties;
using System.Collections.Specialized;

namespace MusicMonitor
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.WindowBg = new SolidColorBrush(WindowBGSelect.SelectedColor);
            Settings.Default.FolderBg = new SolidColorBrush(FolderBGSelect.SelectedColor);
            Settings.Default.FolderHeadBg = new SolidColorBrush(FolderHeaderBGSelect.SelectedColor);
            //if (listBox1.Items != null) Settings.Default.FolderToWatch.AddRange(listBox1.Items.Cast<String>().ToArray());
            Settings.Default.Save();
            this.Close();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reload();
            this.Close();
        }

        private void BrowseButton(object sender, RoutedEventArgs e)
        {
            var directoryDialog = new System.Windows.Forms.FolderBrowserDialog();
            directoryDialog.Description = @"Please select the folder to be watched.";
            directoryDialog.SelectedPath = (listBox1.Items.Count>0 ? (string)listBox1.Items[0] : @"C:\");
            if (directoryDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Settings.Default.FolderToWatch.IndexOf(directoryDialog.SelectedPath) < 0)
                { 
                    Settings.Default.FolderToWatch.Add(directoryDialog.SelectedPath); 
                }
                listBox1.Items.Refresh();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(Settings.Default.FolderToWatch==null) Settings.Default.FolderToWatch  = new StringCollection();
            listBox1.ItemsSource = Settings.Default.FolderToWatch;
            FolderHeaderBGSelect.SelectedColor = Settings.Default.FolderHeadBg.Color;
            WindowBGSelect.SelectedColor = Settings.Default.WindowBg.Color;
            FolderBGSelect.SelectedColor = Settings.Default.FolderBg.Color;
        }

       

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                Settings.Default.FolderToWatch.Remove(listBox1.SelectedItem.ToString());
                listBox1.Items.Refresh();
            }
        }
    }
}
