﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WeißSchwarzSharedClasses;
using WeißSchwarzSharedClasses.Models;
using WeißSchwarzViewer.DB;
using WeißSchwarzViewer.UI;
using static WeißSchwarzViewer.DB.DatabaseContext;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Runtime.InteropServices;

namespace WeißSchwarzViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly float _AppVersion = 1.5f;
#if DEBUG
        [DllImport("Kernel32")]
        private static extern void AllocConsole();
#endif
        private HttpClient _httpClient;

        private bool stopDownloading = false;

        private static object locker = new();

        private static TimeSpan _searchWaitTime = new();

        private static Task? _searchTask;

        private static DateTime _lastUserInputTime = DateTime.Now;

        private static readonly TimeSpan _timeToWait = TimeSpan.FromMilliseconds(300);

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
            LoadSetDataFromDB().Wait();
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
        private async Task LoadSetDataFromDB(string searchTitleString = "")
        {
            // Clear Obs
            ObsLists.Sets.Clear();

            DatabaseContext db = new();
            List<Set> sets = new();

            // Load All
            if(searchTitleString == string.Empty)
            {
                sets = await db.Sets
                .Include(x => x.Cards).ThenInclude(x => x.Traits)
                .Include(x => x.Cards).ThenInclude(x => x.Triggers)
                .ToListAsync();
            }
            else // Load By Name Filter
            {
                sets = sets = await db.Sets
                .Include(x => x.Cards).ThenInclude(x => x.Traits)
                .Include(x => x.Cards).ThenInclude(x => x.Triggers)
                .Where(s => s.Name.ToLower().Contains(searchTitleString.ToLower()))
                .ToListAsync();
            }

            // Sort            
            var orderSet = sets.OrderBy(x => x.Name);

            // Fill Obs
            foreach (var item in orderSet)
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
                if (version.Version < int.Parse(await _httpClient.GetStringAsync(new Uri("https://djnemas.de:3939/v1/ws/dataversion"))))
                {
                    APIHasUpdate();
                }
                else
                {
                    await Task.Delay(1000);
                    lblVersion.Foreground = Brushes.LightGreen;
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
                lblVersion.Foreground = Brushes.LightGreen;
            });
        }

        private async Task UpdateSets()
        {
            btnUpdate.IsEnabled = false;
            lblProcess.Content = "Updating... 0%";
            lblProcess.Foreground = Brushes.Cyan;
            lblVersion.Content = "Updating...";
            lblVersion.Foreground = Brushes.Cyan;

            DatabaseContext db = new();
            int count = 1;
            Stream jsonDataStream = await _httpClient.GetStreamAsync(new Uri("https://djnemas.de:3939/v1/ws/sets/all"));
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
                            EntityEntry<Set> result = await db.Sets.AddAsync(set);
                            Console.WriteLine(result.CurrentValues.GetValue<int>("ID")); 
                            await db.SaveChangesAsync();
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
                    string dataVersionAPI = await _httpClient.GetStringAsync(new Uri("https://djnemas.de:3939/v1/ws/dataversion"));
                    LocalDataVersion? localVersion = db.DataVersion.FirstOrDefault(x => x.ID == 1);
                    localVersion.Version = int.Parse(dataVersionAPI);

                    // Save all To DB
                    await db.SaveChangesAsync();

                    // Load All New Data From DB and Fill UI
                    LoadSetDataFromDB();

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


                    lblVersion.Content = "Error Occured. Try to fix after 5 sec...";
                    lblVersion.Foreground = Brushes.Red;
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    lblVersion.Foreground = Brushes.Aqua;
                    lblVersion.Content = "Fix ongoing please wait...";

                    await FixDBOnError();

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

        private async Task FixDBOnError()
        {
            lblProcess.Foreground = Brushes.Aqua;
            lblProcess.Content = "Fix";
            processBar.Value = 0;

            DatabaseContext db = new();
            db.Database.EnsureDeleted();
            processBar.Value = 50;

            db.Database.EnsureCreated();
            processBar.Value = 100;

            // Add Version 0
            LocalDataVersion version = new() { Version = 0 };
            db.DataVersion.Add(version);
            db.SaveChanges();

            await UpdateSets();
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

        private async void btnDowloadImages_Click(object sender, RoutedEventArgs e)
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
                stopDownloading = false;
                btnDowloadImages.IsEnabled = false;
                btnDownloadText.IsEnabled = false;
                btnStop.IsEnabled = true;
                lblProcess.Content = "Downloading... 0%";
                lblProcess.Foreground = Brushes.Cyan;
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
                if (!Directory.Exists(setFolderPath) && !stopDownloading)
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
                btnDowloadImages.IsEnabled = true;
                btnDownloadText.IsEnabled = true;
                btnStop.IsEnabled = false;
            });
        }

        private bool CheckStopButtonPressed()
        {
            if (stopDownloading)
            {
                btnStop.IsEnabled = false;
                btnDowloadImages.IsEnabled = true;
                btnDownloadText.IsEnabled = true;
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
            stopDownloading = true;
        }

        private async void btnDownloadText_Click(object sender, RoutedEventArgs e)
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
                stopDownloading = false;
                btnDowloadImages.IsEnabled = false;
                btnDownloadText.IsEnabled = false;
                btnStop.IsEnabled = true;
                lblProcess.Content = "Downloading... 0%";
                lblProcess.Foreground = Brushes.Cyan;
            });
            // Workaround for seeing Process in work. On Powerfull PCs its insta finished.
            await Task.Delay(TimeSpan.FromSeconds(2));

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
                if (!Directory.Exists(setFolderPath) && !stopDownloading)
                    Directory.CreateDirectory(setFolderPath);                

                bool? txtSelected = Application.Current.Dispatcher.Invoke(CheckedText);
                // JSON
                if (txtSelected != null && !(bool)txtSelected)
                {
                    if (Application.Current.Dispatcher.Invoke(() => CheckStopButtonPressed()))
                        return;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        lock (locker)
                        {
                            processBar.Value = (int)(100f / copyList.Count * count++);
                            lblProcess.Content = "Downloading Set Text..." + processBar.Value + "%";
                        }
                    });

                    string fileName = "JsonTextData.json";
                    string jsonString = JsonSerializer.Serialize(set);
                    try
                    {
                        File.WriteAllText(System.IO.Path.Combine(setFolderPath, fileName), jsonString);
                    }
                    catch (Exception)
                    {
                        string message = $"Some error occured while downloading JSON for Set.\n" +
                                $"From Set {set.Name} - {Enum.GetName(set.Type)}\n";
                        string logMessage = $"\nFail log.txt can be found in Download Folder";

                        File.AppendAllText(System.IO.Path.Combine(folderDir, "log.txt"), message);

                        MessageBox.Show("[" + DateTime.Now + "] " + message + logMessage + "\n\n" + e);
                        return;
                    }
                }
                // TXT
                else if (txtSelected != null && (bool)txtSelected)
                {
                    await Parallel.ForEachAsync(set.Cards, async (card, token) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            lock (locker)
                            {
                                processBar.Value = (int)(100f / countCards * count++);
                                lblProcess.Content = "Downloading Set Text..." + processBar.Value + "%";
                            }
                        });

                        if (Application.Current.Dispatcher.Invoke(() => CheckStopButtonPressed()))
                            return;

                        string cardFileName = card.LongID.Replace("/", "_").Replace("-", "_") + ".txt";
                        cardFileName = FixInvalidCharsInFile(cardFileName);

                        StringBuilder builder = new();
                        builder.AppendLine("ID: " + card.CardID);
                        builder.AppendLine("Name: " + card.Name);
                        builder.AppendLine("Rarity: " + card.Rarity);
                        builder.AppendLine("Color: " + Enum.GetName(card.Color));
                        builder.AppendLine("Type: " + card.Type);
                        builder.AppendLine("Level: " + card.Level);
                        builder.AppendLine("Cost: " + card.Cost);
                        builder.AppendLine("Power: " + card.Power);
                        builder.AppendLine("Soul: " + card.Soul);
                        if(card.Triggers != null)
                            foreach (var trigger in card.Triggers)
                            {
                                builder.AppendLine("Trigger: " + Enum.GetName(trigger.TriggerType));
                            }
                        if (card.Traits != null)
                            foreach (var trait in card.Traits)
                            {
                                builder.AppendLine("Trait: " + trait.Name);
                            }
                        builder.AppendLine("Skill: " + card.SkillText);
                        builder.AppendLine("Flavor: " + card.FalvorText);
                        builder.AppendLine("Illustration: " + card.IllustrationText);

                        try
                        {
                            File.WriteAllText(System.IO.Path.Combine(setFolderPath, cardFileName), builder.ToString());

                        }
                        catch (Exception e)
                        {
                            string message = $"Some error occured while downloading TXT for Set.\n" +
                                $"From Set {set.Name} - {Enum.GetName(set.Type)}\n";
                            string logMessage = $"\nFail log.txt can be found in Download Folder";

                            File.AppendAllText(System.IO.Path.Combine(folderDir, "log.txt"), message);

                            MessageBox.Show("[" + DateTime.Now + "] " + message + logMessage + "\n\n" + e);
                            return;
                        }
                    });
                }
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                lblProcess.Content = "Done";
                lblProcess.Foreground = Brushes.Black;
                processBar.Value = 0;
                btnDowloadImages.IsEnabled = true;
                btnDownloadText.IsEnabled = true;
                btnStop.IsEnabled = false;
            });

        }
        private bool? CheckedText()
        {
            if (rbTextFormatTXT.IsChecked != null && (bool)rbTextFormatTXT.IsChecked)
                return true;
            else if (rbTextFormatJSON.IsChecked != null && (bool)rbTextFormatJSON.IsChecked)
                return false;
            else
                return null;
        }

        private async void tbSearchFieldSet_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox is null)
                return;

            if(_searchTask is null || _searchTask.IsCompleted)
                _searchTask = Task.Run(() => StartSearchFieldWaitTimer());                

            if (DateTime.Now.Subtract(_lastUserInputTime) < _timeToWait)
            {
                _lastUserInputTime = DateTime.Now;
                _searchWaitTime = new();
                return;
            }
        }

        private async Task StartSearchFieldWaitTimer()
        {
            _searchWaitTime = new();
            while (_searchWaitTime < _timeToWait)
            {
                _searchWaitTime = _searchWaitTime.Add(TimeSpan.FromMilliseconds(5));
                await Task.Delay(TimeSpan.FromMilliseconds(5));
            }

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (tbSearchFieldSet.Text == string.Empty)
                    await LoadSetDataFromDB();
                else
                    await LoadSetDataFromDB(tbSearchFieldSet.Text);
            });
        }
    }
}
