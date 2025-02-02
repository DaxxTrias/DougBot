using Discord;
using Discord.WebSocket;
using AmalgamaBot.Models;
using Fernandezja.ColorHashSharp;

namespace AmalgamaBot.Systems;

public static class Events
{
    private static DiscordSocketClient _Client;

    public static async Task Monitor()
    {
        _Client = Program._Client;;
        _Client.MessageReceived += MessageReceivedHandler;
        _Client.UserJoined += UserJoinedHandler;
        Console.WriteLine("EventHandler Initialized");
    }

    private static Task UserJoinedHandler(SocketGuildUser user)
    {
        return Task.CompletedTask;
    }

    private static Task MessageReceivedHandler(SocketMessage message)
    {
        _ = Task.Run(async () =>
        {
            if (message.Channel is SocketDMChannel && message.Author.MutualGuilds.Any() &&
                message.Author.Id != _Client.CurrentUser.Id)
            {
                //Create embed to send to guild
                var embeds = new List<EmbedBuilder>();
                //Main embed
                var colorHash = new ColorHash();
                var color = colorHash.BuildToColor(message.Author.Id.ToString());
                embeds.Add(new EmbedBuilder()
                    .WithDescription(message.Content)
                    .WithColor((Color)color)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName($"{message.Author.Username}#{message.Author.Discriminator} ({message.Author.Id})")
                        .WithIconUrl(message.Author.GetAvatarUrl()))
                    .WithCurrentTimestamp());
                //Attachment embeds
                embeds.AddRange(message.Attachments.Select(attachment =>
                    new EmbedBuilder().WithTitle(attachment.Filename).WithImageUrl(attachment.Url)
                        .WithUrl(attachment.Url)));
                //Confirm message and where to send
                var builder = new ComponentBuilder();
                builder.WithButton("CANCEL", "dmRecieved:cancel:cancel", ButtonStyle.Danger);
                foreach (var guild in message.Author.MutualGuilds)
                {
                    var guildId = guild.Id.ToString();
                    var guildName = guild.Name;
                    builder.WithButton(guildName, $"dmRecieved:{guildId}:{guildName}");
                }

                await message.Author.SendMessageAsync(
                    "This message will be sent to the Mod team, please select the server you would like to send it to",
                    embeds: embeds.Select(embed => embed.Build()).ToArray(), components: builder.Build());
            }
        });
        return Task.CompletedTask;
    }
}