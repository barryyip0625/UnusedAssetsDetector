# Unused Assets Detector
![GitHub Release](https://img.shields.io/github/v/release/barryyip0625/UnusedAssetsDetector) ![GitHub License](https://img.shields.io/github/license/barryyip0625/UnusedAssetsDetector)

## 介紹
Unused Assets Detector是一個 Unity 編輯器工具，旨在檢測專案中未使用的資產。此工具幫助開發者通過減少不必要的資源使用來清理專案。

![image](https://github.com/user-attachments/assets/6fa1b7d0-d68d-4518-9dbc-d332e4f5f847)

## 功能
- 掃描專案中的所有資產並檢測未使用的資產。
- 支援將資產或資料夾添加到白名單以排除檢測。
- 提供列表和網格視圖模式，便於查看未使用的資產。
- 允許在專案中直接刪除未使用的資產。
- 計算未使用資產的數量和磁碟大小

## 使用方法
1. 在 Unity 編輯器中，導航到 `工具 > BY Utils > Unused Assets Detector` 以打開工具窗口。![image](https://github.com/user-attachments/assets/6d14d3b7-474d-4c59-8985-d75ed184db08)  

2. 點擊 `Find Unused Assets` 按鈕開始掃描專案中的未使用資產。
3. 使用左側的資料夾層次結構導航並查看每個資料夾中的未使用資產。
4. 點擊 `Add Asset to Whitelist` 或 `Add Folder to Whitelist` 以將資產或資料夾添加到白名單。
5. 使用 `Show as List` 或 `Show as Grid` 切換顯示模式。
  * List<br/>
  ![image](https://github.com/user-attachments/assets/8f6d0f60-8b4b-42ed-bb65-2b085690580d)

  * Grid<br/>
  ![image](https://github.com/user-attachments/assets/3b07bba5-e1f0-40cd-9a06-aaf72b7f7de9)

6. 點擊資產旁邊的 `Delete` 按鈕以移除不需要的資產。

## 白名單
白名單功能允許用戶排除某些資產或資料夾的檢測。白名單資訊存儲在`Assets/UnusedAssetsDetector/`的 `Whitelist.asset` 文件中。

## 注意事項
- 使用刪除功能時請謹慎，因為刪除的資產無法恢復。
- 示範用的資產使用 Unity 內建的渲染管道

## 安裝
- 下載[最新版本](https://github.com/barryyip0625/UnusedAssetsDetector/releases)
- 使用 Unity Package Manager 透過 git URL 進行安裝```https://github.com/barryyip0625/UnusedAssetsDetector.git```
  
## 貢獻
歡迎對此工具的改進進行貢獻。請通過提交pull requests或報告問題來參與。
