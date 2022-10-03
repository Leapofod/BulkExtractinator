using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BulkExtractinator;

internal sealed class ExtractinatorUI : ModSystem
{
	internal static bool IsExtractinatorOpen { get; set; }
	internal static bool IsUIOpen => _userInterface?.CurrentState != null;

	internal static UserInterface _userInterface;
	internal static ExtractUIState _extractUI;

	internal static Texture2D ExtractinatorBackgroundTex { get; private set; }

	private static GameTime _lastUIUpdate;

	public override void Load()
	{
		if (!Main.dedServ)
		{
			_userInterface = new UserInterface();
			_extractUI = new ExtractUIState();

			_extractUI.Activate();

			ExtractinatorBackgroundTex = new Texture2D(Main.graphics.GraphicsDevice, 48, 48);

			var extractinatorTex = TextureAssets.Tile[TileID.Extractinator];
			var extractinatorTexData = new Color[54 * 52];
			extractinatorTex.Value.GetData(extractinatorTexData, 0, 54 * 52);
			var newExtractinatorTexData = new Color[48 * 48];
			for (int i = 0; i < extractinatorTexData.Length; i++)
			{
				if (i % 18 > 15 || i / 18 % 18 > 15) 
					continue;

			}
		}
	}

	public override void Unload()
	{
		if (!Main.dedServ)
		{
			_extractUI.Deactivate();
			_userInterface.SetState(null); // might be redundant, check later

			ExtractinatorBackgroundTex.Dispose();
		}

		_userInterface = null;
		_extractUI = null;
	}


	public override void UpdateUI(GameTime gameTime)
	{
		_lastUIUpdate = gameTime;
		IsExtractinatorOpen &= Main.playerInventory && _userInterface?.CurrentState != null
			&& Main.npcShop == 0 && Main.player[Main.myPlayer].talkNPC == -1 
			&& Main.player[Main.myPlayer].chest == -1 && !Main.CreativeMenu.Enabled;
		// todo: write better open/close logic

		if (!IsExtractinatorOpen && _userInterface?.CurrentState != null)
			CloseUI();

		if (IsExtractinatorOpen)
		{

		}
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		if (IsExtractinatorOpen && _lastUIUpdate != null && Main.playerInventory)
		{
			var invIndex = layers.FindIndex(x => x.Name == "Vanilla: Inventory");
			if (invIndex != -1)
			{
				layers.Insert(invIndex + 1, new LegacyGameInterfaceLayer(
					"BulkExtractinator: Extractinator",
					() =>
					{
						_userInterface.Draw(Main.spriteBatch, _lastUIUpdate);
						return true;
					},
					InterfaceScaleType.UI));
			}
		}
	}

	internal static void OpenUI()
	{
		Main.SetNPCShopIndex(0);
		Main.player[Main.myPlayer].chest = -1;
		Main.player[Main.myPlayer].SetTalkNPC(0);
		_userInterface?.SetState(_extractUI);
		IsExtractinatorOpen = true;
	}

	internal static void CloseUI()
	{
		_userInterface?.SetState(null);
	}
}
