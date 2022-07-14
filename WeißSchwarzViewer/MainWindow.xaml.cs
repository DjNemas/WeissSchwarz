using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
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
using Microsoft.WindowsAPICodePack.Dialogs;
using WeißSchwarzViewer.WPFHelper;
using System.Text.RegularExpressions;

namespace WeißSchwarzViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly float _AppVersion = 1.3f;
#if DEBUG
        [DllImport("Kernel32")]
        private static extern void AllocConsole();
#endif
        private HttpClient _httpClient;

        private bool stopDownloadingImages = false;

        private static object locker = new();

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
            lbSets.ItemsSource = ObsLists.Sets;
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
            lblProcess.Content = "Updating... 0%";
            lblProcess.Foreground = Brushes.DarkCyan;
            lblVersion.Content = "Updating...";
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
                    lblProcess.Content = "Updating..." + processBar.Value + "%";
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
            lbSets.IsEnabled = false;
        }

        private void EnableUI()
        {
            lbSets.IsEnabled = true;
        }

        private void lbSets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbSets.SelectedItems.Count <= 1)
            {
                Set? set = lbSets.SelectedItem as Set;
                if (set != null && lbSets.SelectionMode == SelectionMode.Single)
                {
                    cbSorter.SelectedIndex = 0;
                    cbSorter.IsEnabled = true;
                    cbSorterAorD.SelectedIndex = 0;
                    cbSorterAorD.IsEnabled = true;
                    UpdateCardsItemBox(set.Cards);
                }
            }
            else
            {
                cbSorter.IsEnabled = false;
                cbSorterAorD.IsEnabled = false;
                ObsLists.Cards.Clear();
            }
                
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
            SortCards();
        }

        private void cbSorterAorD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SortCards();
        }

        private void SortCards()
        {
            if (cbSorterAorD.SelectedItem == null || cbSorter.SelectedItem == null)
                return;

            var sorterac = Enum.GetName(typeof(ComboBoxEnumSortAorDescending), cbSorterAorD.SelectedItem);
            var sorter = Enum.GetName(typeof(ComboBoxEnumSort), cbSorter.SelectedItem);

            List<Card> cardsList = null;

            switch (sorter)
            {
                case "Id":
                    {
                        if (sorterac == "Ascending")
                            cardsList = ObsLists.Cards.OrderBy(x => x.LongID).ToList();
                        else if ((sorterac == "Descending"))
                            cardsList = ObsLists.Cards.OrderByDescending(x => x.LongID).ToList();
                        break;
                    }
                case "Name":
                    {
                        if (sorterac == "Ascending")
                            cardsList = ObsLists.Cards.OrderBy(x => x.Name).ToList();
                        else if ((sorterac == "Descending"))
                            cardsList = ObsLists.Cards.OrderByDescending(x => x.Name).ToList();
                        break;
                    }
                case "Rarity":
                    {
                        if (sorterac == "Ascending")
                            cardsList = ObsLists.Cards.OrderBy(x => x.Rarity).ToList();
                        else if ((sorterac == "Descending"))
                            cardsList = ObsLists.Cards.OrderByDescending(x => x.Rarity).ToList();
                        break;
                    }
                case "Color":
                    {
                        if (sorterac == "Ascending")
                            cardsList = ObsLists.Cards.OrderBy(x => x.Color).ToList();
                        else if ((sorterac == "Descending"))
                            cardsList = ObsLists.Cards.OrderByDescending(x => x.Color).ToList();
                        break;
                    }
                case "Type":
                    {
                        if (sorterac == "Ascending")
                            cardsList = ObsLists.Cards.OrderBy(x => x.Type).ToList();
                        else if ((sorterac == "Descending"))
                            cardsList = ObsLists.Cards.OrderByDescending(x => x.Type).ToList();
                        break;
                    }
                case "Level":
                    {
                        if (sorterac == "Ascending")
                            cardsList = ObsLists.Cards.OrderBy(x => x.Level).ToList();
                        else if ((sorterac == "Descending"))
                            cardsList = ObsLists.Cards.OrderByDescending(x => x.Level).ToList();
                        break;
                    }
                case "Cost":
                    {
                        if (sorterac == "Ascending")
                            cardsList = ObsLists.Cards.OrderBy(x => x.Cost).ToList();
                        else if ((sorterac == "Descending"))
                            cardsList = ObsLists.Cards.OrderByDescending(x => x.Cost).ToList();
                        break;
                    }
                case "Power":
                    {
                        if (sorterac == "Ascending")
                            cardsList = ObsLists.Cards.OrderBy(x => x.Power).ToList();
                        else if ((sorterac == "Descending"))
                            cardsList = ObsLists.Cards.OrderByDescending(x => x.Power).ToList();
                        break;
                    }
                case "Soul":
                    {
                        if (sorterac == "Ascending")
                            cardsList = ObsLists.Cards.OrderBy(x => x.Soul).ToList();
                        else if ((sorterac == "Descending"))
                            cardsList = ObsLists.Cards.OrderByDescending(x => x.Soul).ToList();
                        break;
                    }
            }
            UpdateCardsItemBox(cardsList);
        }

        private void lbCards_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbCards.SelectedItem == null)
                return; 
            Application.Current.Dispatcher.Invoke(() =>
            {
                Card card = lbCards.SelectedItem as Card;
                BitmapImage bmImage = new();
                bmImage.DownloadProgress += BmImage_DownloadProgress;
                bmImage.DownloadFailed += BmImage_DecodeFailed;
                bmImage.DecodeFailed += BmImage_DecodeFailed;

                bmImage.BeginInit();
                bmImage.UriSource = new Uri(card.ImageURL);
                bmImage.EndInit();
                imgCardImage.Source = bmImage;
                tbCardImage.Visibility = Visibility.Hidden;
                imgCardImage.Visibility = Visibility.Visible;                
            });
            
        }

        private void BmImage_DecodeFailed(object? sender, ExceptionEventArgs e)
        {
            tbCardImage.Text = "Could not load Image :c";
            imgCardImage.Visibility = Visibility.Hidden;
            tbCardImage.Visibility = Visibility.Visible;
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

        private void btnMultiple_Click(object sender, RoutedEventArgs e)
        {
            if (lbSets.SelectionMode == SelectionMode.Single)
            {
                ObsLists.Cards.Clear();
                btnMultiple.Background = Brushes.LightGreen;
                lbSets.SelectionMode = SelectionMode.Multiple;
                btnAll.IsEnabled = true;
                MessageBox.Show("Please click on multiple mode on right side of each element to select them.\nWPF is pain sometimes to code <.<");
            }
            else if (lbSets.SelectionMode == SelectionMode.Multiple)
            {
                btnMultiple.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 221, 221, 221));
                lbSets.SelectionMode = SelectionMode.Single;
                btnAll.IsEnabled = false;
            }

        }

        private void btnDPath_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                tbDirectory.Text = dialog.FileName;
        }

        private async void btnDowload_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbDirectory.Text))
            {
                MessageBox.Show("Please select a folder first.");
                return;
            }
            if (lbSets.SelectedItems.Count < 1)
            {
                MessageBox.Show("Please select minimum one Set.");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                stopDownloadingImages = false;
                btnDowload.IsEnabled = false;
                btnStop.IsEnabled = true;
                lblProcess.Content = "Downloading... 0%";
                lblProcess.Foreground = Brushes.DarkCyan;
            });

            // Copy Card in new List
            List<Set> copyList = new();
            foreach (Set set in lbSets.SelectedItems)
            {
                copyList.Add(set);
            }
            float count = 1;
            float countCards = 0;
            string folderDir = tbDirectory.Text;

            // Calculate Count for Progressbar
            foreach (Set set in copyList)
            {
                countCards += set.Cards.Count();
            }
            // Start Iterate trough all cards
            foreach (Set set in copyList)
            {
                string setFolderPath = System.IO.Path.Combine(folderDir, FixInvalidCharsInFile(set.Name) + " - " + Enum.GetName(set.Type));
                setFolderPath = FixInvalidCharsInPath(setFolderPath);
                if (!Directory.Exists(setFolderPath) && !stopDownloadingImages)
                    Directory.CreateDirectory(setFolderPath);

                await Parallel.ForEachAsync(set.Cards, async (card, token) =>
                {
                    try
                    {
                        if (Application.Current.Dispatcher.Invoke(() => CheckStopButtonPressed()))
                            return;
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            lock (locker)
                            {
                                processBar.Value = (int)(100f / countCards * count++);
                                lblProcess.Content = "Downloading Set Images..." + processBar.Value + "%";
                            }
                        });

                        string cardFileName = card.LongID.Replace("/", "_").Replace("-", "_") + ".jpg";
                        cardFileName = FixInvalidCharsInFile(cardFileName);

                        // Reloop 5x when Exception is thrown
                        bool passed = false;
                        int attempt = 1;
                        do
                        {
                            try
                            {
                                byte[] data = await _httpClient.GetByteArrayAsync(card.ImageURL);
                                await File.WriteAllBytesAsync(System.IO.Path.Combine(setFolderPath, cardFileName), data);
                                passed = true;
                            }
                            catch (Exception e)
                            {
                                Log.Debug($"Card Download Failed. Attempt: {attempt}. Retry...");
                                if (attempt == 5)
                                {
                                    string message = $"Some error occured while downloading for Card.\n" +
                                        $"ID: {card.LongID}\n" +
                                        $"Name: {card.Name}\n" +
                                        $"From Set {set.Name} - {Enum.GetName(set.Type)}\n";
                                    string logMessage = $"\nFail log.txt can be found in Download Folder";

                                    File.AppendAllText(System.IO.Path.Combine(folderDir, "log.txt"), message);

                                    MessageBox.Show("[" + DateTime.Now + "] " + message + logMessage + "\n\n" + e);
                                    break;
                                }
                                attempt++;
                            }
                        } while (!passed);
                    }
                    catch (Exception e)
                    {
                        Log.Error("Fail:\n" + e);
                    }
                });
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                lblProcess.Content = "Done";
                lblProcess.Foreground = Brushes.Black;
                processBar.Value = 0;
                btnDowload.IsEnabled = true;
                btnStop.IsEnabled = false;
            });
        }

        private bool CheckStopButtonPressed()
        {
            if (stopDownloadingImages)
            {
                btnStop.IsEnabled = false;
                btnDowload.IsEnabled = true;
                lblProcess.Content = "Stopped";
                lblProcess.Foreground = Brushes.Black;
                processBar.Value = 0;
                return true;
            }
            return false;
        }

        private string FixInvalidCharsInPath(string str)
        {
            string fixedString = str;
            foreach(char c in System.IO.Path.GetInvalidPathChars())
            {
                fixedString = fixedString.Replace(c, ' ');
            }
            return fixedString;
        }

        private string FixInvalidCharsInFile(string str)
        {
            string fixedString = str;
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                fixedString = fixedString.Replace(c, ' ');
            }
            return fixedString;
        }

        private void btnAll_Click(object sender, RoutedEventArgs e)
        {
            lbSets.SelectAll();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            stopDownloadingImages = true;
        }
    }
}
