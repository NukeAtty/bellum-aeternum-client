using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain
{
    /// <summary>
    /// Stores the campaign medal progress of the local player in an encrypted and
    /// integrity-protected file under the Client directory.
    /// <para/>
    /// This is obfuscation-grade protection only: the key is embedded in the
    /// executable, so it protects against casual editing, not against a determined
    /// reverse engineer. Any manual modification breaks the HMAC, which makes the
    /// client discard the file (resetting progress) instead of granting medals.
    /// </summary>
    public static class CampaignAchievements
    {
        public const int BRONZE = 1;
        public const int SILVER = 2;
        public const int GOLD = 3;

        private const string FILE_NAME = "CampaignMedals.dat";
        private const string MAGIC = "CAMEDA01";
        private const int IV_LENGTH = 16;
        private const int HMAC_LENGTH = 32;

        // Obfuscation-grade key. Keep private and do not reuse for anything else.
        private const string KEY_BASE64 = "Y3OL635CXNgUzlCbuPfPMsswGjrtRSRPQItFufqqnqI=";

        private static readonly object Locker = new object();
        private static Dictionary<string, int> medals;
        private static bool loaded;

        /// <summary>
        /// Gets the highest unlocked medal level (0 = none, 1 = bronze, 2 = silver,
        /// 3 = gold) for the given mission.
        /// </summary>
        public static int GetMedalLevel(Mission mission) => GetMedalLevel(GetMissionKey(mission));

        public static int GetMedalLevel(string missionKey)
        {
            if (string.IsNullOrEmpty(missionKey))
                return 0;

            lock (Locker)
            {
                EnsureLoaded();
                return medals.TryGetValue(missionKey, out int level) ? level : 0;
            }
        }

        /// <summary>
        /// Raises the mission's medal level to at least <paramref name="level"/>.
        /// Levels never decrease. Returns true if the stored level actually increased.
        /// </summary>
        public static bool Unlock(Mission mission, int level) => Unlock(GetMissionKey(mission), level);

        public static bool Unlock(string missionKey, int level)
        {
            if (string.IsNullOrEmpty(missionKey) || level <= 0)
                return false;

            if (level > GOLD)
                level = GOLD;

            lock (Locker)
            {
                EnsureLoaded();

                if (medals.TryGetValue(missionKey, out int existing) && existing >= level)
                    return false;

                medals[missionKey] = level;
                Save();
                return true;
            }
        }

        /// <summary>
        /// Builds a stable key for a mission based on its map file name. Missions
        /// without a scenario fall back to their code name.
        /// </summary>
        public static string GetMissionKey(Mission mission)
        {
            if (mission == null)
                return string.Empty;

            string scenario = mission.Scenario;
            if (!string.IsNullOrEmpty(scenario))
            {
                string fileName = Path.GetFileName(scenario);
                if (!string.IsNullOrEmpty(fileName))
                    return fileName.ToUpperInvariant();
            }

            return mission.CodeName?.ToUpperInvariant() ?? string.Empty;
        }

        private static void EnsureLoaded()
        {
            if (loaded)
                return;

            loaded = true;
            medals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                string path = GetFilePath();
                if (!File.Exists(path))
                    return;

                byte[] plain = Unprotect(File.ReadAllBytes(path));
                if (plain == null)
                {
                    Logger.Log("CampaignAchievements: medal file failed the integrity check; ignoring it.");
                    return;
                }

                Parse(Encoding.UTF8.GetString(plain));
            }
            catch (Exception ex)
            {
                Logger.Log("CampaignAchievements: failed to load the medal file: " + ex.Message);
            }
        }

        private static void Parse(string content)
        {
            foreach (string line in content.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0)
                    continue;

                int separator = trimmed.LastIndexOf('=');
                if (separator <= 0)
                    continue;

                string key = trimmed.Substring(0, separator).Trim().ToUpperInvariant();
                if (int.TryParse(trimmed.Substring(separator + 1).Trim(), out int level) && level > 0)
                    medals[key] = Math.Min(level, GOLD);
            }
        }

        private static void Save()
        {
            try
            {
                var builder = new StringBuilder();
                foreach (var kvp in medals)
                    builder.Append(kvp.Key).Append('=').Append(kvp.Value).Append('\n');

                byte[] protectedData = Protect(Encoding.UTF8.GetBytes(builder.ToString()));

                string path = GetFilePath();
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(path, protectedData);
            }
            catch (Exception ex)
            {
                Logger.Log("CampaignAchievements: failed to save the medal file: " + ex.Message);
            }
        }

        private static string GetFilePath() =>
            SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, FILE_NAME);

        // File layout: [MAGIC:8][IV:16][CIPHERTEXT:N][HMAC:32]
        // The HMAC-SHA256 covers MAGIC + IV + CIPHERTEXT with a derived MAC key.
        private static byte[] Protect(byte[] plain)
        {
            byte[] baseKey = Convert.FromBase64String(KEY_BASE64);
            byte[] encKey = DeriveKey(baseKey, "enc");
            byte[] macKey = DeriveKey(baseKey, "mac");

            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = encKey;
            aes.GenerateIV();
            byte[] iv = aes.IV;

            byte[] cipher;
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
                cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

            byte[] magic = Encoding.ASCII.GetBytes(MAGIC);
            byte[] body = new byte[magic.Length + iv.Length + cipher.Length];
            Buffer.BlockCopy(magic, 0, body, 0, magic.Length);
            Buffer.BlockCopy(iv, 0, body, magic.Length, iv.Length);
            Buffer.BlockCopy(cipher, 0, body, magic.Length + iv.Length, cipher.Length);

            using HMACSHA256 hmac = new HMACSHA256(macKey);
            byte[] mac = hmac.ComputeHash(body);

            byte[] result = new byte[body.Length + mac.Length];
            Buffer.BlockCopy(body, 0, result, 0, body.Length);
            Buffer.BlockCopy(mac, 0, result, body.Length, mac.Length);
            return result;
        }

        private static byte[] Unprotect(byte[] raw)
        {
            int magicLength = MAGIC.Length;
            if (raw.Length < magicLength + IV_LENGTH + HMAC_LENGTH + 1)
                return null;

            if (Encoding.ASCII.GetString(raw, 0, magicLength) != MAGIC)
                return null;

            byte[] baseKey = Convert.FromBase64String(KEY_BASE64);
            byte[] encKey = DeriveKey(baseKey, "enc");
            byte[] macKey = DeriveKey(baseKey, "mac");

            int bodyLength = raw.Length - HMAC_LENGTH;
            byte[] body = new byte[bodyLength];
            Buffer.BlockCopy(raw, 0, body, 0, bodyLength);

            byte[] expectedMac = new byte[HMAC_LENGTH];
            Buffer.BlockCopy(raw, bodyLength, expectedMac, 0, HMAC_LENGTH);

            using (HMACSHA256 hmac = new HMACSHA256(macKey))
            {
                if (!FixedTimeEquals(hmac.ComputeHash(body), expectedMac))
                    return null;
            }

            byte[] iv = new byte[IV_LENGTH];
            Buffer.BlockCopy(raw, magicLength, iv, 0, IV_LENGTH);

            int cipherLength = bodyLength - magicLength - IV_LENGTH;
            if (cipherLength <= 0)
                return null;

            byte[] cipher = new byte[cipherLength];
            Buffer.BlockCopy(raw, magicLength + IV_LENGTH, cipher, 0, cipherLength);

            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = encKey;
            aes.IV = iv;

            try
            {
                using ICryptoTransform decryptor = aes.CreateDecryptor();
                return decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        private static byte[] DeriveKey(byte[] baseKey, string purpose)
        {
            using HMACSHA256 hmac = new HMACSHA256(baseKey);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes("CampaignAchievements:" + purpose));
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];

            return diff == 0;
        }
    }
}
