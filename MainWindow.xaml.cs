using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.IO;

namespace replaysofthevoid
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ReplayReader Reader;
        private List<Replay> CurrentReplayList;

        public MainWindow()
        {
            InitializeComponent();

            // by default fill in user/docs/sc2beta
            edReplayPath.Text = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), "Documents") + "\\" + "StarCraft II Beta" + "\\";

            Reader = new ReplayReader();
        }

        /// <summary>
        /// Parse button click implementation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CurrentReplayList = new List<Replay>();

            string replaypath = edReplayPath.Text;

            // load all files recursively from replaypath
            var dirinfo = new DirectoryInfo(replaypath);
            var files =
                dirinfo.EnumerateFiles(
                    "*.SC2Replay",
                    SearchOption.AllDirectories
                );
            foreach (var file in files) {
                // parse replay file and add to list
                var replay = Reader.ParseReplay(file.DirectoryName + "\\", file.Name, file.CreationTime);
                CurrentReplayList.Add(replay);
            }

            // sort replays by date and show them in the UI
            gridReplays.ItemsSource = CurrentReplayList.OrderBy(o => o.replaydate).ToList();

            // we can now rename then
            btnRenameAll.IsEnabled = true;
        }

        /// <summary>
        /// Rename button click implementation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRenameAll_Click(object sender, RoutedEventArgs e)
        {
            int renamecount = 0;

            foreach (var replay in CurrentReplayList)
            {
                // if user checked the box to rename
                if (replay.dorename)
                {
                    // if it isn't already the same name
                    if (replay.filename != replay.renameto)
                    {
                        // rename
                        File.Move(
                            replay.directory + replay.filename,
                            replay.directory + replay.renameto
                        );

                        renamecount++;
                    }
                }
            }

            MessageBox.Show("Replays renamed: " + renamecount.ToString());
        }
    }
}
