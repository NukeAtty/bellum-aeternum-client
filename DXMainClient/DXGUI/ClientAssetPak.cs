using System;
using System.Collections.Generic;
using System.IO;
using ClientCore;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI
{
    /// <summary>
    /// Handles the optional encrypted asset archive that ships with the client.
    /// Besides textures and sounds, the archive can also store client-side INI
    /// files (theme and window layouts, game options). Those are exposed to the
    /// <see cref="IniFile"/> parser through <see cref="IniFile.PackedContentProvider"/>.
    /// </summary>
    internal static class ClientAssetPak
    {
        /// <summary>
        /// The name of the archive file inside the base resource (Resources) directory.
        /// </summary>
        public const string FILE_NAME = "clientassets.pak";

        /// <summary>
        /// The AES-256 key used to decrypt the archive. Must match the key used by
        /// Scripts/CreateAssetPak.ps1 when the archive is generated.
        /// </summary>
        private const string KEY_BASE64 = "arU5uT8l1LEDPTrab1qegVAluXwh2zxd1Y33kwQ9dTw=";

        private static byte[] encryptionKey;
        private static AssetPak pak;
        private static bool pakInitialized;
        private static bool iniProviderRegistered;

        /// <summary>
        /// Opens the archive (if present) and makes its INI files available to the
        /// INI parser as early as possible. Can be called before the AssetLoader is
        /// initialized; safe to call multiple times.
        /// </summary>
        public static void RegisterEarly()
        {
            EnsurePak();
            RegisterIniProvider();
        }

        /// <summary>
        /// Registers the client asset archive with the <see cref="AssetLoader"/>
        /// so that textures and sounds can be loaded from it. Must be called after
        /// the AssetLoader has been initialized.
        /// </summary>
        public static void Register()
        {
            EnsurePak();

            if (pak != null)
                AssetLoader.RegisterPak(pak);

            RegisterIniProvider();
        }

        /// <summary>
        /// Decrypts and returns the content of the archived file that corresponds
        /// to the given full file path.
        /// </summary>
        /// <param name="path">The full path of the requested file.</param>
        /// <param name="data">The decrypted content on success, otherwise null.</param>
        /// <returns>true if the file was found in the archive, otherwise false.</returns>
        public static bool TryReadFile(string path, out byte[] data)
        {
            data = null;

            if (pak == null || string.IsNullOrEmpty(path))
                return false;

            foreach (string candidate in GetEntryCandidates(path))
            {
                if (pak.TryGetEntryData(candidate, out data))
                    return true;
            }

            return false;
        }

        private static void EnsurePak()
        {
            if (pakInitialized)
                return;

            pakInitialized = true;
            encryptionKey ??= Convert.FromBase64String(KEY_BASE64);

            string pakPath = SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), FILE_NAME);

            if (!File.Exists(pakPath))
            {
                Logger.Log("ClientAssetPak: archive not found: " + pakPath);
                return;
            }

            try
            {
                pak = new AssetPak(pakPath, encryptionKey);
                Logger.Log($"ClientAssetPak: opened {pakPath} with {pak.EntryCount} entries");
            }
            catch (Exception ex)
            {
                Logger.Log("ClientAssetPak: failed to open " + pakPath + "! Message: " + ex.Message);
            }
        }

        private static void RegisterIniProvider()
        {
            if (iniProviderRegistered)
                return;

            iniProviderRegistered = true;

            if (pak == null)
                return;

            IniFile.PackedContentProvider = path => TryReadFile(path, out byte[] data) ? data : null;
            Logger.Log("ClientAssetPak: INI content provider registered.");
        }

        /// <summary>
        /// Yields the archive entry names that may correspond to the given full file
        /// path: the path itself, the bare file name, and the path relative to the
        /// theme and base resource directories.
        /// </summary>
        private static IEnumerable<string> GetEntryCandidates(string path)
        {
            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch
            {
                yield break;
            }

            yield return fullPath;
            yield return Path.GetFileName(fullPath);

            if (TryGetRelative(fullPath, ProgramConstants.GetResourcePath(), out string themeRelative))
                yield return themeRelative;

            if (TryGetRelative(fullPath, ProgramConstants.GetBaseResourcePath(), out string baseRelative))
                yield return baseRelative;
        }

        private static bool TryGetRelative(string fullPath, string directory, out string relative)
        {
            relative = null;

            if (string.IsNullOrEmpty(directory))
                return false;

            string directoryFull;

            try
            {
                directoryFull = Path.GetFullPath(directory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
            }
            catch
            {
                return false;
            }

            if (!fullPath.StartsWith(directoryFull, StringComparison.OrdinalIgnoreCase))
                return false;

            relative = fullPath.Substring(directoryFull.Length).Replace('\\', '/');
            return true;
        }
    }
}
