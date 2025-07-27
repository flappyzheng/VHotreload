
# if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System;
using UnityEditorInternal;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace VHotReload
{
    [Serializable]
    public class AssemblyDefinition
    {
        public string name;
        public string rootNamespace;
        public List<string> references;
        public List<string> includePlatforms;
        public List<string> excludePlatforms;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public List<string> precompiledReferences;
        public bool autoReferenced;
        public List<string> defineConstraints;
        public List<string> versionDefines;
        public bool noEngineReferences;
    }

    public static class HotReloadBuildTools
    {
        static int errorCount;

        static bool isBuilding = false;

        static float timeOut = 10f;

        static DateTime time;

        public class HotReloadBuildTaskData
        {
            public string name;
            public string[] codeDirectorys;
            public string[] excludeReferences;
        }

        public static async void BuildHotReload()
        {
            if (isBuilding)
            {
                Debug.Log("正在重新构建 请稍等");
                return;
            }

            time = DateTime.Now;

            isBuilding = true;

            var path = HotReloadUtils.GetHotReloadConfigDir();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // 加载配置文件
            var config = AssetDatabase.LoadAssetAtPath<HotReloadConfig>(HotReloadUtils.GetHotReloadConfigPath());
            if (config == null)
            {
                Debug.LogError("未找到 HotReloadConfig.asset 配置文件");
                return;
            }

            if (config.additionalReferences == null || config.additionalReferences.Count <= 0)
            {
                Debug.LogError("没有配置热重载的程序集");
                return;
            }

            var codeDirectorys = config.additionalReferences.Select(addRef => Path.GetDirectoryName(AssetDatabase.GetAssetPath(addRef))).ToArray();
            var assemblyDefinitionList = config.additionalReferences.Select(addRef => AssetToAssemblyDefinition(addRef)).ToArray();

            var excludeReferences = assemblyDefinitionList.Select(ass => AssemblyDefinitionToLibraryDll(ass.name)).ToArray();

            DeleteFolder(HotReloadUtils.GetHotfixOutputDir(), "HotReload_*.*", true);

            // 获取当前时间
            var currentTime = DateTime.Now;

            string fullTime = currentTime.ToString("yyyy_MM_dd_HH_mm_ss");

            string logicFile = $"HotReload_{fullTime}";

            BuildMuteAssembly(
                logicFile
                , codeDirectorys
                , null
                , null
                , HotReloadUtils.GetHotfixOutputDir()
                , null
                , null
                , excludeReferences
            );

            bool isTimeOut = false;
            while (EditorApplication.isCompiling)
            {
                if ((DateTime.Now - time).TotalSeconds > timeOut)
                {
                    Debug.LogError("编译超时");
                    isTimeOut = true;
                    break;
                }
                await Task.Delay(10);
            }

            if (errorCount == 0 && !isTimeOut)
            {
                RenameAddBytes(HotReloadUtils.GetHotfixOutputDir());
                HotReloadLoader.LoadHotReloadAssambly();
            }

            isBuilding = false;
        }

        static AssemblyDefinition AssetToAssemblyDefinition(AssemblyDefinitionAsset assemblyDefinitionAsset)
        {
            return JsonUtility.FromJson<AssemblyDefinition>(assemblyDefinitionAsset.text);
        }

        static string AssemblyDefinitionToLibraryDll(string assemblyName)
        {
            return $"Library/ScriptAssemblies/{assemblyName}.dll";
        }

        private static void RenameAddBytes(string tagetPathPath)
        {
            var files = Directory.GetFiles(tagetPathPath);
            for (int i = 0; i < files.Length; i++)
            {
                var newName = $"{files[i]}.bytes";
                if (File.Exists(newName))
                {
                    File.Delete(newName);
                }
                File.Move(files[i], newName);

                Debug.Log(newName);
            }
        }

        // 辅助方法：删除指定目录下符合通配符规则的文件/文件夹
        private static void DeleteFolder(string targetDir, string searchPattern, bool isRecursive)
        {
            if (!Directory.Exists(targetDir)) return;

            var files = Directory.GetFiles(targetDir, searchPattern,
                           isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            for (int i = files.Length - 1; i >= 0; i--)
            {
                File.Delete(files[i]);
            }
        }

        private static void BuildMuteAssembly(string assemblyName, string[] CodeDirectorys, string[] additionalReferences, string[] defines, string outDir, Action OnBuildStart, Action OnBuildEnd, string[] excludeReferences)
        {
            List<string> scripts = new List<string>(); // 待编译文件列表
            for (int i = 0; i < CodeDirectorys.Length; i++) // 读取每个文件夹
            {
                DirectoryInfo dti = new DirectoryInfo(CodeDirectorys[i]);
                FileInfo[] fileInfos = dti.GetFiles("*.cs", System.IO.SearchOption.AllDirectories);
                for (int j = 0; j < fileInfos.Length; j++) // 读取该文件夹的每个文件
                {
                    //Debug.Log($"文件加入生成: {fileInfos[j].FullName}");
                    scripts.Add(fileInfos[j].FullName);
                }
            }
            Debug.Log(string.Join("\r\n", scripts));

            Directory.CreateDirectory(outDir);

            string dllPath = Path.Combine(outDir, $"{assemblyName}.dll");
            string pdbPath = Path.Combine(outDir, $"{assemblyName}.pdb");
            File.Delete(dllPath);
            File.Delete(pdbPath);

            AssemblyBuilder assemblyBuilder = new AssemblyBuilder(dllPath, scripts.ToArray());

            //启用UnSafe
            //assemblyBuilder.compilerOptions.AllowUnsafeCode = true;

            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

            assemblyBuilder.compilerOptions.CodeOptimization = CodeOptimization.Debug;
            assemblyBuilder.compilerOptions.ApiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup);

            assemblyBuilder.additionalReferences = additionalReferences;
            assemblyBuilder.additionalDefines = defines;

            assemblyBuilder.excludeReferences = excludeReferences;

            assemblyBuilder.flags = AssemblyBuilderFlags.None;
            //AssemblyBuilderFlags.None                 正常发布
            //AssemblyBuilderFlags.DevelopmentBuild     开发模式打包
            //AssemblyBuilderFlags.EditorAssembly       编辑器状态
            assemblyBuilder.referencesOptions = ReferencesOptions.UseEngineModules;

            assemblyBuilder.buildTarget = EditorUserBuildSettings.activeBuildTarget;

            assemblyBuilder.buildTargetGroup = buildTargetGroup;

            assemblyBuilder.buildStarted += delegate (string assemblyPath)
            {
                Debug.LogFormat("构建开始：" + assemblyPath);
                OnBuildStart?.Invoke();
            };

            assemblyBuilder.buildFinished += delegate (string assemblyPath, CompilerMessage[] compilerMessages)
            {
                errorCount = compilerMessages.Count(m => m.type == CompilerMessageType.Error);
                int warningCount = compilerMessages.Count(m => m.type == CompilerMessageType.Warning);

                if (warningCount > 0)
                {
                    Debug.LogFormat($"有{warningCount}个Warning");
                    for (int i = 0; i < compilerMessages.Length; i++)
                    {
                        if (compilerMessages[i].type == CompilerMessageType.Warning)
                        {
                            Debug.LogWarning(compilerMessages[i].message);
                        }
                    }
                }

                if (errorCount > 0)
                {
                    for (int i = 0; i < compilerMessages.Length; i++)
                    {
                        if (compilerMessages[i].type == CompilerMessageType.Error)
                        {
                            Debug.LogError(compilerMessages[i].message);
                        }
                    }
                    Debug.LogError($"有{errorCount}个error!!!");
                }
                else
                {
                    OnBuildEnd?.Invoke();
                }
            };

            //开始构建
            if (!assemblyBuilder.Build())
            {
                Debug.LogError("构建失败：" + assemblyBuilder.assemblyPath);
                return;
            }
        }
    }




}

#endif