namespace ReplaysOfTheVoid
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using System.Windows.Threading;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ReplayReader m_replayreader;
        private ConcurrentQueue<Replay> m_currentReplayList = null;
        private ConcurrentQueue<Tuple<string, string>> m_failedToLoadReplays = null;
        private DispatcherTimer m_uiTimer;
        private Task[] m_tasks = null;

        public MainWindow()
        {
            InitializeComponent();

            m_uiTimer = new DispatcherTimer();
            m_uiTimer.Interval = new TimeSpan(100);
            m_uiTimer.Tick += UiTimer_Tick;

            // by default fill in user/docs/sc2beta
            edReplayPath.Text =
                System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), "Documents") +
                "\\StarCraft II Beta\\";

            m_replayreader = new ReplayReader();
        }

        private void UiTimer_Tick(object sender, EventArgs e)
        {
            if ((m_currentReplayList != null) && (m_tasks != null))
            {
                // update progressbar
                pbMain.Value = m_currentReplayList.Count();

                // check if all tasks are completed
                if (Task.WaitAll(m_tasks, 10))
                {
                    m_uiTimer.Stop();

                    pbMain.Value = pbMain.Maximum;

                    // sort replays by date and show them in the UI
                    gridReplays.ItemsSource = m_currentReplayList.OrderBy(o => o.ReplayDate).ToList();

                    // we can now rename then
                    btnRenameAll.IsEnabled = true;
                    btnInvertCheckboxes.IsEnabled = true;

                    btnParse.IsEnabled = true;

                    if (m_failedToLoadReplays.Count() > 0)
                    {
                        MessageBox.Show(m_failedToLoadReplays.Count() + " replays failed to load");
                    }

                    // todo: do something with the information in FailedToLoadReplays
                }
            }
        }

        /// <summary>
        /// Parse button click implementation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnParse_Click(object sender, RoutedEventArgs e)
        {
            btnParse.IsEnabled = false;

            pbMain.Value = 0;

            m_currentReplayList = new ConcurrentQueue<Replay>();
            m_failedToLoadReplays = new ConcurrentQueue<Tuple<string, string>>();

            string replaypath = edReplayPath.Text;

            // list all files recursively from replaypath
            var dirinfo = new DirectoryInfo(replaypath);
            var files =
                dirinfo.EnumerateFiles(
                    "*.SC2Replay",
                    SearchOption.AllDirectories
                );

            pbMain.Maximum = files.Count();

            var format = edFormat.Text;

            var i = 0;
            m_tasks = new Task[files.Count()];

            foreach (var file in files)
            {
                // load replays as threaded tasks
                m_tasks[i] = Task.Factory.StartNew(() =>
                {
                    // parse replay file and add to list
                    try
                    {
                        var replay = m_replayreader.ParseReplay(file.DirectoryName + "\\", file.Name, file.CreationTime, format);

                        m_currentReplayList.Enqueue(replay);
                    }
                    catch (Exception except)
                    {
                        // ignore exception, just don't add to list
                        m_failedToLoadReplays.Enqueue(
                            new Tuple<string, string>(file.DirectoryName + "\\" + file.Name, except.Message)
                        );
                    }
                });

                i++;
            }

            // start timer that checks status of all the running tasks
            m_uiTimer.Start();
        }

        /// <summary>
        /// Rename button click implementation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRenameAll_Click(object sender, RoutedEventArgs e)
        {
            int renamecount = 0;

            // todo: can we run this threaded like we did with parsing all the replays
            foreach (var replay in m_currentReplayList)
            {
                // if user checked the box to rename
                if (replay.DoRename)
                {
                    var renameto = replay.RenameTo;

                    // if it isn't already the same name
                    if (replay.Filename != renameto)
                    {
                        // if filename is a duplicate, try a different name (by appending a count)
                        var duplicatecount = 1;
                        while (File.Exists(replay.Directory + renameto) && (replay.Filename != renameto))
                        {
                            duplicatecount++;
                            renameto = replay.GetAlternateRename(duplicatecount);
                        }

                        // rename
                        if (replay.Filename != renameto)
                        {
                            try
                            {
                                File.Move(
                                    replay.Directory + replay.Filename,
                                    replay.Directory + renameto
                                );

                                renamecount++;
                            }
                            catch (Exception)
                            {
                                // ignore
                                // todo: what to do with this?
                            }
                        }
                    }
                }
            }

            MessageBox.Show("Replays renamed: " + renamecount.ToString());
        }

        private void btnInvertCheckboxes_Click(object sender, RoutedEventArgs e)
        {
            gridReplays.ItemsSource = null;

            foreach (var replay in m_currentReplayList)
            {
                replay.DoRename = !replay.DoRename;
            }

            gridReplays.ItemsSource = m_currentReplayList.OrderBy(o => o.ReplayDate).ToList();
        }
    }
}
