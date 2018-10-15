using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Enums;
using Wobble;
using Wobble.Assets;

namespace Quaver.Assets
{
    public static class CompetitiveBadgeHelper
    {
        public static Texture2D Get(CompetitveBadge badge)
        {
            // ReSharper disable once ArrangeMethodOrOperatorBody
            return AssetLoader.LoadTexture2D(GameBase.Game.Resources.Get($"Textures/UI/Competitive/comp-{(int) badge}.png"));
        }
    }
}