using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using MoonSharp.Interpreter;
using SpaceLife.Localization;
using UnityEngine;

/// <summary>
/// Contains the description of a single overlay type. Contains LUA function name, id and coloring details.
/// </summary>
[MoonSharpUserData]
public class OverlayDescriptor : IPrototypable
{
    public OverlayDescriptor()
    {
        ColorMap = ColorMapOption.Jet;
        Min = 0;
        Max = 255;
    }

    /// <summary>
    /// Select the type of color map (coloring scheme) you want to use.
    /// TODO: More color maps.
    /// </summary>
    public enum ColorMapOption
    {
        Jet,
        Random
    }

    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Gets the localized name.
    /// </summary>
    public string Name
    {
        get { return "overlay: " + Type; }
    }

    /// <summary>
    /// Type of color map used by this descriptor.
    /// </summary>
    public ColorMapOption ColorMap { get; private set; }

    /// <summary>
    /// Name of function returning int (index of color) given a tile t.
    /// </summary>
    public string LuaFunctionName { get; private set; }

    /// <summary>
    /// Gets the min bound for clipping coloring.
    /// </summary>
    public int Min { get; private set; }

    /// <summary>
    /// Gets the max bound for clipping coloring.
    /// </summary>
    public int Max { get; private set; }

    /// <summary>
    /// Creates an OverlayDescriptor form a xml subtree with node \<Overlay></Overlay>\.
    /// </summary>
    /// <param name="xmlReader">The subtree pointing to Overlay.</param>
    public void ReadXmlPrototype(XmlReader xmlReader)
    {
        Type = xmlReader.GetAttribute("type");

        if (xmlReader.GetAttribute("min") != null)
        {
            Min = XmlConvert.ToInt32(xmlReader.GetAttribute("min"));
        }

        if (xmlReader.GetAttribute("max") != null)
        {
            Max = XmlConvert.ToInt32(xmlReader.GetAttribute("max"));
        }

        if (xmlReader.GetAttribute("colorMap") != null)
        {
            try
            {
                ColorMap = (ColorMapOption)Enum.Parse(typeof(ColorMapOption), xmlReader.GetAttribute("colorMap"));
            }
            catch (ArgumentException e)
            {
                Debug.ULogErrorChannel("OverlayMap", "Invalid color map!", e);
            }
        }

        xmlReader.Read();
        LuaFunctionName = xmlReader.ReadContentAsString();
    }
}
