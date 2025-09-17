using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
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
    public static InventoryPaneList.PaneTypes lastOpen = InventoryPaneList.PaneTypes.None;
        
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
        [HarmonyPrefix]
        static void Prefix(HeroActions ia)
        {
            if(ButtPressed(ia))
            {
                if(lastInst != null)
                        FileLog.Log("lastInst pre is " + lastInst.paneControl.ToString());
                
            }
        }

        [HarmonyPostfix]
        static void Postfix(// InventoryPaneInput __instance, 
            InventoryPaneList.PaneTypes __result
            // , ref InventoryPaneList ___paneList
            , HeroActions ia
            )
        {
            

            /*
            var ___paneList = paneListPtr;

            if(___paneList == null)
            {
                FileLog.Log("Failed to get PaneList");
                return;
            } else {
                FileLog.Log(___paneList.ToString());
                if(___paneList.panes == null)
                {
                    FileLog.Log("panes are null?? wrong instance of InventoryPaneInput???");
                    if(__instance == null)
                    {
                        FileLog.Log("Instance is null. Called statically :/");
                    } else {
                        FileLog.Log(__instance.ToString());
                    }
                    return;
                } else {
                    FileLog.Log(___paneList.panes.ToString());
                }
            } */

            try {

                if(ButtPressed(ia))
                {
                    FileLog.Log("Button Press = " + __result.ToString() );
                
                    // if(paneInputPtr != null)
                    //     FileLog.Log("pane control is " + paneInputPtr.paneControl.ToString());
                    
                    FileLog.Log("Last Open = " + lastOpen.ToString());

                    if(paneListPtr == null)
                    {
                        FileLog.Log("Failed to get PaneList");
                        return;
                    }
                    
			        InventoryPane tryGetPane = paneListPtr.GetPane(__result);
                    
			        if (tryGetPane == null || !tryGetPane.IsAvailable)
			        {
                        FileLog.Log("null or unavailable");
				        // __instance.PressCancel();
			            return;
			        }
                    FileLog.Log("available");


                    if(__result == InventoryPaneList.PaneTypes.Inv)
                    {
                    

                        // InventoryPane inventoryPane = ___paneList.GetPane(INV_INDEX);
                        if(lastOpen != __result)
                            FsmSwitchToInv(paneListPtr.gameObject);

                
                        return;
                    }
                    
                }
            } catch (Exception e) {
                FileLog.Log("Crashed with exception " + e.Message);
            }
        }

        static void FsmSwitchToInv(UnityEngine.GameObject go)
        {
		    PlayMakerFSM playMakerFSM = PlayMakerFSM.FindFsmOnGameObject(go, "Inventory Control");
		    playMakerFSM.FsmVariables.FindFsmInt("Target Pane Index").Value = INV_INDEX;
		    playMakerFSM.SendEvent("MOVE PANE TO");
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
            lastInst = __instance;

            /*
            FileLog.Log("paneControl true is " + ___paneControl.ToString());
            FileLog.Log("contravene access is " + __instance.paneControl.ToString());
                // both correct


            FileLog.Log("inst is " + __instance.ToString());
            FileLog.Log("inst saved is " + paneInputPtr.ToString());
                // appear same
            FileLog.Log("match is " + (__instance == paneInputPtr).ToString());
                // false somehow??

            FileLog.Log("paneList obj is " + ___paneList.gameObject.name.ToString());
                // Inventory
            FileLog.Log("inst obj is " + __instance.gameObject.name.ToString());
                // Inv/Quests/...
                // ... so, ok, it makes sense they're different, I guess?
                // and also, I guess, paneControl is fixed on an inst then?
                
            */

            lastOpen = ___paneControl;

        }

        /*
        [HarmonyPostfix]
        static bool Prefix(InventoryPaneInput __instance,
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
            // FileLog.Log("Custom Update");
            // NOTE: Only runs when menus are open!
            // will NOT fix re-opening to wrong menu!!

            var IsInputBlocked = InventoryPaneInput.IsInputBlocked;
            var ih = ___ih;
            var platform = ___platform;
            var wasSubmitPressed = ___wasSubmitPressed;
            var paneControl = ___paneControl;
            var paneList = ___paneList;
            var allowRightStickSpeed = ___allowRightStickSpeed;
            var isInInventory = ___isInInventory;
            var isRepeatingSubmit = ___isRepeatingSubmit;
            var lastPressedDirection = ___lastPressedDirection;

	        if (___actionCooldown > 0f)
	        {
		        ___actionCooldown -= Time.unscaledDeltaTime;
	        }
	        if (IsInputBlocked || CheatManager.IsOpen)
	        {
		        return false;
	        }
	        HeroActions inputActions = ih.inputActions;
	        switch (platform.GetMenuAction(inputActions))
	        {
	        case Platform.MenuActions.Submit:
		        __instance.PressSubmit();
		        return false;
	        case Platform.MenuActions.Cancel:
		        __instance.PressCancel();
		        return false;
	        case Platform.MenuActions.Extra:
		        if (___actionCooldown <= 0f)
		        {
			        FSMUtility.SendEventToGameObject(__instance.gameObject, "UI EXTRA");
			        ___wasExtraPressed = true;
			        ___isRepeatingDirection = false;
			        ___actionCooldown = 0.25f;
		        }
		        return false;
	        case Platform.MenuActions.Super:
		        if (___actionCooldown <= 0f)
		        {
			        FSMUtility.SendEventToGameObject(__instance.gameObject, "UI SUPER");
			        ___isRepeatingDirection = false;
			        ___actionCooldown = 0.25f;
		        }
		        return false;
	        }

            // FileLog.Log("Custom Update - Got this far (105) pre-switch");



	        Platform.MenuActions menuAction = platform.GetMenuAction(inputActions, ignoreAttack: false, isContinuous: true);
	        InventoryPaneList.PaneTypes inventoryInputPressed = InventoryPaneInput.GetInventoryInputPressed(inputActions);
	        if (wasSubmitPressed && menuAction != Platform.MenuActions.Submit)
	        {
		        __instance.ReleaseSubmit();
	        }
	        if (___wasExtraPressed && menuAction != Platform.MenuActions.Extra)
	        {
		        FSMUtility.SendEventToGameObject(__instance.gameObject, "UI EXTRA RELEASED");
		        ___wasExtraPressed = false;
		        ___isRepeatingDirection = false;
	        }
	        else if (inputActions.Right.WasPressed)
	        {
		        __instance.PressDirection(InventoryPaneBase.InputEventType.Right);
	        }
	        else if (inputActions.Left.WasPressed)
	        {
		        __instance.PressDirection(InventoryPaneBase.InputEventType.Left);
	        }
	        else if (inputActions.Up.WasPressed)
	        {
		        __instance.PressDirection(InventoryPaneBase.InputEventType.Up);
	        }
	        else if (inputActions.Down.WasPressed)
	        {
		        __instance.PressDirection(InventoryPaneBase.InputEventType.Down);
	        }
	        else if (inventoryInputPressed != InventoryPaneList.PaneTypes.None)
	        {
		        bool flag = paneControl switch
		        {
			        InventoryPaneList.PaneTypes.None => true, 
			        InventoryPaneList.PaneTypes.Inv => inputActions.OpenInventory.WasPressed, 
			        InventoryPaneList.PaneTypes.Map => inputActions.OpenInventoryMap.WasPressed || inputActions.SwipeInventoryMap.WasPressed, 
			        InventoryPaneList.PaneTypes.Journal => inputActions.OpenInventoryJournal.WasPressed || inputActions.SwipeInventoryJournal.WasPressed, 
			        InventoryPaneList.PaneTypes.Tools => inputActions.OpenInventoryTools.WasPressed || inputActions.SwipeInventoryTools.WasPressed, 
			        InventoryPaneList.PaneTypes.Quests => inputActions.OpenInventoryQuests.WasPressed || (bool)inputActions.SwipeInventoryQuests, 
			        _ => throw new ArgumentOutOfRangeException(), 
		        };


                FileLog.Log("Flag is: " + flag.ToString());
                FileLog.Log("Instance is: " + __instance.ToString());



		        if (!flag)
		        {
			        InventoryPane inventoryPane = paneList.GetPane(inventoryInputPressed);
			        if (inventoryPane == null || !inventoryPane.IsAvailable)
			        {
				        flag = true;
			        }
		        }
		        if (flag)
		        {
			        __instance.PressCancel();
			        return false;
		        }
		        PlayMakerFSM playMakerFSM = PlayMakerFSM.FindFsmOnGameObject(paneList.gameObject, "Inventory Control");
		        playMakerFSM.FsmVariables.FindFsmInt("Target Pane Index").Value = (int)inventoryInputPressed;
		        playMakerFSM.SendEvent("MOVE PANE TO");
	        }
	        else if (inputActions.RsDown.WasPressed)
	        {
		        FSMUtility.SendEventToGameObject(__instance.gameObject, "UI RS DOWN");
		        if (allowRightStickSpeed)
		        {
			        __instance.PressDirection(InventoryPaneBase.InputEventType.Down);
			        ___isScrollingFast = true;
			        ___directionRepeatTimer = __instance.ListScrollSpeed;
		        }
	        }
	        else if (inputActions.RsUp.WasPressed)
	        {
		        FSMUtility.SendEventToGameObject(__instance.gameObject, "UI RS UP");
		        if (allowRightStickSpeed)
		        {
			        __instance.PressDirection(InventoryPaneBase.InputEventType.Up);
			        ___isScrollingFast = true;
			        ___directionRepeatTimer = __instance.ListScrollSpeed;
		        }
	        }
	        else if (inputActions.RsLeft.WasPressed)
	        {
		        if (isInInventory)
		        {
			        FSMUtility.SendEventToGameObject(__instance.gameObject, "UI RS LEFT");
		        }
		        if (allowRightStickSpeed)
		        {
			        __instance.PressDirection(InventoryPaneBase.InputEventType.Left);
			        ___isScrollingFast = true;
			        ___directionRepeatTimer = __instance.ListScrollSpeed;
		        }
	        }
	        else if (inputActions.RsRight.WasPressed)
	        {
		        if (isInInventory)
		        {
			        FSMUtility.SendEventToGameObject(__instance.gameObject, "UI RS RIGHT");
		        }
		        if (allowRightStickSpeed)
		        {
			        __instance.PressDirection(InventoryPaneBase.InputEventType.Right);
			        ___isScrollingFast = true;
			        ___directionRepeatTimer = __instance.ListScrollSpeed;
		        }
	        }
	        else if (___isRepeatingDirection)
	        {
		        if (lastPressedDirection switch
		        {
			        InventoryPaneBase.InputEventType.Left => inputActions.Left.IsPressed, 
			        InventoryPaneBase.InputEventType.Right => inputActions.Right.IsPressed, 
			        InventoryPaneBase.InputEventType.Up => ___isScrollingFast ? inputActions.RsUp.IsPressed : inputActions.Up.IsPressed, 
			        InventoryPaneBase.InputEventType.Down => ___isScrollingFast ? inputActions.RsDown.IsPressed : inputActions.Down.IsPressed, 
			        _ => throw new ArgumentOutOfRangeException(), 
		        })
		        {
			        ___directionRepeatTimer -= Time.unscaledDeltaTime;
			        if (___directionRepeatTimer <= 0f)
			        {
				        __instance.PressDirection(lastPressedDirection);
				        ___directionRepeatTimer = __instance.ListScrollSpeed;
			        }
		        }
		        else
		        {
			        ___isRepeatingDirection = false;
		        }
	        }
	        else if (isRepeatingSubmit)
	        {
		        ___directionRepeatTimer -= Time.unscaledDeltaTime;
		        if (___directionRepeatTimer <= 0f)
		        {
			        __instance.ReleaseSubmit();
			        __instance.PressSubmit();
			        ___directionRepeatTimer = __instance.ListScrollSpeed;
		        }
	        }
	        else
	        {
		        ___isScrollingFast = false;
	        }



            return false;
        }
        */
    }
    

    /*
    [HarmonyPatch(typeof(HutongGames.PlayMaker.Actions.ListenForInventory), "OnUpdate")]
    public class HutongGrr
    {
        [HarmonyPostfix]
        static void Postfix(
            HutongGames.PlayMaker.Actions.ListenForInventory __instance)
        {
            FileLog.Log("in Hutong");
        }
    }
    */
}
