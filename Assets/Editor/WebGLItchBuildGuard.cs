using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Forces safe WebGL publishing settings for hosts like itch.io on every build.
/// </summary>
public class WebGLItchBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
        {
            return;
        }

        // Gzip + fallback prevents "Failed to download .data.gz/.wasm.gz" when
        // the host does not send compression headers exactly as Unity expects.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
    }
}
