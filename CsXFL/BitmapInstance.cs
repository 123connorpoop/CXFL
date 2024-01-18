using System.Xml.Linq;

namespace CsXFL;
public class BitmapInstance : Instance
{
    new public static class DefaultValues
    {
        public const int HPixels = 0;
        public const int VPixels = 0;
    }

    private readonly int hPixels, vPixels;

    public int HPixels { get { return hPixels; } }
    public int VPixels { get { return vPixels; } }
    public BitmapInstance(in XElement instanceNode) : base(instanceNode)
    {
        hPixels = (int?)instanceNode.Attribute("hPixels") ?? DefaultValues.HPixels;
        vPixels = (int?)instanceNode.Attribute("vPixels") ?? DefaultValues.VPixels;
    }
    public BitmapInstance(ref BitmapInstance other) : base(other)
    {
        hPixels = other.hPixels;
        vPixels = other.vPixels;
    }
    // cast from BitmapItem to BitmapInstance
    public BitmapInstance(in BitmapItem item) : base(item, "bitmap", "DOMBitmapInstance")
    {
        hPixels = item.HPixels;
        vPixels = item.VPixels;
    }
}