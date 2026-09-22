using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed class OakTreeStageVisual : MonoBehaviour
    {
        private GameObject felled;
        private GameObject sapling;
        private GameObject young;
        private GameObject mature;

        public OakTreeStage Stage { get; private set; }

        public void Initialize(
            GameObject felledRoot,
            GameObject saplingRoot,
            GameObject youngRoot,
            GameObject matureRoot,
            OakTreeStage initialStage)
        {
            felled = felledRoot;
            sapling = saplingRoot;
            young = youngRoot;
            mature = matureRoot;
            SetStage(initialStage);
        }

        public void SetStage(OakTreeStage stage)
        {
            Stage = stage;
            if (felled != null)
            {
                felled.SetActive(stage == OakTreeStage.Felled);
            }
            if (sapling != null)
            {
                sapling.SetActive(stage == OakTreeStage.Sapling);
            }
            if (young != null)
            {
                young.SetActive(stage == OakTreeStage.Young);
            }
            if (mature != null)
            {
                mature.SetActive(stage == OakTreeStage.Mature);
            }
        }
    }
}
