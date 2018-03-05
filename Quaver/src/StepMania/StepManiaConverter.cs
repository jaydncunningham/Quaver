﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quaver.API.Maps;
using Quaver.API.StepMania;
using Quaver.Config;
using Quaver.Logging;
using Quaver.Utility;

namespace Quaver.StepMania
{
    internal class StepManiaConverter
    {
        /// <summary>
        ///     Converts a .sm file to Quaver's format.
        /// </summary>
        internal static void ConvertSm(string path)
        {
            try
            {
                var quaverMaps = Qua.ConvertStepManiaChart(StepManiaFile.Parse(path));

                // Create a new directory in the songs folder
                var quaverDir = $"{Configuration.SongDirectory}/StepMania - {new DirectoryInfo(path).Name} - {GameBase.GameTime.ElapsedMilliseconds}/";
                Directory.CreateDirectory(quaverDir);

                foreach (var map in quaverMaps)
                    map.Save($"{quaverDir}/{Util.FileNameSafeString(map.Artist)} - {Util.FileNameSafeString(map.Title)} [{Util.FileNameSafeString(map.DifficultyName)}].qua");

                // Now copy over the background + audio file
                try
                {
                    File.Copy(Path.GetDirectoryName(path) + "/" + quaverMaps[0].AudioFile, quaverDir + "/" + quaverMaps[0].AudioFile);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, LogType.Runtime);
                }

                try
                {
                    File.Copy(Path.GetDirectoryName(path) + "/" + quaverMaps[0].BackgroundFile, quaverDir + "/" + quaverMaps[0].BackgroundFile);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, LogType.Runtime);
                }

                Logger.LogSuccess("StepMania file has been successfully converted!", LogType.Runtime);
            }
            catch (Exception e)
            {
                Logger.LogError(e, LogType.Runtime);
            }
        }
    }
}