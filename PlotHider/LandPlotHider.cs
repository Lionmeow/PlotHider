using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using MelonLoader;
using UnityEngine;

namespace PlotHider
{
    [RegisterTypeInIl2Cpp]
    [Il2CppImplements(typeof(ITechActivator))]
    public class LandPlotHider : SRBehaviour
    {
        public LandPlotHider(IntPtr ptr) : base(ptr) { }

        public LandPlot location;
        public GadgetItem gadgetItem;

        public void Awake() => location = GetComponentInParent<LandPlot>();

        public void Start() => gadgetItem = SceneContext.Instance.Player.GetComponentInChildren<PlayerItemController>().GadgetItem;

        public void Activate()
        {
            FXHelpers.SpawnAndPlayFX(EntryPoint.showHidePoofFX, location.transform.position, Quaternion.identity);
            gadgetItem.PlayTransientAudio(gadgetItem._gadgetItemMetadata.PickupGadgetCue);
            Hide();
        }

        public void Hide()
        {
            GameObject child = location.gameObject;

            if (!child.active)
                return;

            if (child.active)
            {
                GameObject go = Instantiate(EntryPoint.gearThing, location.transform.parent);
                go.layer = 14;
                go.GetComponentInChildren<LandPlotUnhider>(true).location = location;
                go.transform.SetPositionAndRotation(location.transform.position + new Vector3(0, 2, 0), Quaternion.identity);
            }

            if (!EntryPoint.disabledLocations.Contains(location.GetComponentInParent<LandPlotLocation>().Id))
                EntryPoint.disabledLocations.Add(location.GetComponentInParent<LandPlotLocation>().Id);
            child.SetActive(!child.active);
        }

        public GameObject GetCustomGuiPrefab() => null;
    }
}
