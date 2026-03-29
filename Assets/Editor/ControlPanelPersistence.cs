using System.Collections.Generic;
using System.IO;
using Thisaislan.ControlPanel.Editor.Data;
using UnityEditor;
using UnityEngine;

namespace Thisaislan.ControlPanel.Editor
{
    /// <summary>
    /// Handles persistence operations for Control Panel tabs (save, load, delete)
    /// </summary>
    internal static class ControlPanelPersistence
    {
        private const string TabsFolderPath = "Assets/Tools/ControlPanel/Tabs";
        private const string TabAssetFilter = "t:ControlPanelTabData";
        private const string AssetFileExtension = ".asset";

        /// <summary>
        /// Saves a tab to disk. Creates new asset or updates existing one.
        /// Renames the asset file if the TabName has changed.
        /// </summary>
        /// <param name="tab">The tab data to save</param>
        internal static void SaveTab(ControlPanelTabData tab)
        {
            EnsureTabsFolderExists();

            string currentAssetPath = AssetDatabase.GetAssetPath(tab);
            string expectedAssetPath = GetAssetPath(tab.TabName);

            if (string.IsNullOrEmpty(currentAssetPath))
            {
                CreateNewTabAsset(tab, expectedAssetPath);
            }
            else
            {
                UpdateExistingTabAsset(tab, currentAssetPath);
            }

            MarkTabDirtyAndSave(tab);
        }

        /// <summary>
        /// Checks if a tab with the given name already exists.
        /// </summary>
        /// <param name="tabName">The name to check</param>
        /// <returns>True if a tab with this name exists, false otherwise</returns>
        internal static bool TabNameExists(string tabName)
        {
            if (string.IsNullOrEmpty(tabName))
            {
                return false;
            }

            string expectedPath = GetAssetPath(tabName);

            return AssetDatabase.LoadAssetAtPath<ControlPanelTabData>(expectedPath) != null;
        }

        /// <summary>
        /// Loads all tabs from the Tabs folder.
        /// </summary>
        /// <returns>List of loaded tab data objects</returns>
        internal static List<ControlPanelTabData> LoadTabs()
        {
            List<ControlPanelTabData> tabs = new List<ControlPanelTabData>();

            if (!AssetDatabase.IsValidFolder(TabsFolderPath))
            {
                return tabs;
            }

            string[] guids = AssetDatabase.FindAssets(TabAssetFilter, new[] { TabsFolderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ControlPanelTabData tab = AssetDatabase.LoadAssetAtPath<ControlPanelTabData>(path);

                if (tab == null)
                {
                    continue;
                }

                tabs.Add(tab);
            }

            return tabs;
        }

        /// <summary>
        /// Deletes a tab asset from disk.
        /// </summary>
        /// <param name="tab">The tab to delete</param>
        internal static void DeleteTab(ControlPanelTabData tab)
        {
            string path = AssetDatabase.GetAssetPath(tab);

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        /// <summary>
        /// Ensures the tabs folder exists, creating it if necessary.
        /// </summary>
        private static void EnsureTabsFolderExists()
        {
            if (AssetDatabase.IsValidFolder(TabsFolderPath))
            {
                return;
            }

            Directory.CreateDirectory(TabsFolderPath);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Gets the full asset path for a tab with the given name.
        /// </summary>
        /// <param name="tabName">Name of the tab</param>
        /// <returns>Full asset path</returns>
        private static string GetAssetPath(string tabName)
        {
            return $"{TabsFolderPath}/{tabName}{AssetFileExtension}";
        }

        /// <summary>
        /// Creates a new asset for a tab.
        /// </summary>
        private static void CreateNewTabAsset(ControlPanelTabData tab, string assetPath)
        {
            AssetDatabase.CreateAsset(tab, assetPath);
        }

        /// <summary>
        /// Updates an existing tab asset, renaming the file if needed.
        /// </summary>
        private static void UpdateExistingTabAsset(ControlPanelTabData tab, string currentPath)
        {
            string currentFileName = Path.GetFileNameWithoutExtension(currentPath);
            
            if (currentFileName == tab.TabName)
            {
                return;
            }
            
            MarkTabDirtyAndSave(tab);
            AssetDatabase.RenameAsset(currentPath, tab.TabName);
        }

        /// <summary>
        /// Marks a tab as dirty and saves all assets.
        /// </summary>
        private static void MarkTabDirtyAndSave(ControlPanelTabData tab)
        {
            EditorUtility.SetDirty(tab);
            AssetDatabase.SaveAssets();
        }
    }
}