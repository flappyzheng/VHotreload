
# if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System;

namespace VHotReload
{
    public static class HotReloadManager
    {
        // 生成热重载项目_F8”，按 F8 可快速触发
        [MenuItem("VHotReload/热重载 _F8", false, 500)]
        public static void BuildHotReload()
        {
            // 热重载构建
            HotReloadBuildTools.BuildHotReload();
        }


        [MenuItem("VHotReload/创建热重载配置", false, 80)]
        public static void CreateHotReloadConfig()
        {
            var path = HotReloadUtils.GetHotReloadConfigDir();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            HotReloadConfig config = ScriptableObject.CreateInstance<HotReloadConfig>();
            AssetDatabase.CreateAsset(config, HotReloadUtils.GetHotReloadConfigPath());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;
        }

        //[MenuItem("VHotReload/加载热重载_F9", false, 500)]
        //public static void LoadHotReloadAssambly()
        //{
        //    HotReloadLoader.LoadHotReloadAssambly();
        //}
    }
}

#endif