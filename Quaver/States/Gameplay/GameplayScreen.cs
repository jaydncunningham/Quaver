﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Enums;
using Quaver.API.Maps;
using Quaver.Audio;
using Quaver.Config;
using Quaver.Database.Scores;
using Quaver.Discord;
using Quaver.GameState;
using Quaver.Graphics.Sprites;
using Quaver.Graphics.UserInterface;
using Quaver.Helpers;
using Quaver.Logging;
using Quaver.Main;
using Quaver.Modifiers;
using Quaver.States.Gameplay.GameModes.Keys;
using Quaver.States.Gameplay.Replays;
using Quaver.States.Gameplay.UI;

namespace Quaver.States.Gameplay
{
    internal class GameplayScreen : IGameState
    {
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public State CurrentState { get; set; } = State.Gameplay;
        
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public bool UpdateReady { get; set; }

        /// <summary>
        ///     The specific audio timimg for this gameplay state.
        /// </summary>
        internal GameplayTiming Timing { get; }

        /// <summary>
        ///     The curent game mode ruleset
        /// </summary>
        internal GameModeRuleset Ruleset { get; }

        /// <summary>
        ///     The general gameplay UI.
        /// </summary>
        internal GameplayInterface UI { get; }

        /// <summary>
        ///     If the game is currently paused.
        /// </summary>
        internal bool IsPaused { get; set; }

        /// <summary>
        ///     The amount of times the user has paused.
        /// </summary>
        private int PauseCounter { get; set; }

        /// <summary>
        ///     If the game session has already been started.
        /// </summary>
        internal bool HasStarted { get; set; }

        /// <summary>
        ///     The current parsed .qua file that is being played.
        /// </summary>
        internal Qua Map { get; }
        
        /// <summary>
        ///     The hash of the map that was played.
        /// </summary>
        internal string MapHash { get; }

        /// <summary>
        ///     Dictates if we are currently resuming the game.
        /// </summary>
        internal bool IsResumeInProgress { get; private set; }

        /// <summary>
        ///     The time the user resumed the game.
        /// </summary>
        private long ResumeTime { get; set; }

        /// <summary>
        ///     The last recorded combo. We use this value for combo breaking.
        /// </summary>
        private int LastRecordedCombo { get; set; }

        /// <summary>
        ///     If the user is currently on a break in the song.
        /// </summary>
        private bool _onBreak;
        internal bool OnBreak
        {
            get
            {
                // By default if there aren't any objects left we aren't on a break.
                if (Ruleset.HitObjectManager.ObjectPool.Count <= 0) 
                    return false;

                // Grab the next object in the object pool.
                var nextObject = Ruleset.HitObjectManager.ObjectPool.First();
                
                // If the player is currently not on a break, then we want to detect if it's on a break
                // by checking if the next object is 10 seconds away.
                if (nextObject.TrueStartTime - Timing.CurrentTime >= GameplayTiming.StartDelay + 10000)
                    _onBreak = true;                 
                // If the user is already on a break, then we need to turn the break off if the next object is at the start delay.
                else if (_onBreak && nextObject.TrueStartTime - Timing.CurrentTime <= GameplayTiming.StartDelay)
                    _onBreak = false;

                return _onBreak;
            }
        }

        /// <summary>
        ///     If the play is finished.
        /// </summary>
        internal bool IsPlayComplete => Ruleset.HitObjectManager.IsComplete;

        /// <summary>
        ///     If the play was failed (0 health)
        /// </summary>
        internal bool Failed => Ruleset.ScoreProcessor.Health <= 0 || ForceFail;

        /// <summary>
        ///     Flag that makes sure the failure sound only gets played once.
        /// </summary>
        private bool FailureHandled { get; set; }

        /// <summary>
        ///     The amount of time the restart key has been held down for.
        /// </summary>
        private double RestartKeyHoldTime { get; set; }

        /// <summary>
        ///     Flag that dictates if the user is currently restarting the play.
        /// </summary>
        internal bool IsRestartingPlay { get; set; }

        /// <summary>
        ///     If we're force failing the user.
        /// </summary>
        internal bool ForceFail { get; set; }

        /// <summary>
        ///     If the user quit the game themselves.
        /// </summary>
        internal bool HasQuit { get; set; }

        /// <summary>
        ///     When the play is either failed or completed, this is a counter that
        ///     will increase and dictates when to go to the results screen.
        /// </summary>
        internal double TimeSincePlayEnded { get; set; }

        /// <summary>
        ///     All of the local scores for this map.
        /// </summary>
        internal List<LocalScore> LocalScores { get; }

        /// <summary>
        ///     If we are currently viewing a replay.
        /// </summary>
        internal bool InReplayMode { get; }

        /// <summary>
        ///     The amount of times the user requested to quit.
        /// </summary>
        private int TimesRequestedToPause { get; set; }

        /// <summary>
        ///     The replay that is currently loaded that the player is watching.
        /// </summary>
        internal Replay LoadedReplay { get; }

        /// <summary>
        ///     Ctor - 
        /// </summary>
        internal GameplayScreen(Qua map, string md5, List<LocalScore> scores, Replay replay = null)
        {
            LocalScores = scores;
            Map = map;
            MapHash = md5;
            LoadedReplay = replay;
            Timing = new GameplayTiming(this);
            UI = new GameplayInterface(this);
            
            if (ModManager.IsActivated(ModIdentifier.Autoplay))
                LoadedReplay = Replay.GeneratePerfectReplay(map);
            
            if (LoadedReplay != null)
                InReplayMode = true;
            
            // Set the game mode component.
            switch (map.Mode)
            {
                case GameMode.Keys4:
                case GameMode.Keys7:
                    Ruleset = new GameModeRulesetKeys(this, map.Mode, map);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Game mode must be a valid!");
            }
        }
        
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Initialize()
        {           
            Timing.Initialize(this);
            UI.Initialize(this);
            
            // Change discord rich presence.
            DiscordController.ChangeDiscordPresenceGameplay(false);
            
            // Initialize the game mode.
            Ruleset.Initialize();
               
            UpdateReady = true;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void UnloadContent()
        {
            Timing.UnloadContent();
            UI.UnloadContent();
            Ruleset.Destroy();
            Logger.Clear();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="dt"></param>
        public void Update(double dt)
        {
            UI.Update(dt);
            Timing.Update(dt);
            
            if (!Failed && !IsPlayComplete)
            {
                HandleResuming();
                PauseIfWindowInactive();
                PlayComboBreakSound();
            }

            HandleInput(dt);
            HandleFailure();
            Ruleset.Update(dt);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Draw()
        {
            GameBase.GraphicsDevice.Clear(Color.Black);
            
            // Draw BG Manager
            GameBase.SpriteBatch.Begin();            
            BackgroundManager.Draw();
            GameBase.SpriteBatch.End();
            
            // Draw the ruleset which gets its own spritebatch.       
            Ruleset.Draw();
            
            GameBase.SpriteBatch.Begin();
            UI.Draw();
            
            /*Logger.Update("Paused", $"Paused: {IsPaused}");
            Logger.Update("Resume In Progress", $"Resume In Progress {IsResumeInProgress}");
            Logger.Update($"Max Combo", $"Max Combo: {Ruleset.ScoreProcessor.MaxCombo}");
            Logger.Update($"Objects Left", $"Objects Left {Ruleset.HitObjectManager.ObjectsLeft}");
            Logger.Update($"Finished", $"Finished: {IsPlayComplete}");
            Logger.Update($"On Break", $"On Break: {OnBreak}");
            Logger.Update($"Failed", $"Failed: {Failed}");*/
            
            GameBase.SpriteBatch.End();
        }
        
#region INPUT               
        /// <summary>
        ///     Handles the input of the game + individual game modes.
        /// </summary>
        /// <param name="dt"></param>
        private void HandleInput(double dt)
        {
            if (!Failed && !IsPlayComplete && InputHelper.IsUniqueKeyPress(ConfigManager.KeyPause.Value))
                Pause();

            // Show/hide scoreboard.
            if (InputHelper.IsUniqueKeyPress(ConfigManager.KeyScoreboardVisible.Value))
                ConfigManager.ScoreboardVisible.Value = !ConfigManager.ScoreboardVisible.Value;
            
            if (IsPaused || Failed)
                return;

            if (!IsPlayComplete)
            {
                // Handle the restarting of the map.
                HandlePlayRestart(dt);
            
                if (InputHelper.IsUniqueKeyPress(ConfigManager.KeySkipIntro.Value))
                    SkipToNextObject();              
            }
            
            // Handle input per game mode.
            Ruleset.HandleInput(dt);
        }

        /// <summary>
        ///     Pauses the game.
        /// </summary>
        internal void Pause()
        {
            // Don't allow any sort of pausing if the play is already finished.
            if (IsPlayComplete)
                return;

            if (ModManager.IsActivated(ModIdentifier.NoPause))
            {
                TimesRequestedToPause++;

                // Force fail the user if they request to quit more than once.
                switch (TimesRequestedToPause)
                {
                    case 1:
                        Logger.LogImportant($"Press the pause button one more time to exit.", LogType.Runtime);
                        break;
                    default:
                        ForceFail = true;
                        HasQuit = true;
                        break;
                }
                return;
            }
            
            // Handle pause.
            if (!IsPaused || IsResumeInProgress)
            {
                IsPaused = true;
                IsResumeInProgress = false;
                PauseCounter++;
                
                DiscordController.ChangeDiscordPresence($"{Map.Artist} - {Map.Title} [{Map.DifficultyName}]", $"Paused for the {StringHelper.AddOrdinal(PauseCounter)} time");
                
                try
                {
                    GameBase.AudioEngine.Pause();
                }
                catch (AudioEngineException e) {}

                return;
            }

            // Setting the resume time in this case allows us to give the user time to react 
            // with a delay before starting the audio track again.
            // When that resume time is past the specific set offset, it'll unpause the game.
            IsResumeInProgress = true;
            ResumeTime = GameBase.GameTime.ElapsedMilliseconds;
            DiscordController.ChangeDiscordPresenceGameplay(true);
        }

        /// <summary>
        ///     Handles resuming of the game.
        ///     Essentially gives a delay before starting the game back up.
        /// </summary>
        private void HandleResuming()
        {
            if (!IsPaused || !IsResumeInProgress)
                return;

            // We don't want to resume if the time difference isn't at least or greter than the start delay.
            if (GameBase.GameTime.ElapsedMilliseconds - ResumeTime > 800)
            {
                // Unpause the game and reset the resume in progress.
                IsPaused = false;
                IsResumeInProgress = false;
            
                // Resume the game audio stream.
                try
                {
                    GameBase.AudioEngine.Resume();
                } 
                catch (AudioEngineException e) {}
            }
        }

        /// <summary>
       ///     Skips the song to the next object if on a break.
       /// </summary>
        private void SkipToNextObject()
        {
            if (!OnBreak || IsPaused || IsResumeInProgress)
                return;

            // Get the skip time of the next object.           
            var skipTime = Ruleset.HitObjectManager.ObjectPool.First().TrueStartTime - GameplayTiming.StartDelay + AudioEngine.BassDelayOffset;

            try
            {
                // Skip to the time if the audio already played once. If it hasn't, then play it.
                if (GameBase.AudioEngine.HasPlayed)
                    GameBase.AudioEngine.ChangeSongPosition(skipTime);
                else
                    GameBase.AudioEngine.Play((int)skipTime);

                // Set the actual song time to the position in the audio if it was successful.
                Timing.CurrentTime = GameBase.AudioEngine.Position;
            }
            catch (AudioEngineException ex)
            {
                Logger.LogWarning("Trying to skip with no audio file loaded. Still continuing..", LogType.Runtime);

                // If there is no audio file, make sure the actual song time is set to the skip time.
                const int actualSongTimeOffset = 10000; // The offset between the actual song time and audio position (?)
                Timing.CurrentTime = skipTime + actualSongTimeOffset;
            }
            finally
            {
                // Skip to 3 seconds before the notes start
                DiscordController.ChangeDiscordPresenceGameplay(true);
            }
        }

        /// <summary>
        ///     Restarts the game if the user is holding down the key for a specified amount of time
        ///     
        /// </summary>
        private void HandlePlayRestart(double dt)
        {
            if (InputHelper.IsUniqueKeyPress(ConfigManager.KeyRestartMap.Value))
                IsRestartingPlay = true;
            
            if (GameBase.KeyboardState.IsKeyDown(ConfigManager.KeyRestartMap.Value) && IsRestartingPlay)
            {                
                RestartKeyHoldTime += dt;
                UI.ScreenTransitioner.FadeIn(dt, 60);
                
                // Restart the map if the user has held it down for 
                if (RestartKeyHoldTime >= 350)
                {
                    GameBase.AudioEngine.PlaySoundEffect(GameBase.Skin.SoundRetry);
                    GameBase.GameStateManager.ChangeState(new GameplayScreen(Map, MapHash, LocalScores));
                }

                return;
            }

            RestartKeyHoldTime = 0;
            IsRestartingPlay = false;
        }
 #endregion
        
        /// <summary>
        ///     Checks if the window is currently active and pauses the game if it isn't.
        /// </summary>
        private void PauseIfWindowInactive()
        {
            if (IsPaused)
                return;
            
            // Pause the game
            if (!QuaverGame.Game.IsActive && !ModManager.IsActivated(ModIdentifier.NoPause))
                Pause();
        }

        /// <summary>
        ///     Plays a combo break sound if we've 
        /// </summary>
        private void PlayComboBreakSound()
        {
            if (LastRecordedCombo >= 20 && Ruleset.ScoreProcessor.Combo == 0)
                GameBase.AudioEngine.PlaySoundEffect(GameBase.Skin.SoundComboBreak);

            LastRecordedCombo = Ruleset.ScoreProcessor.Combo;
        }

        /// <summary>
        ///     Stops the music and begins the failure process.
        /// </summary>
        private void HandleFailure()
        {
            if (!Failed || FailureHandled)
                return;

            try
            {
                // Pause the audio if applicable.
                GameBase.AudioEngine.Pause();
            }
            // No need to handle this exception.
            catch (AudioEngineException e) {}
            
            // Play failure sound.
            GameBase.AudioEngine.PlaySoundEffect(GameBase.Skin.SoundFailure);

            FailureHandled = true;
        }
    }
}