#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 放進 Assets/Editor。只限制目前 Unity 編輯器程序的 Bee 建置並行數，
/// 不寫入 Windows 的使用者／系統環境變數，也不會放進手機 Player。
/// </summary>
[InitializeOnLoad]
public sealed class LowMemoryAndroidBuild : IPreprocessBuildWithReport
{
    private const string VariableName = "BEE_BUILD_THREADS";
    private const string WorkerCount = "1";

    static LowMemoryAndroidBuild()
    {
        ApplyLimit();
    }

    public int callbackOrder => -10000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        ApplyLimit();
        Debug.Log(
            "[LowMemoryAndroidBuild] BEE_BUILD_THREADS=1. " +
            "Android native compilation is limited to reduce peak memory."
        );
    }

    [MenuItem("Tools/Android Build/Apply Low Memory Limit")]
    public static void ApplyFromMenu()
    {
        ApplyLimit();
        Debug.Log("[LowMemoryAndroidBuild] BEE_BUILD_THREADS=" +
            Environment.GetEnvironmentVariable(VariableName));
    }

    private static void ApplyLimit()
    {
        Environment.SetEnvironmentVariable(
            VariableName,
            WorkerCount,
            EnvironmentVariableTarget.Process
        );
    }
}
#endif