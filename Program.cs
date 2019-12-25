using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CLIPSNET;
using AIMLbot;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.IO;
using System.Net;
using System.Threading;

namespace resultproject
{
    class Program
    {
        private static CLIPSNET.Environment clips;
        private static ITelegramBotClient botClient;
        private static Bot AI;
        private static Dictionary<long, User> users;
        private static Random dice;
        private static string LoadKey(string pathToFile)
        {
            StreamReader sr = new StreamReader(pathToFile);
            string key = sr.ReadLine().Replace(" ", string.Empty);

            return key;
        }
        private static async void actionOnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                Console.WriteLine($"Входящее сообщение в чате: {e.Message.Chat.Id}");
                Console.WriteLine($"Входящее сообщение: {e.Message.Text}");

                AI.isAcceptingUserInput = false;
                User currentUser;
                if (users.ContainsKey(e.Message.Chat.Id))
                    currentUser = users[e.Message.Chat.Id];
                else
                {
                    currentUser = new User(e.Message.Chat.Id.ToString(), AI);
                    users.Add(e.Message.Chat.Id, currentUser);
                }
                AI.isAcceptingUserInput = true;
                Request r = new Request(e.Message.Text, currentUser, AI);
                Result res = AI.Chat(r);

                string robot_answer = res.Output;
                Console.WriteLine("Robot: " + robot_answer);
                Thread.Sleep(dice.Next(10, 956));
                if (robot_answer == "Я..")
                {
                    await botClient.SendTextMessageAsync(
                    chatId: e.Message.Chat,
                    text: "Robot:\n" + "Я.."
                    );
                    robot_answer = "Я не знаю.";
                    Thread.Sleep(1000);
                    await botClient.SendTextMessageAsync(
                    chatId: e.Message.Chat,
                    text: robot_answer
                );
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: "Robot:\n" + robot_answer
                    );
                }
            }
        }
        static void Main(string[] args)
        {
            AI = new Bot();
            AI.loadSettings();
            AI.loadAIMLFromFiles();
            users = new Dictionary<long, User>();

            string pathToFileKey = "config/bot_key";
            string accessToken = LoadKey(pathToFileKey);

            var proxy = new WebProxy("51.158.111.229:8811"); // 51.158.111.229:8811 

            botClient = new TelegramBotClient(accessToken, proxy);
            var me = botClient.GetMeAsync().Result;

            dice = new Random();

            Console.WriteLine(
                $"I'M ALIVE 4AW3 1S {me.Id} :: {me.FirstName}"
            );

            clips = new CLIPSNET.Environment();

            clips.Clear();
            string pathToFileCLIPS = "CLIPSfiles/cars_all_available.clp";
            clips.Load(pathToFileCLIPS);
            clips.Reset();

            botClient.OnMessage += actionOnMessage;
            botClient.StartReceiving();

            Thread.Sleep(int.MaxValue);
        }
    }
}
