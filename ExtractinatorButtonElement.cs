﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace BulkExtractinator;

internal sealed class ExtractinatorButtonElement : UIElement
{
	private bool ShowButton => ExtractinatorPlayer.ExtractinatorOpenLocally 
		&& !Main.LocalPlayer.GetModPlayer<ExtractinatorPlayer>().ExtractinatorSlotItem.IsAir;

	public override void OnInitialize()
	{
		OnMouseDown += HandlePress;

		// check if redundant later
		Width.Set(TextureAssets.Reforge[0].Value.Width + 16f, 0f);
		Height.Set(TextureAssets.Reforge[0].Value.Height + 16f, 0f);
		//SetPadding(40f);

		// MaxWidth.Set(TextureAssets.Reforge[1].Value.Width, 0f);
		// MaxHeight.Set(TextureAssets.Reforge[1].Value.Height, 0f);
	}

	private void HandlePress(UIMouseEvent evt, UIElement listeningElement)
	{
		if (evt.Target != this)
			return;

		var res = ExtractionHelper.MassExtract(Main.LocalPlayer, true);
		if (res)
			SoundEngine.PlaySound(in SoundID.Grab);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		if (!ShowButton)
			return;

		var tex = TextureAssets.Reforge[0].Value;
		if (IsMouseHovering)
			tex = TextureAssets.Reforge[1].Value;

		spriteBatch.Draw(tex, GetViewCullingArea().Center(), null, Color.White, 0f, tex.Size() / 2f, 1f, SpriteEffects.None, 0f);

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.hoverItemName = Language.GetTextValue("Mods.BulkExtractinator.UI.ExtractUntilFull");
		}
	}
}
