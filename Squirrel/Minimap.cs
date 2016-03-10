using System.Drawing;
using System.Drawing.Imaging;
using Decagon.EE;
using nQuant;

public class Minimap
{
    private Bitmap bmp;
    private WuQuantizer quantizer = new WuQuantizer();
    public FastPixel stage;

    public int height { get; internal set; }
    public int width { get; internal set; }

    public void initialize()
    {
        bmp = new Bitmap(width, height);
        var gr = Graphics.FromImage(bmp);
        gr.Clear(Color.Black); // Set the empty color: black
        //gr.DrawImage(bmp, new Rectangle(0, 0, width, height));

        stage = new FastPixel(bmp);
        stage.Lock();
    }

    public void drawBlock(int x, int y, uint blockId)
    {
        byte[] c;
        if (!Program.BlockDict.TryGetValue(blockId, out c))
        {
            // Unknown blockId: skip
            return;
        }

        // Skip blank blocks
        if (!((c[0] == 0) && (c[1] == 0) && (c[2] == 0)))
            stage.SetPixel(x, y, c);
    }

    public void Save(string v)
    {
        stage.Unlock(true);

        bmp.Save(v, ImageFormat.Png);
    }
}