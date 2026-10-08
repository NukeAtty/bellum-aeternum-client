using System;
using ClientCore;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI
{
    /// <summary>
    /// Handles the optional encrypted asset archive that ships with the client.
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

        /// <summary>
        /// Registers the client asset archive with the <see cref="AssetLoader"/>,
        /// if it exists. Safe to call when no archive is present.
        /// </summary>
        public static void Register()
        {
            encryptionKey ??= Convert.FromBase64String(KEY_BASE64);

            string pakPath = SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), FILE_NAME);
            AssetLoader.RegisterPak(pakPath, encryptionKey);
        }
    }
}
