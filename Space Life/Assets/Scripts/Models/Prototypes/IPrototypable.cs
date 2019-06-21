using System.Xml;

public interface IPrototypable
{
    /// <summary>
    /// Gets the Type of the prototype.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Reads the prototype from the specified XML reader.
    /// </summary>
    /// <param name="readerParent">The XML reader to read from.</param>
    void ReadXmlPrototype(XmlReader reader);
}
