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
using static WeißSchwarzViewer.DB.DatabaseContext;

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
        private void CheckForUpdate()
        {
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                lblVersion.Foreground = Brushes.Cyan;
                lblVersion.Content = "Checking...";

                DatabaseContext db = new();
                LocalDataVersion? version = await db.DataVersion.FirstOrDefaultAsync(x => x.ID == 1);
                // If no Data Exist in DB this will be null
                if (version == null)
                {
                    // Add Version 0
                    version = new LocalDataVersion() { Version = 0 };
                    db.DataVersion.Add(version);
                    db.SaveChanges();
                }
                // Check if new Data Exist on Rest API
                if (version.Version < int.Parse(await _httpClient.GetStringAsync(new Uri("https://djnemashome.de:3939/v1/ws/dataversion"))))
                {
                    APIHasUpdate();
                }
                else
                {
                    await Task.Delay(1000);
                    lblVersion.Foreground = Brushes.Green;
                    lblVersion.Content = "Latest Version";
                }
            });
        }

        private void APIHasUpdate()
        {
            btnUpdate.IsEnabled = true;
            lblVersion.Foreground = Brushes.Red;
            lblVersion.Content = "New Update Available";
        }

        private void btnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdate();
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                DisableUI();
                await UpdateSets();
                EnableUI();

                lblVersion.Content = "Latest Version";
                lblVersion.Foreground = Brushes.Green;
            });
        }

        private async Task UpdateSets()
        {
            btnUpdate.IsEnabled = false;
            lblProcess.Content = "Updateing... 0%";
            lblProcess.Foreground = Brushes.DarkCyan;
            lblVersion.Content = "Updateing...";
            lblVersion.Foreground = Brushes.DarkCyan;

            DatabaseContext db = new();
            int count = 1;
            Stream jsonDataStream = await _httpClient.GetStreamAsync(new Uri("https://djnemashome.de:3939/v1/ws/sets/all"));
            List<Set>? sets = null;
            try
            {
                sets = await JsonSerializer.DeserializeAsync<List<Set>?>(jsonDataStream);
            }
            catch (Exception ex)
            {
                Log.Error("Error occures whil Deserializeing Data from Rest API\n" + ex);
            }
            finally
            {
                jsonDataStream.Close();
                jsonDataStream.Dispose();
            }
            if (sets != null)
            {
                foreach (var set in sets)
                {
                    processBar.Value = (int)(100 / (float)sets.Count() * count++);
                    lblProcess.Content = "Updateting..." + processBar.Value + "%";
                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                    Set? setDB = await db.Sets.FirstOrDefaultAsync(x => x.ID == set.ID);
                    if (setDB == null)
                    {
                        try
                        {
                            await db.Sets.AddAsync(set);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Could not Add SetCards to Releases.\n" + ex);
                        }
                    }
                }
                try
                {
                    // Get new Database Version
                    string dataVersionAPI = await _httpClient.GetStringAsync(new Uri("https://djnemashome.de:3939/v1/ws/dataversion"));
                    LocalDataVersion? localVersion = db.DataVersion.FirstOrDefault(x => x.ID == 1);
                    localVersion.Version = int.Parse(dataVersionAPI);

                    // Save all To DB
                    await db.SaveChangesAsync();

                    // Load All New Data From DB and Fill UI
                    LoadDataFromDB();

                    // Change UI Lable back
                    lblProcess.Content = "Done";
                    lblProcess.Foreground = Brushes.Black;
                    processBar.Value = 0;
                }
                catch (Exception ex)
                {
                    Log.Error("Could not Save new Data to Database.\n" + ex);
                    lblProcess.Content = "Error";
                    lblProcess.Foreground = Brushes.Red;
                    processBar.Value = 0;
                }
                finally
                {
                    db.Dispose();
                }
            }
            else
            {
                db.Dispose();
                lblProcess.Content = "Error";
                lblProcess.Foreground = Brushes.Red;
                processBar.Value = 0;
            }
        }

        private void DisableUI()
        {
            lbReleases.IsEnabled = false;
        }

        private void EnableUI()
        {
            lbReleases.IsEnabled = true;
        }

        private void lbSets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Set? set = lbReleases.SelectedItem as Set;
            UpdateCardsItemBox(set.Cards);
        }

        private void UpdateCardsItemBox(List<Card> cardsList)
        {
            ObsLists.Cards.Clear();
            foreach (var item in cardsList)
            {
                ObsLists.Cards.Add(item);
            }
        }

        private void cbSorter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void cbSorterAorD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void lbCards_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbCards.SelectedItem == null)
                return;
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                Card card = lbCards.SelectedItem as Card;
                BitmapImage bmImage = new();
                bmImage.BeginInit();
                bmImage.DownloadProgress += BmImage_DownloadProgress;
                bmImage.UriSource = new Uri(card.ImageURL);
                bmImage.EndInit();
                imgCardImage.Source = bmImage;
            });
            
        }

        private void BmImage_DownloadProgress(object? sender, DownloadProgressEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                processBar.Value = e.Progress;
                lblProcess.Content = $"Downloading Image... {e.Progress}";
                if (e.Progress == 100)
                {
                    lblProcess.Content = "Done";
                    processBar.Value = 0;
                }
            });
        }
    }
}
