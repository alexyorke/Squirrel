using PlayerIOClient;
using System.Threading;
using Rabbit;
using System.Drawing;
namespace Yonom.EE
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    public class item
    {
        [XmlAttribute]
        public string id;
        [XmlAttribute]
        public string value;
    }
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
            Client conn = new RabbitAuth().LogOn("everybody-edits-su9rn58o40itdbnw69plyw", Config.Email, Config.Password);
            while (conn.ConnectUserId == "") { Thread.Sleep(100); } // wasteful

            Console.WriteLine("Connected");

            stopwatch.Start();
            conn.Multiplayer.JoinRoom("PWUzNk3PZ4bkI", null, delegate (Connection connection)
            {
                connection.OnMessage += Connection_OnMessage;
                globalConn = connection;
                connection.Send("init");

            });
            
            DatabaseObject obj = conn.BigDB.Load("Worlds", "PWUzNk3PZ4bkI");
            FromDatabaseObject(obj);
            Thread.Sleep(Timeout.Infinite);
        }


        public static void FromDatabaseObject(DatabaseObject obj)
        {
            var width = obj.GetInt("width", 200);
            var height = obj.GetInt("height", 200);
            var worldData = obj.GetArray("worlddata");
            if (worldData != null)
            {
                UnserializeFromComplexObject(worldData,width,height);
            }
        }

        public static void UnserializeFromComplexObject(DatabaseArray worlddata,int width,int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            var gr = Graphics.FromImage(bmp);
            gr.Clear(Color.Black); // empty blocks (null) are black
            gr.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));

            FastPixel fp = new FastPixel(bmp);
            fp.Lock();
            List<Tuple<int,int,Color>> rewrittenBlocks = new List<Tuple<int,int,Color>>();
            foreach (DatabaseObject ct in worlddata)
            {
                if (ct.Count == 0) continue;
                var type = (uint)ct.GetValue("type");
                var layerNum = ct.GetInt("layer", 0);
                var xs = ct.GetBytes("x", new byte[0]);
                var ys = ct.GetBytes("y", new byte[0]);

                for (var b = 0; b < xs.Length; b += 2)
                {
                    var nx = (xs[b] << 8) + xs[b + 1];
                    var ny = (ys[b] << 8) + ys[b + 1];

                    string blockColor;
                    blockDict.TryGetValue(Convert.ToString(type), out blockColor);
                    Color c = UIntToColor(Convert.ToUInt32(blockColor));

                    if (layerNum == 0)
                    {
                        // background block
                        Tuple<int, int, Color> tuple =
                                new Tuple<int, int, Color>(nx,ny, Color.FromArgb(255, c.R, c.G, c.B));
                        rewrittenBlocks.Add(tuple);
                    }
                    fp.SetPixel(Convert.ToInt32(nx), Convert.ToInt32(ny), Color.FromArgb(255, c.R, c.G, c.B));

                }
            }
	        foreach (var element in rewrittenBlocks)
	        {
                fp.SetPixel(Convert.ToInt32(element.Item1), Convert.ToInt32(element.Item2), element.Item3);
            }

            Console.WriteLine("unlocked image");
            fp.Unlock(true);
            
            bmp.Save("Image_initdata.png");
            Console.WriteLine("Saved image");
        }

        private static int byteConverter(byte bytes)
        {
            // If the system architecture is little-endian (that is, little end first),
            // reverse the byte array.
            /*if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            int i = BitConverter.ToInt32(bytes, 0);
            return i;*/

            return Convert.ToInt32(bytes);
        }

        private static void LoadBlocks()
        {
            string text = null;
            using (var wc = new System.Net.WebClient())
                text = wc.DownloadString("https://raw.githubusercontent.com/Tunous/EEBlocks/master/Colors.txt");

            if (text == null) {
                throw new InvalidDataException("The blocks did not download correctly.");
            }

            if (!text.Contains("ID: ") || !text.Contains("Mapcolor:"))
            {
                throw new InvalidDataException("The blocks are not in the correct format.");
            }

            text = text.Replace("ID: ","").Replace(" Mapcolor: ", ",");

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

            XmlSerializer serializer = new XmlSerializer(typeof(item[]),
                                 new XmlRootAttribute() { ElementName = "items" });
            using (StreamWriter stream = File.CreateText("blocks.acorn"))
            {
                serializer.Serialize(stream,
              blockDict.Select(kv => new item() { id = kv.Key, value = kv.Value }).ToArray());
              
            }

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
                    stopwatch.Stop();

                    Console.WriteLine("Elapsed: " + stopwatch.ElapsedMilliseconds);
                    break;
            }
        }
    }
}
