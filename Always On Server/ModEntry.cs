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
using StardewValley.Locations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Characters;

namespace Always_On_Server
{
    class ModConfig
    {
        public string serverHotKey { get; set; } = Keys.F9.ToString();

        public float profitmargin { get; set; } = 100f;
        public string petname { get; set; } = "Funnysnek";
        public bool farmcavechoicemushrooms { get; set; } = true;
        public bool communitycenterrun { get; set; } = true;
        public int timeOfDayToSleep { get; set; } = 2200;

        public bool clientsCanPause { get; set; } = false;
        public bool copyInviteCodeToClipboard { get; set; } = true;
        

        public bool festivalsOn { get; set; } = true;
        public int eggHuntCountDownConfig { get; set; } = 60;
        public int flowerDanceCountDownConfig { get; set; } = 60;
        public int luauSoupCountDownConfig { get; set; } = 60;
        public int jellyDanceCountDownConfig { get; set; } = 60;
        public int grangeDisplayCountDownConfig { get; set; } = 60;
        public int iceFishingCountDownConfig { get; set; } = 60;

        public int endofdayTimeOut { get; set; } = 300;
        public int fairTimeOut { get; set; } = 1200;
        public int spiritsEveTimeOut { get; set; } = 900;
        public int winterStarTimeOut { get; set; } = 900;

        public int eggFestivalTimeOut { get; set; } = 120;
        public int flowerDanceTimeOut { get; set; } = 120;
        public int luauTimeOut { get; set; } = 120;
        public int danceOfJelliesTimeOut { get; set; } = 120;
        public int festivalOfIceTimeOut { get; set; } = 120;
        
    }

    class ModData
    {
        public int FarmingLevel { get; set; }
        public int MiningLevel { get; set; }
        public int ForagingLevel { get; set; }
        public int FishingLevel { get; set; }
        public int CombatLevel { get; set; }
        
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
        public bool clientPaused = false;
        private string inviteCode = "a";
        private string inviteCodeTXT = "a";
        //debug tools
        private bool debug = false;
        private bool shippingMenuActive = false;

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

        //variables for timeout reset code
        private int gameTicksForReset;
        private int gameCounterForReset;
        private int timeOutTicksForReset;
        private int festivalTicksForReset;
        private int shippingMenuTimeoutTicks;
        
        SDate currentDateForReset = SDate.Now();
        SDate danceOfJelliesForReset = new SDate(28, "summer");
        SDate spiritsEveForReset = new SDate(27, "fall");
        //////////////////////////




        public override void Entry(IModHelper helper)
        {

            this.Config = this.Helper.ReadConfig<ModConfig>();

            helper.ConsoleCommands.Add("server", "Toggles headless server on/off", this.ServerToggle);
            helper.ConsoleCommands.Add("debug_server", "Turns debug mode on/off", this.DebugToggle);

            SaveEvents.AfterLoad += this.AfterLoad;
            SaveEvents.BeforeSave += this.Shipping_Menu; // Shipping Menu handler
            GameEvents.OneSecondTick += this.GameEvents_OneSecondTick; //game tick event handler
            TimeEvents.TimeOfDayChanged += this.TimeEvents_TimeOfDayChanged; // Time of day change handler
            TimeEvents.TimeOfDayChanged += this.FullAutoHandler; //handles various events the host normally has to click through
            ControlEvents.KeyPressed += this.ControlEvents_KeyPressed;
            GraphicsEvents.OnPostRenderEvent += this.GraphicsEvents_OnPostRenderEvent;
            MultiplayerEvents.BeforeMainSync += Sync; //used bc only thing that gets throug save window

        }






        // turns on server after the game loads
        private void AfterLoad(object sender, EventArgs e)
        {
            if (Game1.IsServer)
            {
                //store levels, set in game levels to max
                var data = this.Helper.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();
                data.FarmingLevel = Game1.player.FarmingLevel;
                data.MiningLevel = Game1.player.MiningLevel;
                data.ForagingLevel = Game1.player.ForagingLevel;
                data.FishingLevel = Game1.player.FishingLevel;
                data.CombatLevel = Game1.player.CombatLevel;
                this.Helper.WriteJsonFile<ModData>($"data/{Constants.SaveFolderName}.json", data);
                Game1.player.FarmingLevel = 10;
                Game1.player.MiningLevel = 10;
                Game1.player.ForagingLevel = 10;
                Game1.player.FishingLevel = 10;
                Game1.player.CombatLevel = 10;
                ////////////////////////////////////////
                IsEnabled = true;
                Game1.chatBox.addInfoMessage("The Host is in Server Mode!");
                this.Monitor.Log("Server Mode On!", LogLevel.Info);
            }

        }

        //debug for running with no one online
        private void DebugToggle(string command, string[] args)
        {
            if (Context.IsWorldReady)
            {
                if (debug == false)
                {
                    debug = true;
                    this.Monitor.Log("Server Debug On", LogLevel.Info);
                }
                if (debug == true)
                {
                    debug = false;
                    this.Monitor.Log("Server Debug Off", LogLevel.Info);
                }
            }
        }

        //draw textbox rules
        public static void DrawTextBox(int x, int y, SpriteFont font, string message, int align = 0, float colorIntensity = 1f)
        {
            SpriteBatch spriteBatch = Game1.spriteBatch;
            int width = (int)font.MeasureString(message).X + 32;
            int num = (int)font.MeasureString(message).Y + 21;
            switch (align)
            {
                case 0:
                    IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, num + 4, Color.White * colorIntensity, 1f, true);
                    Utility.drawTextWithShadow(spriteBatch, message, font, new Vector2((float)(x + 16), (float)(y + 16)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
                    break;
                case 1:
                    IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x - width / 2, y, width, num + 4, Color.White * colorIntensity, 1f, true);
                    Utility.drawTextWithShadow(spriteBatch, message, font, new Vector2((float)(x + 16 - width / 2), (float)(y + 16)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
                    break;
                case 2:
                    IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x - width, y, width, num + 4, Color.White * colorIntensity, 1f, true);
                    Utility.drawTextWithShadow(spriteBatch, message, font, new Vector2((float)(x + 16 - width), (float)(y + 16)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
                    break;
            }
        }

        //draw a textbox in the top left corner saying Server On
        private void GraphicsEvents_OnPostRenderEvent(object sender, EventArgs e)
        {
            if (Game1.options.enableServer == true && IsEnabled == true)
            {
                int connectionsCount = Game1.server.connectionsCount;
                DrawTextBox(5, 100, Game1.dialogueFont, "Server Mode On", 0, 1f);
                DrawTextBox(5, 180, Game1.dialogueFont, $"Press {this.Config.serverHotKey} On/Off", 0, 1f);
                float profitMargin = this.Config.profitmargin;
                DrawTextBox(5, 260, Game1.dialogueFont, $"Profit Margin: {profitMargin}%", 0, 1f);
                DrawTextBox(5, 340, Game1.dialogueFont, $"{connectionsCount} Players Online", 0, 1f);
                if (Game1.server.getInviteCode() != null)
                {
                    string inviteCode = Game1.server.getInviteCode();
                    DrawTextBox(5, 420, Game1.dialogueFont, $"Invite Code: {inviteCode}", 0, 1f);
                }
            }
        }
        

        // toggles server on/off with console command "server"
        private void ServerToggle(string command, string[] args)
        {
            if (Context.IsWorldReady)
            {
                if (IsEnabled == false)
                {
                    IsEnabled = true;


                    this.Monitor.Log("Server Mode On!", LogLevel.Info);
                    Game1.chatBox.addInfoMessage("The Host is in Server Mode!");

                    Game1.displayHUD = true;
                    Game1.addHUDMessage(new HUDMessage("Server Mode On!", ""));

                    Game1.options.pauseWhenOutOfFocus = false;


                    // store levels, set in game levels to max
                    var data = this.Helper.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();
                    data.FarmingLevel = Game1.player.FarmingLevel;
                    data.MiningLevel = Game1.player.MiningLevel;
                    data.ForagingLevel = Game1.player.ForagingLevel;
                    data.FishingLevel = Game1.player.FishingLevel;
                    data.CombatLevel = Game1.player.CombatLevel;
                    this.Helper.WriteJsonFile<ModData>($"data/{Constants.SaveFolderName}.json", data);
                    Game1.player.FarmingLevel = 10;
                    Game1.player.MiningLevel = 10;
                    Game1.player.ForagingLevel = 10;
                    Game1.player.FishingLevel = 10;
                    Game1.player.CombatLevel = 10;
                    ///////////////////////////////////////////

                }
                else if (IsEnabled == true)
                {
                    IsEnabled = false;
                    this.Monitor.Log("The server off!", LogLevel.Info);

                    Game1.chatBox.addInfoMessage("The Host has returned!");

                    Game1.displayHUD = true;
                    Game1.addHUDMessage(new HUDMessage("Server Mode Off!", ""));

                    //set player levels to stored levels
                    var data = this.Helper.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();
                    Game1.player.FarmingLevel = data.FarmingLevel;
                    Game1.player.MiningLevel = data.MiningLevel;
                    Game1.player.ForagingLevel = data.ForagingLevel;
                    Game1.player.FishingLevel = data.FishingLevel;
                    Game1.player.CombatLevel = data.CombatLevel;
                    //////////////////////////////////////
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
                        
                        Game1.displayHUD = true;
                        Game1.addHUDMessage(new HUDMessage("Server Mode On!", ""));

                        Game1.options.pauseWhenOutOfFocus = false;
                        // store levels, set in game levels to max
                        var data = this.Helper.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();
                        data.FarmingLevel = Game1.player.FarmingLevel;
                        data.MiningLevel = Game1.player.MiningLevel;
                        data.ForagingLevel = Game1.player.ForagingLevel;
                        data.FishingLevel = Game1.player.FishingLevel;
                        data.CombatLevel = Game1.player.CombatLevel;
                        this.Helper.WriteJsonFile<ModData>($"data/{Constants.SaveFolderName}.json", data);
                        Game1.player.FarmingLevel = 10;
                        Game1.player.MiningLevel = 10;
                        Game1.player.ForagingLevel = 10;
                        Game1.player.FishingLevel = 10;
                        Game1.player.CombatLevel = 10;
                        ///////////////////////////////////////////
                    }
                    else if (IsEnabled == true)
                    {
                        IsEnabled = false;
                        this.Monitor.Log("The server is off!", LogLevel.Info);

                        Game1.chatBox.addInfoMessage("The Host has returned!");
                        
                        Game1.displayHUD = true;
                        Game1.addHUDMessage(new HUDMessage("Server Mode Off!", ""));
                        //set player levels to stored levels
                        var data = this.Helper.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();
                        Game1.player.FarmingLevel = data.FarmingLevel;
                        Game1.player.MiningLevel = data.MiningLevel;
                        Game1.player.ForagingLevel = data.ForagingLevel;
                        Game1.player.FishingLevel = data.FishingLevel;
                        Game1.player.CombatLevel = data.CombatLevel;
                        //////////////////////////////////////

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


            NoClientsPause();  

            if (this.Config.clientsCanPause == true)
            {
                List<ChatMessage> messages = this.Helper.Reflection.GetField<List<ChatMessage>>(Game1.chatBox, "messages").GetValue();
                string[] messageDumpString = messages.SelectMany(p => p.message).Select(p => p.message).ToArray();
                string lastFragment = messageDumpString.LastOrDefault()?.Split(':').LastOrDefault()?.Trim();

                if (lastFragment != null && lastFragment == "!pause")
                {
                    Game1.netWorldState.Value.IsPaused = true;
                    clientPaused = true;
                    Game1.chatBox.activate();
                    Game1.chatBox.setText($"Game Paused");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');

                }
                if (lastFragment != null && lastFragment == "!unpause")
                {
                    Game1.netWorldState.Value.IsPaused = false;
                    clientPaused = false;
                    Game1.chatBox.activate();
                    Game1.chatBox.setText($"Game UnPaused");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                }
            }



            //Invite Code Copier 
            if (this.Config.copyInviteCodeToClipboard == true)
            {
                
                if (Game1.options.enableServer == true)
                {
                    if (!String.Equals(inviteCode, Game1.server.getInviteCode()))
                    {
                        DesktopClipboard.SetText("Invite Code: " + Game1.server.getInviteCode());
                        inviteCode = Game1.server.getInviteCode();
                    }
                }
            }

            //write code to a InviteCode.txt in the Always On Server mod folder
            if (Game1.options.enableServer == true)
            {
                if (!String.Equals(inviteCodeTXT, Game1.server.getInviteCode()))
                {


                    inviteCodeTXT = Game1.server.getInviteCode();

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



            //left click menu spammer and event skipper to get through random events happening
            if (IsEnabled == true) // server toggle
            {
                if (Game1.activeClickableMenu != null)
                {
                    this.Helper.Reflection.GetMethod(Game1.activeClickableMenu, "receiveLeftClick").Invoke(0, 0, true);
                }
                if (Game1.CurrentEvent != null && Game1.CurrentEvent.skippable == true)
                {
                    Game1.CurrentEvent.skipEvent();
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
                    //festival timeout
                    festivalTicksForReset += 1;
                    if (festivalTicksForReset >= this.Config.eggFestivalTimeOut + 180)
                    {
                        Game1.options.setServerMode("offline");
                    }
                    ///////////////////////////////////////////////


                }
            }

            
             //flowerDance event
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

                     //festival timeout
                    festivalTicksForReset += 1;
                    if (festivalTicksForReset >= this.Config.flowerDanceTimeOut+90)
                    {
                        Game1.options.setServerMode("offline");
                    }
                    ///////////////////////////////////////////////

                 }
             }

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

                    //add iridum starfruit to soup
                    var item = new StardewValley.Object(268, 1, false, -1, 3);
                    this.Helper.Reflection.GetMethod(new Event(), "addItemToLuauSoup").Invoke(item, Game1.player);

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
                    //festival timeout
                    festivalTicksForReset += 1;
                    if (festivalTicksForReset >= this.Config.luauTimeOut+80)
                    {
                        Game1.options.setServerMode("offline");
                    }
                    ///////////////////////////////////////////////


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
                    //festival timeout
                    festivalTicksForReset += 1;
                    if (festivalTicksForReset >= this.Config.danceOfJelliesTimeOut+180)
                    {
                        Game1.options.setServerMode("offline");
                    }
                    ///////////////////////////////////////////////

                }
            }

            //Grange Display event
            if (grangeDisplayAvailable == true && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                
                grangeDisplayCountDown += 1;
                festivalTicksForReset += 1;
                //festival timeout code
                if (festivalTicksForReset == this.Config.fairTimeOut-120)
                {
                    Game1.chatBox.activate();
                    Game1.chatBox.setText("2 minutes to the exit or");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                    Game1.chatBox.activate();
                    Game1.chatBox.setText("everyone will be kicked.");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                }
                if (festivalTicksForReset >= this.Config.fairTimeOut)
                {
                    Game1.options.setServerMode("offline");
                }
                ///////////////////////////////////////////////
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
                festivalTicksForReset += 1;
                //festival timeout code
                if (festivalTicksForReset == this.Config.spiritsEveTimeOut - 120)
                {
                    Game1.chatBox.activate();
                    Game1.chatBox.setText("2 minutes to the exit or");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                    Game1.chatBox.activate();
                    Game1.chatBox.setText("everyone will be kicked.");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                }
                if (festivalTicksForReset >= this.Config.spiritsEveTimeOut)
                {
                    Game1.options.setServerMode("offline");
                }
                ///////////////////////////////////////////////
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
                    //festival timeout
                    festivalTicksForReset += 1;
                    if (festivalTicksForReset >= this.Config.festivalOfIceTimeOut+180)
                    {
                        Game1.options.setServerMode("offline");
                    }
                    ///////////////////////////////////////////////

                }
            }

            //Feast of the Winter event
            if (winterFeastAvailable == true && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                winterFeastCountDown += 1;
                festivalTicksForReset += 1;
                //festival timeout code
                if (festivalTicksForReset == this.Config.winterStarTimeOut - 120)
                {
                    Game1.chatBox.activate();
                    Game1.chatBox.setText("2 minutes to the exit or");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                    Game1.chatBox.activate();
                    Game1.chatBox.setText("everyone will be kicked.");
                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                }
                if (festivalTicksForReset >= this.Config.winterStarTimeOut)
                {
                    Game1.options.setServerMode("offline");
                }
                ///////////////////////////////////////////////
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

                if (numPlayers >= 1 || debug == true)
                {
                    if (clientPaused == true)
                    { Game1.netWorldState.Value.IsPaused = true; }
                    if (clientPaused == false)
                    { Game1.paused = false; }

                }
                else if (numPlayers <= 0 && Game1.timeOfDay >= 610 && currentDate != eggFestival && currentDate != flowerDance && currentDate != luau && currentDate != danceOfJellies && currentDate != stardewValleyFair && currentDate != spiritsEve && currentDate != festivalOfIce && currentDate != feastOfWinterStar)
                {
                    Game1.paused = true;

                }

                gameTicks = 0;
            }
        }



        //handles various events that the host normally has to click through
        private void FullAutoHandler(object sender, EventArgs e) 
        {
            if (IsEnabled == true)
            {

                    var currentTime = Game1.timeOfDay;
                    var currentDate = SDate.Now();
                    var grampasGhost = new SDate(1, "spring", 3);
                    var eggFestival = new SDate(13, "spring");
                    var flowerDance = new SDate(24, "spring");
                    var luau = new SDate(11, "summer");
                    var danceOfJellies = new SDate(28, "summer");
                    var stardewValleyFair = new SDate(16, "fall");
                    var spiritsEve = new SDate(27, "fall");
                    var festivalOfIce = new SDate(8, "winter");
                    var feastOfWinterStar = new SDate(25, "winter");

                    if (currentDate != grampasGhost && currentDate != eggFestival && currentDate != flowerDance && currentDate != luau && currentDate != danceOfJellies && currentDate != stardewValleyFair && currentDate != spiritsEve && currentDate != festivalOfIce && currentDate != feastOfWinterStar)
                    {
                        if (currentTime == 620)
                        {
                            //check mail 10 a day
                            for (int i = 0; i < 10; i++)
                            {
                                this.Helper.Reflection.GetMethod(Game1.currentLocation, "mailbox").Invoke();
                            }
                        }
                        if (currentTime == 630)
                        {
                            //petchoice
                            if (!Game1.player.hasPet())
                            {
                                this.Helper.Reflection.GetMethod(new Event(), "namePet", true).Invoke(this.Config.petname.Substring(0, 9));
                            }
                            if (Game1.player.hasPet() && Game1.getCharacterFromName(Game1.player.getPetName(), false) is Pet pet)
                            {
                                pet.Name = this.Config.petname.Substring(0, 9);
                                pet.displayName = this.Config.petname.Substring(0, 9);
                            }
                        //cave choice unlock 
                        if (!Game1.player.eventsSeen.Contains(65))
                            {
                                Game1.player.eventsSeen.Add(65);


                                if (this.Config.farmcavechoicemushrooms == true)
                                {
                                    Game1.MasterPlayer.caveChoice.Value = 2;
                                    (Game1.getLocationFromName("FarmCave") as FarmCave).setUpMushroomHouse();
                                }
                                else
                                {
                                    Game1.MasterPlayer.caveChoice.Value = 1;
                                }
                            }
                            //rustkey-sewers unlock
                            if (Game1.player.hasRustyKey == false)
                            {
                                int items1 = this.Helper.Reflection.GetMethod(new LibraryMuseum(), "numberOfMuseumItemsOfType").Invoke<int>("Arch");
                                int items2 = this.Helper.Reflection.GetMethod(new LibraryMuseum(), "numberOfMuseumItemsOfType").Invoke<int>("Minerals");
                                int items3 = items1 + items2;
                                if (items3 >= 60)
                                {
                                    Game1.player.eventsSeen.Add(295672);
                                    Game1.player.eventsSeen.Add(66);
                                    Game1.player.hasRustyKey = true;
                                }
                            }

                            //community center unlock
                            if (!Game1.player.eventsSeen.Contains(611439))
                            {

                                Game1.player.eventsSeen.Add(611439);
                                Game1.MasterPlayer.mailReceived.Add("ccDoorUnlock");
                            }
                            //community center complete
                            if (this.Config.communitycenterrun == true)
                            {
                                if (!Game1.player.eventsSeen.Contains(191393) && Game1.player.mailReceived.Contains("ccCraftsRoom") && Game1.player.mailReceived.Contains("ccVault") && Game1.player.mailReceived.Contains("ccFishTank") && Game1.player.mailReceived.Contains("ccBoilerRoom") && Game1.player.mailReceived.Contains("ccPantry") && Game1.player.mailReceived.Contains("ccBulletin"))
                                {
                                    CommunityCenter locationFromName = Game1.getLocationFromName("CommunityCenter") as CommunityCenter;
                                    for (int index = 0; index < locationFromName.areasComplete.Count; ++index)
                                        locationFromName.areasComplete[index] = true;
                                    Game1.player.eventsSeen.Add(191393);

                                }
                            }
                            //Joja run 
                            if (this.Config.communitycenterrun == false)
                            {
                                if (Game1.player.money >= 10000 && !Game1.player.mailReceived.Contains("JojaMember"))
                                {
                                    Game1.player.money -= 5000;
                                    Game1.player.mailReceived.Add("JojaMember");
                                    Game1.chatBox.activate();
                                    Game1.chatBox.setText("Buying Joja Membership");
                                    Game1.chatBox.chatBox.RecieveCommandInput('\r');

                                }

                                if (Game1.player.money >= 30000 && !Game1.player.mailReceived.Contains("jojaBoilerRoom"))
                                {
                                    Game1.player.money -= 15000;
                                    Game1.player.mailReceived.Add("ccBoilerRoom");
                                    Game1.player.mailReceived.Add("jojaBoilerRoom");
                                    Game1.chatBox.activate();
                                    Game1.chatBox.setText("Buying Joja Minecarts");
                                    Game1.chatBox.chatBox.RecieveCommandInput('\r');

                                }

                                if (Game1.player.money >= 40000 && !Game1.player.mailReceived.Contains("jojaFishTank"))
                                {
                                    Game1.player.money -= 20000;
                                    Game1.player.mailReceived.Add("ccFishTank");
                                    Game1.player.mailReceived.Add("jojaFishTank");
                                    Game1.chatBox.activate();
                                    Game1.chatBox.setText("Buying Joja Panning");
                                    Game1.chatBox.chatBox.RecieveCommandInput('\r');

                                }

                                if (Game1.player.money >= 50000 && !Game1.player.mailReceived.Contains("jojaCraftsRoom"))
                                {
                                    Game1.player.money -= 25000;
                                    Game1.player.mailReceived.Add("ccCraftsRoom");
                                    Game1.player.mailReceived.Add("jojaCraftsRoom");
                                    Game1.chatBox.activate();
                                    Game1.chatBox.setText("Buying Joja Bridge");
                                    Game1.chatBox.chatBox.RecieveCommandInput('\r');

                                }

                                if (Game1.player.money >= 70000 && !Game1.player.mailReceived.Contains("jojaPantry"))
                                {
                                    Game1.player.money -= 35000;
                                    Game1.player.mailReceived.Add("ccPantry");
                                    Game1.player.mailReceived.Add("jojaPantry");
                                    Game1.chatBox.activate();
                                    Game1.chatBox.setText("Buying Joja Greenhouse");
                                    Game1.chatBox.chatBox.RecieveCommandInput('\r');

                                }

                                if (Game1.player.money >= 80000 && !Game1.player.mailReceived.Contains("jojaVault"))
                                {
                                    Game1.player.money -= 40000;
                                    Game1.player.mailReceived.Add("ccVault");
                                    Game1.player.mailReceived.Add("jojaVault");
                                    Game1.chatBox.activate();
                                    Game1.chatBox.setText("Buying Joja Bus");
                                    Game1.chatBox.chatBox.RecieveCommandInput('\r');
                                    Game1.player.eventsSeen.Add(502261);
                                }


                            }

                        }

                        if (currentTime == 640)
                        {
                            Game1.warpFarmer("Farm", 64, 15, false);
                        }
                        //get fishing rod (standard spam clicker will get through cutscene)
                        if (currentTime == 900 && !Game1.player.eventsSeen.Contains(739330))
                        {
                            Game1.player.increaseBackpackSize(1);
                            Game1.warpFarmer("Beach", 1, 20, 1);
                        }

                    }
                
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
                        Game1.chatBox.setText("I will not be in bed until after 2:00 P.M.");
                        Game1.chatBox.chatBox.RecieveCommandInput('\r');

                    }
                     FlowerDance();
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
                        Game1.chatBox.setText("I will not be in bed until after 2:00 P.M.");
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

                gameClockTicks = 0;   




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
                        Game1.options.setServerMode("online");
                        eggHuntCountDown = 0;
                        festivalTicksForReset = 0;
                        GoToBed();
                    }
                }

                // flower dance turned off causes game crashes with more than 4 players
                void FlowerDance()
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
                        
                        flowerDanceAvailable = false;
                        Game1.options.setServerMode("online");
                        flowerDanceCountDown = 0;
                        festivalTicksForReset = 0;
                        GoToBed();
                    }
                }

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
                        Game1.options.setServerMode("online");
                        luauSoupCountDown = 0;
                        festivalTicksForReset = 0;
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
                        Game1.options.setServerMode("online");
                        jellyDanceCountDown = 0;
                        festivalTicksForReset = 0;
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
                        Game1.options.setServerMode("online");
                        grangeDisplayCountDown = 0;
                        festivalTicksForReset = 0;
                        
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
                        Game1.options.setServerMode("online");
                        goldenPumpkinCountDown = 0;
                        festivalTicksForReset = 0;
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
                        Game1.options.setServerMode("online");
                        iceFishingCountDown = 0;
                        festivalTicksForReset = 0;
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
                        Game1.options.setServerMode("online");
                        winterFeastCountDown = 0;
                        festivalTicksForReset = 0;
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

            this.Monitor.Log("This is the Shipping Menu");
            shippingMenuActive = true;
            if (Game1.activeClickableMenu is ShippingMenu)
            {
                this.Helper.Reflection.GetMethod(Game1.activeClickableMenu, "okClicked").Invoke();
            }

        }

        //resets server connection after certain amount of time end of day
        private void Sync(object sender, EventArgs e)
        {

            if (Game1.timeOfDay >= this.Config.timeOfDayToSleep || Game1.timeOfDay == 600 && currentDateForReset != danceOfJelliesForReset && currentDateForReset != spiritsEveForReset && this.Config.endofdayTimeOut != 0)
            {
                
                timeOutTicksForReset += 1;
                var countdowntoreset = (2600 - this.Config.timeOfDayToSleep) * .01 * 6 * 7 * 60;
                if (timeOutTicksForReset >= (countdowntoreset + (this.Config.endofdayTimeOut*60)))
                {
                    Game1.options.setServerMode("offline");
                }
            }
            if (currentDateForReset == danceOfJelliesForReset || currentDateForReset == spiritsEveForReset && this.Config.endofdayTimeOut != 0)
            {
                if (Game1.timeOfDay >= 2400 || Game1.timeOfDay == 600)
                {
                    
                    timeOutTicksForReset += 1;
                    if (timeOutTicksForReset >= (5040  +(this.Config.endofdayTimeOut * 60)))
                    {
                        Game1.options.setServerMode("offline");
                    }
                }

            }
            if (shippingMenuActive == true && this.Config.endofdayTimeOut != 0)
            {
                
                shippingMenuTimeoutTicks += 1;
                if (shippingMenuTimeoutTicks >= this.Config.endofdayTimeOut*60)
                {
                    Game1.options.setServerMode("offline");
                }

            }

            if (Game1.timeOfDay == 610)
            {
                shippingMenuActive = false;
                Game1.player.difficultyModifier = this.Config.profitmargin * .01f;

                    Game1.options.setServerMode("online");
                    timeOutTicksForReset = 0;
                    shippingMenuTimeoutTicks = 0;
                    
                
            }


        }





    }
}