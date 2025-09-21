using BepInEx;
using BepInEx.Logging;
using GlobalEnums;
using HarmonyLib;
using HutongGames.PlayMaker;
using System;
using UnityEngine;

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
        [HarmonyPrefix]
        static void Prefix(HeroActions ia)
        {
            if(ButtPressed(ia))
            {
                if(lastInst != null)
                        FileLog.Log("lastInst pre is " + lastInst.paneControl.ToString());
                
                snapshot = true;
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
                    FileLog.Log("InvOpen Button Press = " + __result.ToString() );
                
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
                        // listenForInvShortInst.StoreShortcut.Value = InventoryShortcutButtons.Inventory;

                        // InventoryPane inventoryPane = ___paneList.GetPane(INV_INDEX);
                        if(lastOpen != __result)
                            FsmSwitchToInv();

                
                        return;
                    }
                    
                }
            } catch (Exception e) {
                FileLog.Log("Crashed with exception " + e.Message);
            }
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
    
    [HarmonyPatch(typeof(InventoryPaneInput), "PressCancel")]
    public class preCloseSwitch
    {

        [HarmonyPrefix]
        static bool Prefix(InventoryPaneInput __instance
            // ,
            )
        {
            FileLog.Log("Not running???");
            FsmSwitchToInv();
            FileLog.Log("Closing inventory");
            return false;
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
            if(inventoryObject == null) inventoryObject = ___paneList.gameObject;
            lastInst = __instance;
            lastOpen = ___paneControl;

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


        }

        [HarmonyPrefix]
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
                FileLog.Log("Custom Update - Release Submit");
		        __instance.ReleaseSubmit();
	        }
	        if (___wasExtraPressed && menuAction != Platform.MenuActions.Extra)
	        {
                FileLog.Log("Custom Update - UI EXTRA RELEASED");
		        FSMUtility.SendEventToGameObject(__instance.gameObject, "UI EXTRA RELEASED");
		        ___wasExtraPressed = false;
		        ___isRepeatingDirection = false;
	        }
	        else if (inputActions.Right.WasPressed)
	        {
                FileLog.Log("Custom Update - Right Pressed");
		        __instance.PressDirection(InventoryPaneBase.InputEventType.Right);
	        }
	        else if (inputActions.Left.WasPressed)
	        {
                FileLog.Log("Custom Update - Left Pressed");
		        __instance.PressDirection(InventoryPaneBase.InputEventType.Left);
	        }
	        else if (inputActions.Up.WasPressed)
	        {
                FileLog.Log("Custom Update - Up Pressed");
		        __instance.PressDirection(InventoryPaneBase.InputEventType.Up);
	        }
	        else if (inputActions.Down.WasPressed)
	        {
                FileLog.Log("Custom Update - Down Pressed");
		        __instance.PressDirection(InventoryPaneBase.InputEventType.Down);
	        }
	        else if (inventoryInputPressed != InventoryPaneList.PaneTypes.None)
	        {
                FileLog.Log("Custom Update - PaneButton Pressed");
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
                    FileLog.Log("PRESSING CANCEL!!?!?");
			        __instance.PressCancel();
			        return false;
		        }
		        PlayMakerFSM playMakerFSM = PlayMakerFSM.FindFsmOnGameObject(paneList.gameObject, "Inventory Control");
		        playMakerFSM.FsmVariables.FindFsmInt("Target Pane Index").Value = (int)inventoryInputPressed;
		        playMakerFSM.SendEvent("MOVE PANE TO");
	        }
	        else if (inputActions.RsDown.WasPressed)
	        {
                FileLog.Log("Custom Update - UI RS DOWN Pressed");

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
                FileLog.Log("Custom Update - UI RS UP Pressed");

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
                FileLog.Log("Custom Update - UI RS Left Pressed");

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
                FileLog.Log("Custom Update - UI RS RIGHT Pressed");

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
                FileLog.Log("Custom Update - Repeating Direction Pressed");

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
                FileLog.Log("Custom Update - Repeating submit Pressed");

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
            // Isn't called???
        }
    }*/
    

    [HarmonyPatch(typeof(HutongGames.PlayMaker.Actions.ListenForInventoryShortcut), "OnUpdate")]
    public class HutongGrra
    {
        [HarmonyPrefix]
        static bool Prefix(
            HutongGames.PlayMaker.Actions.ListenForInventoryShortcut __instance,
            ref FsmEvent ___WasPressed
            , ref GameManager ___gm
            , ref InputHandler ___inputHandler
            , ref FsmInt ___CurrentPaneIndex
            , ref FsmEnum ___StoreShortcut
            )
        {

            return true;
        }

        /*
        [HarmonyPrefix]
        static bool Prefix(
            HutongGames.PlayMaker.Actions.ListenForInventoryShortcut __instance,
            ref FsmEvent ___WasPressed
            , ref GameManager ___gm
            , ref InputHandler ___inputHandler
            , ref FsmInt ___CurrentPaneIndex
            , ref FsmEnum ___StoreShortcut
            )
        {
            // FileLog.Log("in Hutong 2");
            // IS CALLED
            
	        if (___gm.isPaused)
	        {
		        return false;
	        }
	        HeroActions inputActions = ___inputHandler.inputActions;
	        InventoryPaneList.PaneTypes paneTypes = InventoryPaneInput.GetInventoryInputPressed(inputActions);
	        
            switch (paneTypes)
	        {
	        case InventoryPaneList.PaneTypes.None:
		        return false;
	        default:
		        if (!___CurrentPaneIndex.IsNone && paneTypes != (InventoryPaneList.PaneTypes)___CurrentPaneIndex.Value)
		        {
			        return false;
		        }
		        break;
	        case InventoryPaneList.PaneTypes.Inv:
		        break;
	        }
            
	        if (inputActions.Pause.WasPressed && PlayerData.instance.isInventoryOpen)
	        {
		        paneTypes = InventoryPaneList.PaneTypes.Inv;
	        }


            
            // knocking out entire logic just opens last window
            // even inv

	        FsmEnum storeShortcut = ___StoreShortcut;

            // FileLog.Log("in Hutong 2 " + storeShortcut.ToString());

	        storeShortcut.Value = // InventoryShortcutButtons.Inventory;
                paneTypes switch
	        {
		        InventoryPaneList.PaneTypes.Inv => InventoryShortcutButtons.Inventory, 
		        InventoryPaneList.PaneTypes.Tools => InventoryShortcutButtons.Tools, 
		        InventoryPaneList.PaneTypes.Quests => InventoryShortcutButtons.Quests, 
		        InventoryPaneList.PaneTypes.Journal => InventoryShortcutButtons.Journal, 
		        InventoryPaneList.PaneTypes.Map => InventoryShortcutButtons.Map, 
		        _ => throw new ArgumentOutOfRangeException(), 
	        };

            FileLog.Log("in Hutong 2 post " + storeShortcut.ToString());
                // "correctly remembers, but seems to have no effect??
            FileLog.Log("in Hutong 2 current pane " + ___CurrentPaneIndex.ToString());
                // also correct, but no effect??
            

            // knock out all logic = always open to the last one
            //      no matter which button was pressed

            // ___StoreShortcut.Value = InventoryShortcutButtons.Quests;
            // ALWAYS opens to Quests :thinking:
            
            ___StoreShortcut.Value = InventoryShortcutButtons.Inventory;
            // COMPLETELY IGNORED
            // so this logic is probably elsewhere??


            // FileLog.Log("event is " + ___WasPressed.ToString());
            // FileLog.Log("event name is " + ___WasPressed.name.ToString());
            
            // FileLog.Log("states are " + __instance.Fsm.States.ToString());

            FileLog.Log("current state = " + __instance.Fsm.activeState.name.ToString());
            // if CurrentState = "Closed" -- what we're targeting

            
            //foreach(var state in __instance.Fsm.States)
            //{
            //    FileLog.Log("state name: " + state.name);
            //}

            // FileLog.Log("vars are " + __instance.Fsm.Variables.ToString());
            
            if(paneTypes == InventoryPaneList.PaneTypes.Inv)
            {
                savedVars = new FsmVariables(__instance.Fsm.Variables);
            }
            FileLog.Log("Real: ");
            foreach(var var in __instance.Fsm.Variables._allVariables)
            {
                
                //if(var.name == "Current Pane Num" || var.name == "Next Pane Num")
                //{
                //    var.SetVariable<FsmInt>(var.name, ___CurrentPaneIndex);
                //}
                FileLog.Log("var: " + var.name + ", val = " + (var.RawValue == null ? "null" : var.RawValue.ToString()));

            }
            FileLog.Log("Saved: ");
            foreach(var var in savedVars._allVariables)
            {
                FileLog.Log("var: " + var.name + ", val = " + (var.RawValue == null ? "null" : var.RawValue.ToString()));
            }
            
            // FileLog.Log("evt is " + ___WasPressed.ToString());
            // FileLog.Log("evt name = " + ___WasPressed.name.ToString());
            // FileLog.Log("evt path = " + ___WasPressed.Path.ToString());

            __instance.Fsm.Event(___WasPressed);
            // __instance.Fsm.Event(new FsmEvent("BUTTON PRESSED"));
                // nope, has to be "correctly formed"

            

            return false;
        }
        */
        
        
        [HarmonyPostfix]
        static void Postfix(
            HutongGames.PlayMaker.Actions.ListenForInventoryShortcut __instance,
            ref FsmEvent ___WasPressed
            , ref GameManager ___gm
            , ref InputHandler ___inputHandler
            , ref FsmInt ___CurrentPaneIndex
            , ref FsmEnum ___StoreShortcut
            )
        {
            if(listenForInvShortInst == null) listenForInvShortInst = __instance;
            // FileLog.Log("setting shortcut as inv");


            // FileLog.Log("current pane = " + ___CurrentPaneIndex.ToString());
            // FileLog.Log("storeshortcut = " + ___StoreShortcut.Value.ToString());
            // FileLog.Log("invshortcut = " + InventoryShortcutButtons.Inventory.ToString());

            var tempStore = ___StoreShortcut.Value.ToString();
            
            if(NewPaneSelected(___StoreShortcut.Value))
            {
                FileLog.Log("----------");
                FileLog.Log("current state = " + ___StoreShortcut.Value.ToString());
                FileLog.Log("save state = " + saveState.ToString());
                
                ___StoreShortcut.Value = InventoryShortcutButtons.Inventory;
                // ^-- Breaks all "shortcut" buttons working outside inventory.

                FsmSwitchToInv();
            }
            
            // FileLog.Log("post_msg_state = " + ___StoreShortcut.Value.ToString());
            // FileLog.Log("temp_store = " + tempStore.ToString());
            saveState = tempStore;
            // FileLog.Log("state saved = " + saveState);
        }

        private static bool NewPaneSelected(Enum newState)
        {
            if(newState == null || saveState == null) return false;
            return newState.ToString() == InventoryShortcutButtons.Inventory.ToString()
                && saveState.ToString() != InventoryShortcutButtons.Inventory.ToString();
        }
    }
    
    /*
    [HarmonyPatch(typeof(HutongGames.PlayMaker.Actions.ListenForInventoryShortcut), "OnUpdate")]
    public class HutongGrrb
    {
        [HarmonyPostfix]
        static void Postfix(
            HutongGames.PlayMaker.Actions.ListenForInventory __instance)
        {
            FileLog.Log("in Hutong");
        }
    } */
    
    

    [HarmonyPatch(typeof(HutongGames.PlayMaker.Actions.SetCurrentInventoryPane), "OnEnter")]
    public class SetInvPane
    {
        [HarmonyPrefix]
        static bool Prefix(
            HutongGames.PlayMaker.Actions.SetCurrentInventoryPane __instance
            , ref FsmInt ___PaneIndex
            )
        {
            FileLog.Log("Setting current inventory pane");
            FileLog.Log("Pane Index = " + ___PaneIndex.ToString());
            if(__instance.PaneIndex.ToInt() == -1)
            {
                FileLog.Log("EqualTO -1");
                __instance.PaneIndex = INV_INDEX;
            }
            FileLog.Log("Pane Index = " + ___PaneIndex.ToString());
            FileLog.Log("/////");

            return true;
        }
    }
}
