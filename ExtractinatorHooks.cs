using MonoMod.RuntimeDetour.HookGen;
using System.Reflection;
using Terraria.GameContent.ObjectInteractions;

namespace BulkExtractinator;

internal static class ExtractinatorHooks
{
	private static readonly MethodInfo m_HasSmartInteract = typeof(Terraria.ModLoader.TileLoader).GetMethod("HasSmartInteract", BindingFlags.Static | BindingFlags.Public);

	internal delegate bool Orig_HasSmartInteract(int i, int j, int type, SmartInteractScanSettings settings);
	internal delegate bool Hook_HasSmartInteract(Orig_HasSmartInteract orig, int i, int j, int type, SmartInteractScanSettings settings);
	
	internal static event Hook_HasSmartInteract On_HasSmartInteract
	{
		add => HookEndpointManager.Add(m_HasSmartInteract, value);
		remove => HookEndpointManager.Remove(m_HasSmartInteract, value);
	}

}
