using UnityEngine;

public static class WebGLPerformanceBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyWebGLRuntimeTuning()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            return;
        }

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        QualitySettings.pixelLightCount = 1;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowDistance = 0f;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.lodBias = 0.6f;
        QualitySettings.globalTextureMipmapLimit = 1;
        QualitySettings.globalTextureMipmapLimit = 1;

        // Keep startup smoother on slower browsers/GPUs.
        QualitySettings.asyncUploadTimeSlice = 8;
        QualitySettings.asyncUploadBufferSize = 8;
    }
}
