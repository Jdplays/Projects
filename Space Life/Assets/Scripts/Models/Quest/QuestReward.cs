using System.Xml;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class QuestReward
{
    public string Description { get; set; }

    public string OnRewardLuaFunction { get; set; }

    public Parameter Parameters { get; set; }

    public bool IsCollected { get; set; }

    public void ReadXmlPrototype(XmlReader reader_parent)
    {
        Description = reader_parent.GetAttribute("Description");
        OnRewardLuaFunction = reader_parent.GetAttribute("OnRewardLuaFunction");

        XmlReader reader = reader_parent.ReadSubtree();

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Params":
                    Parameters = Parameter.ReadXml(reader);
                    break;
            }
        }
    }
}