using UnityEditor;

namespace BYUtils.AssetsManagement
{
    public class AssetChangeListener : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importedAssets.Length > 0 || deletedAssets.Length > 0 || movedAssets.Length > 0 || movedFromAssetPaths.Length > 0)
            {
                UnusedAssetsDetector.Refresh();
            }
        }
    }
}