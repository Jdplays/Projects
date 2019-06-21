using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISelectable
{
    bool IsSelected { get; set; }

    string GetName();

    string GetDescription();
    
    string GetJobDescription();

    string GetStatus();

    Dictionary<string, List<Inventory>> GetInternalInventory();

    IEnumerable<string> GetAdditionalInfo();
}
