using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Reflection;


namespace ThreeDimensionalDashOnScreen
{
	public class ThreeDimensionalDashOnScreen : NeosMod
	{
		public override string Name => "3DDashOnScreen";
		public override string Author => "rampa3";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/rampa3/3DDashOnScreen";
		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("net.rampa3.3DDashOnScreen");
			harmony.PatchAll();
			Debug("Dash patched successfully!");
		}
		/*
		   _______________________________
		   |     cursor parking lot      |
		   |                             |
		   |_____________________________|

				Users present at one point: art0007i, eia485, rampa3
		*/

		[HarmonyPatch(typeof(Userspace), "OnCommonUpdate")]
		class KeybindPatch{
			static void Postfix(Userspace __instance){
				if(__instance.InputInterface.GetKey(Key.F4)){
					Userspace.UserInterfaceEditMode = !Userspace.UserInterfaceEditMode;
				}
			}
		}

		[HarmonyPatch(typeof(SlotPositioning), "PositionInFrontOfUser")]
		class OverlayParentPatch{
			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				var codes = new List<CodeInstruction>(instructions);
				if(codes[0].opcode == OpCodes.Ldarg_0 && codes[1].opcode == OpCodes.Callvirt && codes[2].opcode == OpCodes.Call && codes[3].opcode == OpCodes.Bne_Un_S)
				{
					codes[0].opcode = OpCodes.Nop;
					codes[1].opcode = OpCodes.Nop;
					codes[2].opcode = OpCodes.Nop;
					codes[3].opcode = OpCodes.Br_S;
				}else{
					Error("SlotPositioning.PositionInFrontOfUser: Could not patch because of unexpected opcode");
					return instructions;
				}
				return codes;
			}
		}

		[HarmonyPatch(typeof(UserspaceRadiantDash))]
		class ThreeDimensionalDashOnScreenPatch
		{
			[HarmonyTranspiler]
			[HarmonyPatch("OnCommonUpdate")]
			static IEnumerable<CodeInstruction> RadiantDashCommonUpdateTranspiler(IEnumerable<CodeInstruction> instructions){
				var codes = new List<CodeInstruction>(instructions);
				for (var i = 0; i < codes.Count; i++)
				{
					if (codes[i].opcode == OpCodes.Ldsfld && ((FieldInfo)codes[i].operand == typeof(KeyboardBlock).GetField("GLOBAL_BLOCK") || (FieldInfo)codes[i].operand == typeof(MouseBlock).GetField("GLOBAL_BLOCK"))){
						codes[i - 2].opcode = OpCodes.Nop;
						codes[i - 1].opcode = OpCodes.Nop;
						codes[i].opcode = OpCodes.Nop;
						codes[i + 1].opcode = OpCodes.Nop;
						i += 1;
					}
					if(codes[i].opcode == OpCodes.Callvirt && ((MethodInfo)codes[i].operand == typeof(InputBindingManager).GetMethod("RegisterCursorUnlock") ||
					(MethodInfo)codes[i].operand == typeof(InputBindingManager).GetMethod("UnregisterCursorUnlock")))
					{
						codes[i-3].opcode = OpCodes.Nop;
						codes[i-2].opcode = OpCodes.Nop;
						codes[i-1].opcode = OpCodes.Nop;
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

			[HarmonyPrefix]
			[HarmonyPatch("UpdateOverlayState")]
			static bool UpdateOverlayStatePatch(UserspaceRadiantDash __instance)
			{
				RadiantDash dash = __instance.Dash;
				dash.VisualsRoot.SetParent(dash.Slot, false);
				dash.VisualsRoot.SetIdentityTransform();
				return false;
			}

			[HarmonyPostfix]
			[HarmonyPatch("OnCommonUpdate")]
			static void ThePatchRewritten(UserspaceRadiantDash __instance)
			{
				__instance.Dash.ScreenProjection.Value = false;
			}
		}
	}
}