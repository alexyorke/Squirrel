using PlayerIOClient;
using System.Threading;
using Rabbit;
using System.Drawing;
namespace Yonom.EE
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    class Program
    {
        static Stopwatch stopwatch = new Stopwatch();
        static Connection globalConn = null;
        static string[] blocks;

        public static Dictionary<string, string> blockDict { get; private set; }

        static void Main(string[] args)
        {
            // Load the blocks into memory
            LoadBlocks();

            // Log on
            Client conn = new RabbitAuth().LogOn("everybody-edits-su9rn58o40itdbnw69plyw", "[USERNAME]", "[PASSWORD]");
            while (conn.ConnectUserId == "") { Thread.Sleep(100); } // wasteful

            Console.WriteLine("Connected");

            conn.Multiplayer.CreateJoinRoom("[WORLDID]", "public", true, null, null, delegate (Connection connection)
            {
                connection.OnMessage += Connection_OnMessage;
                globalConn = connection;
                connection.Send("init");

            });
            Thread.Sleep(Timeout.Infinite);
        }

        private static void LoadBlocks()
        {
            string text = System.IO.File.ReadAllText(@"blocks.txt"); // https://raw.githubusercontent.com/Tunous/EEBlocks/master/Colors.txt
            blocks = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

            var blockIDs = new List<string>();
            var blockColors = new List<string>();
            foreach (var block in blocks)
            {
                string[] tokens = block.Split(new string[] { "," }, StringSplitOptions.None);
                blockIDs.Add(tokens[0]);
                blockColors.Add(tokens[1]);
            }

            blockDict = blockIDs.Zip(blockColors, (s, i) => new { s, i })
                          .ToDictionary(item => item.s, item => item.i);
        }

        public static Color UIntToColor(uint color)
        {
            byte a = (byte)(color >> 24);
            byte r = (byte)(color >> 16);
            byte g = (byte)(color >> 8);
            byte b = (byte)(color >> 0);
            return Color.FromArgb(a, r, g, b);
        }

        static void Connection_OnMessage(object sender, Message e)
        {
            switch (e.Type)
            {
                case "init":
                    globalConn.Disconnect(); // already have init data; don't need to be connected
                    
                    int width = e.GetInt(15), height = e.GetInt(16);

                    //bitmap
                    Bitmap bmp = new Bitmap(width, height);
                    var gr = Graphics.FromImage(bmp);
                    gr.Clear(Color.Black); // empty blocks (null) are black
                    gr.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));

                    FastPixel fp = new FastPixel(bmp);
                    fp.Lock();

                    var roomData = new uint[2, e.GetInt(15), e.GetInt(16)];
                    
                    var chunks = InitParse.Parse(e);
                    foreach (var chunk in chunks)
                    {
                        foreach (var pos in chunk.Locations)
                        {
                            string blockColor = "4281729592"; // black

                            blockDict.TryGetValue(Convert.ToString(chunk.Type), out blockColor);
                            Color c = UIntToColor(Convert.ToUInt32(blockColor));
                            fp.SetPixel(pos.X, pos.Y, Color.FromArgb(255, c.R, c.G, c.B));
                        }
                    }

                    fp.Unlock(true);
                    bmp.Save("Image.png");

                    break;
            }
        }
    }
}
