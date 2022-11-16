using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BulkExtractinator;

internal sealed class ExtractinatorUI : ModSystem
{
	internal static UserInterface _userInterface;
	internal static ExtractUIState _extractUI;

	private static GameTime _lastUIUpdate;

	public override void Load()
	{
		if (!Main.dedServ)
		{
			_userInterface = new UserInterface();
			_extractUI = new ExtractUIState();

			_extractUI.Activate();
			_userInterface.SetState(_extractUI);
		}
	}

	public override void Unload()
	{
		if (!Main.dedServ)
		{
			_extractUI.Deactivate();
			_userInterface.SetState(null);
		}

		_userInterface = null;
		_extractUI = null;
	}


	public override void UpdateUI(GameTime gameTime)
	{
		_lastUIUpdate = gameTime;
		
		if (ExtractinatorPlayer.ExtractinatorOpenLocally)
			_userInterface.Update(gameTime);
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		if (_lastUIUpdate != null)
		{
			var invIndex = layers.FindIndex(x => x.Name == "Vanilla: Inventory");
			if (invIndex != -1)
			{
				layers.Insert(invIndex + 1, new LegacyGameInterfaceLayer(
					"BulkExtractinator: Extractinator",
					() =>
					{
						if (ExtractinatorPlayer.ExtractinatorOpenLocally)
							_userInterface.Draw(Main.spriteBatch, _lastUIUpdate);
						return true;
					},
					InterfaceScaleType.UI));
			}
		}
	}
}
