using System;
using System.Collections.Generic;
using System.IO;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain
{
    /// <summary>
    /// Reads the per-scenario local variables that the game engine (Phobos
    /// SaveVariablesOnScenarioEnd) writes to <c>locals.ini</c> in the game root
    /// directory when a scenario ends, and optionally erases the section again.
    /// </summary>
    public static class ScenarioVariables
    {
        public const string FILE_NAME = "locals.ini";

        // Index 0 => bronze (Easy), 1 => silver (Medium), 2 => gold (Hard).
        private static readonly string[] MedalKeys = { "bronze", "silver", "gold" };

        /// <summary>
        /// Reads the completion medal level (1 = bronze, 2 = silver, 3 = gold) of the
        /// given scenario from locals.ini. Returns 0 when the file or section is
        /// missing or no medal variable is set.
        /// </summary>
        public static int ReadCompletionLevel(string scenario)
        {
            if (string.IsNullOrEmpty(scenario))
                return 0;

            string path = GetFilePath();
            if (!File.Exists(path))
                return 0;

            var ini = new IniFile(path);
            string section = FindSection(ini, scenario);
            if (section == null)
                return 0;

            int level = 0;
            for (int i = 0; i < MedalKeys.Length; i++)
            {
                if (ini.GetIntValue(section, MedalKeys[i], 0) != 0)
                    level = i + 1;
            }

            return level;
        }

        /// <summary>
        /// Removes the scenario's section from locals.ini. A read-only attribute is
        /// cleared first so that the engine's output does not accumulate. All
        /// failures are logged and swallowed; the caller's result is never lost.
        /// </summary>
        public static void EraseSection(string scenario)
        {
            if (string.IsNullOrEmpty(scenario))
                return;

            string path = GetFilePath();
            if (!File.Exists(path))
                return;

            try
            {
                var ini = new IniFile(path);
                string section = FindSection(ini, scenario);
                if (section == null)
                    return;

                ini.RemoveSection(section);

                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);

                ini.WriteIniFile();
            }
            catch (Exception ex)
            {
                Logger.Log("ScenarioVariables: failed to erase the locals.ini section: " + ex.Message);
            }
        }

        private static string GetFilePath() =>
            SafePath.CombineFilePath(ProgramConstants.GamePath, FILE_NAME);

        /// <summary>
        /// Gets the last write time of locals.ini, or <see cref="DateTime.MinValue"/>
        /// when the file does not exist.
        /// </summary>
        public static DateTime GetLastWriteTime()
        {
            string path = GetFilePath();
            return File.Exists(path) ? File.GetLastWriteTime(path) : DateTime.MinValue;
        }

        private static string FindSection(IniFile ini, string scenario)
        {
            List<string> sections = ini.GetSections();
            string scenarioFileName = Path.GetFileName(scenario);

            foreach (string section in sections)
            {
                if (string.Equals(section, scenario, StringComparison.OrdinalIgnoreCase))
                    return section;
            }

            foreach (string section in sections)
            {
                if (string.Equals(Path.GetFileName(section), scenarioFileName, StringComparison.OrdinalIgnoreCase))
                    return section;
            }

            return null;
        }
    }
}
