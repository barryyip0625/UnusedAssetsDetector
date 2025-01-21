using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BYUtils.AssetsManagement
{
    public class UnusedAssetsDetector : EditorWindow
    {
        private List<string> unusedAssets = new List<string>();
        private Dictionary<string, List<string>> folderAssetsMap = new Dictionary<string, List<string>>();
        private string selectedFolder = null;
        private Vector2 folderScrollPos;
        private Vector2 assetScrollPos;
        private bool showAsList = true; // Default to show as list
        private HashSet<string> whitelist = new HashSet<string>();
        private string whitelistFilePath;

        [MenuItem("Tools/BY Utils/Unused Assets Detector")]
        public static void ShowWindow()
        {
            GetWindow<UnusedAssetsDetector>("Unused Assets Detector");
        }
        
        private void OnEnable()
        {
            // Get the script directory and set the whitelist file path
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string directory = System.IO.Path.GetDirectoryName(scriptPath);
            whitelistFilePath = System.IO.Path.Combine(directory, "whitelist.txt");

            LoadWhitelist(); // Load whitelist on startup
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Find Unused Assets"))
            {
                LoadWhitelist(); // Reload whitelist before each search
                FindUnusedAssets();
                
                // Display the first folder
                if (folderAssetsMap.Keys.Count > 0)
                {
                    selectedFolder = folderAssetsMap.Keys.OrderBy(f => f).First();
                }
            }

            EditorGUILayout.BeginHorizontal();
            DrawFolderHierarchy();
            DrawVerticalSeparator();
            DrawAssetList();
            EditorGUILayout.EndHorizontal();

            // Move buttons to the bottom of the panel
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add Asset to Whitelist", GUILayout.Width(200)))
            {
                string path = EditorUtility.OpenFilePanel("Select Asset", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                    whitelist.Add(path);
                    SaveWhitelist(); // Save whitelist
                    FindUnusedAssets(); // Refresh display by re-finding unused assets
                }
            }

            if (GUILayout.Button("Add Folder to Whitelist", GUILayout.Width(200)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                    whitelist.Add(path);
                    SaveWhitelist(); // Save whitelist
                    FindUnusedAssets(); // Refresh display by re-finding unused assets
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Show as List", GUILayout.Width(100)))
            {
                showAsList = true;
            }
            if (GUILayout.Button("Show as Grid", GUILayout.Width(100)))
            {
                showAsList = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFolderHierarchy()
        {
            float folderWidth = EditorGUIUtility.currentViewWidth * 0.25f; // Set to 25% of window width
            folderScrollPos = EditorGUILayout.BeginScrollView(folderScrollPos, GUILayout.Width(folderWidth), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical();
            foreach (var folder in folderAssetsMap.Keys.OrderBy(f => f))
            {
                if (!IsInWhitelist(folder)) // Filter out folders in the whitelist
                {
                    DrawFolderItem(folder);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawFolderItem(string folder)
        {
            int indentLevel = folder.Count(c => c == '/');
            EditorGUILayout.BeginHorizontal();
            
            // Add left margin
            int marginLeft = 10;
            GUILayout.Space(indentLevel * 5 + marginLeft);
            
            GUIStyle style = new GUIStyle(EditorStyles.label);
            if (folder == selectedFolder)
            {
                style.normal.textColor = Color.white;
                style.normal.background = Texture2D.grayTexture;
            }
            GUIContent content = new GUIContent(System.IO.Path.GetFileName(folder), EditorGUIUtility.IconContent("Folder Icon").image);
            
            // Set button width to match folder hierarchy width, considering indentation
            float folderWidth = EditorGUIUtility.currentViewWidth * 0.25f;
            float buttonWidth = folderWidth - indentLevel * 5 - marginLeft - 10;
            if (GUILayout.Button(content, style, GUILayout.Height(16), GUILayout.Width(buttonWidth)))
            {
                selectedFolder = folder;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawVerticalSeparator()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(5));
            GUILayout.Box("", GUILayout.ExpandHeight(true), GUILayout.Width(5));
            EditorGUILayout.EndVertical();
        }

        private void DrawAssetList()
        {
            assetScrollPos = EditorGUILayout.BeginScrollView(assetScrollPos);
            EditorGUILayout.BeginVertical();
            if (selectedFolder != null && folderAssetsMap.ContainsKey(selectedFolder))
            {
                GUILayout.Label("Unused Assets in " + selectedFolder + ":", EditorStyles.boldLabel);

                // Create a copy of the asset list to avoid errors when modifying
                var assets = new List<string>(folderAssetsMap[selectedFolder]);

                if (showAsList)
                {
                    foreach (var asset in assets)
                    {
                        DrawAssetItem(asset);
                    }
                }
                else
                {
                    int itemWidth = 110;
                    int itemMargin = 20;
                    int itemsPerRow = Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - itemWidth * 1.5f - itemMargin) / (itemWidth));
                    int currentItem = 0;

                    EditorGUILayout.BeginHorizontal();
                    foreach (var asset in assets)
                    {
                        if (currentItem >= itemsPerRow)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            currentItem = 0;
                        }

                        DrawAssetItemAsIcon(asset);
                        currentItem++;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawAssetItem(string asset)
        {
            EditorGUILayout.BeginHorizontal("box");
            
            // Try to get the asset's preview image
            Texture2D previewTexture = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Object>(asset));
            if (previewTexture == null)
            {
                // Use a mini thumbnail if no preview image is available
                previewTexture = AssetPreview.GetMiniThumbnail(AssetDatabase.LoadAssetAtPath<Object>(asset));
            }
            
            // Display asset icon or preview
            if (GUILayout.Button(previewTexture, GUILayout.Width(50), GUILayout.Height(50)))
            {
                PingAsset(asset);
            }
            
            if (GUILayout.Button(asset, EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.Height(50)))
            {
                PingAsset(asset);
            }
            if (GUILayout.Button("Delete", GUILayout.Width(60), GUILayout.Height(50)))
            {
                DeleteAsset(asset);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssetItemAsIcon(string asset)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(100), GUILayout.Height(120));
            
            // Try to get the asset's preview image
            Texture2D previewTexture = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Object>(asset));
            if (previewTexture == null)
            {
                // Use a mini thumbnail if no preview image is available
                previewTexture = AssetPreview.GetMiniThumbnail(AssetDatabase.LoadAssetAtPath<Object>(asset));
            }
            
            // Display asset icon or preview
            if (GUILayout.Button(previewTexture, GUILayout.Width(100), GUILayout.Height(100)))
            {
                PingAsset(asset);
            }
            
            // Display asset name
            GUILayout.Label(System.IO.Path.GetFileNameWithoutExtension(asset), EditorStyles.label, GUILayout.Width(100));
            
            // Display delete button
            if (GUILayout.Button("Delete", GUILayout.Width(100)))
            {
                DeleteAsset(asset);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void PingAsset(string assetPath)
        {
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            EditorGUIUtility.PingObject(obj);
        }

        private void FindUnusedAssets()
        {
            unusedAssets.Clear();
            folderAssetsMap.Clear();
            string[] allAssets = AssetDatabase.GetAllAssetPaths();
            HashSet<string> usedAssets = new HashSet<string>();

            // Collect all used assets
            string[] allScenes = AssetDatabase.FindAssets("t:Scene");
            foreach (var sceneGUID in allScenes)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
                string[] dependencies = AssetDatabase.GetDependencies(scenePath, true);
                foreach (var dependency in dependencies)
                {
                    usedAssets.Add(dependency);
                }
            }

            // Find unused assets and organize by folder
            foreach (var asset in allAssets)
            {
                if (!usedAssets.Contains(asset) && asset.StartsWith("Assets/") && 
                    !asset.Contains("/Editor/") && !AssetDatabase.IsValidFolder(asset) &&
                    !IsInWhitelist(asset))
                {
                    unusedAssets.Add(asset);
                    string folder = System.IO.Path.GetDirectoryName(asset);
                    if (!folderAssetsMap.ContainsKey(folder))
                    {
                        folderAssetsMap[folder] = new List<string>();
                    }
                    folderAssetsMap[folder].Add(asset);
                }
            }
        }

        private bool IsInWhitelist(string assetPath)
        {
            foreach (var path in whitelist)
            {
                if (assetPath.StartsWith(path))
                {
                    return true;
                }
            }
            return false;
        }

        private void DeleteAsset(string assetPath)
        {
            if (EditorUtility.DisplayDialog("Delete Asset", "Are you sure you want to delete " + assetPath + "?", "Yes", "No"))
            {
                AssetDatabase.DeleteAsset(assetPath);
                unusedAssets.Remove(assetPath);
                string folder = System.IO.Path.GetDirectoryName(assetPath);

                if (folderAssetsMap.ContainsKey(folder))
                {
                    folderAssetsMap[folder].Remove(assetPath);

                    // Remove the folder if the asset list is empty
                    if (folderAssetsMap[folder].Count == 0)
                    {
                        folderAssetsMap.Remove(folder);
                    }
                }
            }
        }

        public static void Refresh()
        {
            var window = GetWindow<UnusedAssetsDetector>("Unused Assets Finder");
            window.FindUnusedAssets();
            window.Repaint();
        }

        private void SaveWhitelist()
        {
            System.IO.File.WriteAllLines(whitelistFilePath, whitelist);
        }

        private void LoadWhitelist()
        {
            if (System.IO.File.Exists(whitelistFilePath))
            {
                var lines = System.IO.File.ReadAllLines(whitelistFilePath);
                whitelist = new HashSet<string>(lines);
            }
        }
    } 
}