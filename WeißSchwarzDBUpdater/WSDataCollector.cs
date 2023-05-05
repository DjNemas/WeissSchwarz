using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WeißSchwarzSharedClasses;
using WeißSchwarzSharedClasses.DB;
using WeißSchwarzSharedClasses.Models;
using static System.Net.Mime.MediaTypeNames;

namespace WeißSchwarzDBUpdater
{
    internal class WSDataCollector
    {
        private readonly string wsURL = @"https://en.ws-tcg.com/cardlist/list/";

        private Selenium mainWebsite;

        private readonly TimeSpan clickDelay = TimeSpan.FromSeconds(8); // In Sec

        private string mainWindowName;

        private readonly string chromeDriverPath;

        private readonly bool headless;

        private readonly bool logWithEx = false; // Set true for more detailed Exception Log

        private readonly int instancesOfTasks = 1;

        private readonly int beginFromSet = 1 - 1; // (x) - 1 default x = 1

        private readonly int endFromSet = 150;  // (x)

        private readonly bool onlyRange = false; // If true only from begin to range
 
        public WSDataCollector(string chromeDriverPath, string chromePath, bool headless)
        {
            this.chromeDriverPath = chromeDriverPath;
            this.headless = headless;
            this.mainWebsite = new(chromeDriverPath, chromePath, headless);
        }

        public void StartCollect()
        {
            while(true)
            {
                mainWebsite.Driver.Navigate().GoToUrl(wsURL);
                mainWindowName = mainWebsite.Driver.CurrentWindowHandle;
                var setLinkElements = SelectSetLinks(mainWebsite.Driver);
                IterateClickOnSet(setLinkElements);

                // Wait until next Day 0 Hour and redo the collection.
                DateTime nextDay = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0).AddDays(1);
                TimeSpan waitUntilNextDay = nextDay.Subtract(DateTime.UtcNow);
                Task.Delay(waitUntilNextDay).Wait();
            }
            
        }

        private void IterateClickOnSet(ReadOnlyCollection<IWebElement> sets)
        {
            List<string> tapList = new List<string>();
            Log.Info(sets.Count + " Sets found.");

            // Define How much Task allowed at same Time
            SemaphoreSlim maxThread = new SemaphoreSlim(instancesOfTasks);
            List<Task> taskListToWait = new();

            // from range or all sets
            int range = sets.Count;
            if (onlyRange)
                range = endFromSet;

            // Load every Set into Task.
            for (int i = beginFromSet; i < range; i++)
            {
                maxThread.Wait();
                Log.Info("Start Task for Set " + (i + 1));
                taskListToWait.Add(Task.Factory.StartNew((index) =>
                {                    
                    // Open New Window and Load Weiß Schwarz Page
                    Selenium window = CreateNewWindow(headless);
                    ClickOnXSet(window.Driver, (int)index);
                    // Wait for first Card Information Loaded.
                    IWebElement cardNo = GetFirstCardNumber(window.Driver, (int)index);

                    // Check if Set Already in DB, if yes close Window and continue with next Set
                    if (CheckCardInDB(cardNo))
                    {
                        Log.Info("First Card found in DB! Set " + sets[(int)index].Text + " already in Database. Continue with next Set.");
                        window.Driver.Close();
                        window.Driver.Dispose();
                        // End Task
                        return;
                    }
                    // Start Collection
                    IterateEveryPage(window.Driver, (int)index + 1);
                    window.Driver.Close();
                    window.Driver.Dispose();

                    // Some Task Settings
                }, i, TaskCreationOptions.LongRunning)
                .ContinueWith((task) => maxThread.Release()));

                Task.Delay(TimeSpan.FromMilliseconds(10)).Wait();
            }
            Task.WaitAll(taskListToWait.ToArray());
            mainWebsite.Driver.Close();
            mainWebsite.Driver.Dispose();
            // Update DataVersion in DB if new Data was Found
            // 
            Program.db.Dispose();
            Console.WriteLine("Finished :D");
            Console.ReadKey();
        }

        private IWebElement GetFirstCardNumber(ChromeDriver driver, int setIndex)
        {
            IWebElement cardNo = null;
            do
            {
                try
                {
                    cardNo = driver.FindElement(By.Id("expansionDetail_table")).FindElement(By.XPath("table/tbody/tr/td"));
                }
                catch (Exception ex)
                {
                    // If Fail Reload Page
                    Log.Error("Fail to Get CardNo. Reload Page...");
                    if (logWithEx) Log.Error(ex.ToString());
                    ClickOnXSet(driver, setIndex);
                }
            } while (cardNo == null);
            return cardNo;
        }

        private void ClickOnXSet(ChromeDriver driver, int setIndex)
        {
            driver.Navigate().GoToUrl(wsURL);
            // Accept Cookie
            AcceptCookies(driver);
            // Select all sets for this Driver again and click on Set
            var sets = SelectSetLinks(driver);
            // Click on Set and Wait 2 sec to load new Dom
            new Actions(driver).Click(sets[setIndex]).Pause(TimeSpan.FromSeconds(5)).Perform();
        }

        private void AcceptCookies(ChromeDriver driver)
        {
            bool acceptedCookie = false;
            do
            {
                try
                {
                    driver.FindElement(By.Id("CybotCookiebotDialogBodyLevelButtonAccept")).Click();
                    acceptedCookie = true;
                }
                catch (Exception ex)
                {
                    acceptedCookie = false;
#if DEBUG
                    Log.Error("Bad Gateway on Cookies.\n" + ex);
#else
                    Log.Error("Bad Gateway on Cookies.\n");
#endif
                    if (logWithEx) Log.Error(ex.ToString());
                    driver.Navigate().GoToUrl(wsURL);
                    new Actions(driver).Pause(clickDelay).Perform();

                }
            }
            while (!acceptedCookie);
        }


        private void IterateEveryPage(ChromeDriver driver, int currentSetNumber)
        {
            // Get Pages
            int pages = GetPageCount(driver);

            Dictionary<int, List<Card>> cards = new Dictionary<int, List<Card>>();
            cards.Add(currentSetNumber, new List<Card>());
            // Iterate every Page
            for (int i = 1; i <= pages; i++)
            {
                Log.Info("Start collecting Page " + i + "/" + pages + " for Set " + currentSetNumber);
                // Get Card Information on Page i
                IterateCards(driver, cards, currentSetNumber);
                if (i < pages)
                    NavigateNextSetPage(driver, i + 1, currentSetNumber);
            }
            Log.Info("Set " + currentSetNumber + " Collected. Add Set to Database...");
            // Set To DB
            AddNewSetDB(driver, cards, currentSetNumber);
            Log.Info("Done with Set " + currentSetNumber);
        }
        private void AddNewSetDB(ChromeDriver driver, Dictionary<int, List<Card>> cards, int currentSetNumber)
        {
            // Build Set
            // ID
            string setID = cards[currentSetNumber][0].LongID.Split("/")[1];
            setID = setID.Substring(0, setID.LastIndexOf("-"));

            string typeandname = driver.FindElement(By.Id("expansionHeader")).FindElement(By.XPath("h3")).Text;
            // Set
            SetType setType = GetSetType(typeandname.Split("-")[0]);
            // Name
            string setName = typeandname.Substring(typeandname.IndexOf("-") + 2, typeandname.Length - (typeandname.IndexOf("-") + 2));

            Set set = new Set()
            {
                SetID = setID,
                Name = setName,
                Type = setType,
                Cards = cards[currentSetNumber]
            };

            WSContext wsContext = new WSContext();
            wsContext.Sets.Add(set);
            // Save to DB
            try
            {
                wsContext.SaveChanges();
                IncreaseVersionNumber();
            } 
            catch (Exception ex)
            {
                Log.Error("Error on Saving Set to DB.\n" + ex);
            }
            
            wsContext.Dispose();
        }

        private void IncreaseVersionNumber()
        {
            DataVersion version = Program.db.DataVersion.FirstOrDefault(x => x.ID == 1);
            if (version == null)
            {
                Program.db.DataVersion.Add(new DataVersion() { Version = 1 });
            }
            else
                version.Version += 1;
            Program.db.SaveChanges();
        }

        private SetType GetSetType(string str)
        {
            SetType setType = SetType.None;
            if (str.Contains("Booster pack"))
                setType = SetType.BoosterPack;
            else if (str.Contains("Extra pack"))
                setType = SetType.ExtraPack;
            else if (str.Contains("Trial deck"))
                setType = SetType.TrialDeck;
            else if (str.Contains("PR card"))
                setType = SetType.PRCard;
            else if (str.Contains("Others"))
                setType = SetType.Others;
            return setType;
        }

        private void NavigateNextSetPage(ChromeDriver driver, int page, int currentSetNumber)
        {
            Log.Info("Move to next Page for Set " + currentSetNumber);
            int currentPage;
            do
            {
                // Preform Click
                IWebElement element = driver.FindElement(By.Id("expansionDetail")).FindElements(By.XPath("p/a")).FirstOrDefault(x => x.Text == page.ToString());
                new Actions(driver)
                        .Click(element)
                        .Pause(clickDelay)
                        .Perform();
                // Check if on Correct Page, else reloop
                currentPage = int.Parse(driver.FindElement(By.Id("expansionDetail")).FindElement(By.XPath("p/strong")).Text);
                if (page != currentPage)
                    Log.Error("Wrong Page. Click Again for Set " + currentSetNumber);
            } while (page != currentPage);
        }

        private void IterateCards(ChromeDriver driver, Dictionary<int, List<Card>> cards, int currentSetNumber)
        {
            // Get Every Card
            ReadOnlyCollection<IWebElement> tabelA = driver.FindElement(By.Id("expansionDetail_table")).FindElements(By.XPath("table/tbody/tr/td/a"));
            List<string> currentPageCardList = GetCurrentPageCardsLinks(tabelA);
            Log.Info("Found " + currentPageCardList.Count + " Cards for Set " + currentSetNumber);
            // Open multiple instances to Collect faster Data
            List<Task> seleniumInstances = new();
            Log.Info("Open New Tap for each Card for Set " + currentSetNumber);

            int counter = 1;
            foreach (var url in currentPageCardList)
            {
                seleniumInstances.Add(Task.Run(() =>
                {
                    Log.Info("Collecting Card " + counter + "/" + currentPageCardList.Count + " for Set " + currentSetNumber);

                    // Open New Card Window
                    Selenium newWindow = CreateNewWindow(headless);
                    // Load Card URL
                    newWindow.Driver.Navigate().GoToUrl(url);
                    // Collect Card Data
                    CollectCardData(newWindow.Driver, cards, currentSetNumber, url);
                    // Close Card Window
                    newWindow.Driver.Close();
                    newWindow.Driver.Dispose();
                }));
                Task.Delay(TimeSpan.FromSeconds(10)).Wait();
                counter++;
            }
            Task.WaitAll(seleniumInstances.ToArray());
        }

        private Selenium CreateNewWindow(bool headless)
        {
            var service = ChromeDriverService.CreateDefaultService(chromeDriverPath);
            // Don't use Console
            service.HideCommandPromptWindow = true;
            return new Selenium(service, headless);
        }

        private void CollectCardData(ChromeDriver driver, Dictionary<int, List<Card>> cards, int currentSetNumber, string cardURL)
        {
            // Get Table of Card Value.
            // Index [0] = Image / Card Name
            // Index [1] = Card No. (Contains Serie/Set-Card ID) / Rarity
            // Index [2] = Set / Side 
            // Index [3] = Card Type / Color
            // Index [4] = Level / Cost
            // Index [5] = Power / Soul
            // Index [6] = Trigger / Trait
            // Index [7] = SkillText
            // Index [8] = FlavorText
            // Index [9] = Illustrator !! CAUTION !! This index doesn't exist on every Card! 
            ReadOnlyCollection<IWebElement> cardDetail = null;

            // Prevent Bad Gateway with while loop
            bool error = false;
            do
            {
                try
                {
                    cardDetail = driver.FindElement(By.Id("cardDetail")).FindElements(By.XPath("table/tbody/tr"));
#region Index [0]
                    // ImageURL
                    string cardImageURL = cardDetail[0].FindElement(By.XPath("td/img")).GetAttribute("src");
                    Log.Debug("ImageURL: " + cardImageURL);

                    // CardName
                    string cardName = string.Empty;
                    // Figure out if <shadow> Tag exist
                    try
                    {
                        cardName = cardDetail[0].FindElements(By.XPath("td"))[1].FindElement(By.XPath("shadow")).Text.Split("\n")[0];
                    }
                    // if no shadow tag do this
                    catch (Exception)
                    {
                        cardName = cardDetail[0].FindElements(By.XPath("td"))[1].Text.Split("\n")[0];
                    }

                    // Remove ASCII CR ("\r")
                    if(cardName.Contains("\r")) // sometimes no \r exist
                        cardName = cardName.Remove(cardName.IndexOf("\r"), 1);
                    Log.Debug("CardName: " + cardName);
#endregion

#region Index [1]
                    var elements = cardDetail[1].FindElements(By.XPath("td"));

                    // CardPrefix
                    string cardPrefix = elements[0].Text.Split("/")[0];
                    Log.Debug("CardPrefix: " + cardPrefix);

                    // CardID
                    string cardID = elements[0].Text.Substring(elements[0].Text.LastIndexOf("-") + 1, elements[0].Text.Length - (elements[0].Text.LastIndexOf("-") + 1));
                    Log.Debug("CardID: " + cardID);

                    // CardLongID
                    string cardLongID = elements[0].Text;
                    Log.Debug("CardLongID: " + cardLongID);

                    // Card Rarity
                    string cardRarity = elements[1].Text;
                    Log.Debug("Rarity: " + cardRarity);
#endregion

#region Index [2]
                    elements = cardDetail[2].FindElements(By.XPath("td"));
                    // Card Side
                    string sideImgURL = elements[1].FindElement(By.XPath("img")).GetAttribute("src");
                    Side cardSide = new Side();
                    if (sideImgURL.Contains("w.gif"))
                        cardSide = Side.Weiß;
                    else if (sideImgURL.Contains("s.gif"))
                        cardSide = Side.Schwarz;
                    Log.Debug("CardSide: " + cardSide.ToString());
#endregion

#region Index [3]
                    elements = cardDetail[3].FindElements(By.XPath("td"));
                    // Card Type
                    WeißSchwarzSharedClasses.Models.CardType cardType = WeißSchwarzSharedClasses.Models.CardType.None;
                    switch (elements[0].Text)
                    {
                        case "Character":
                            cardType = WeißSchwarzSharedClasses.Models.CardType.Character;
                            break;
                        case "Event":
                            cardType = WeißSchwarzSharedClasses.Models.CardType.Event;
                            break;
                        case "Climax":
                            cardType = WeißSchwarzSharedClasses.Models.CardType.Climax;
                            break;
                    }
                    Log.Debug("CardType: " + cardType.ToString());
                    // Color
                    Color cardColor = Color.None;
                    string colorLink = null;

#region Fix Card RWBY/BRO2021-01 PR
                    if (cardLongID == "RWBY/BRO2021-01 PR")
                        colorLink = "green.gif";
                    else if (cardLongID == "FS/S36-PE02")
                        colorLink = "red.gif";
#endregion
                    else
                        colorLink = elements[1].FindElement(By.XPath("img")).GetAttribute("src");

                    if (colorLink.Contains("yellow.gif"))
                        cardColor = Color.Yellow;
                    else if (colorLink.Contains("green.gif"))
                        cardColor = Color.Green;
                    else if (colorLink.Contains("red.gif"))
                        cardColor = Color.Red;
                    else if (colorLink.Contains("blue.gif"))
                        cardColor = Color.Blue;
                    Log.Debug("CardColor: " + cardColor.ToString());
#endregion

#region Index [4]
                    elements = cardDetail[4].FindElements(By.XPath("td"));
                    // Level
                    int? cardLevel = null;
                    if (elements[0].Text == string.Empty || elements[0].Text == "-" || elements[0].Text == "－" || elements[0].Text == "CX")
                        cardLevel = null;
                    else
                        cardLevel = int.Parse(elements[0].Text);
                    Log.Debug("Card Level: " + cardLevel.ToString());

                    // Cost
                    int? cardCost = null;
                    if (elements[1].Text == string.Empty || elements[1].Text == "-" || elements[1].Text == "－")
                        cardCost = null;
                    else
                        cardCost = int.Parse(elements[1].Text);
                    Log.Debug("CardCost: " + cardCost.ToString());
#endregion

#region Index [5]
                    elements = cardDetail[5].FindElements(By.XPath("td"));
                    // Power
                    int? cardPower = null;
                    if (elements[0].Text == string.Empty || elements[0].Text == "-" || elements[0].Text == "－" || elements[0].Text.Contains("戻"))
                        cardPower = null;
                    else
                        cardPower = int.Parse(elements[0].Text);
                    Log.Debug("CardPower: " + cardPower.ToString());

                    var imgElementsSouls = elements[1].FindElements(By.XPath("img"));
                    // Soul
                    int? cardSoul = null;
                    if (imgElementsSouls == null || imgElementsSouls.Count == 0)
                        cardSoul = null;
                    else
                        cardSoul = imgElementsSouls.Count;
                    Log.Debug("CardSoul: " + cardSoul.ToString());
#endregion

                    // Fix broken Table Card // Homepage doesn't create right doom
#region Fix Card SAO/S51-E017
                    if (cardLongID == "SAO/S51-E017")
                    {
                        List<Trigger> cardTriggerListFix = new();
                        cardTriggerListFix.Add(new Trigger() { TriggerType = TriggerType.Soul });

                        List<Trait> cardTraitListFix = new();
                        cardTraitListFix.Add(new Trait() { Name = "Net" });
                        cardTraitListFix.Add(new Trait() { Name = "Science" });

                        string cardSkillTextFix = "【AUTO】 When this card becomes 【REVERSE】 in battle, put the top card of your deck into your clock, and 【REST】 this card.";
                        string cardFalvorTextFix = "The man I am now believes… in the existence of a power that transcends the system itself…";
                        string cardIllustratorFix = "©2016 REKI KAWAHARA/PUBLISHED BY KADOKAWA CORPORATION ASCII MEDIA WORKS/SAO MOVIE Project";
                        Card cardFix = new()
                        {
                            CardID = cardID,
                            Prefix = cardPrefix,
                            LongID = cardLongID,
                            Name = cardName,
                            Type = cardType,
                            Color = cardColor,
                            Cost = cardCost,
                            FalvorText = cardFalvorTextFix,
                            IllustrationText = cardIllustratorFix,
                            ImageURL = cardImageURL,
                            Level = cardLevel,
                            Power = cardPower,
                            Rarity = cardRarity,
                            SkillText = cardSkillTextFix,
                            Soul = cardSoul,
                            Traits = cardTraitListFix,
                            Triggers = cardTriggerListFix,
                            Side = cardSide
                        };
                        cards[currentSetNumber].Add(cardFix);
                        break;
                    }
#endregion
#region Fix Card RZ/S46-E068 || RZ/S46-E068SP
                    if (cardLongID == "RZ/S46-E068" || cardLongID == "RZ/S46-E068SP")
                    {
                        List<Trigger> cardTriggerListFix = new();
                        cardTriggerListFix.Add(new Trigger() { TriggerType = TriggerType.Soul });

                        List<Trait> cardTraitListFix = new();
                        cardTraitListFix.Add(new Trait() { Name = "Death" });
                        cardTraitListFix.Add(new Trait() { Name = "Magic" });

                        string cardSkillTextFix = "【CONT】 Great Performance【AUTO】 Memory At the beginning of your climax phase, if a card named \"Subaru Natsuki\" is in your memory, choose one of your 《Magic》 or 《Weapon》 characters, and that character gets the following ability until end of turn. \"【AUTO】 When this card attacks, you may deal one damage to your opponent.\"(This damage may be canceled)【AUTO】 【CXCOMBO】 Memory[(7) Put four cards and a card named \"Like A Demon\" from your hand into your waiting room & Put this card into your memory] At the beginning of the encore step, if you have a card named \"Return by Death\" in your memory, and this card is 【REVERSE】, you may pay the cost.If you do, put all cards from your clock into your waiting room, all players return all their characters to their hand, and return all cards in their waiting room to their deck. Search your deck afterwards for up to one card named \"Subaru Natsuki\", put it on the stage position that this card was on, and all players shuffle their decks.";
                        string cardFalvorTextFix = null;
                        string cardIllustratorFix = "©Tappei Nagatsuki,PUBLISHED BY KADOKAWA CORPORATION/Re:ZERO PARTNERS";
                        Card cardFix = new()
                        {
                            CardID = cardID,
                            Prefix = cardPrefix,
                            LongID = cardLongID,
                            Name = cardName,
                            Type = cardType,
                            Color = cardColor,
                            Cost = cardCost,
                            FalvorText = cardFalvorTextFix,
                            IllustrationText = cardIllustratorFix,
                            ImageURL = cardImageURL,
                            Level = cardLevel,
                            Power = cardPower,
                            Rarity = cardRarity,
                            SkillText = cardSkillTextFix,
                            Soul = cardSoul,
                            Traits = cardTraitListFix,
                            Triggers = cardTriggerListFix,
                            Side = cardSide
                        };
                        cards[currentSetNumber].Add(cardFix);
                        break;
                    }
#endregion

#region Index [6]
                    elements = cardDetail[6].FindElements(By.XPath("td"));
                    // Trigger
                    List<Trigger> cardTriggerList = null;
                    var imgElementsTriggers = elements[0].FindElements(By.XPath("img"));

                    if (imgElementsTriggers == null || imgElementsTriggers.Count == 0)
                        imgElementsTriggers = null;
                    else
                    {
                        cardTriggerList = new();

                        foreach (var img in imgElementsTriggers)
                        {
                            cardTriggerList.Add(GetTrigger(img));
                        }
                    }
                    if (cardTriggerList != null)
                        foreach (var item in cardTriggerList)
                        {
                            Log.Debug("CardTrigger: " + item.TriggerType.ToString());
                        }

                    // Trait
                    List<Trait> cardTraitList = null;
                    if (elements[1].Text == string.Empty || elements[1].Text == "-" || elements[1].Text == "－")
                        cardTraitList = null;
                    else
                    {
                        if (elements[1].Text.Contains("・"))
                        {
                            string cardTrait1 = elements[1].Text.Split("・")[0];
                            string cardTrait2 = elements[1].Text.Split("・")[1];

                            if (cardTrait1 != "-" || cardTrait1 == "－")
                            {
                                cardTraitList = new();
                                cardTraitList.Add(new Trait() { Name = cardTrait1 });
                                if (cardTrait2 != "-" || cardTrait2 == "－")
                                    cardTraitList.Add(new Trait() { Name = cardTrait2 });
                            }
                        }
                        else
                        {
                            cardTraitList = new();
                            string trait = elements[1].Text;
                            if (trait[0] == ' ')
                                trait = trait.Remove(0, 1);
                            cardTraitList.Add(new Trait() { Name = elements[1].Text });
                        }
                    }
                    if (cardTraitList != null)
                        foreach (var item in cardTraitList)
                        {
                            Log.Debug("CardTrait: " + item.Name);
                        }
#endregion

#region Index [7]
                    elements = cardDetail[7].FindElements(By.XPath("td"));
                    string cardSkillText = null;
                    if (elements[0].Text == string.Empty || elements[0].Text == "-" || elements[0].Text == "－")
                        cardSkillText = null;
                    else
                        cardSkillText = elements[0].Text;
                    Log.Debug("CardSkillText: " + cardSkillText);
#endregion

#region Index [8]
                    elements = cardDetail[8].FindElements(By.XPath("td"));
                    string cardFalvorText = null;
                    if (elements[0].Text == string.Empty || elements[0].Text == "#NAME?" || elements[0].Text == "-" || elements[0].Text == "－")
                        cardFalvorText = null;
                    else
                        cardFalvorText = elements[0].Text;
                    Log.Debug("CardFalvorText: " + cardFalvorText);
#endregion

#region Index [9]
                    string cardIllustrator = null;
                    if (cardDetail.Count > 9)
                    {
                        elements = cardDetail[9].FindElements(By.XPath("td"));
                        if (elements[0].Text == string.Empty || elements[0].Text == "-" || elements[0].Text == "－")
                            cardIllustrator = null;
                        else
                            cardIllustrator = elements[0].Text;
                        Log.Debug("CardIllustrator: " + cardIllustrator);
                    }
#endregion

#region Create Card Object
                    Card card = new()
                    {
                        CardID = cardID,
                        Prefix = cardPrefix,
                        LongID = cardLongID,
                        Name = cardName,
                        Type = cardType,
                        Color = cardColor,
                        Cost = cardCost,
                        FalvorText = cardFalvorText,
                        IllustrationText = cardIllustrator,
                        ImageURL = cardImageURL,
                        Level = cardLevel,
                        Power = cardPower,
                        Rarity = cardRarity,
                        SkillText = cardSkillText,
                        Soul = cardSoul,
                        Traits = cardTraitList,
                        Triggers = cardTriggerList,
                        Side = cardSide
                    };
                    cards[currentSetNumber].Add(card);
#endregion

                    error = false;
                }
                catch (Exception ex)
                {
                    Log.Error("Bad Gateway in Card Collector Funktion for Set " + currentSetNumber + " Retry...");
                    if (logWithEx) Log.Error(ex.ToString());
                    driver.Navigate().GoToUrl(cardURL);
                    new Actions(driver).Pause(TimeSpan.FromSeconds(3)).Perform();
                    error = true;
                }

            } while (error);
        }

        private Trigger GetTrigger(IWebElement img)
        {
            Trigger trigger = new();
            string imgName = img.GetAttribute("src").Split("/").Last();
            switch (imgName)
            {
                case "soul.gif":
                    trigger.TriggerType = TriggerType.Soul;
                    break;
                case "bounce.gif":
                    trigger.TriggerType = TriggerType.Bounce;
                    break;
                case "treasure.gif":
                    trigger.TriggerType = TriggerType.Treasure;
                    break;
                case "draw.gif":
                    trigger.TriggerType = TriggerType.Draw;
                    break;
                case "gate.gif":
                    trigger.TriggerType = TriggerType.Gate;
                    break;
                case "stock.gif":
                    trigger.TriggerType = TriggerType.Stock;
                    break;
                case "salvage.gif":
                    trigger.TriggerType = TriggerType.Salvage;
                    break;
                case "shot.gif":
                    trigger.TriggerType = TriggerType.Shot;
                    break;
                case "choice.gif":
                    trigger.TriggerType = TriggerType.Choice;
                    break;
                case "standby.gif":
                    trigger.TriggerType = TriggerType.StandBy;
                    break;
            }
            return trigger;
        }

        private int GetPageCount(ChromeDriver driver)
        {
            ReadOnlyCollection<IWebElement> pages = driver.FindElement(By.Id("expansionDetail")).FindElements(By.XPath("p/a"));
            // Select second last Page
            return Convert.ToInt32(pages[pages.Count - 2].Text);
        }

        private List<string> GetCurrentPageCardsLinks(ReadOnlyCollection<IWebElement> tabelA)
        {
            List<string> list = new();
            foreach (var a in tabelA)
            {
                string href = a.GetAttribute("href");
                if (href != null && href.Contains("/?cardno="))
                    list.Add(href);
            }
            return list;
        }

        private bool CheckCardInDB(IWebElement cardNo)
        {
            string cardLongID = cardNo.Text;
            WSContext newContext = new WSContext();
            Card card = newContext.Cards.FirstOrDefault(x => x.LongID == cardLongID);
            newContext.Dispose();
            if (card != null)
                return true;
            else
                return false;
        }

        private ReadOnlyCollection<IWebElement> SelectSetLinks(ChromeDriver driver)
        {
            ReadOnlyCollection<IWebElement> elements = null;
            do
            {
                try
                {
                    elements = driver.FindElement(By.Id("expansionList")).FindElements(By.XPath("div/ul/li/a"));
                }
                catch (Exception ex)
                {
                    Log.Error("Bad Getway on Start URL");
                    if (logWithEx) Log.Error(ex.ToString());
                    driver.Navigate().GoToUrl(wsURL);
                    AcceptCookies(driver);
                }

            } while (elements == null);

            return elements;
        }
    }
}
