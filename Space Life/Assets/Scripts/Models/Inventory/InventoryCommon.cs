using System.Xml;

public class InventoryCommon : IPrototypable
{
    public string type;
    public int maxStackSize;
    public float basePrice = 1f;
    public string category;

    public string Type
    {
        get { return type; }
    }

    public void ReadXmlPrototype(XmlReader reader_parent)
    {
        type = reader_parent.GetAttribute("type");
        maxStackSize = int.Parse(reader_parent.GetAttribute("maxStackSize") ?? "50");
        basePrice = float.Parse(reader_parent.GetAttribute("basePrice") ?? "1");
        category = reader_parent.GetAttribute("category");
    }
}
