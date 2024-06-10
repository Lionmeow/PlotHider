using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Input;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using Il2CppMonomiPark.SlimeRancher.UI;
using MelonLoader;
using PlotHider;
using UnityEngine;

[assembly: MelonInfo(typeof(EntryPoint), "Plot Hider", "1.0", "Lionmeow")]

namespace PlotHider
{
    [HarmonyPatch]
    public class EntryPoint : MelonMod
    {
        public static Transform prefabParent;
        public static GameObject gearThing;
        public static Mesh gearMesh;

        public static InputEvent interactionInput;
        public static GameObject activateUI;
        public static GameObject showHidePoofFX;

        public static List<string> disabledLocations = new List<string>();

        // For reasons unbeknownst to myself, the game REALLY likes to be selective with how it loads the gadget chargeup prefab.
        // It will unload the shader, the mesh, and the material after quitting a save.
        // Not only does the material have to be remade on every save load, but, for some reason having to do with how it's stored,
        // you physically cannot use the gear mesh more than once. Therefore, I needed an entire assetbundle for this one thing.
        internal static AssetBundle bundle = AssetBundle.LoadFromMemory(GetAsset("plothiderwhydoineedanassetbundleforthis"));

        public static byte[] GetAsset(string path)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{path}");
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            return data;
        }

        // To cover for every instance an empty plot may be created (default plots in the scene, plots being instantiated from a save, a plot being cleared),
        // this code must be repeated on every instantiation.
        // Because of how the game handles loading, if you try to add it to the prefab, this causes the game to get confused and instantiate multiple empty plots.
        public static void AddHider(GameObject emptyPlot)
        {
            LandPlotUIActivator uiActivator = emptyPlot.GetComponentInChildren<LandPlotUIActivator>();
            GameObject newActivator = UnityEngine.Object.Instantiate(uiActivator.gameObject);
            newActivator.transform.SetParent(uiActivator.transform.parent);
            newActivator.transform.localPosition = uiActivator.transform.localPosition;
            UnityEngine.Object.DestroyImmediate(newActivator.GetComponent<LandPlotUIActivator>());
            newActivator.AddComponent<LandPlotHider>().location = emptyPlot.GetComponentInChildren<LandPlot>();
            TechUIInteractable interactable = newActivator.gameObject.AddComponent<TechUIInteractable>();
            interactable.interactionInput = interactionInput;
            interactable.defaultInteractionPrompt = activateUI;

            DeactivateBasedOnGadgetMode deactivator = newActivator.transform.parent.gameObject.AddComponent<DeactivateBasedOnGadgetMode>();
            deactivator.ActivateOnModeOff = false;
            deactivator.ToDeactivate = newActivator;
            DeactivateBasedOnGadgetMode deactivator2 = newActivator.transform.parent.gameObject.AddComponent<DeactivateBasedOnGadgetMode>();
            deactivator2.ActivateOnModeOff = true;
            deactivator2.ToDeactivate = uiActivator.gameObject;
        }

        [HarmonyPatch(typeof(GameContext), "Start")]
        [HarmonyPostfix]
        public static void OnGameContext()
        {
            interactionInput = SRLookup.Get<InputEvent>("Interact");
            activateUI = SRLookup.Get<GameObject>("ActivateUI");

            // DUMB
            gearMesh = UnityEngine.Object.Instantiate(bundle.LoadAllAssets().First(x => x.name == "gearMesh" && x.GetIl2CppType() == Il2CppType.Of<GameObject>())
                .Cast<GameObject>().GetComponentInChildren<MeshFilter>().mesh);
            gearMesh.hideFlags = HideFlags.HideAndDontSave;

            GameObject emptyPlot = GameContext.Instance.LookupDirector.GetPlotPrefab(LandPlot.Id.EMPTY);
            AddHider(emptyPlot);
        }

        [HarmonyPatch(typeof(GadgetItem), "Awake")]
        [HarmonyPostfix]
        public static void OnGadgetItem(GadgetItem __instance)
        {
            if (showHidePoofFX)
                return;

            showHidePoofFX = UnityEngine.Object.Instantiate(__instance._gadgetItemMetadata.GadgetPlacedVFX, prefabParent);
            foreach (ParticleSystem particles in showHidePoofFX.GetComponentsInChildren<ParticleSystem>())
                particles.shape.radius = 5;
        }

        [HarmonyPatch(typeof(GadgetDirector), "Awake")]
        [HarmonyPrefix]
        public static void OnGadgetDirector(GadgetDirector __instance)
        {
            Material m = __instance.WaitForChargeupPrefab.transform.GetChild(0).GetChild(1).GetComponentInChildren<MeshRenderer>().material;
            if (gearThing)
            {
                // hate

                Material newMat2 = UnityEngine.Object.Instantiate(m);
                newMat2.name = "GearThing";
                newMat2.hideFlags = HideFlags.HideAndDontSave;
                newMat2.SetColor("_ColorFill", new Color32(20, 120, 255, 192));
                newMat2.SetColor("_ColorEdge", new Color32(0, 105, 255, 192));
                newMat2.SetColor("_ColorEdgeShadows", new Color32(0, 68, 255, 192));
                newMat2.SetFloat("_FillMeter", 1);

                gearThing.GetComponentInChildren<MeshRenderer>().material = newMat2;
            }

            if (gearThing != null)
                return;

            gearThing = new GameObject("GearThing");
            gearThing.transform.SetParent(prefabParent, false);
            gearThing.layer = 14;
            GameObject activator = UnityEngine.Object.Instantiate(__instance._waitForChargeupPrefab.transform.GetChild(0).GetChild(1).gameObject, gearThing.transform);
            activator.transform.localPosition = Vector3.zero;
            activator.layer = 14;

            Material newMat = UnityEngine.Object.Instantiate(m);
            newMat.name = "GearThing";
            newMat.hideFlags = HideFlags.HideAndDontSave;
            newMat.SetColor("_ColorFill", new Color32(20, 120, 255, 192));
            newMat.SetColor("_ColorEdge", new Color32(0, 105, 255, 192));
            newMat.SetColor("_ColorEdgeShadows", new Color32(0, 68, 255, 192));
            newMat.SetFloat("_FillMeter", 1);

            activator.GetComponent<MeshRenderer>().material = newMat;
            activator.GetComponent<MeshFilter>().mesh = gearMesh;
            activator.AddComponent<SphereCollider>().radius = 0.5f;
            activator.AddComponent<LandPlotUnhider>();

            TechUIInteractable interactable = activator.AddComponent<TechUIInteractable>();
            interactable.interactionInput = interactionInput;
            interactable.defaultInteractionPrompt = activateUI;

            DeactivateBasedOnGadgetMode deactivator = gearThing.AddComponent<DeactivateBasedOnGadgetMode>();
            deactivator.ActivateOnModeOff = false;
            deactivator.ToDeactivate = activator;
        }

        [HarmonyPatch(typeof(LandPlotLocation), "Replace")]
        [HarmonyPrefix]
        public static void EnsureNoGearExistsAnymorePatch(LandPlotLocation __instance, GameObject __result)
        {
            LandPlotUnhider activator = __instance.GetComponentInChildren<LandPlotUnhider>();
            if (activator != null)
                UnityEngine.Object.DestroyImmediate(activator.gameObject);

            if (__instance.transform.parent.GetComponentInChildren<LandPlotHider>(true) != null)
                return;

            LandPlot plot = __result.GetComponentInChildren<LandPlot>();
            if (plot.TypeId == LandPlot.Id.EMPTY)
                AddHider(plot.gameObject);
        }

        [HarmonyPatch(typeof(LandPlotModel), "InstantiatePlot")]
        [HarmonyPostfix]
        public static void LandPlotModelGameObjectPatch(LandPlotModel __instance)
        {
            if (__instance.typeId != LandPlot.Id.EMPTY)
                return;

            GameObject plotlocation = __instance.gameObj;

            if (__instance.gameObj.transform.parent.GetComponentInChildren<LandPlotHider>(true) != null)
                AddHider(__instance.gameObj.GetComponentInChildren<LandPlot>().gameObject);

            if (disabledLocations.Contains(plotlocation.GetComponent<LandPlotLocation>().Id))
                plotlocation.GetComponentInChildren<LandPlotHider>().Hide();
        }
    }
}
