using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game;
using Dalamud.Plugin;
using ImGuiNET;
using ImGuiScene;
using System;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using TwitchLib.Api.Helix;
using TwitchLib.Client.Models;
using Veda;

namespace TwitchXIV
{
    public class PluginUI
    {
        public bool IsVisible;
        public bool ShowSupport;
        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("Twitch XIV Config", ref IsVisible, ImGuiWindowFlags.AlwaysAutoResize))
                return;
            ImGui.Text("Enter your twitch username here:");
            ImGui.SetNextItemWidth(310);
            ImGui.InputText("Username", ref Plugin.PluginConfig.Username, 25);
            ImGui.Text("Enter the initial channel name to join here:");
            ImGui.SetNextItemWidth(310);
            ImGui.InputText("Channel", ref Plugin.PluginConfig.ChannelToSend, 25);
            ImGui.Text("The last channel you join will be remembered and\nautomatically joined at plugin start.");
            ImGui.Text("Enter your oath code here (including the \"oath:\" part):");
            ImGui.SetNextItemWidth(310);
            ImGui.InputText("OAuth", ref Plugin.PluginConfig.OAuthCode, 36);
            if (ImGui.Button("Save"))
            {
                Plugin.PluginConfig.Save();
                this.IsVisible = false;
                Plugin.Chat.Print(Functions.BuildSeString("Twitch XIV","<c17>DO <c25>NOT <c37>SHARE <c45>YOUR <c48>OAUTH <c52>CODE <c500>WITH <c579>ANYONE!"));
                if (WOLClient.Client.IsConnected) { WOLClient.Client.Disconnect(); }
                WOLClient.DoConnect();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox("Relay twitch chat to chatbox", ref Plugin.PluginConfig.TwitchEnabled))
            {
                Plugin.Chat.Print(Functions.BuildSeString("TwitchXIV",$"Toggled twitch chat {(Plugin.PluginConfig.TwitchEnabled ? "on" : "off")}."));
            }
            ImGui.SameLine();
            ImGui.Indent(275);
            if (ImGui.Button("Get OAuth code"))
            {
                Functions.OpenWebsite("https://twitchapps.com/tmi/");
            }
            ImGui.Spacing();
            ImGui.Indent(-275);
            if (ImGui.Button("Want to help support my work?"))
            {
                ShowSupport = !ShowSupport;
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Click me!"); }
            if (ShowSupport)
            {
                ImGui.Text("Here are the current ways you can support the work I do.\nEvery bit helps, thank you! Have a great day!");
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.19f, 0.52f, 0.27f, 1));
                if (ImGui.Button("Donate via Paypal"))
                {
                    Functions.OpenWebsite("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=QXF8EL4737HWJ");
                }
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.95f, 0.39f, 0.32f, 1));
                if (ImGui.Button("Become a Patron"))
                {
                    Functions.OpenWebsite("https://www.patreon.com/bePatron?u=5597973");
                }
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.25f, 0.67f, 0.87f, 1));
                if (ImGui.Button("Support me on Ko-Fi"))
                {
                    Functions.OpenWebsite("https://ko-fi.com/Y8Y114PMT");
                }
                ImGui.PopStyleColor();
            }
            ImGui.End();
        }
    }
}
