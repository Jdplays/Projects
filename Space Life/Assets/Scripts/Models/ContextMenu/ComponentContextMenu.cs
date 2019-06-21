using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ComponentContextMenu
{
    public string Name { get; set; }

    public Action<NestedObject, string> Function { get; set; }
}
