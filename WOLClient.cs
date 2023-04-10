using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;
using TwitchLib.Client;
using TwitchLib.Communication.Models;
using TwitchLib.Communication.Clients;
using System.Text.RegularExpressions;
using TwitchLib.Api.Helix;
using TwitchLib.Client.Events;
using Veda;

namespace TwitchXIV
{
    internal class WOLClient
    {
        public static TwitchClient Client;

        public static void DoConnect()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(Plugin.PluginConfig.Username, Plugin.PluginConfig.OAuthCode);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            Client = new TwitchClient(customClient);
            Client.Initialize(credentials, Plugin.PluginConfig.ChannelToSend);
            Client.OnLog += Client_OnLog;
            Client.OnJoinedChannel += Client_OnJoinedChannel;
            Client.OnLeftChannel += Client_OnLeftChannel;
            Client.OnMessageSent += Client_OnMessageSent;
            Client.OnMessageReceived += Client_OnMessageReceived;

            //WOLClient.OnWhisperReceived += Client_OnWhisperReceived;
            //WOLClient.OnNewSubscriber += Client_OnNewSubscriber;
            //WOLClient.OnConnected += Client_OnConnected;
            Client.Connect();
        }

        public static void Client_OnLog(object sender, OnLogArgs e)
        {
            //Filter out all the stuff we don't need to see
            if (e.Data.StartsWith("Finished channel joining queue.")) { return; }
            if (e.Data.Contains("@msg-id=msg_channel_suspended"))
            {
                //Chat.Print(e.Data);
                //string Channel = e.Data.Replace("@msg-id=msg_channel_suspended :tmi.twitch.tv NOTICE #", "").Replace(" :This channel does not exist or has been suspended.", "");
                Plugin.Chat.Print(Functions.BuildSeString(Plugin.PluginInterface.InternalName, "<c17>Unable <c17>to <c17>join <c575>" + Plugin.PluginConfig.ChannelToSend + " <c17>channel, <c17>reverting <c17>back <c17>to <c17>your <c17>channel. <c17>Please <c17>check <c17>the <c17>name <c17>and <c17>try <c17>again."));
                Plugin.PluginConfig.ChannelToSend = Client.TwitchUsername;
                Client.JoinChannel(Client.TwitchUsername);
            }
            if (e.Data.Contains("Received: @msg-id=") && e.Data.Contains("NOTICE"))
            {
                //Chat.Print(e.Data);
                string Message = Regex.Match(e.Data, Plugin.PluginConfig.ChannelToSend.ToLower() + " :.*").Value.Replace(Plugin.PluginConfig.ChannelToSend.ToLower() + " :", "");
                Plugin.Chat.Print(Functions.BuildSeString(Plugin.PluginInterface.InternalName, Message, ColorType.Info));
            }
            if (e.Data.StartsWith("Received:")) { return; }
            if (e.Data.StartsWith("Writing:")) { return; }
            if (e.Data.StartsWith("Connecting to")) { return; }
            if (e.Data.StartsWith("Joining ") || e.Data.StartsWith("Leaving ")) { return; }
            if (e.Data == "Should be connected!") { Plugin.Chat.Print(Functions.BuildSeString(Plugin.PluginInterface.InternalName, "Connected to twitch chat", ColorType.Twitch)); return; }
            if (e.Data == "Disconnect Twitch Chat Client...") { Plugin.Chat.Print(Functions.BuildSeString(Plugin.PluginInterface.InternalName, "Disconnected from twitch chat", ColorType.Twitch)); return; }

            Plugin.Chat.Print(Functions.BuildSeString(Plugin.PluginInterface.InternalName, e.Data, ColorType.Twitch));
        }

        public static void Log(string Message)
        {
            Plugin.Chat.Print(Functions.BuildSeString(Plugin.PluginInterface.InternalName, Message));
            //if (WriteToFile) File.AppendAllText("logs/ThemperorLog.txt", $"[{System.DateTime.Now.ToString("G")}] " + Message + Environment.NewLine);
        }

        public static void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Log("<c541>Joined <c541>channel <c575>" + e.Channel);
        }

        public static void Client_OnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            Log("<c541>Left <c541>channel <c575>" + e.Channel);
        }
        public static void Client_OnMessageSent(object sender, OnMessageSentArgs e)
        {
            string DisplayName = e.SentMessage.DisplayName;
            if (e.SentMessage.IsModerator) { DisplayName = "" + DisplayName; }
            Plugin.Chat.Print(Functions.BuildSeString("<c555>TWXIV", GetUsercolor(Client.TwitchUsername) + DisplayName + ": <c0>" + e.SentMessage.Message.Replace(" ", " <c0>")));
        }
        public static void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (!Plugin.PluginConfig.TwitchEnabled) { return; }
            string DisplayName = e.ChatMessage.DisplayName;
            if (e.ChatMessage.IsModerator) { DisplayName = "" + DisplayName; }
            Plugin.Chat.Print(Functions.BuildSeString("<c555>TWXIV", GetUsercolor(e.ChatMessage.Username) + DisplayName + ": <c0>" + e.ChatMessage.Message.Replace(" ", " <c0>")));
        }

        public static string GetUsercolor(string Username)
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
            catch (Exception f)
            {
                Plugin.Chat.PrintError("Something went wrong - " + f.ToString());
                return "<c0>";
            }
        }
    }
}
