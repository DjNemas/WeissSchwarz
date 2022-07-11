using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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
using WeißSchwarzSharedClasses;
using WeißSchwarzSharedClasses.Models;
using WeißSchwarzViewer.DB;
using WeißSchwarzViewer.UI;

namespace WeißSchwarzViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly float _AppVersion = 1.0f;
#if DEBUG
        [DllImport("Kernel32")]
        private static extern void AllocConsole();
#endif
        private HttpClient _httpClient;
        private List<Set>? _apiReleaseList;

        public MainWindow()
        {
            _httpClient = new HttpClient();
            // Get Console here :P
#if DEBUG
            AllocConsole();
#endif
            // Create Database File and Tables of Models
            CreateDatabaseIfNotExist();

            // Some WPF Stuff is going on here. Just Ignore it ;D
            InitializeComponent();

            // Now the real Magic happens! Doing my own stuff. ヽ(*⌒▽⌒*)ﾉ
            // Load DB Data
            LoadDataFromDB();
            // Let the Windows appere in the middle of Screen.
            CenterWindowOnScreen();

            // Display the Version of this Application in UI
            lblAppVersion.Content = "V" + _AppVersion.ToString("0.0").Replace(",", ".");

            // Fill the UI with Obs
            SetUIWithObs();

            // Check for Updates at Start
            CheckForUpdate();

        }

        private void CreateDatabaseIfNotExist()
        {
            // Create Database File and Tables of Models
            DatabaseContext.DB.Database.EnsureCreated();
            Log.Info("DB Created or Loaded");
        }
        private void LoadDataFromDB()
        {
            // Clear Obs
            ObsLists.Sets.Clear();

            DatabaseContext db = new();

            // Sort 
            var sets = db.Sets
                .Include(x => x.Cards).ThenInclude(x => x.Traits)
                .Include(x => x.Cards).ThenInclude(x => x.Triggers).ToList().OrderBy(x => x.Name);
            // Fill Obs
            foreach (var item in sets)
            {
                ObsLists.Sets.Add(item);
            }
        }

        private void CenterWindowOnScreen()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        private void SetUIWithObs()
        {
            lbReleases.ItemsSource = ObsLists.Sets;
            lbCards.ItemsSource = ObsLists.Cards;
            cbSorter.ItemsSource = Enum.GetValues(typeof(ComboBoxEnumSort));
            cbSorterAorD.ItemsSource = Enum.GetValues(typeof(ComboBoxEnumSortAorDescending));
        }

        private async Task CheckForUpdate()
        {
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                lblVersion.Foreground = Brushes.Cyan;
                lblVersion.Content = "Checking...";
                // Get API Json Data as String
                Stream releasesJson = await _httpClient.GetStreamAsync("https://djnemashome.de:3939/v1/ws/sets/all");

                // Convert Entries to Release Class as List
                _apiReleaseList = JsonSerializer.DeserializeAsync<List<Set>?>(releasesJson).Result;
                _apiReleaseList?.OrderBy(x => x.SetID);


                // Get Table from Database
                var releasesList = ObsLists.Sets.OrderBy(x => x.SetID);

                /// Compare for differents
                // Same Count?
                if (releasesList.Count() != _apiReleaseList?.Count())
                    APIHasUpdate();
                else
                {
                    bool hasChanges = false;

                    for (int i = 0; i < releasesList.Count(); i++)
                    {
                        // Same Member Value?
                        if (releasesList.ElementAt(i).Id != _apiReleaseList.ElementAt(i).Id ||
                            releasesList.ElementAt(i).Name != _apiReleaseList.ElementAt(i).Name ||
                            releasesList.ElementAt(i).NumberOfCards != _apiReleaseList.ElementAt(i).NumberOfCards
                            )
                            hasChanges = true;
                    }

                    if (hasChanges)
                        APIHasUpdate();
                    else
                    {
                        await Task.Delay(1000);
                        lblVersion.Foreground = Brushes.Green;
                        lblVersion.Content = "Latest Version";
                    }
                }
            });
        }

        private void btnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void lbReleases_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void cbSorter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void cbSorterAorD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

    }
}
