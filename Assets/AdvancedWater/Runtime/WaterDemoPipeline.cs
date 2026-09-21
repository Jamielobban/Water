using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedWater
{
    // The demo uses its own renderer without changing the project's saved URP assets.
    public sealed class WaterDemoPipeline : MonoBehaviour
    {
        public RenderPipelineAsset pipeline;
        RenderPipelineAsset previous;
        void OnEnable()
        {
            previous=QualitySettings.renderPipeline;
            if (pipeline) QualitySettings.renderPipeline=pipeline;
        }
        void OnDisable()
        {
            if (pipeline && QualitySettings.renderPipeline==pipeline) QualitySettings.renderPipeline=previous;
        }
    }
}
