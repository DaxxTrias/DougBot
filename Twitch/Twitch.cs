using DougBot.Models;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

namespace DougBot.Twitch;

public class Twitch
{
    public static async Task RunClient()
    {
        try
        {
            Console.WriteLine("Twitch Initialized");
            //Load settings
            var settings = (await Guild.GetGuild("567141138021089308")).TwitchSettings;
            //Setup API
            var API = new TwitchAPI();
            var monitor = new LiveStreamMonitorService(API);
            monitor.SetChannelsByName(new List<string> { settings.ChannelName });
            monitor.OnStreamOnline += Monitor_OnStreamOnline;
            monitor.OnStreamOffline += Monitor_OnStreamOffline;
            monitor.Start();
            //Setup PubSub
            var pubSub = new PubSub().Initialize();
            //Setup IRC anonymously
            var irc = new IRC().Initialize(API, "853660174", settings.BotName, settings.ChannelName);
            //Refresh token when expired
            while (true)
            {
                Console.WriteLine("Refreshing Tokens");
                //Refresh tokens
                var botRefresh =
                    await API.Auth.RefreshAuthTokenAsync(settings.BotRefreshToken, settings.ClientSecret, settings.ClientId);
                var dougRefresh =
                    await API.Auth.RefreshAuthTokenAsync(settings.ChannelRefreshToken, settings.ClientSecret,
                        settings.ClientId);
                API.Settings.AccessToken = botRefresh.AccessToken;
                API.Settings.ClientId = settings.ClientId;
                //Connect IRC
                irc.Connect();
                //Update PubSub
                pubSub.Connect();
                pubSub.ListenToChannelPoints(settings.ChannelId);
                pubSub.ListenToPredictions(settings.ChannelId);
                pubSub.OnPubSubServiceConnected += (Sender, e) =>
                {
                    pubSub.SendTopics(dougRefresh.AccessToken);
                    Console.WriteLine("PubSub Connected");
                };
                //Get the lowest refresh time
                var refreshTime = botRefresh.ExpiresIn < dougRefresh.ExpiresIn ? botRefresh.ExpiresIn : dougRefresh.ExpiresIn;
                refreshTime = (int)(refreshTime - TimeSpan.FromMinutes(30).TotalSeconds);
                Console.WriteLine($"Refreshed Tokens in {refreshTime} seconds");
                await Task.Delay((refreshTime-1800)*1000);
                //Disconnected
                pubSub.Disconnect();
                irc.Disconnect();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static void Monitor_OnStreamOnline(object? sender, OnStreamOnlineArgs Stream)
    {
        Console.WriteLine($"Stream Online: {Stream.Channel}");
        //Automate online ticker, ping, perhaps twitch things like disable emote only mode
    }

    private static void Monitor_OnStreamOffline(object? sender, OnStreamOfflineArgs Stream)
    {
        Console.WriteLine($"Stream Offline: {Stream.Channel}");
        //Automate delete ticker, perhaps twitch things like enable emote only mode for offline chat
    }
}