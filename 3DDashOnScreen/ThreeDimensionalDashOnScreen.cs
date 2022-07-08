﻿using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Reflection;
using System;
using BaseX;


namespace ThreeDimensionalDashOnScreen
{
    public class ThreeDimensionalDashOnScreen : NeosMod
	{
		public override string Name => "3DDashOnScreen";
		public override string Author => "rampa3";
		public override string Version => "3.4.0";
		public override string Link => "https://github.com/rampa3/3DDashOnScreen";
		private static ModConfiguration Config;
		private static bool desktopNotificationsPresent = false;


		public override void OnEngineInit()
		{
			checkForDesktopNotifications();  //detect if DesktopNotifications mod is present
			Config = GetConfiguration();
			Config.Save(true);
			Harmony harmony = new Harmony("net.rampa3.3DDashOnScreen");
			//harmony.PatchAll();
			if (Config.GetValue(MOD_ENABLED))
            {
				patchDash(harmony);
				patchSlotPositioning(harmony);
				addUIEditKey(harmony);
				addDesktopControlPanelKeybind(harmony);
				patchCameraUI(harmony);
				disableForceItemKeepGrabbed(harmony);
				restoreLegacyDashBind(harmony);
				if (!desktopNotificationsPresent)
				{
					patchNotifications(harmony);
				}
				Debug("All patches applied successfully!");
            } else {
				Debug("3DDashOnScreen disabled!");
			}
			
		}
		/*
		   _______________________________
		   |     cursor parking lot      |
		   |                             |
		   |_____________________________|

				Users present at one point: art0007i, eia485, rampa3
		*/

		void checkForDesktopNotifications()
        {
			IEnumerable<NeosModBase> mods = ModLoader.Mods();
			foreach (NeosModBase mod in mods)
            {
				if (mod.Name == "DesktopNotifications")  //check for DesktopNotifications, and if present, set a boolean to tell the patch about its presence
                {
					desktopNotificationsPresent = true;
					break;
                }
            }
			Debug("DesktopNotificatons found: " + desktopNotificationsPresent);
		}

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> MOD_ENABLED = new ModConfigurationKey<bool>("ModEnabled", "Enabled (requires restart on change)", () => true);

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<Key> DESKTOP_CONTROL_PANEL_KEY = new ModConfigurationKey<Key>("DesktopControlPanelKey", "Desktop tab control panel key", () => Key.N);

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> RELEASE_CAM_UI = new ModConfigurationKey<bool>("ReleaseCamUI", "Release Camera Controls UI from its slider (requires restart on change)", () => false);

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<Key> UI_EDIT_MODE_KEY = new ModConfigurationKey<Key>("UIEditModeKey", "UI edit mode key", () => Key.F4);

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> USE_LEGACY_DASH_BIND = new ModConfigurationKey<bool>("UseLegacyDashBind", "Restore legacy dash toggle bind - Ctrl+Space (requires restart on change)", () => true);

		private static void restoreLegacyDashBind(Harmony harmony)
        {
			MethodInfo originalBind = AccessTools.DeclaredMethod(typeof(KeyboardAndMouseBindingGenerator), "Bind", new Type[] { typeof(InputGroup) });
			MethodInfo originalDash = AccessTools.DeclaredMethod(typeof(UserspaceRadiantDash), "OnCommonUpdate", new Type[] { });
			MethodInfo transpiler = AccessTools.DeclaredMethod(typeof(ThreeDimensionalDashOnScreen), nameof(RemoveDefaultDashBindTranspiler));
			MethodInfo postfixDash = AccessTools.DeclaredMethod(typeof(ThreeDimensionalDashOnScreen), nameof(SetCustomDashBindPostfix));
			harmony.Patch(originalBind, transpiler: new HarmonyMethod(transpiler));
			harmony.Patch(originalDash, postfix: new HarmonyMethod(postfixDash));
			Debug("Legacy dash toggle bind restored!");
		}

		private static IEnumerable<CodeInstruction> RemoveDefaultDashBindTranspiler(IEnumerable<CodeInstruction> instructions)
        {
			var codes = new List<CodeInstruction>(instructions);
			for (var i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldc_I4_S && codes[i].operand.Equals((sbyte)10) && codes[i + 5].opcode == OpCodes.Ldc_I4_S && codes[i + 5].operand.Equals((sbyte)27) && codes[i + 6].opcode == OpCodes.Call)
				{
                    codes.RemoveRange(i + 5, 2);
                    var legacybind = new List<CodeInstruction>();
                    legacybind.Add(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)32));
                    MethodInfo keyinput = AccessTools.DeclaredMethod(typeof(InputNode), "key", new Type[] { typeof(Key) });
                    legacybind.Add(new CodeInstruction(OpCodes.Call, keyinput));
                    legacybind.Add(new CodeInstruction(OpCodes.Ldc_I4, 513));
                    legacybind.Add(new CodeInstruction(OpCodes.Call, keyinput));
                    legacybind.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
                    legacybind.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                    MethodInfo keygate = AccessTools.DeclaredMethod(typeof(InputNode), "gate", new Type[] { typeof(IInputNode<>), typeof(IInputNode<bool>), typeof(bool), typeof(bool) });
                    legacybind.Add(new CodeInstruction(OpCodes.Call, keygate));
                    codes.InsertRange(i + 5, legacybind);
                }

			}
			return codes.AsEnumerable();
		}	

		private static void disableForceItemKeepGrabbed(Harmony harmony)
        {
			MethodInfo original = AccessTools.DeclaredMethod(typeof(CommonTool), "OnInputUpdate", new Type[] { });
			MethodInfo transpiler = AccessTools.DeclaredMethod(typeof(ThreeDimensionalDashOnScreen), nameof(disableForceItemKeepGrabbedTranspiler));
			harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
			Debug("Forcing keep last item held when dash is open patched out!");
        }

		private static IEnumerable<CodeInstruction> disableForceItemKeepGrabbedTranspiler(IEnumerable<CodeInstruction> instructions)
        {
			var codes = new List<CodeInstruction>(instructions);
			for (var i = 0; i < codes.Count; i++)
            {
				if (codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 2].opcode == OpCodes.Stloc_0 && codes[i + 3].opcode == OpCodes.Ldarg_0 && codes[i + 4].opcode == OpCodes.Call && codes[i + 5].opcode == OpCodes.Callvirt && ((MethodInfo)codes[i + 5].operand == typeof(InputInterface).GetMethod("get_VR_Active")))
				{
					codes[i + 3].opcode = OpCodes.Nop;
					codes[i + 4].opcode = OpCodes.Nop;
					codes[i + 5].opcode = OpCodes.Ldc_I4_1;
				}
			}
			return codes.AsEnumerable();
		}

		private static void addDesktopControlPanelKeybind(Harmony harmony)
        {
			MethodInfo original = AccessTools.DeclaredMethod(typeof(DesktopController), "OnCommonUpdate", new Type[] { });
			MethodInfo postfix = AccessTools.DeclaredMethod(typeof(ThreeDimensionalDashOnScreen), nameof(DesktopControlsKeybindPostfix));
			harmony.Patch(original, postfix: new HarmonyMethod(postfix));
			Debug("Desktop tab control panel key added!");
        }

		private static void DesktopControlsKeybindPostfix(DesktopController __instance)
        {
			MethodInfo toggleControls = __instance.GetType().GetMethod("ToggleControls", BindingFlags.NonPublic | BindingFlags.Instance);
			if (__instance.InputInterface.GetKeyDown(Config.GetValue(DESKTOP_CONTROL_PANEL_KEY)))
			{
				toggleControls.Invoke(__instance, new Object[] { });
			}
		}

		private static void patchNotifications(Harmony harmony)
        {
			MethodInfo original = AccessTools.DeclaredMethod(typeof(NotificationPanel), "AddNotification", new Type[] { typeof(string), typeof(string), typeof(Uri), typeof(color), typeof(string), typeof(Uri), typeof(IAssetProvider<AudioClip>) });
			MethodInfo transpiler = AccessTools.DeclaredMethod(typeof(ThreeDimensionalDashOnScreen), nameof(NotificationsTranspiler));
			harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
			Debug("Notifications patched!");
		}

		private static IEnumerable<CodeInstruction> NotificationsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);
			for (var i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].opcode == OpCodes.Call && ((MethodInfo)codes[i + 1].operand == typeof(InputInterface).GetMethod("get_Slot")) && codes[i + 2].opcode == OpCodes.Ldarg_S && codes[i + 3].opcode == OpCodes.Ldc_R4) //find the right getter to spoof by looking at stuff above it
				{
					codes[i + 4].opcode = OpCodes.Nop;  //Nop loading base reference onto stack
					codes[i + 5].opcode = OpCodes.Nop;  //Nop call to get InputInterface instance, that would use the base refernce
					codes[i + 6].opcode = OpCodes.Ldc_I4_1;  //instead nof callvirt for method getting the state of VR_Active, load true on stack
				}

				if (codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].opcode == OpCodes.Call && ((MethodInfo)codes[i + 1].operand == typeof(Worker).GetMethod("get_InputInterface")) && codes[i + 2].opcode == OpCodes.Callvirt && ((MethodInfo)codes[i + 2].operand == typeof(InputInterface).GetMethod("get_VR_Active")) && codes[i + 3].opcode == OpCodes.Brtrue_S && codes[i + 4].opcode == OpCodes.Ret)
				{
					codes[i].opcode = OpCodes.Nop;
					codes[i + 1].opcode = OpCodes.Nop;
					codes[i + 2].opcode = OpCodes.Nop;  //change the whole if statement and it's contents to do nothing
					codes[i + 3].opcode = OpCodes.Nop;
					codes[i + 4].opcode = OpCodes.Nop;
				}
			}
			return codes.AsEnumerable();
		}

		private static void patchCameraUI(Harmony harmony)
        {
			MethodInfo original = AccessTools.DeclaredMethod(typeof(InteractiveCameraControl), "OnAttach", new Type[] { });
			MethodInfo transpiler = AccessTools.DeclaredMethod(typeof(ThreeDimensionalDashOnScreen), nameof(CameraUITranspiler));
			MethodInfo postfix = AccessTools.DeclaredMethod(typeof(ThreeDimensionalDashOnScreen), nameof(removeCamUISlider));
			harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
			harmony.Patch(original, postfix: new HarmonyMethod(postfix));
			Debug("Camera Controls patched!");
		}

		private static IEnumerable<CodeInstruction> CameraUITranspiler(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);
			for (var i = 0; i < codes.Count; i++)
			{
				if (!Config.GetValue(RELEASE_CAM_UI) && codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].opcode == OpCodes.Call && codes[i + 2].opcode == OpCodes.Callvirt && ((MethodInfo)codes[i + 2].operand == typeof(InputInterface).GetMethod("get_VR_Active")) && codes[i + 3].opcode == OpCodes.Brfalse_S)
				{
					/*Debug(codes[i]);
					Debug(codes[i + 1]);
					Debug(codes[i + 2]);
					Debug(codes[i + 3]);*/
					codes[i].opcode = OpCodes.Nop;
					codes[i + 1].opcode = OpCodes.Nop;
					codes[i + 2].opcode = OpCodes.Nop;  //change the whole if statement and it's contents to do nothing
					codes[i + 3].opcode = OpCodes.Nop;
					/*Debug(codes[i]);
					Debug(codes[i + 1]);
					Debug(codes[i + 2]);
					Debug(codes[i + 3]);*/
				}

				if (Config.GetValue(RELEASE_CAM_UI) && codes[i].opcode == OpCodes.Dup && codes[i + 1].opcode == OpCodes.Brtrue_S && codes[i + 2].opcode == OpCodes.Pop && codes[i + 3].opcode == OpCodes.Br_S)  //find the grabbable destroy call
				{
					codes[i + 4].opcode = OpCodes.Pop;  //remove it and instead remove the surplus grabbacle reference
				}
			}
			return codes.AsEnumerable();
		}

		private static void removeCamUISlider(InteractiveCameraControl __instance)
		{
			if (Config.GetValue(RELEASE_CAM_UI))
			{
				Slider slider = __instance.Slot.GetComponent<Slider>(null, false);
				slider.Destroy();
			}
		}

		private static void addUIEditKey(Harmony harmony)
        {
			MethodInfo original = AccessTools.DeclaredMethod(typeof(Userspace), "OnCommonUpdate", new Type[] { });
			MethodInfo postfix = AccessTools.DeclaredMethod(typeof(ThreeDimensionalDashOnScreen), nameof(KeybindPostfix));
			harmony.Patch(original, postfix: new HarmonyMethod(postfix));
			Debug("UI Edit Mode keybind added!");
        }
		private static void KeybindPostfix(Userspace __instance)
		{
			if (__instance.InputInterface.GetKeyDown(Config.GetValue(UI_EDIT_MODE_KEY)))
			{
				Userspace.UserInterfaceEditMode = !Userspace.UserInterfaceEditMode;
			}
		}

		private static void patchSlotPositioning(Harmony harmony)
        {
			MethodInfo original = AccessTools.DeclaredMethod(typeof(SlotPositioning), "PositionInFrontOfUser", new Type[] { typeof(Slot), typeof(float3?), typeof(float3?), typeof(float), typeof(User), typeof(bool), typeof(bool), typeof(bool) });
			MethodInfo transpiler = AccessTools.DeclaredMethod(typeof(ThreeDimensionalDashOnScreen), nameof(positioningTranspiler));
			harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
			Debug("Slot positioning patched!");
		}

		private static IEnumerable<CodeInstruction> positioningTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);
			if (codes[0].opcode == OpCodes.Ldarg_0 && codes[1].opcode == OpCodes.Callvirt && codes[2].opcode == OpCodes.Call && codes[3].opcode == OpCodes.Bne_Un_S)
			{
				codes[0].opcode = OpCodes.Nop;
				codes[1].opcode = OpCodes.Nop; //replace if with unconditional jump
				codes[2].opcode = OpCodes.Nop;
				codes[3].opcode = OpCodes.Br_S;
			}
			else
			{
				Error("SlotPositioning.PositionInFrontOfUser: Could not patch because of unexpected opcode");
				return instructions;
			}
			return codes;
		}

		private static void patchDash(Harmony harmony)
        {
			MethodInfo originalOnCommonUpdate = AccessTools.DeclaredMethod(typeof(UserspaceRadiantDash), "OnCommonUpdate", new Type[] { });
			MethodInfo originalUpdateOverlayState = AccessTools.DeclaredMethod(typeof(UserspaceRadiantDash), "UpdateOverlayState", new Type[] { });
			MethodInfo radiantDashCommonUpdateTranspiler = AccessTools.DeclaredMethod(typeof(ThreeDimensionalDashOnScreen), nameof(RadiantDashCommonUpdateTranspiler));
			MethodInfo updateOverlayStatePrefix = AccessTools.DeclaredMethod(typeof(ThreeDimensionalDashOnScreen), nameof(UpdateOverlayStatePatch));
			MethodInfo onCommonUpdatePostfix = AccessTools.DeclaredMethod(typeof(ThreeDimensionalDashOnScreen), nameof(ScreenProjectionPatch));
			harmony.Patch(originalOnCommonUpdate, transpiler: new HarmonyMethod(radiantDashCommonUpdateTranspiler));
			harmony.Patch(originalOnCommonUpdate, postfix: new HarmonyMethod(onCommonUpdatePostfix));
			harmony.Patch(originalUpdateOverlayState, prefix: new HarmonyMethod(updateOverlayStatePrefix));
			Debug("Dash patched!");
		}

		private static IEnumerable<CodeInstruction> RadiantDashCommonUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
		{  //remove blocking of controls when dash is open
			var codes = new List<CodeInstruction>(instructions);
			for (var i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldsfld && ((FieldInfo)codes[i].operand == typeof(KeyboardBlock).GetField("GLOBAL_BLOCK") || (FieldInfo)codes[i].operand == typeof(MouseBlock).GetField("GLOBAL_BLOCK")))
				{
					codes[i - 2].opcode = OpCodes.Nop;
					codes[i - 1].opcode = OpCodes.Nop;
					codes[i].opcode = OpCodes.Nop;
					codes[i + 1].opcode = OpCodes.Nop;
					i += 1;
				}
				if (codes[i].opcode == OpCodes.Callvirt && ((MethodInfo)codes[i].operand == typeof(InputBindingManager).GetMethod("RegisterCursorUnlock") ||
				(MethodInfo)codes[i].operand == typeof(InputBindingManager).GetMethod("UnregisterCursorUnlock")))
				{
					codes[i - 3].opcode = OpCodes.Nop;
					codes[i - 2].opcode = OpCodes.Nop;
					codes[i - 1].opcode = OpCodes.Nop;
					codes[i].opcode = OpCodes.Nop;
				}
			}
			//This prints the il code, useful for debugging
			/*    
					for(var i = 0; i < codes.Count; i++)
					{
							Debug("IL_"+i.ToString("0000") + ": " + codes[i].ToString());
					}
			*/
			return codes.AsEnumerable();
		}

		private static bool UpdateOverlayStatePatch(UserspaceRadiantDash __instance)
		{
			RadiantDash dash = __instance.Dash;
			dash.VisualsRoot.SetParent(dash.Slot, false);
			dash.VisualsRoot.SetIdentityTransform();
			return false;
		}

		private static void ScreenProjectionPatch(UserspaceRadiantDash __instance)
		{
			__instance.Dash.ScreenProjection.Value = false;
		}
	}
}