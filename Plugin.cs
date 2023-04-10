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

            //public string Username = "Your twitch.tv username";
            //public string ChannelToSend = "Channel to send chat to";
            //public string OAuthCode = "";
            try
            {
                if (PluginConfig.Username != "Your twitch.tv username" && PluginConfig.OAuthCode.Length == 36)
                {
                    WOLClient.DoConnect();
                }
                else
                {
                    Chat.Print(Functions.BuildSeString(PluginInterface.InternalName, "Please open the config with <c575>/twitch and set your credentials."));
                }
            }
            catch(Exception f)
            {
                Chat.PrintError("Something went wrong - " + f.Message.ToString());
                Chat.Print(Functions.BuildSeString(PluginInterface.InternalName, "Please open the config with <c575>/twitch and double check your credentials."));
            }
        }

        

        [Command("/twitch")]
        [HelpMessage("Shows TwitchXIV configuration options")]
        public void ShowTwitchOptions(string command, string args)
        {
            ui.IsVisible = !ui.IsVisible;
        }

        [Command("/toff")]
        [HelpMessage("Disconnect from Twitch")]
        public void DisconnectFromTwitch(string command, string args)
        {
            if (WOLClient.Client.IsConnected)
            {
                WOLClient.Client.Disconnect();
                Chat.Print(Functions.BuildSeString(PluginInterface.InternalName, "You have left the channel.", ColorType.Twitch));
            }
            else
            {
                Chat.Print(Functions.BuildSeString(PluginInterface.InternalName, "You are not currently connected!", ColorType.Warn));
            }
        }

        [Command("/tt")]
        [HelpMessage("Turn twitch chat relay on/off")]
        public void ToggleTwitch(string command, string args)
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
                Chat.Print(Functions.BuildSeString(PluginInterface.InternalName, "Usage: /tw Hey guys, how is the stream going?", ColorType.Warn));
                return;
            }
            if (WOLClient.Client.IsConnected == false)
            {
                Chat.Print(Functions.BuildSeString(PluginInterface.InternalName, "You are not currently connected to a channel.", ColorType.Twitch));
                return;
            }
            WOLClient.Client.SendMessage(WOLClient.Client.JoinedChannels.First(), args);
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
                    Chat.Print(Functions.BuildSeString(PluginInterface.InternalName, "Usage: /tchannel streamer_username\nExample: /tchannel zackrawrr", ColorType.Warn));
                    return;
                }
                if (WOLClient.Client.JoinedChannels.Count() > 0) { WOLClient.Client.LeaveChannel(WOLClient.Client.JoinedChannels.First()); }
                PluginConfig.ChannelToSend = args;
                if (WOLClient.Client.IsConnected == false)
                {
                    Chat.Print(Functions.BuildSeString(PluginInterface.InternalName, "Connecting to Twitch...", ColorType.Twitch));
                    WOLClient.DoConnect();
                }
                WOLClient.Client.JoinChannel(args);
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

            if (WOLClient.Client.IsConnected) { WOLClient.Client.Disconnect(); }
            WOLClient.Client.OnLog -= WOLClient.Client_OnLog;
            WOLClient.Client.OnJoinedChannel -= WOLClient.Client_OnJoinedChannel;
            WOLClient.Client.OnLeftChannel -= WOLClient.Client_OnLeftChannel;
            WOLClient.Client.OnMessageSent -= WOLClient.Client_OnMessageSent;
            WOLClient.Client.OnMessageReceived -= WOLClient.Client_OnMessageReceived;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}