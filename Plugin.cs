using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker;
using System;

namespace inventory_key_fix;

[BepInPlugin(modGUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string modGUID = "com.bigfootmech.silksong.inv_keyfix";

    internal static new ManualLogSource Logger;
    private readonly Harmony harmony = new Harmony(modGUID);

    public const int INV_INDEX = (int) InventoryPaneList.PaneTypes.Inv;

    public static InventoryPaneList paneListPtr;
    public static InventoryPaneInput lastInst;
    public static UnityEngine.GameObject inventoryObject;
    public static InventoryPaneList.PaneTypes lastOpen = InventoryPaneList.PaneTypes.None;
    public static bool snapshot = false;
    public static FsmVariables savedVars = new FsmVariables();

    public static HutongGames.PlayMaker.Actions.ListenForInventoryShortcut listenForInvShortInst;

    public static string saveState = "None";
    
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    
        harmony.PatchAll();
    }
    
    // [OnExit]
    // public void OnApplicationQuit() => harmony.UnpatchSelf();
    
    [HarmonyPatch(typeof(InventoryPaneInput), "GetInventoryInputPressed")]
    public class AtStart
    {
        [HarmonyPostfix]
        static void Postfix(// InventoryPaneInput __instance, 
            InventoryPaneList.PaneTypes __result
            // , ref InventoryPaneList ___paneList
            , HeroActions ia
            )
        {
            try {

                // polling time
                if(ButtPressed(ia))
                {
                    // button pressed time

                    if(!SanityCheck(__result))
                        return;

                    if(__result != InventoryPaneList.PaneTypes.Inv)
                        return;

                    if(lastOpen != __result)
                        FsmSwitchToInv();
                    
                }
            } catch (Exception e) {
                // silent
            }
        }

        static bool SanityCheck(InventoryPaneList.PaneTypes result)
        {
            if(paneListPtr == null)
            {
                return false;
            }
                    
			InventoryPane tryGetPane = paneListPtr.GetPane(result);
                    
			if (tryGetPane == null || !tryGetPane.IsAvailable)
			{
			    return false;
			}

            return true;
        }

        static bool ButtPressed(HeroActions ia)
        {
            return ia.OpenInventory.WasPressed ||
                ia.OpenInventoryMap.WasPressed ||
                ia.OpenInventoryJournal.WasPressed ||
                ia.OpenInventoryTools.WasPressed ||
                ia.OpenInventoryQuests.WasPressed;

        }
    }

    static void FsmSwitchToInv()
    {
        if(inventoryObject == null) return;
		PlayMakerFSM playMakerFSM = PlayMakerFSM.FindFsmOnGameObject(inventoryObject, "Inventory Control");
		playMakerFSM.FsmVariables.FindFsmInt("Target Pane Index").Value = INV_INDEX;
		playMakerFSM.SendEvent("MOVE PANE TO");
    }
    
    [HarmonyPatch(typeof(InventoryPaneInput), "Update")]
    public class OnUpdateDo
    {

        [HarmonyPostfix]
        static void Postfix(InventoryPaneInput __instance,
            ref float ___actionCooldown, ref InputHandler ___ih,
            ref Platform ___platform, ref bool ___wasExtraPressed,
            ref bool ___isRepeatingDirection, ref bool ___wasSubmitPressed,
            ref InventoryPaneList.PaneTypes ___paneControl,
            ref InventoryPaneList ___paneList, ref bool ___allowRightStickSpeed,
            ref bool ___isScrollingFast, ref float ___directionRepeatTimer,
            ref bool ___isInInventory, ref bool ___isRepeatingSubmit,
            ref InventoryPaneBase.InputEventType ___lastPressedDirection
            ) // , ref HeroActions ___ia
        {
            if(paneListPtr == null) paneListPtr = ___paneList;
            if(inventoryObject == null) inventoryObject = ___paneList.gameObject;
            lastInst = __instance;
            lastOpen = ___paneControl;
        }
    }
    

    [HarmonyPatch(typeof(HutongGames.PlayMaker.Actions.SetCurrentInventoryPane), "OnEnter")]
    public class SetInvPane
    {
        [HarmonyPrefix]
        static bool Prefix(
            HutongGames.PlayMaker.Actions.SetCurrentInventoryPane __instance
            , ref FsmInt ___PaneIndex
            )
        {
            if(__instance.PaneIndex.ToInt() == -1) // instead of re-open
            {
                __instance.PaneIndex = INV_INDEX; // open inv
            }

            return true;
        }
    }
}
