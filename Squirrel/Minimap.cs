using Decagon.EE;
using System;
using System.Drawing;
using System.Collections.Generic;

public class Minimap
{
    private Bitmap bmp;
    private FastPixel fp;
    public Minimap()
	{
        
    }

    public void initialize()
    {
        bmp = new Bitmap(width, height);
        var gr = Graphics.FromImage(bmp);
        gr.Clear(Color.Black); // empty blocks (null) are black
        gr.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));

        fp = new FastPixel(bmp);

        fp.Lock();
    }

    public Tuple<int, int, Color> drawBlock(DataChunk chunk, Decagon.EE.Point pos)
    {
        Color c;
        Tuple<int, int, Color> rewrittenBlock_local = null;

        blockDict.TryGetValue(Convert.ToString(chunk.Type), out c);
        if (chunk.Layer == 1)
        {
            // background block
            rewrittenBlock_local = new Tuple<int, int, Color>(pos.X, pos.Y, Color.FromArgb(255, c.R, c.G, c.B));
        }

        fp.SetPixel(pos.X, pos.Y, Color.FromArgb(255, c.R, c.G, c.B));


        // workaround to get reference to a tuple
        return rewrittenBlock_local;
    }

    public int height { get; internal set; }
    public int width { get; internal set; }
    public Dictionary<string, Color> blockDict = Acorn.LoadBlocks();

    internal void Save(string v)
    {
        fp.Unlock(true);

        bmp.Save(v);
    }

    public void rewriteForegroundBlocks(List<Tuple<int, int, Color>> rewrittenBlocks)
    {
        foreach (var element in rewrittenBlocks)
        {
            if (element != null)
            {
                fp.SetPixel(Convert.ToInt32(element.Item1), Convert.ToInt32(element.Item2), element.Item3);
            }
        }
    }

}
