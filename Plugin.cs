using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using Veda;

namespace TwitchXIV
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Twitch XIV";

        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static CommandManager Commands { get; set; }
        [PluginService] public static Dalamud.Game.ClientState.Conditions.Condition Conditions { get; set; }
        [PluginService] public static DataManager Data { get; set; }
        [PluginService] public static Dalamud.Game.Framework Framework { get; set; }
        [PluginService] public static GameGui GameGui { get; set; }
        [PluginService] public static SigScanner SigScanner { get; set; }
        [PluginService] public static KeyState KeyState { get; set; }
        [PluginService] public static ChatGui Chat { get; set; }
        [PluginService] public static ClientState ClientState { get; set; }
        [PluginService] public static PartyList PartyList { get; set; }

        public static Configuration PluginConfig { get; set; }
        private PluginCommandManager<Plugin> commandManager;
        private PluginUI ui;
        public static TwitchClient WOLClient;

        public static bool FirstRun = true;
        public string PreviousWorkingChannel;
        public bool SuccessfullyJoined;

        public Plugin(DalamudPluginInterface pluginInterface, ChatGui chat, PartyList partyList, CommandManager commands, SigScanner sigScanner)
        {
            PluginInterface = pluginInterface;
            PartyList = partyList;
            Chat = chat;
            SigScanner = sigScanner;

            // Get or create a configuration object
            PluginConfig = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);
           
            ui = new PluginUI();
            PluginInterface.UiBuilder.Draw += new System.Action(ui.Draw);
            PluginInterface.UiBuilder.OpenConfigUi += () =>
            {
                PluginUI ui = this.ui;
                ui.IsVisible = !ui.IsVisible;
            };

            // Load all of our commands
            this.commandManager = new PluginCommandManager<Plugin>(this, commands);

            //        public string Username = "Your twitch.tv username";
            //public string ChannelToSend = "Channel to send chat to";
            //public string OAuthCode = "";
            try
            {
                if (PluginConfig.Username != "Your twitch.tv username" && PluginConfig.OAuthCode.Length == 36)
                {
                    Connect();
                }
                else
                {
                    Chat.Print(Functions.BuildSeString(this.Name, "Please open the config with <c575>/twitch and set your credentials, then re-enable the plugin."));
                }
            }
            catch(Exception f)
            {
                Chat.PrintError("Something went wrong - " + f.Message.ToString());
                Chat.Print(Functions.BuildSeString(this.Name, "Please open the config with <c575>/twitch and double check your credentials, then re-enable the plugin."));
            }
        }

        public void Connect()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(PluginConfig.Username, PluginConfig.OAuthCode);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            WOLClient = new TwitchClient(customClient);
            WOLClient.Initialize(credentials, PluginConfig.ChannelToSend);
            WOLClient.OnLog += Client_OnLog;
            WOLClient.OnJoinedChannel += Client_OnJoinedChannel;
            WOLClient.OnLeftChannel += Client_OnLeftChannel;
            WOLClient.OnMessageSent += Client_OnMessageSent;
            WOLClient.OnMessageReceived += Client_OnMessageReceived;
            //WOLClient.OnWhisperReceived += Client_OnWhisperReceived;
            //WOLClient.OnNewSubscriber += Client_OnNewSubscriber;
            //WOLClient.OnConnected += Client_OnConnected;
            WOLClient.Connect();
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            //Filter out all the stuff we don't need to see
            if (e.Data.StartsWith("Finished channel joining queue.")) { return; }
            if (e.Data.Contains("@msg-id=msg_channel_suspended"))
            {
                //Chat.Print(e.Data);
                //string Channel = e.Data.Replace("@msg-id=msg_channel_suspended :tmi.twitch.tv NOTICE #", "").Replace(" :This channel does not exist or has been suspended.", "");
                Chat.Print(Functions.BuildSeString(this.Name, "<c17>Unable <c17>to <c17>join <c575>" + PluginConfig.ChannelToSend + " <c17>channel, <c17>reverting <c17>back <c17>to <c17>your <c17>channel. <c17>Please <c17>check <c17>the <c17>name <c17>and <c17>try <c17>again."));
                PluginConfig.ChannelToSend = WOLClient.TwitchUsername;
                WOLClient.JoinChannel(WOLClient.TwitchUsername);
            }
            if (e.Data.Contains("Received: @msg-id=") && e.Data.Contains("NOTICE"))
            {
                //Chat.Print(e.Data);
                string Message = Regex.Match(e.Data, PluginConfig.ChannelToSend.ToLower() + " :.*").Value.Replace(PluginConfig.ChannelToSend.ToLower() + " :","");
                Chat.Print(Functions.BuildSeString(this.Name,Message,ColorType.Info));
            }
            if (e.Data.StartsWith("Received:")) { return; }
            if (e.Data.StartsWith("Writing:")) { return; }
            if (e.Data.StartsWith("Connecting to")) { return; }
            if (e.Data.StartsWith("Joining ") || e.Data.StartsWith("Leaving ")) { return; }
            if (e.Data == "Should be connected!") { Chat.Print(Functions.BuildSeString(this.Name,"Connected to twitch chat", ColorType.Twitch)); return; }
            if (e.Data == "Disconnect Twitch Chat Client...") { Chat.Print(Functions.BuildSeString(this.Name, "Disconnected from twitch chat", ColorType.Twitch)); return; }

            Chat.Print(Functions.BuildSeString(this.Name, e.Data,ColorType.Twitch));
        }

        private void Log(string Message)
        {
            Chat.Print(Functions.BuildSeString(this.Name, Message));
            //if (WriteToFile) File.AppendAllText("logs/ThemperorLog.txt", $"[{System.DateTime.Now.ToString("G")}] " + Message + Environment.NewLine);
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Log("<c541>Joined <c541>channel <c575>" + e.Channel);
        }

        private void Client_OnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            Log("<c541>Left <c541>channel <c575>" + e.Channel);
        }
        private void Client_OnMessageSent(object sender, OnMessageSentArgs e)
        {
            string DisplayName = e.SentMessage.DisplayName;
            if (e.SentMessage.IsModerator) { DisplayName = "" + DisplayName; }
            Chat.Print(Functions.BuildSeString("<c555>TWXIV", GetUsercolor(WOLClient.TwitchUsername) + DisplayName + ": <c0>" + e.SentMessage.Message.Replace(" ", " <c0>")));
        }
        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (!PluginConfig.TwitchEnabled) { return; }
            string DisplayName = e.ChatMessage.DisplayName;
            if (e.ChatMessage.IsModerator) { DisplayName = "" + DisplayName; }
            Chat.Print(Functions.BuildSeString("<c555>TWXIV", GetUsercolor(e.ChatMessage.Username) + DisplayName + ": <c0>" + e.ChatMessage.Message.Replace(" "," <c0>")));
        }

        private string GetUsercolor(string Username)
        {
            try
            {
                int ColorNumber;
                if (Username.Length > 19)
                {
                    ColorNumber = Username.Length - 20;
                }
                else if (Username.Length > 9)
                {
                    ColorNumber = Username.Length - 10;
                }
                else
                {
                    ColorNumber = Username.Length;
                }

                switch (ColorNumber)
                {
                    case 0:
                        return "<c518>"; //Red
                    case 1:
                        return "<c56>";  //Fushia?
                    case 2:
                        return "<c570>"; //Green
                    case 3:
                        return "<c14>";  //Dark Red
                    case 4:
                        return "<c531>"; //Peach
                    case 5:
                        return "<c42>";  //Forest Green
                    case 6:
                        return "<c561>"; //Light pink
                    case 7:
                        return "<c555>"; //Twitch purple
                    case 8:
                        return "<c566>"; //Bright blue
                    case 9:
                        return "<c500>"; //Orange
                    default:
                        return "<c0>";   //Broken, white
                }
            }
            catch(Exception f)
            {
                Chat.PrintError("Something went wrong - " + f.ToString());
                return "<c0>";
            }
        }

        [Command("/twitch")]
        [HelpMessage("Shows TwitchXIV configuration options")]
        public void ShowTwitchOptions(string command, string args)
        {
            ui.IsVisible = !ui.IsVisible;
        }

        [Command("/tt")]
        [HelpMessage("Turn twitch chat relay on/off")]
        public void ToggleTwitcc(string command, string args)
        {
            PluginConfig.TwitchEnabled = !PluginConfig.TwitchEnabled;
            Chat.Print($"Toggled twitch chat {(PluginConfig.TwitchEnabled ? "on" : "off")}.");
        }

        [Command("/tw")]
        [HelpMessage("Sends a message to the specified channel in options\nUsage: /tw Hey guys, how is the stream going?")]
        public void SendTwitchChat(string command, string args)
        {
            if(String.IsNullOrWhiteSpace(args))
            {
                Chat.PrintError("Error: No message specified");
                Chat.Print(Functions.BuildSeString(this.Name, "Usage: /tw Hey guys, how is the stream going?", ColorType.Warn));
                return;
            }
            WOLClient.SendMessage(WOLClient.JoinedChannels.First(), args);
        }

        [Command("/tchannel")]
        [HelpMessage("Switch chat to the specified channel\nUsage: /tchannel streamer_username")]
        public void SwitchTwitchChannel(string command, string args)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(args))
                {
                    Chat.PrintError("Error: No channel specified");
                    Chat.Print(Functions.BuildSeString(this.Name, "Usage: /tchannel streamer_username\nExample: /tchannel zackrawrr", ColorType.Warn));
                    return;
                }
                if (WOLClient.JoinedChannels.Count() > 0) { WOLClient.LeaveChannel(WOLClient.JoinedChannels.First()); }
                PluginConfig.ChannelToSend = args;
                WOLClient.JoinChannel(args);
            }
            catch(Exception f)
            {
                Chat.PrintError(f.ToString());
            }
        }


        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            this.commandManager.Dispose();

            PluginInterface.SavePluginConfig(PluginConfig);

            PluginInterface.UiBuilder.Draw -= ui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= () =>
            {
                PluginUI ui = this.ui;
                ui.IsVisible = !ui.IsVisible;
            };

            if (WOLClient.IsConnected) { WOLClient.Disconnect(); }
            WOLClient.OnLog -= Client_OnLog;
            WOLClient.OnJoinedChannel -= Client_OnJoinedChannel;
            WOLClient.OnLeftChannel -= Client_OnLeftChannel;
            WOLClient.OnMessageSent -= Client_OnMessageSent;
            WOLClient.OnMessageReceived -= Client_OnMessageReceived;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}