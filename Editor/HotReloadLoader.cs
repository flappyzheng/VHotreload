
#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace VHotReload
{
    public class HotReloadLoader
    {
        static Action<Assembly> OnReloadAction;

        public static void LoadHotReloadAssambly()
        {
            Debug.Log($"加载热重载程序集");

            byte[] dll = null;
            byte[] pdb = null;

            var files = Directory.GetFiles(HotReloadUtils.GetHotfixOutputDir(), "HotReload_*.dll.bytes");
            if (files.Length != 1)
            {
                throw new Exception("没有找到热重载程序集");
            }

            Debug.Log($"正在加载热重载程序集：{files[0]}");
            var pathName = Path.GetFileNameWithoutExtension(files[0]);
            pathName = Path.GetFileNameWithoutExtension(pathName);
            dll = File.ReadAllBytes($"{HotReloadUtils.GetHotfixOutputDir()}/{pathName}.dll.bytes");
            pdb = File.ReadAllBytes($"{HotReloadUtils.GetHotfixOutputDir()}/{pathName}.pdb.bytes");

            var assReload = Assembly.Load(dll, pdb);

            Debug.Assert(assReload != null);

            Debug.Log("<color=green>加载完成</color>");

            OnReloadAction?.Invoke(assReload);
        }

        public static void AddListener(Action<Assembly> action)
        {
            OnReloadAction += action;
        }
    }
}
#endif
