using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using CLIPSNET;
using AIMLbot;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.Net;
using System.Threading;
using System.IO;

using Emgu;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.UI;
using Emgu.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using NeuralNetwork1;

namespace resultproject
{
    class TelegramBot
    {
        private CLIPSNET.Environment clips;
        private ITelegramBotClient botClient;
        private Bot AI;
        private Dictionary<long, User> users;
        private Random dice;
        private bool CLIPSmode;
        private HashSet<string> currentFacts;
        private NeuralNetwork Net;
        public TelegramBot(string botKey, WebProxy proxy)
        {
            AI = new Bot();
            AI.loadSettings();
            AI.loadAIMLFromFiles();
            users = new Dictionary<long, User>();

            botClient = new TelegramBotClient(botKey, proxy);
            var me = botClient.GetMeAsync().Result;

            dice = new Random();

            Console.WriteLine(
                $"I'M ALIVE 4AW3 1S {me.Id} :: {me.FirstName}"
            );

            clips = new CLIPSNET.Environment();

            currentFacts = new HashSet<string>();
            CLIPSmode = false;

            botClient.OnMessage += actionOnMessage;

            Net = new NeuralNetwork("NN/model/");
        }
        public void Run()
        {
            botClient.StartReceiving();
            Thread.Sleep(int.MaxValue);
        }
        private string HandleResponse()
        {
            string evalStr = "(find-fact ((?f ioproxy)) TRUE)";
            FactAddressValue fv = (FactAddressValue)((MultifieldValue)clips.Eval(evalStr))[0];

            MultifieldValue damf = (MultifieldValue)fv["messages"];

            string result = "";
            for (int i = damf.Count - 1; i >= 0; i--)
            {
                LexemeValue da = (LexemeValue)damf[i];
                byte[] bytes = Encoding.Default.GetBytes(da.Value);
                result += Encoding.UTF8.GetString(bytes) + System.Environment.NewLine;
            }

            clips.Eval("(assert (clearmessage))");
            return result;
        }
        private string clipsStep()
        {
            string result = "Текущие факты:" + System.Environment.NewLine;
            foreach (var fact in currentFacts)
            {
                string mess = String.Format("(assert({0}))", fact);
                Console.WriteLine(mess);
                clips.Eval(mess);
                result += fact + " ";
            }
            bool wasEmpty = true;
            clips.Run();
            string handlingResponse = HandleResponse();
            while (handlingResponse != "")
            {
                Console.WriteLine(handlingResponse);
                result += System.Environment.NewLine + handlingResponse;
                clips.Run();
                handlingResponse = HandleResponse();
                wasEmpty = false;
            }
            if (wasEmpty)
                result += "Предоставь больше характеристик";
            return result;
        }
        private async void textAnswer(object sender, MessageEventArgs e)
        {
            Console.WriteLine($"Входящее сообщение в чате: {e.Message.Chat.Id}");
            Console.WriteLine($"Входящее сообщение: {e.Message.Text}");
            if (CLIPSmode && e.Message.Text == "Стоп")
            {
                CLIPSmode = false;
                await botClient.SendTextMessageAsync(
                    chatId: e.Message.Chat,
                    text: "Robot:\n" + "Хорошо, закончили."
                    );
            }
            else if (CLIPSmode)
            {
                currentFacts.Add(e.Message.Text.Trim().ToLower());
                string result = clipsStep();
                await botClient.SendTextMessageAsync(
                    chatId: e.Message.Chat,
                    text: "Robot:\n" + result
                    );
            }
            else
            {
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
                else if (robot_answer.EndsWith("/CLIPS."))
                {
                    await botClient.SendTextMessageAsync(
                    chatId: e.Message.Chat,
                    text: "Robot:\n" + robot_answer.Split('/')[0]);
                    CLIPSmode = true;
                    clips.Clear();
                    string pathToFileCLIPS = "CLIPSfiles/cars_all_available.clp";
                    clips.Load(pathToFileCLIPS);
                    clips.Reset();
                    await botClient.SendTextMessageAsync(
                    chatId: e.Message.Chat,
                    text: "Вводи по одной характеристики и я буду формировать результат");
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
        private Mat ImgToMat(Image image)
        {
            int stride;
            Bitmap bmp = new Bitmap(image);

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

            System.Drawing.Imaging.PixelFormat pf = bmp.PixelFormat;
            if (pf == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                stride = bmp.Width * 4;
            else
                stride = bmp.Width * 3;

            Image<Bgra, byte> cvImage = new Image<Bgra, byte>(bmp.Width, bmp.Height, stride, (IntPtr)bmpData.Scan0);
            bmp.UnlockBits(bmpData);
            
            return cvImage.Mat;
        }
        private async void photoAnswer(object sender, MessageEventArgs e)
        {
            await botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: "Robot:\n" + "Я посмотрю."
                    );

            var fileid = e.Message.Photo[e.Message.Photo.Length - 1].FileId;
            var imageFile = await botClient.GetFileAsync(fileid);

            Image img;
            using (MemoryStream mStream = new MemoryStream(new byte[800000]))
            {
                await botClient.DownloadFileAsync(imageFile.FilePath, mStream);
                mStream.Seek(0, SeekOrigin.Begin);
                img = Image.FromStream(mStream);
            }
            Console.WriteLine(img.Size);
            Mat imgMat = ImgToMat(img);
            Console.WriteLine(imgMat.NumberOfChannels);
            CvInvoke.CvtColor(imgMat, imgMat, ColorConversion.Bgr2Gray);

            CvInvoke.Threshold(imgMat, imgMat, 125, 255, Emgu.CV.CvEnum.ThresholdType.Binary);
            CvInvoke.BitwiseNot(imgMat, imgMat);
            Size size = new Size(28, 28);
            CvInvoke.Resize(imgMat, imgMat, size, 1, 1, Emgu.CV.CvEnum.Inter.Linear);
            imgMat.Save("../../../test.jpg");
            Console.WriteLine(imgMat.Size);
            Console.WriteLine(imgMat.NumberOfChannels);

            double[] netInput = new double[size.Width * size.Height];
            for (int i = 0; i < size.Width; i++)
                for (int j = 0; j < size.Height; j++)
                    netInput[i  + j * 28] = imgMat.Bitmap.GetPixel(i, j).B;

            Sample sample = new Sample(netInput);
            Console.WriteLine(sample.input.Length);
            var result = Net.Predict(sample);

            await botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: "Robot:\n" + "Кажется, это " + ((int)result).ToString()
                    ) ;
        }
        private void actionOnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                textAnswer(sender, e);
            }
            if (e.Message.Photo != null)
            {
                photoAnswer(sender, e);
            }
        }
    }
}
