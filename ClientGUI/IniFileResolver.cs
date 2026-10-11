using System.IO;
using ClientCore;
using Rampastring.Tools;

namespace ClientGUI
{
    /// <summary>
    /// Resolves INI file paths from the theme and base resource directories.
    /// Both loose files on the file system and INI files stored inside the
    /// encrypted client asset archive are considered, with loose files taking
    /// priority.
    /// </summary>
    internal static class IniFileResolver
    {
        /// <summary>
        /// Resolves the full path of an INI file from the theme directory first,
        /// then the base resource directory, checking the file system before the
        /// asset archive for each location.
        /// </summary>
        /// <param name="iniFileName">The INI file name, including the extension.</param>
        /// <returns>The resolved path, or null when the file cannot be found anywhere.</returns>
        public static string Resolve(string iniFileName)
        {
            string themePath = SafePath.CombineFilePath(ProgramConstants.GetResourcePath(), iniFileName);
            string basePath = SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), iniFileName);

            if (File.Exists(themePath))
                return themePath;

            if (File.Exists(basePath))
                return basePath;

            if (IniFile.FileExists(themePath))
                return themePath;

            if (IniFile.FileExists(basePath))
                return basePath;

            return null;
        }
    }
}
