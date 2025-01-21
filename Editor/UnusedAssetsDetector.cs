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
        private bool showAsList = true; // 默認顯示為列表
        private HashSet<string> whitelist = new HashSet<string>();
        private string whitelistFilePath;

        [MenuItem("Tools/BY Utils/Unused Assets Detector")]
        public static void ShowWindow()
        {
            GetWindow<UnusedAssetsDetector>("Unused Assets Detector");
        }
        
        private void OnEnable()
        {
            // 獲取腳本所在目錄並設置白名單文件路徑
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string directory = System.IO.Path.GetDirectoryName(scriptPath);
            whitelistFilePath = System.IO.Path.Combine(directory, "whitelist.txt");

            LoadWhitelist(); // 啟動時加載白名單
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Find Unused Assets"))
            {
                LoadWhitelist(); // 每次查找前重新加載白名單
                FindUnusedAssets();
                
                // 顯示第一個文件夾
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

            // 將按鈕移動到面板底部
            GUILayout.FlexibleSpace(); // 添加彈性空間
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add Asset to Whitelist", GUILayout.Width(200)))
            {
                string path = EditorUtility.OpenFilePanel("Select Asset", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                    whitelist.Add(path);
                    SaveWhitelist(); // 保存白名單
                    FindUnusedAssets(); // 重新查找未使用資產以刷新顯示
                }
            }

            if (GUILayout.Button("Add Folder to Whitelist", GUILayout.Width(200)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                    whitelist.Add(path);
                    SaveWhitelist(); // 保存白名單
                    FindUnusedAssets(); // 重新查找未使用資產以刷新顯示
                }
            }

            GUILayout.FlexibleSpace(); // 在按鈕之間添加彈性空間

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
            float folderWidth = EditorGUIUtility.currentViewWidth * 0.25f; // 設置為窗口寬度的 25%
            folderScrollPos = EditorGUILayout.BeginScrollView(folderScrollPos, GUILayout.Width(folderWidth), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical();
            foreach (var folder in folderAssetsMap.Keys.OrderBy(f => f))
            {
                if (!IsInWhitelist(folder)) // 過濾掉白名單中的文件夾
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
            
            // 添加左邊距
            int marginLeft = 10; // 設置左邊距的大小
            GUILayout.Space(indentLevel * 5 + marginLeft); // 增加左邊距
            
            GUIStyle style = new GUIStyle(EditorStyles.label);
            if (folder == selectedFolder)
            {
                style.normal.textColor = Color.white;
                style.normal.background = Texture2D.grayTexture;
            }
            GUIContent content = new GUIContent(System.IO.Path.GetFileName(folder), EditorGUIUtility.IconContent("Folder Icon").image);
            
            // 設置按鈕寬度與文件夾層次結構寬度相匹配，並考慮縮進
            float folderWidth = EditorGUIUtility.currentViewWidth * 0.25f; // 與 DrawFolderHierarchy 中的寬度計算一致
            float buttonWidth = folderWidth - indentLevel * 5 - marginLeft - 10; // 減去縮進、左邊距和額外的空間
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

                // 創建資產列表的副本以避免修改時的錯誤
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
                    int itemWidth = 110; // 每個項目的寬度
                    int itemMargin = 20; // 每個項目之間的間距
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
            
            // 嘗試獲取資產的預覽圖像
            Texture2D previewTexture = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Object>(asset));
            if (previewTexture == null)
            {
                // 如果沒有預覽圖像，則使用小縮略圖
                previewTexture = AssetPreview.GetMiniThumbnail(AssetDatabase.LoadAssetAtPath<Object>(asset));
            }
            
            // 顯示資產圖標或預覽
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
            
            // 嘗試獲取資產的預覽圖像
            Texture2D previewTexture = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Object>(asset));
            if (previewTexture == null)
            {
                // 如果沒有預覽圖像，則使用小縮略圖
                previewTexture = AssetPreview.GetMiniThumbnail(AssetDatabase.LoadAssetAtPath<Object>(asset));
            }
            
            // 顯示資產圖標或預覽
            if (GUILayout.Button(previewTexture, GUILayout.Width(100), GUILayout.Height(100)))
            {
                PingAsset(asset);
            }
            
            // 顯示資產名稱
            GUILayout.Label(System.IO.Path.GetFileNameWithoutExtension(asset), EditorStyles.label, GUILayout.Width(100));
            
            // 顯示刪除按鈕
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
                    !IsInWhitelist(asset)) // 使用新的白名單檢查方法
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

                    // 如果文件夾中的資產列表為空，則移除該文件夾
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