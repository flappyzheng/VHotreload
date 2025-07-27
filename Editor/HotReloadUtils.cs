#if UNITY_EDITOR

using System.IO;
using UnityEngine;

namespace VHotReload
{
    public static class HotReloadUtils
    {
        // 热重载输出目录，可自定义，这里示例放在 Assets 下的临时目录
        private const string HotReloadDirName = "Assets/VHotReload/Editor";

        public static string GetHotReloadConfigDir()
        {
            return $"{HotReloadDirName}/HotReloadConfig";
        }

        public static string GetHotReloadConfigPath()
        {
            return $"{GetHotReloadConfigDir()}/HotReloadConfig.asset";
        }

        public static string GetHotfixOutputDir()
        {
            return $"{Application.temporaryCachePath}/HotReloadOutput";
        }
    }
}
#endif
