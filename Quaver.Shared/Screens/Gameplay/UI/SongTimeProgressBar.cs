/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. 
 * Copyright (c) 2017-2018 Swan & The Quaver Team <support@quavergame.com>.
*/

using System;
using Microsoft.Xna.Framework;
using Quaver.Shared.Audio;
using Quaver.Shared.Graphics;
using Wobble.Graphics;
using Wobble.Graphics.UI;

namespace Quaver.Shared.Screens.Gameplay.UI
{
    public class SongTimeProgressBar : ProgressBar
    {
        /// <summary>
        ///     The display for the current time.
        /// </summary>
        public NumberDisplay CurrentTime { get; }

        /// <summary>
        ///     The display for the time left.
        /// </summary>
        public NumberDisplay TimeLeft { get; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="size"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="defaultValue"></param>
        /// <param name="inactiveColor"></param>
        /// <param name="activeColor"></param>
        public SongTimeProgressBar(Vector2 size, double minValue, double maxValue, double defaultValue, Color inactiveColor, Color activeColor)
            : base(size, minValue, maxValue, defaultValue, inactiveColor, activeColor)
        {
            CurrentTime = new NumberDisplay(NumberDisplayType.SongTime, "00:00", new Vector2(0.6f, 0.6f))
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Y = -Height - 25,
                X = 10
            };

            TimeLeft = new NumberDisplay(NumberDisplayType.SongTime, "-00:00", new Vector2(0.6f, 0.6f))
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                Y = CurrentTime.Y
            };

            TimeLeft.X = -TimeLeft.TotalWidth - 10;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            Bindable.Value = AudioEngine.Track.Time;

            // Set the time of the current time
            if (Bindable.Value > 0)
            {
                var currTime = new DateTime(1970, 1, 1) + TimeSpan.FromMilliseconds((int) Bindable.Value);
                CurrentTime.Value = currTime.ToString("mm:ss");
            }

            // Set the time of the time left.
            if (Bindable.MaxValue - Bindable.Value >= 0)
            {
                var timeLeft = new DateTime(1970, 1, 1) + TimeSpan.FromMilliseconds((int)(Bindable.MaxValue - Bindable.Value));

                // Get the old value.
                var oldValue = TimeLeft.Value;

                // Set the new value.
                TimeLeft.Value = "-" + timeLeft.ToString("mm:ss");

                // Check if we need to reposition it since it's on the right side of the screen.
                if (oldValue.Length != TimeLeft.Value.Length)
                    TimeLeft.X = -TimeLeft.TotalWidth - 10;
            }

            base.Update(gameTime);
        }
    }
}
