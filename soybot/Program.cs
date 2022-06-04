using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Timers;
public class Program
{
    private DiscordSocketClient _client;
    private int count;
    public static Task Main(string[] args) => new Program().MainAsync();
    public async Task MainAsync()
    {
        string path = "./count.txt";
        if (!File.Exists(path))
        {
            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine("0");
            }
        }
        count = int.Parse(File.ReadAllText("./count.txt"));

        var config = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            MessageCacheSize = 100
        };
        _client = new DiscordSocketClient(config);
        _client.MessageReceived += OnMessageReceived;
        _client.Ready += Client_Ready;
        _client.SlashCommandExecuted += SlashCommandHandler;
        _client.Log += Log;
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += new ElapsedEventHandler(CheckConnection);
        timer.Start();

        var token = "token here";

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }
    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
    void CheckConnection(object sender, ElapsedEventArgs e)
    {
        if (_client.ConnectionState == ConnectionState.Disconnecting)
            Environment.Exit(1);
    }
    private async Task SlashCommandHandler(SocketSlashCommand command)
    {


        switch (command.Data.Name)
        {
            case "soyjak":
                await HandleSoyCommand(command);
                break;
        }
        return;
    }
    private async Task HandleSoyCommand(SocketSlashCommand command)
    {
        var client = new System.Net.Http.HttpClient();
        var content = client.GetStringAsync(@"https://booru.soy/random_image/view").Result;
        HtmlDocument document = new HtmlDocument();
        document.OptionFixNestedTags = true;
        document.LoadHtml(content);
        var source = document.GetElementbyId("text_image-src").Attributes["value"].Value;

        await command.RespondAsync(source);
        return;
    }
    public async Task OnMessageReceived(SocketMessage socketMessage)
    {
        // We only want messages sent by real users 
        if (!(socketMessage is SocketUserMessage message))
            return;

        // This message handler would be called infinitely
        if (message.Author.Id == _client.CurrentUser.Id)
            return;

        if (message.Author is SocketGuildUser socketUser)
        {
            SocketGuild socketGuild = socketUser.Guild;
            SocketRole socketRole = socketGuild.GetRole(801574526382899201);
            if (socketUser.Roles.Any(r => r.Id == socketRole.Id))
            {
                if (count == 9)
                {
                    count = 0;
                    File.WriteAllText("./count.txt", count.ToString());
                    await message.Channel.SendFileAsync("./file.png");
                    return;
                }
                else
                {
                    count++;
                    File.WriteAllText("./count.txt",count.ToString());
                    return;
                }
            }
        }
    }
    public async Task Client_Ready()
    {

        // Let's do our global command
        var globalCommand = new SlashCommandBuilder();
        globalCommand.WithName("soyjak");
        globalCommand.WithDescription("Gets a random soyjak");

        try
        {

            // With global commands we don't need the guild.
            await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
            // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
            // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
        }
        catch (ApplicationCommandException exception)
        {
            // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

            // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
            Console.WriteLine(json);
        }
    }
}