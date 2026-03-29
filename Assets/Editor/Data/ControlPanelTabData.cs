using System.Collections.Generic;
using UnityEngine;

namespace Thisaislan.ControlPanel.Editor.Data
{
    /// <summary>
    /// ScriptableObject that stores data for a single tab in the Control Panel.
    /// Each tab contains a name, description, and a list of ScriptableObject references (by GUID).
    /// </summary>
    [System.Serializable]
    internal class ControlPanelTabData : ScriptableObject
    {
        [SerializeField]
        internal string TabName = string.Empty;

        [SerializeField]
        internal string Description  = string.Empty;

        [SerializeField]
        internal List<string> ScriptableObjectGuids = new List<string>();
    }
}
