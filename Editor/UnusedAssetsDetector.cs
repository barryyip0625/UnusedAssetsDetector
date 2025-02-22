using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BYUtils.AssetsManagement
{
    public class UnusedAssetsDetector : EditorWindow
    {
        // List to store unused asset paths
        private List<string> unusedAssets = new List<string>();
        // Dictionary to map folders to their contained assets
        private Dictionary<string, List<string>> folderAssetsMap = new Dictionary<string, List<string>>();
        // Currently selected folder in the UI
        private string selectedFolder = null;
        // Scroll positions for folder and asset views
        private Vector2 folderScrollPos;
        private Vector2 assetScrollPos;
        // Flag to toggle between list and grid view
        private bool showAsList = true;
        // Set to store whitelisted asset paths
        private HashSet<string> whitelist = new HashSet<string>();
        // Path to the whitelist file
        private string whitelistFilePath;
        // Reference to the WhitelistObject asset
        private WhitelistObject whitelistObject;
        // Static path for the WhitelistObject asset
        private static string whitelistObjectPath;

        // Menu item to open the Unused Assets Detector window
        [MenuItem("Tools/BY Utils/Unused Assets Detector")]
        public static void ShowWindow()
        {
            GetWindow<UnusedAssetsDetector>("Unused Assets Detector");
        }

        private void OnEnable()
        {
            // Determine the path for the WhitelistObject once
            if (string.IsNullOrEmpty(whitelistObjectPath))
            {
                // Get the path of the current script
                string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
                // Get the directory of the script
                string directory = System.IO.Path.GetDirectoryName(scriptPath);
                // Get the parent directory of the current directory
                string parentDirectory = System.IO.Path.GetDirectoryName(directory);
                // Set the path for the Whitelist.asset in the parent directory
                whitelistObjectPath = System.IO.Path.Combine(parentDirectory, "Whitelist.asset");
            }
            
            // Load the whitelist data when the editor window is enabled
            LoadWhitelist();
        }

        private void OnGUI()
        {
            // Button to find unused assets
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

            // Layout for folder and asset views
            EditorGUILayout.BeginHorizontal();
            DrawFolderHierarchy();
            DrawVerticalSeparator();
            DrawAssetList();
            EditorGUILayout.EndHorizontal();

            // Move buttons to the bottom of the panel
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            
            // Button to add an asset to the whitelist
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

            // Button to add a folder to the whitelist
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

            // Buttons to toggle between list and grid view
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
            // Set folder view width to 25% of window width
            float folderWidth = EditorGUIUtility.currentViewWidth * 0.25f;
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
            // Calculate indentation level based on folder depth
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
            
            // Adjust the folder name to fit within the view
            string displayFolderName = folder;
            float folderWidth = EditorGUIUtility.currentViewWidth * 0.25f;
            float buttonWidth = folderWidth - indentLevel * 5 - marginLeft - 10;
            if (style.CalcSize(new GUIContent(displayFolderName)).x > buttonWidth)
            {
                displayFolderName = "..." + System.IO.Path.GetFileName(folder);
            }
            
            // Display folder icon and name
            GUIContent content = new GUIContent(displayFolderName, EditorGUIUtility.IconContent("Folder Icon").image);
            
            if (GUILayout.Button(content, style, GUILayout.Height(16), GUILayout.Width(buttonWidth)))
            {
                selectedFolder = folder;
                PingFolder(folder); // Ping the folder in the project view
            }
            EditorGUILayout.EndHorizontal();
        }

        private void PingFolder(string folderPath)
        {
            // Ping the folder in the project view
            Object folderObject = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            if (folderObject != null)
            {
                EditorGUIUtility.PingObject(folderObject);
            }
        }

        private void DrawVerticalSeparator()
        {
            // Draw a vertical separator between folder and asset views
            EditorGUILayout.BeginVertical(GUILayout.Width(5));
            GUILayout.Box("", GUILayout.ExpandHeight(true), GUILayout.Width(5));
            EditorGUILayout.EndVertical();
        }

        private void DrawAssetList()
        {
            // Display the list of unused assets in the selected folder
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
            // Draw each asset as a list item with a preview and delete button
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
            // Draw each asset as an icon with a delete button
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
            // Ping the asset in the project view
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            EditorGUIUtility.PingObject(obj);
        }

        private void FindUnusedAssets()
        {
            // Clear previous results
            unusedAssets.Clear();
            folderAssetsMap.Clear();
            // Get all asset paths in the project
            string[] allAssets = AssetDatabase.GetAllAssetPaths();
            HashSet<string> usedAssets = new HashSet<string>();

            // Collect all used assets from scenes
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
            // Check if the asset path is in the whitelist
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
            // Confirm and delete the asset
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
            // Check if the window is already open
            if (EditorWindow.HasOpenInstances<UnusedAssetsDetector>())
            {
                var window = GetWindow<UnusedAssetsDetector>("Unused Assets Finder");
                
                window.FindUnusedAssets();
                window.Repaint();
            }
        }

        private void SaveWhitelist()
        {
            // Save the current whitelist to the WhitelistObject
            if (whitelistObject != null)
            {
                whitelistObject.folders = whitelist.Where(path => AssetDatabase.IsValidFolder(path)).ToList();
                whitelistObject.files = whitelist.Where(path => !AssetDatabase.IsValidFolder(path)).ToList();
                EditorUtility.SetDirty(whitelistObject);
                AssetDatabase.SaveAssets();
            }
        }

        private void LoadWhitelist()
        {
            // Load the whitelist from the WhitelistObject
            if (whitelistObject == null)
            {
                whitelistObject = AssetDatabase.LoadAssetAtPath<WhitelistObject>(whitelistObjectPath);
                if (whitelistObject == null)
                {
                    // Create a new WhitelistObject if it doesn't exist
                    whitelistObject = ScriptableObject.CreateInstance<WhitelistObject>();
                    AssetDatabase.CreateAsset(whitelistObject, whitelistObjectPath);
                    AssetDatabase.SaveAssets();
                }
            }

            if (whitelistObject != null)
            {
                // Ensure folders and files lists are initialized
                if (whitelistObject.folders == null)
                {
                    whitelistObject.folders = new List<string>();
                }
                if (whitelistObject.files == null)
                {
                    whitelistObject.files = new List<string>();
                }

                // Combine folders and files into a single whitelist set
                whitelist = new HashSet<string>(whitelistObject.folders.Concat(whitelistObject.files));
            }
        }
    } 
}