# Unused Assets Detector
![GitHub Release](https://img.shields.io/github/v/release/barryyip0625/UnusedAssetsDetector) ![GitHub License](https://img.shields.io/github/license/barryyip0625/UnusedAssetsDetector) [![Readme_ZH](https://img.shields.io/badge/UnusedAssetsDetector-%E4%B8%AD%E6%96%87%E6%96%87%E6%AA%94-red)](https://github.com/barryyip0625/UnusedAssetsDetector/blob/main/README_ZH.md)

## Introduction
Unused Assets Detector is a Unity editor tool designed to detect unused assets in your project. This tool helps developers clean up their projects by reducing unnecessary resource usage.

![image](https://github.com/user-attachments/assets/122f3fe8-8a13-4335-8860-1dda768f3afc)

## Features
- Scans all assets in the project and detects unused ones.
- Supports adding assets or folders to a whitelist to exclude them from detection.
- Provides list and grid view modes for easy viewing of unused assets.
- Allows direct deletion of unused assets within the project.

## Usage
1. In the Unity editor, navigate to `Tools > BY Utils > Unused Assets Detector` to open the tool window. ![image](https://github.com/user-attachments/assets/6d14d3b7-474d-4c59-8985-d75ed184db08)

2. Click the `Find Unused Assets` button to start scanning for unused assets in the project.
3. Use the folder hierarchy on the left to navigate and view unused assets in each folder.
4. Click `Add Asset to Whitelist` or `Add Folder to Whitelist` to add assets or folders to the whitelist.
5. Use `Show as List` or `Show as Grid` to switch view modes. 
  * List<br/>
  ![image](https://github.com/user-attachments/assets/1ece6a0d-b135-4175-8a8d-c51dc4b06036)

  * Grid<br/>
  ![image](https://github.com/user-attachments/assets/54bb6d8d-93d5-40c6-b27b-36799c196dcc)

6. Click the `Delete` button next to an asset to remove unwanted assets.

## Whitelist
The whitelist feature allows users to exclude certain assets or folders from detection. Whitelist information is stored in the `whitelist.txt` file located in the same directory as the tool script.

## Notes
- Use the delete function with caution, as deleted assets cannot be recovered.
- The demo assets are using Unity's built-in render pipeline

## Installation
- Download the [latest release package](https://github.com/barryyip0625/UnusedAssetsDetector/releases)
- Install with Unity Package Manager via git URL ```https://github.com/barryyip0625/UnusedAssetsDetector.git```

## Contribution
Contributions to improve this tool are welcome. Please participate by submitting pull requests or reporting issues.
