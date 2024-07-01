using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;

namespace TwitchXIV
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public string Username = "Your twitch.tv username";
        public string ChannelToSend = "Channel to send chat to";
        public string OAuthCode = "";
        public bool TwitchEnabled = true;

        private IDalamudPluginInterface pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
