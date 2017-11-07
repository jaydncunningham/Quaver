﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Audio;
using Quaver.QuaFile;
using Quaver.Utility;
using SQLite;

namespace Quaver.Beatmaps
{
    internal class Beatmap
    {   /// <summary>
        ///     The MD5 Of the file.
        /// </summary>
        [PrimaryKey]
        public string Md5Checksum { get; set; }

        /// <summary>
        ///     Is the beatmap valid and able to be played?
        /// </summary>
        [Ignore]
        public bool IsValidBeatmap { get; set; }

        /// <summary>
        ///     The absolute path of the .qua file
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///     The beatmap set id of the map.
        /// </summary>
        public int BeatmapSetId { get; set; }

        /// <summary>
        ///     The beatmap id of the map.
        /// </summary>
        public int BeatmapId { get; set; }

        /// <summary>
        ///     The song's artist
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        ///     The song's title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///     The difficulty name of the beatmap
        /// </summary>
        public string DifficultyName { get; set; }

        /// <summary>
        ///     The highest rank that the player has gotten on the map.
        /// </summary>
        public string HighestRank { get; set; }

        /// <summary>
        ///     The ranked status of the map.
        /// </summary>
        public int RankedStatus { get; set; }

        /// <summary>
        ///     The last time the user has played the map.
        /// </summary>
        public DateTime LastPlayed { get; set; } =
            new DateTime(0001, 1, 1, 00, 00, 00); // 01/01/0001 00:00:00 - If never played

        /// <summary>
        ///     The difficulty rating of the beatmap.
        /// </summary>
        public float DifficultyRating { get; set; }

        /// <summary>
        ///     The creator of the map.
        /// </summary>
        public string Creator { get; set; }

        /// <summary>
        ///     The absolute path of the beatmap's background.
        /// </summary>
        public string BackgroundPath { get; set; }

        /// <summary>
        ///     The absolute path of the beatmap's audio.
        /// </summary>
        public string AudioPath { get; set; }

        /// <summary>
        ///     The audio preview time of the beatmap
        /// </summary>
        public int AudioPreviewTime { get; set; }

        /// <summary>
        ///     The description of the beatmap
        /// </summary>
        public string Description { get; set; } = "No Description";

        /// <summary>
        ///     The source (album/mixtape/etc) of the beatmap
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        ///     Tags for the beatmap
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        ///     The most common bpm for the beatmap
        /// </summary>
        public decimal Bpm { get; set; }

        /// <summary>
        ///     The beatmap's length (Time of the last hit object)
        /// </summary>
        public int SongLength { get; set; }

        /// <summary>
        ///     The specific beatmap's song.
        /// </summary>
        [Ignore]
        public GameAudio Song { get; set; }

        /// <summary>
        ///     The beatmap's background texture.
        /// </summary>
        [Ignore]
        public Texture2D Background { get; set; }

        /// <summary>
        ///     The actual parsed qua file for the beatmap.
        /// </summary>
        [Ignore]
        public Qua Qua { get; set; }

        /// <summary>
        ///     The amount of keys the beatmap has.
        /// </summary>
        public int Keys { get; set; }

        /// <summary>
        ///     Loads a beatmaps's GameAudio file
        /// </summary>
        internal void LoadAudio()
        {
            Song = new GameAudio(AudioPath);
        }

        /// <summary>
        ///     Loads a beatmaps background file.
        /// </summary>
        internal void LoadBackground()
        {
            Background = ImageLoader.Load(System.IO.Path.GetDirectoryName(Path) + "/" + Background);
        }

        /// <summary>
        ///     Responsible for converting a Qua object, to a Beatmap object
        ///     a Beatmap object is one that is stored in the db.
        /// </summary>
        /// <param name="qua"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        internal Beatmap ConvertQuaToBeatmap(Qua qua, string path)
        {
            return new Beatmap
            {
                Md5Checksum = BeatmapUtils.GetMd5Checksum(path),
                IsValidBeatmap = true,
                Path = path,
                Artist = qua.Artist,
                Title = qua.Title,
                AudioPath = System.IO.Path.GetDirectoryName(path) + "/" + qua.AudioFile,
                AudioPreviewTime = qua.SongPreviewTime,
                BackgroundPath = qua.BackgroundFile,
                BeatmapId = qua.MapId,
                BeatmapSetId = qua.MapSetId,
                Bpm = Qua.FindCommonBpm(qua),
                Creator = qua.Creator,
                DifficultyName = qua.DifficultyName,
                Source = qua.Source,
                Tags = qua.Tags,
                SongLength = Qua.FindSongLength(qua),
                Keys = qua.KeyCount
            };
        }
    }
}
