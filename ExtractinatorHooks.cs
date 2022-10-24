using MonoMod.RuntimeDetour.HookGen;
using System.Reflection;
using Terraria.ModLoader;

namespace BulkExtractinator;

internal static class ExtractinatorHooks
{
	private static readonly MethodInfo m_TileLoader_ModifySmartInteractCoords = typeof(TileLoader).GetMethod("ModifySmartInteractCoords", BindingFlags.Static | BindingFlags.Public);

	internal delegate void orig_ModifySmartInteractCoords(int type, ref int width, ref int height, ref int frameWidth, ref int frameHeight, ref int extraY);
	internal delegate void hook_ModifySmartInteractCoords(orig_ModifySmartInteractCoords orig, int type, ref int width, ref int height, ref int frameWidth, ref int frameHeight, ref int extraY);

	internal static event hook_ModifySmartInteractCoords OnModifySmartInteractCoords
	{
		add => HookEndpointManager.Add(m_TileLoader_ModifySmartInteractCoords, value);
		remove => HookEndpointManager.Remove(m_TileLoader_ModifySmartInteractCoords, value);
	}
}
