using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using Il2CppMonomiPark.SlimeRancher.Springs;
using MelonLoader;
using UnityEngine;

namespace PlotHider
{
    [RegisterTypeInIl2Cpp]
    [Il2CppImplements(typeof(ITechActivator))]
    public class LandPlotUnhider : SRBehaviour
    {
        public LandPlotUnhider(IntPtr ptr) : base(ptr) { }

        public LandPlot location;
        public GadgetItem gadgetItem;

        private readonly static int fadeInThresholdID = 5522;
        private float effectTimer = -0.1f;
        private Material gearMat;

        private float multiplier;
        private bool destroying;

        public void Start() => gadgetItem = SceneContext.Instance.Player.GetComponentInChildren<PlayerItemController>().GadgetItem;

        public void OnEnable()
        {
            if (destroying)
                return;

            effectTimer = -0.1f;
            multiplier = 3;

            gearMat = GetComponentInChildren<MeshRenderer>().GetMaterial();
            gearMat.SetFloatImpl(fadeInThresholdID, effectTimer);
        }

        public void OnDisable()
        {
            if (destroying)
                Destroy(transform.parent.gameObject);
        }

        public void FadeThenDestroy()
        {
            multiplier = -3;
            destroying = true;
            effectTimer = 1;
        }

        public void Update()
        {
            effectTimer += Time.deltaTime * multiplier;
            gearMat.SetFloatImpl(fadeInThresholdID, effectTimer);

            if (destroying && effectTimer <= 0)
                Destroy(transform.parent.gameObject);
        }

        public void Activate()
        {
            if (destroying)
                return;

            GameObject child = location.gameObject;
            child.SetActive(true);

            if (child.GetComponent<Spring>())
                DestroyImmediate(child.GetComponent<Spring>());

            GadgetItemMetadata metadata = SceneContext.Instance.Player.GetComponentInChildren<PlayerItemController>().GadgetItem._gadgetItemMetadata;

            Spring s = child.AddComponent<Spring>();
            s.InitializeConfiguration(metadata.GadgetPlacedSpringConfiguration,
                new ReferenceBinding[] { new ReferenceBinding(metadata.GadgetPlacedSpringConfiguration.References[0], child.transform) });
            s.Nudge(5);

            FXHelpers.SpawnAndPlayFX(EntryPoint.showHidePoofFX, child.transform.position, Quaternion.identity);
            gadgetItem.PlayTransientAudio(gadgetItem._gadgetItemMetadata.PlaceGadgetCue);

            EntryPoint.disabledLocations.Remove(location.GetComponentInParent<LandPlotLocation>().Id);
            FadeThenDestroy();
        }

        public GameObject GetCustomGuiPrefab() => null;
    }
}
