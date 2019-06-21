using System.Xml;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class QuestGoal
{
    public string Description { get; set; }

    public string IsCompletedLuaFunction { get; set; }

    public Parameter Parameters { get; set; }

    public bool IsCompleted { get; set; }

    public void ReadXmlPrototype(XmlReader reader_parent)
    {
        Description = reader_parent.GetAttribute("Description");
        IsCompletedLuaFunction = reader_parent.GetAttribute("IsCompletedLuaFunction");

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