#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MusicMonitor.Properties;
using Application = System.Windows.Application;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Orientation = System.Windows.Controls.Orientation;
using MessageBox = System.Windows.MessageBox;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Window = System.Windows.Window;

#endregion

namespace MusicMonitor
{
    /// <summary>
    ///   Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int SetErrorMode(int wMode);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra,
                                                    [Out] StringBuilder pszOut, [In] [Out] ref uint pcchOut);


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        public static string assocapp;
        private readonly BackgroundWorker InitBackgroundWorker = new BackgroundWorker();
        private readonly string[] searchTerms = new string[] { ".mp3", ".flac", ".MP3", ".FLAC", ".ogg", ".m4a" };
        private readonly Regex imagePattern = new Regex(@"^.*\.(jpg|gif|png|jpeg|bmp)$");
        private NotifyIcon _notifyIcon;
        private List<FileSystemWatcher> fswList =new List<FileSystemWatcher>();
        private int i;
        private bool initializing = true;
        private string lastPath;
        private MusicItem lastSubItem;
        private Dictionary<String, Object> nodes = new Dictionary<String, Object>();
        private SettingsWindow sw;
        private uint fileCount = 0;

        
        Style folderTitleStyle, folderBorderStyle, itemStyle, subItemStyle;

        public MainWindow()
        {
            InitializeComponent();
            ShowInTaskbar = false;
        }

        ~MainWindow()
        {
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            folderTitleStyle = (Style)stackPanel1.FindResource("FolderTitle");
            subItemStyle = (Style)stackPanel1.FindResource("SubItem");
            itemStyle = (Style)stackPanel1.FindResource("Item");
            folderBorderStyle = (Style)stackPanel1.FindResource("FolderBorder");
            

            _notifyIcon = new NotifyIcon {Icon = Properties.Resources.appIcon, Visible = true};
            _notifyIcon.BalloonTipClicked += _notifyIcon_BalloonTipClicked;
            _notifyIcon.MouseUp += _notifyIcon_MouseUp;

            InitBackgroundWorker.DoWork += InitBackgroundWorker_DoWork;
            InitBackgroundWorker.RunWorkerAsync();

        }

        [STAThread]
        private void InitBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            initTree();
        }

        private void _notifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            Restore();
            scrollViewer1.ScrollToVerticalOffset(0);
        }

        private void _notifyIcon_MouseUp(object sender, MouseEventArgs e)
        {
            Restore();
        }

        private void Restore()
        {
            if (WindowState == WindowState.Minimized)
            {
                Show();
                WindowState = WindowState.Normal;
                BringIntoView();
                Activate();
            }
            else
            {
                Show();
                BringIntoView();
                Activate();
            }
        }

        private void fsw_Renamed(object sender, RenamedEventArgs e)
        {
            fsw_Created(sender, new FileSystemEventArgs(WatcherChangeTypes.Renamed, Path.GetDirectoryName(e.FullPath), Path.GetFileName(e.FullPath)));
        }

        private void fsw_Created(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine(@"{0}", e.FullPath);
            if ((from l in searchTerms where l == Path.GetExtension(e.FullPath) select 1).Count() > 0)
                Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    DateTime d = DateTime.Now;
                    var p = Path.GetDirectoryName(e.FullPath);
                    var fn = Path.GetFileName(e.FullPath);
                    Object n = null;

                    if (!nodes.TryGetValue(p, out n))
                    {
                        var spb = createFolderItem(p, d);

                        stackPanel1.Children.Insert(0, spb);
                        n = nodes[p] = spb.Child;
                    }
                    var itp = createSubItem(e.FullPath);
                    ((MusicFolder)n).AddChild(itp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"{0}", ex);
                }
                //}));
            }));
        }

        private void initTree()
        {
            initializing = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                chold.Visibility = Visibility.Visible;
            }));
            if (nodes.Count > 0)
            {
                nodes = new Dictionary<string, Object>();
                stackPanel1.Children.Clear();
            }
            assocapp = GetAssocApp();
            var datedFiles = new SortedDictionary<DateTime, string>();
            DateTime time = DateTime.Today.Subtract(TimeSpan.FromDays(Settings.Default.DaysToShow));
            if (Settings.Default.FolderToWatch != null)
            {
                foreach (object f2wo in Settings.Default.FolderToWatch)
                {
                    var f2w = f2wo as string;
                    /* look for existing watcher */
                    var res = from l in fswList   where l.Path == f2w select l.Path;
                    if (res.Count() > 0)
                    {
                        fswList.ForEach((watcher) => { watcher.Dispose(); });
                    }
                    
                        if(true)
                    {
                        /* no watcher */
                        try
                        {
                            Dispatcher.BeginInvoke(new Action(() => { counter.Text = "Processing "+f2w+"..."; }));

                            {
                                var oi = 0;

                                FileSearch searcher = new FileSearch();
                                searcher.SearchExtensions.AddRange(searchTerms);
                                FileInfo[] files = searcher.Search(f2w);
                                files.All(f =>
                                              {

                                                  {
                                                      var date = f.CreationTime;
                                                      if (date.Ticks >= time.Ticks) datedFiles.Add(date.AddMilliseconds(i++), f.FullName);
                                                      Dispatcher.BeginInvoke(new Action(()=> { counter.Text = i.ToString(); }));

                                                  }
                                                  return true;

                                              });
                            }


                            var fsw = new FileSystemWatcher(f2w, "*.*") { IncludeSubdirectories = true, EnableRaisingEvents = true };
                            fsw.EnableRaisingEvents = true;
                            fsw.Created += fsw_Created;
                            fsw.Renamed += fsw_Renamed;
                            fswList.Add(fsw);
                            Console.WriteLine("NEW FSW... {0}", f2w);

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("{0}",ex);
                        }
                        
                    }

                }
                datedFiles.OrderByDescending(ft => ft.Key.Ticks).All(procItem);
            }
            else
            {
                Console.WriteLine("NO NEW FOLDERS... {0}",Settings.Default.FolderToWatch);
                /* no (new) folders */
            }
            Dispatcher.BeginInvoke(new Action(() =>
            {
                chold.Visibility = Visibility.Hidden;
            }));
            initializing = false;
        }

        private bool procItem(KeyValuePair<DateTime, string> f)
        {
            try
            {
                

                    stackPanel1.Dispatcher.Invoke(new Action(() =>
                                                                 {
                                                                     var d = f.Key;
                                                                     var p = Path.GetDirectoryName(f.Value);
                                                                     var fn = Path.GetFileName(f.Value);
                                                                     Object n = null;

                                                                     if (!nodes.TryGetValue(p, out n))
                                                                     {
                                                                         var spb = createFolderItem(p, d);
                                                                         stackPanel1.Children.Add(spb);
                                                                         n = nodes[p] = spb.Child;
                                                                     }
                                                                     {
                                                                         var itp = createSubItem(f.Value);
                                                                         if (itp != null)
                                                                         {
                                                                             if (itp.GetType() == typeof (MusicItem))
                                                                             {
                                                                                 ((MusicFolder) n).AddChild(itp);
                                                                             }
                                                                         }
                                                                     }
                                                                 }));
                    Refresh();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}", ex);
            }
            return true;
        }

        private MusicItem createSubItem(string p)
        {


            var itp = new MusicItem(p, subItemStyle);
            itp.Launched += MusicItemLaunch;
            if (_notifyIcon != null && !initializing && Settings.Default.ShowNotifications)
            {
                _notifyIcon.ShowBalloonTip(2000, @"New file detected...", String.Format(@"{0}", itp.fileName), ToolTipIcon.Info);
            }
            lastSubItem = itp;
            return itp;
        }

        static void MusicItemLaunch(object sender, LaunchArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.fullPath);

//                Application.Current.MainWindow.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("{0}", ex), "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border createFolderItem(string p, DateTime d)
        {
            var sp = new MusicFolder(p, d, folderTitleStyle, itemStyle);
            sp.Launched += MusicItemLaunch;
            if(Settings.Default.GetAlbumArt) Dispatcher.BeginInvoke(new Action(() => Directory.GetFiles(p, "*.*").All(a =>
                                                                                           {
                                                                                               if (imagePattern.Matches(a).Count > 0)
                                                                                               {
                                                                                               sp.AddArt(a);
                                                                                               }

                                                                                               return true;
                                                                                           })));
            
            return new Border { Style = folderBorderStyle, Child = sp }; ;
        }


     
        private string GetAssocApp()
        {
            var doctype = ".mp3";
            uint pcchOut = 0; 
            AssocQueryString(AssocF.Verify, AssocStr.Executable, doctype, null, null, ref pcchOut);
            var pszOut = new StringBuilder((int) pcchOut);
            AssocQueryString(AssocF.Verify, AssocStr.Executable, doctype, "Open", pszOut, ref pcchOut);
            string doc = pszOut.ToString();
            return doc;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }


        private void scrollViewer1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // TODO: Scroll in folder blocks instead of single tracks
        }


        private void Exit(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
        public  void Refresh() { Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { })); }
        private void Minimize(object sender, MouseButtonEventArgs e) { WindowState = WindowState.Minimized; }
        private void Window_Closed(object sender, EventArgs e) { Settings.Default.Save(); }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if(WindowState == WindowState.Minimized){Hide();} else{Show();}
        }

        bool getArtwork = Settings.Default.GetAlbumArt;
        private void ShowSettings(object sender, MouseButtonEventArgs e)
        {
            //lastPath = Settings.Default.FoldersToWatch;
            if (sw == null)
            {
                sw = new SettingsWindow();
                sw.Closed += sw_Closed;
            }
            sw.Show();
        }

        private void sw_Closed(object sender, EventArgs e)
        {
            sw = null;
            //if (Settings.Default.FoldersToWatch != lastPath || getArtwork!=Settings.Default.GetAlbumArt)
            {
               initTree();
                getArtwork = Settings.Default.GetAlbumArt;
            }
        }

        private void textBox1_GotFocus(object sender, RoutedEventArgs e)
        {
            if (textBox1.Text == "search ")
            {
                textBox1.Text = "";
            }
        }

        private void textBox1_LostFocus(object sender, RoutedEventArgs e)
        {
            if (textBox1.Text == "")
            {
                textBox1.Text = "search ";
            }
        }

        [Flags]
        public enum AssocF { Verify = 0x40 }
        public enum AssocStr { Command = 1, Executable }

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBox1.Text != "search ") {
                foreach (var bo in stackPanel1.Children)
                {
                    if (bo is Border)
                    {
                        var b = bo as Border;
                        var v = (MusicFolder)b.Child;
                        b.Visibility = v.fullPath.IndexOf(textBox1.Text, StringComparison.CurrentCultureIgnoreCase) > -1 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                    }
                }
            } 
        }

    }


       
    public class MusicItem : Grid, IComparable
    {
       

        public string fileName = "";
        public string fullPath = "";

        public MusicItem(string fullPath, Style tbStyle)
        {
            MouseLeftButtonDown += MusicItem_MouseLeftButtonUp;
            this.fullPath = fullPath;
            var tb = new TextBlock {Style = tbStyle};
            var b = new Border {Child = tb, CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(3)};
            fileName = tb.Text = Path.GetFileName(fullPath);
            tb.Padding = new Thickness(22, 0, 0, 2);
            Width = double.NaN;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            Children.Add(b);
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return fileName.CompareTo(((MusicItem) obj).fileName);
        }

        #endregion

        private void MusicItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(e.ClickCount == 2) OnLaunch();
        }
        public event EventHandler<LaunchArgs> Launched;
        protected virtual void OnLaunch()
        {
            EventHandler<LaunchArgs> handler = Launched;
            if (handler != null)
            {
                var evargs = new LaunchArgs();
                evargs.fullPath = this.fullPath;
                handler(this,evargs);
            }

        }
    }
    public class LaunchArgs : EventArgs
    {
        public string fullPath;
    }

    internal class MusicFolder : Grid
    {
        private readonly ObservableCollection<MusicItem> items = new ObservableCollection<MusicItem>();
        private readonly ItemsControl list = new ItemsControl();
        public string fullPath = "";
        private StackPanel artHold = new StackPanel { Orientation =  Orientation.Vertical, Margin = new Thickness(6,0,6,6)};
        
        public MusicFolder(string path, DateTime date, Style titleStyle, Style itemStyle)
        {

            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions[0].Width = new GridLength(100, GridUnitType.Star);
            ColumnDefinitions[1].Width = GridLength.Auto;
            

            RowDefinitions.Add(new RowDefinition());
            RowDefinitions.Add(new RowDefinition());
            RowDefinitions[0].Height = new GridLength(24);
            RowDefinitions[1].Height = GridLength.Auto;


            
            fullPath = path;
            Width = double.NaN;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            
            var tbDate = new TextBlock {Text = date.ToString(), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(0,0,3,0)};
            var tbLabel = new TextBlock {Width = double.NaN, Style = itemStyle, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = System.Windows.VerticalAlignment.Center};
            var fldrTitle = new Grid { Style = titleStyle };

            var sp = path.Split('\\');
            var pt = sp.Last();
            if (sp.Last().IndexOf("CD") == 0) { pt = sp[sp.Length - 2] +"\\"+ pt; }
            tbLabel.Text = pt;


            fldrTitle.ColumnDefinitions.Add(new ColumnDefinition());
            fldrTitle.ColumnDefinitions.Add(new ColumnDefinition());
            fldrTitle.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            fldrTitle.ColumnDefinitions[1].Width = GridLength.Auto;
            fldrTitle.Children.Add(tbLabel);
            fldrTitle.Children.Add(tbDate);

            fldrTitle.MouseLeftButtonDown += FldrTitle_MouseLeftButtonUp;

            Children.Add(fldrTitle);
            
            Grid.SetColumnSpan(fldrTitle,2);
            Grid.SetRow(fldrTitle, 0);

            Grid.SetRow(artHold, 1);
            Grid.SetRow(list, 1);

            Grid.SetColumn(artHold, 1);
            Grid.SetColumn(list, 0);

            Grid.SetColumn(tbLabel, 0);
            Grid.SetColumn(tbDate, 1);
            
            
            list.DataContext = items;
            list.ItemsSource = items;
            Children.Add(list);
            Children.Add(artHold);
        }
 
        public void AddArt(string path)
        {
            try
            {
                var pb = new System.Windows.Controls.Image() { Width = 128, Height = 128 };
                byte[] buffer = File.ReadAllBytes(path);
                MemoryStream mem = new MemoryStream(buffer);
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.DecodePixelWidth = 128;
                bi.DecodePixelHeight = 128;
                bi.CreateOptions = BitmapCreateOptions.None;
                bi.StreamSource = mem;
                bi.EndInit();
                pb.Source = bi;
                pb.Margin=new Thickness(0,6,0,0);
                artHold.Children.Add(pb);
                
            }
            catch (Exception ex)
            {
            }
        }
        public event EventHandler<LaunchArgs> Launched;
        protected virtual void OnLaunch()
        {
            EventHandler<LaunchArgs> handler = Launched;
            if (handler != null)
            {
                var evargs = new LaunchArgs();
                evargs.fullPath = this.fullPath;
                handler(this, evargs);
            }

        }
        private void FldrTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) this.OnLaunch();
        }


        public void AddChild(MusicItem item)
        {
            items.Add(item);
            items.Sort();
        }
    }

    internal static class Extensions
    {
        public static void Sort<T>(this ObservableCollection<T> collection) where T : IComparable
        {
            lock (collection)
            {
                List<T> sorted = collection.OrderBy(x => x).ToList();
                for (int newidx = 0; newidx < sorted.Count(); newidx++)
                {
                    var oldidx = collection.IndexOf(sorted[newidx]);
                    if (oldidx != newidx) collection.Move(oldidx, newidx);
                }
            }
        }
    }
}