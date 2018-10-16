using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.IO;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Utilities;

//things to do

// set sleep time config
// set up remote pause command with true/false toggle in config


namespace Always_On_Server
{
    class ModConfig
    {
        public string serverHotKey { get; set; } = Keys.F9.ToString();
        public int timeOfDayToSleep { get; set; } = 2200;
        public bool festivalsOn { get; set; } = true;
        public bool clientsCanPause { get; set; } = false;
        public bool copyInviteCodeToClipboard { get; set; } = true;

        public int eggHuntCountDownConfig { get; set; } = 60;
        public int flowerDanceCountDownConfig { get; set; } = 60;
        public int luauSoupCountDownConfig { get; set; } = 60;
        public int jellyDanceCountDownConfig { get; set; } = 60;
        public int grangeDisplayCountDownConfig { get; set; } = 60;
        public int iceFishingCountDownConfig { get; set; } = 60;


    }






    public class ModEntry : Mod
    {
        /// <summary>The mod configuration from the player.</summary>
        private ModConfig Config;

        private int gameTicks; //stores 1s game ticks for pause code
        private int gameClockTicks; //stores in game clock change
        private int inviteDelayTicks = 1; //stores time until next invite code is copied to cliboard
        private int numPlayers = 0; //stores number of players
        private bool IsEnabled = false;  //stores if the the server mod is enabled 
        public int bedX;
        public int bedY;
        private string inviteCode;
        private string inviteCodeTXT;
        private readonly Dictionary<string, int> PreviousFriendships = new Dictionary<string, int>();  //stores friendship values

        

        private bool eggHuntAvailable = false; //is egg festival ready start timer for triggering eggHunt Event
        private int eggHuntCountDown; //to trigger egghunt after set time

        private bool flowerDanceAvailable = false;
        private int flowerDanceCountDown;

        private bool luauSoupAvailable = false;
        private int luauSoupCountDown;

        private bool jellyDanceAvailable = false;
        private int jellyDanceCountDown;

        private bool grangeDisplayAvailable = false;
        private int grangeDisplayCountDown;

        private bool goldenPumpkinAvailable = false;
        private int goldenPumpkinCountDown;

        private bool iceFishingAvailable = false;
        private int iceFishingCountDown;

        private bool winterFeastAvailable = false;
        private int winterFeastCountDown;








        public override void Entry(IModHelper helper)
        {

            this.Config = this.Helper.ReadConfig<ModConfig>();





            helper.ConsoleCommands.Add("server", "Toggles headless server on/off", this.ServerToggle);

            SaveEvents.AfterLoad += this.AfterLoad;
            SaveEvents.BeforeSave += this.Shipping_Menu; // Shipping Menu handler
            GameEvents.OneSecondTick += this.GameEvents_OneSecondTick; //game tick event handler
            TimeEvents.TimeOfDayChanged += this.TimeEvents_TimeOfDayChanged; // Time of day change handler
            ControlEvents.KeyPressed += this.ControlEvents_KeyPressed;
            


        }


        // turns on server after the game loads
        private void AfterLoad(object sender, EventArgs e)
        {
            IsEnabled = true;
            Game1.chatBox.addInfoMessage("The Host is in Server Mode!");
            
        }





        // toggles server on/off with console command "server"
        private void ServerToggle(string command, string[] args)
        {
            if (Context.IsWorldReady)
            {
                if (IsEnabled == false)
                {
                    IsEnabled = true;
                    this.Monitor.Log("This has changed", LogLevel.Info);
                    Game1.chatBox.addInfoMessage("The Host is in Server Mode!");
                    //Game1.chatBox.activate();
                    //Game1.chatBox.setText("The Host is in Server Mode!");
                    //Game1.chatBox.chatBox.RecieveCommandInput('\r');

                    Game1.displayHUD = true;
                    Game1.addHUDMessage(new HUDMessage("Server Mode On!", ""));

                    Game1.options.pauseWhenOutOfFocus = false;
                    
                }
                else if (IsEnabled == true)
                {
                    IsEnabled = false;
                    this.Monitor.Log("The server off!", LogLevel.Info);

                    Game1.chatBox.addInfoMessage("The Host has returned!");
                    //Game1.chatBox.activate();
                    //Game1.chatBox.setText("The Host has returned!");
                    //Game1.chatBox.chatBox.RecieveCommandInput('\r');
                    Game1.displayHUD = true;
                    Game1.addHUDMessage(new HUDMessage("Server Mode Off!", ""));
                    
                }
            }
        }

        //toggles server on/off with configurable hotkey
        private void ControlEvents_KeyPressed(object sender, EventArgsKeyPressed e)
        {

            if (Context.IsWorldReady)
            {
                if (e.KeyPressed.ToString() == this.Config.serverHotKey)
                {
                    if (IsEnabled == false)
                    {
                        IsEnabled = true;
                        this.Monitor.Log("The server is on!", LogLevel.Info);
                        Game1.chatBox.addInfoMessage("The Host is in Server Mode!");
                        //Game1.chatBox.activate();
                        //Game1.chatBox.setText("The Host is in Server Mode!");
                        //Game1.chatBox.chatBox.RecieveCommandInput('\r');

                        Game1.displayHUD = true;
                        Game1.addHUDMessage(new HUDMessage("Server Mode On!", ""));

                        Game1.options.pauseWhenOutOfFocus = false;
                        
                    }
                    else if (IsEnabled == true)
                    {
                        IsEnabled = false;
                        this.Monitor.Log("The server is off!", LogLevel.Info);

                        Game1.chatBox.addInfoMessage("The Host has returned!");
                        //Game1.chatBox.activate();
                        //Game1.chatBox.setText("The Host has returned!");
                        //Game1.chatBox.chatBox.RecieveCommandInput('\r');
                        Game1.displayHUD = true;
                        Game1.addHUDMessage(new HUDMessage("Server Mode Off!", ""));
                        

                    }
                }
            }
        }


        private void FestivalsToggle()
        {
            if (this.Config.festivalsOn == false)
                return;
        }



        private void GameEvents_OneSecondTick(object sender, EventArgs e)
        {


            if (IsEnabled == false) // server toggle
            {

                Game1.paused = false;
                return;
            }

            NoClientsPause();  //Turn back on when done testing!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            if (this.Config.clientsCanPause == true)
            {
                clientControlledPause();
            }



            //Invite Code Copier 
            if (this.Config.copyInviteCodeToClipboard == true)
            {
                if (!String.Equals(inviteCode, Game1.server.getInviteCode()))
                {
                    DesktopClipboard.SetText("Invite Code: " + Game1.server.getInviteCode());
                    inviteCode = Game1.server.getInviteCode();
                }
            }

            //write code to a InviteCode.txt in the Always On Server mod folder
            if (!String.Equals(inviteCodeTXT, Game1.server.getInviteCode()))
            {
                inviteCodeTXT = Game1.server.getInviteCode();

                if (eggHuntAvailable == true)
                {
                    inviteCodeTXT = "Festival In Progress Try Again Later";
                }

                else if (luauSoupAvailable == true)
                {
                    inviteCodeTXT = "Festival In Progress Try Again Later";
                }

                else if (jellyDanceAvailable == true)
                {
                    inviteCodeTXT = "Festival In Progress Try Again Later";
                }

                else if (grangeDisplayAvailable == true)
                {
                    inviteCodeTXT = "Festival In Progress Try Again Later";
                }

                else if (goldenPumpkinAvailable == true)
                {
                    inviteCodeTXT = "Festival In Progress Try Again Later";
                }

                else if (iceFishingAvailable == true)
                {
                    inviteCodeTXT = "Festival In Progress Try Again Later";
                }

                else if (winterFeastAvailable == true)
                {
                    inviteCodeTXT = "Festival In Progress Try Again Later";
                }

                else
                {

                    try
                    {

                        //Pass the filepath and filename to the StreamWriter Constructor
                        StreamWriter sw = new StreamWriter("Mods/Always On Server/InviteCode.txt");

                        //Write a line of text
                        sw.WriteLine(inviteCodeTXT);
                        //Close the file
                        sw.Close();
                    }
                    catch (Exception b)
                    {
                        Console.WriteLine("Exception: " + b.Message);
                    }
                    finally
                    {
                        Console.WriteLine("Executing finally block.");
                    }
                }
            }





            //left click menu spammer to get through random events happening
             if (IsEnabled == true) // server toggle
             {
                if (Game1.activeClickableMenu != null)
                {
                    this.Helper.Reflection.GetMethod(Game1.activeClickableMenu, "receiveLeftClick").Invoke(0, 0, true);
                }
             }



            //disable friendship decay
            if (IsEnabled == true) // server toggle
            {
                if (this.PreviousFriendships.Any())
                {
                    foreach (string key in Game1.player.friendshipData.Keys)
                    {
                        Friendship friendship = Game1.player.friendshipData[key];
                        if (this.PreviousFriendships.TryGetValue(key, out int oldPoints) && oldPoints > friendship.Points)
                            friendship.Points = oldPoints;
                    }
                }

                this.PreviousFriendships.Clear();
                foreach (var pair in Game1.player.friendshipData.FieldDict)
                    this.PreviousFriendships[pair.Key] = pair.Value.Value.Points;
            }







            //eggHunt event
            if (eggHuntAvailable == true && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                eggHuntCountDown += 1;

                float chatEgg = this.Config.eggHuntCountDownConfig / 60f;
                if (eggHuntCountDown == 1)
                {
                    Game1.chatBox.activate();
                    Game1.chatBox.setText($"The Egg Hunt will begin in {chatEgg:0.#} minutes.");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                }

                if (eggHuntCountDown == this.Config.eggHuntCountDownConfig + 1)
                {
                    this.Helper.Reflection.GetMethod(Game1.CurrentEvent, "answerDialogueQuestion", true).Invoke(Game1.getCharacterFromName("Lewis"), "yes");
                }
                if (eggHuntCountDown >= this.Config.eggHuntCountDownConfig + 5)
                {
                    if (Game1.activeClickableMenu != null)
                    {
                        this.Helper.Reflection.GetMethod(Game1.activeClickableMenu, "receiveLeftClick").Invoke(10, 10, true);
                    }
                }
            }

            //flower dance turned off, causes server crashes
           /* //flowerDance event
            if (flowerDanceAvailable == true && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                flowerDanceCountDown += 1;

                float chatFlower = this.Config.flowerDanceCountDownConfig / 60f;
                if (flowerDanceCountDown == 1)
                {
                    Game1.chatBox.activate();
                    Game1.chatBox.setText($"The Flower Dance will begin in {chatFlower:0.#} minutes.");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                }

                if (flowerDanceCountDown == this.Config.flowerDanceCountDownConfig + 1)
                {
                    this.Helper.Reflection.GetMethod(Game1.CurrentEvent, "answerDialogueQuestion", true).Invoke(Game1.getCharacterFromName("Lewis"), "yes");
                }
                if (flowerDanceCountDown >= this.Config.flowerDanceCountDownConfig + 5)
                {
                    if (Game1.activeClickableMenu != null)
                    {
                        this.Helper.Reflection.GetMethod(Game1.activeClickableMenu, "receiveLeftClick").Invoke(10, 10, true);
                    }
                }
            }*/

            //luauSoup event
            if (luauSoupAvailable == true && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {

                luauSoupCountDown += 1;

                float chatSoup = this.Config.luauSoupCountDownConfig / 60f;
                if (luauSoupCountDown == 1)
                {
                    Game1.chatBox.activate();
                    Game1.chatBox.setText($"The Soup Tasting will begin in {chatSoup:0.#} minutes.");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                }

                if (luauSoupCountDown == this.Config.luauSoupCountDownConfig + 1)
                {
                    this.Helper.Reflection.GetMethod(Game1.CurrentEvent, "answerDialogueQuestion", true).Invoke(Game1.getCharacterFromName("Lewis"), "yes");
                }
                if (luauSoupCountDown >= this.Config.luauSoupCountDownConfig + 5)
                {
                    if (Game1.activeClickableMenu != null)
                    {
                        this.Helper.Reflection.GetMethod(Game1.activeClickableMenu, "receiveLeftClick").Invoke(10, 10, true);
                    }
                }
            }

            //Dance of the Moonlight Jellies event
            if (jellyDanceAvailable == true && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {

                jellyDanceCountDown += 1;

                float chatJelly = this.Config.jellyDanceCountDownConfig / 60f;
                if (jellyDanceCountDown == 1)
                {
                    Game1.chatBox.activate();
                    Game1.chatBox.setText($"The Dance of the Moonlight Jellies will begin in {chatJelly:0.#} minutes.");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                }

                if (jellyDanceCountDown == this.Config.jellyDanceCountDownConfig + 1)
                {
                    this.Helper.Reflection.GetMethod(Game1.CurrentEvent, "answerDialogueQuestion", true).Invoke(Game1.getCharacterFromName("Lewis"), "yes");
                }
                if (jellyDanceCountDown >= this.Config.jellyDanceCountDownConfig + 5)
                {
                    if (Game1.activeClickableMenu != null)
                    {
                        this.Helper.Reflection.GetMethod(Game1.activeClickableMenu, "receiveLeftClick").Invoke(10, 10, true);
                    }
                }
            }

            //Grange Display event
            if (grangeDisplayAvailable == true && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {

                grangeDisplayCountDown += 1;

                float chatGrange = this.Config.grangeDisplayCountDownConfig / 60f;
                if (grangeDisplayCountDown == 1)
                {
                    Game1.chatBox.activate();
                    Game1.chatBox.setText($"The Grange Judging will begin in {chatGrange:0.#} minutes.");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                }

                if (grangeDisplayCountDown == this.Config.grangeDisplayCountDownConfig + 1)
                {
                    this.Helper.Reflection.GetMethod(Game1.CurrentEvent, "answerDialogueQuestion", true).Invoke(Game1.getCharacterFromName("Lewis"), "yes");
                }
                if (grangeDisplayCountDown == this.Config.grangeDisplayCountDownConfig + 5)
                {
                    Game1.player.team.SetLocalReady("festivalEnd", true);
                    Game1.activeClickableMenu = (IClickableMenu)new ReadyCheckDialog("festivalEnd", true, (ConfirmationDialog.behavior)(who =>
                    {
                        getBedCoordinates();
                        Game1.exitActiveMenu();
                        Game1.warpFarmer("Farmhouse", bedX, bedY, false);
                        Game1.timeOfDay = 2200;
                        Game1.shouldTimePass();
                        
                    }), (ConfirmationDialog.behavior)null);
                    
                }
            }

            //golden pumpkin maze event
            if (goldenPumpkinAvailable == true && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                goldenPumpkinCountDown += 1;

                if (goldenPumpkinCountDown == 10)
                {
                    Game1.player.team.SetLocalReady("festivalEnd", true);
                    Game1.activeClickableMenu = (IClickableMenu)new ReadyCheckDialog("festivalEnd", true, (ConfirmationDialog.behavior)(who =>
                    {
                        getBedCoordinates();
                        Game1.exitActiveMenu();
                        Game1.warpFarmer("Farmhouse", bedX, bedY, false);
                        Game1.timeOfDay = 2400;
                        Game1.shouldTimePass();
                        
                    }), (ConfirmationDialog.behavior)null);
                    
                }
            }

            //ice fishing event
            if (iceFishingAvailable == true && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                iceFishingCountDown += 1;

                float chatIceFish = this.Config.iceFishingCountDownConfig / 60f;
                if (iceFishingCountDown == 1)
                {
                    Game1.chatBox.activate();
                    Game1.chatBox.setText($"The Ice Fishing Contest will begin in {chatIceFish:0.#} minutes.");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                }

                if (iceFishingCountDown == this.Config.iceFishingCountDownConfig + 1)
                {
                    this.Helper.Reflection.GetMethod(Game1.CurrentEvent, "answerDialogueQuestion", true).Invoke(Game1.getCharacterFromName("Lewis"), "yes");
                }
                if (iceFishingCountDown >= this.Config.iceFishingCountDownConfig + 5)
                {
                    if (Game1.activeClickableMenu != null)
                    {
                        this.Helper.Reflection.GetMethod(Game1.activeClickableMenu, "receiveLeftClick").Invoke(10, 10, true);
                    }
                }
            }

            //Feast of the Winter event
            if (winterFeastAvailable == true && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                winterFeastCountDown += 1;

                if (winterFeastCountDown == 10)
                {
                    Game1.player.team.SetLocalReady("festivalEnd", true);
                    Game1.activeClickableMenu = (IClickableMenu)new ReadyCheckDialog("festivalEnd", true, (ConfirmationDialog.behavior)(who =>
                    {
                        getBedCoordinates();
                        Game1.exitActiveMenu();
                        Game1.warpFarmer("Farmhouse", bedX, bedY, false);
                        Game1.timeOfDay = 2200;
                        Game1.shouldTimePass();
                        
                    }), (ConfirmationDialog.behavior)null);

                    
                }
            }

        }







        //Pause game if no clients Code
        private void NoClientsPause()
        {
            var currentDate = SDate.Now();
            var eggFestival = new SDate(13, "spring");
            var flowerDance = new SDate(24, "spring");
            var luau = new SDate(11, "summer");
            var danceOfJellies = new SDate(28, "summer");
            var stardewValleyFair = new SDate(16, "fall");
            var spiritsEve = new SDate(27, "fall");
            var festivalOfIce = new SDate(8, "winter");
            var feastOfWinterStar = new SDate(25, "winter");




            gameTicks += 1;

            if (gameTicks >= 3)
            {
                this.numPlayers = Game1.otherFarmers.Count;

                if (numPlayers >= 1)
                {
                    Game1.paused = false;
                }
                else if (numPlayers <= 0 && Game1.timeOfDay > 600 && currentDate != eggFestival && currentDate != flowerDance && currentDate != luau && currentDate != danceOfJellies && currentDate != stardewValleyFair && currentDate != spiritsEve && currentDate != festivalOfIce && currentDate != feastOfWinterStar)
                {
                    Game1.paused = true;

                }

                gameTicks = 0;
            }
        }

        // player command pause/unpause 
        private void clientControlledPause()
        {
            List<ChatMessage> messages = this.Helper.Reflection.GetField<List<ChatMessage>>(Game1.chatBox, "messages").GetValue();
            string[] messageDumpString = messages.SelectMany(p => p.message).Select(p => p.message).ToArray();
            string lastFragment = messageDumpString.Last().Split(':').Last().Trim();

            if (lastFragment == "!pause")
            {
                Game1.paused = true;
            }
            if (lastFragment == "!unpause")
            {
                Game1.paused = false;
            }



        }


        // auto-sleep and Holiday code
        private void TimeEvents_TimeOfDayChanged(object sender, EventArgs e)
        {
            if (IsEnabled == false) // server toggle
                return;

            

            gameClockTicks += 1;


            if (gameClockTicks >= 3)
            {
                var currentTime = Game1.timeOfDay;
                var currentDate = SDate.Now();
                var eggFestival = new SDate(13, "spring");
                var dayAfterEggFestival = new SDate(14, "spring");
                var flowerDance = new SDate(24, "spring");
                var luau = new SDate(11, "summer");
                var danceOfJellies = new SDate(28, "summer");
                var stardewValleyFair = new SDate(16, "fall");
                var spiritsEve = new SDate(27, "fall");
                var festivalOfIce = new SDate(8, "winter");
                var feastOfWinterStar = new SDate(25, "winter");



                if (currentDate == eggFestival && numPlayers >= 1)   //set back to 1 after testing~!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                {
                    FestivalsToggle();

                    if (currentTime >= 600 && currentTime <= 630)
                    {
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("Egg Festival Today!");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("I will not be in bed until after 2:00 P.M.");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');

                    }
                    EggFestival();
                }


                  //flower dance message changed to disabled bc it causes crashes
                else if (currentDate == flowerDance && numPlayers >= 1)
                {
                    FestivalsToggle();

                    if (currentTime >= 600 && currentTime <= 630)
                    {
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("Flower Dance Today.");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("I will not be going. It breaks my code.");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');

                    }
                   // FlowerDance();
                }

                else if (currentDate == luau && numPlayers >= 1)
                {
                    FestivalsToggle();

                    if (currentTime >= 600 && currentTime <= 630)
                    {
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("Luau Today!");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("Luau Today! I will not be in bed until after 2:00 P.M.");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                    }
                    Luau();
                }

                else if (currentDate == danceOfJellies && numPlayers >= 1)
                {
                    FestivalsToggle();

                    if (currentTime >= 600 && currentTime <= 630)
                    {
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("Dance of the Moonlight Jellies Tonight!");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("I will not be in bed until after 12:00 A.M.");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                    }
                    DanceOfTheMoonlightJellies();
                }

                else if (currentDate == stardewValleyFair && numPlayers >= 1)
                {
                    FestivalsToggle();

                    if (currentTime >= 600 && currentTime <= 630)
                    {
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("Stardew Valley Fair Today!");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("I will not be in bed until after 3:00 P.M.");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                    }
                    StardewValleyFair();
                }

                else if (currentDate == spiritsEve && numPlayers >= 1)
                {
                    FestivalsToggle();

                    if (currentTime >= 600 && currentTime <= 630)
                    {
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("Spirit's Eve Tonight!");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("I will not be in bed until after 12:00 A.M.");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                    }
                    SpiritsEve();
                }

                else if (currentDate == festivalOfIce && numPlayers >= 1)
                {
                    FestivalsToggle();

                    if (currentTime >= 600 && currentTime <= 630)
                    {
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("Festival of Ice Today!");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("I will not be in bed until after 2:00 P.M.");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                    }
                    FestivalOfIce();
                }

                else if (currentDate == feastOfWinterStar && numPlayers >= 1)
                {
                    FestivalsToggle();

                    if (currentTime >= 600 && currentTime <= 630)
                    {
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("Feast of the Winter Star Today!");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                        Game1.chatBox.activate();
                        Game1.chatBox.setText("I will not be in bed until after 2:00 P.M.");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');
                    }
                    FeastOfWinterStar();
                }

                else if (currentTime >= this.Config.timeOfDayToSleep && numPlayers >= 1)  //turn back to 1 after testing!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                {
                    GoToBed();
                }

                gameClockTicks = 0;   // never reaches rest of code bc gameClockTicks is reset to 0, these methods below are called higher up.




                void EggFestival()
                {
                    if (currentTime >= 900 && currentTime <= 1400)
                    {



                        //teleports to egg festival
                        Game1.player.team.SetLocalReady("festivalStart", true);
                        Game1.activeClickableMenu = (IClickableMenu)new ReadyCheckDialog("festivalStart", true, (ConfirmationDialog.behavior)(who =>
                        {
                            Game1.exitActiveMenu();
                            Game1.warpFarmer("Town", 1, 20, 1);
                            
                        }), (ConfirmationDialog.behavior)null);

                        eggHuntAvailable = true;

                    }
                    else if (currentTime >= 1410)
                    {
                        
                        eggHuntAvailable = false;
                        eggHuntCountDown = 0;
                        GoToBed();
                    }
                }

                // flower dance turned off causes game crashes with more than 4 players
                /*void FlowerDance()
                {
                    if (currentTime >= 900 && currentTime <= 1400)
                    {

                        Game1.player.team.SetLocalReady("festivalStart", true);
                        Game1.activeClickableMenu = (IClickableMenu)new ReadyCheckDialog("festivalStart", true, (ConfirmationDialog.behavior)(who =>
                        {
                            Game1.exitActiveMenu();
                            Game1.warpFarmer("Forest", 1, 20, 1);
                        }), (ConfirmationDialog.behavior)null);

                        flowerDanceAvailable = true;

                    }
                    else if (currentTime >= 1410 && currentTime >= this.Config.timeOfDayToSleep)
                    {
                        GoToBed();
                        flowerDanceAvailable = false;
                        flowerDanceCountDown = 0;
                    }
                }*/

                void Luau()
                {

                    if (currentTime >= 900 && currentTime <= 1400)
                    {

                        Game1.player.team.SetLocalReady("festivalStart", true);
                        Game1.activeClickableMenu = (IClickableMenu)new ReadyCheckDialog("festivalStart", true, (ConfirmationDialog.behavior)(who =>
                        {
                            Game1.exitActiveMenu();
                            Game1.warpFarmer("Beach", 1, 20, 1);
                            
                        }), (ConfirmationDialog.behavior)null);

                        luauSoupAvailable = true;

                    }
                    else if (currentTime >= 1410)
                    {
                        
                        luauSoupAvailable = false;
                        luauSoupCountDown = 0;
                        GoToBed();
                    }
                }

                void DanceOfTheMoonlightJellies()
                {

                    /*if (currentTime < 2200)  //triggers weird bug if you try to go to sleep then jump to a festival. Maybe try to fix in future?
                    {
                        GoToBed();
                    }*/

                    if (currentTime >= 2200 && currentTime <= 2400)
                    {


                        Game1.player.team.SetLocalReady("festivalStart", true);
                        Game1.activeClickableMenu = (IClickableMenu)new ReadyCheckDialog("festivalStart", true, (ConfirmationDialog.behavior)(who =>
                        {
                            Game1.exitActiveMenu();
                            Game1.warpFarmer("Beach", 1, 20, 1);
                            
                        }), (ConfirmationDialog.behavior)null);

                        jellyDanceAvailable = true;

                    }
                    else if (currentTime >= 2410)
                    {
                        
                        jellyDanceAvailable = false;
                        jellyDanceCountDown = 0;
                        GoToBed();
                    }
                }

                void StardewValleyFair()
                {
                    if (currentTime >= 900 && currentTime <= 1500)
                    {
                       


                        Game1.player.team.SetLocalReady("festivalStart", true);
                        Game1.activeClickableMenu = (IClickableMenu)new ReadyCheckDialog("festivalStart", true, (ConfirmationDialog.behavior)(who =>
                        {
                            Game1.exitActiveMenu();
                            Game1.warpFarmer("Town", 1, 20, 1);
                            
                        }), (ConfirmationDialog.behavior)null);

                        grangeDisplayAvailable = true;

                    }
                    else if (currentTime >= 1510)
                    {
                        
                        Game1.displayHUD = true;
                        grangeDisplayAvailable = false;
                        grangeDisplayCountDown = 0;
                        GoToBed();
                    }
                }

                void SpiritsEve()
                {
                    /*if (currentTime < 2200)  //triggers weird bug if you try to go to sleep then jump to a festival. Maybe try to fix in future?
                        {
                            GoToBed();
                        }*/

                    if (currentTime >= 2200 && currentTime <= 2350)
                    {



                        Game1.player.team.SetLocalReady("festivalStart", true);
                        Game1.activeClickableMenu = (IClickableMenu)new ReadyCheckDialog("festivalStart", true, (ConfirmationDialog.behavior)(who =>
                        {
                            Game1.exitActiveMenu();
                            Game1.warpFarmer("Town", 1, 20, 1);
                          
                        }), (ConfirmationDialog.behavior)null);

                        goldenPumpkinAvailable = true;

                    }
                    else if (currentTime >= 2400)
                    {
                       
                        Game1.displayHUD = true;
                        goldenPumpkinAvailable = false;
                        goldenPumpkinCountDown = 0;
                        GoToBed();
                    }
                }

                void FestivalOfIce()
                {
                    if (currentTime >= 900 && currentTime <= 1400)
                    {


                        Game1.player.team.SetLocalReady("festivalStart", true);
                        Game1.activeClickableMenu = (IClickableMenu)new ReadyCheckDialog("festivalStart", true, (ConfirmationDialog.behavior)(who =>
                        {
                            Game1.exitActiveMenu();
                            Game1.warpFarmer("Forest", 1, 20, 1);
                           
                        }), (ConfirmationDialog.behavior)null);

                        iceFishingAvailable = true;

                    }
                    else if (currentTime >= 1410)
                    {
                        
                        iceFishingAvailable = false;
                        iceFishingCountDown = 0;
                        GoToBed();
                    }
                }

                void FeastOfWinterStar()
                {
                    if (currentTime >= 900 && currentTime <= 1400)
                    {


                        Game1.player.team.SetLocalReady("festivalStart", true);
                        Game1.activeClickableMenu = (IClickableMenu)new ReadyCheckDialog("festivalStart", true, (ConfirmationDialog.behavior)(who =>
                        {
                            Game1.exitActiveMenu();
                            Game1.warpFarmer("Town", 1, 20, 1);
                           
                        }), (ConfirmationDialog.behavior)null);

                        winterFeastAvailable = true;

                    }
                    else if (currentTime >= 1410)
                    {
                        
                        winterFeastAvailable = false;
                        winterFeastCountDown = 0;
                        GoToBed();
                    }
                }

            }


        }

        private void getBedCoordinates()
        {
            int houseUpgradeLevel = Game1.player.HouseUpgradeLevel;
            if (houseUpgradeLevel == 0)
            {
                bedX = 9;
                bedY = 9;
            }
            else if (houseUpgradeLevel == 1)
            {
                bedX = 21;
                bedY = 4;
            }
            else
            {
                bedX = 27;
                bedY = 13;
            }

        }

        private void GoToBed()
        {
            getBedCoordinates();
            Game1.displayHUD = true;
            Game1.warpFarmer("Farmhouse", bedX, bedY, false);
            
            Game1.itemsToShip.Add(new StardewValley.Object(168, 1 /* the number of items */));
            this.Helper.Reflection.GetMethod(Game1.currentLocation, "startSleep").Invoke();

        }



        // shipping menu"OK" click through code
        private void Shipping_Menu(object sender, EventArgs e)
        {
            if (IsEnabled == false) // server toggle
                return;

            //string itemstoship = Game1.itemsToShip.ToString();
            //Game1.itemsToShip.Add("Driftwood" );
            

            this.Monitor.Log("This is the Shipping Menu");
            this.Helper.Reflection.GetMethod(Game1.activeClickableMenu, "okClicked").Invoke();
            
            
            


        }

    }
}

