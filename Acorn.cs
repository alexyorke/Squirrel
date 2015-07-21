using System;

public class Acorn
{
    public static void LoadBlocks()
    {
        string text = null;
        using (var wc = new System.Net.WebClient())
            text = wc.DownloadString("https://raw.githubusercontent.com/Tunous/EEBlocks/master/Colors.txt");

        if (text == null)
        {
            throw new InvalidDataException("The blocks did not download correctly.");
        }

        if (!text.Contains("ID: ") || !text.Contains("Mapcolor:"))
        {
            throw new InvalidDataException("The blocks are not in the correct format.");
        }

        text = text.Replace("ID: ", "").Replace(" Mapcolor: ", ",");

        blocks = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

        var blockIDs = new List<string>();
        var blockColors = new List<Color>();
        foreach (var block in blocks)
        {
            string[] tokens = block.Split(new string[] { "," }, StringSplitOptions.None);
            blockIDs.Add(tokens[0]);
            blockColors.Add(UIntToColor(Convert.ToUInt32(tokens[1])));
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

    public Acorn()
	{
	}
}
