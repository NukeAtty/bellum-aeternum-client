using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A class for storing the collection of supported CnCNet games.
    /// </summary>
    public class GameCollection
    {
        public List<CnCNetGame> GameList { get; private set; }

        public GameCollection()
        {
            Initialize();
        }

        public void Initialize()
        {
            GameList = new List<CnCNetGame>();

            // The only supported mod.
            var game = new CustomCnCNetGame("beicon.png")
            {
                ChatChannel = "#bellum-aeternum",
                ClientExecutableName = "BellumAeternum.exe",
                GameBroadcastChannel = "#bellum-aeternum-games",
                InternalName = "be",
                RegistryInstallPath = "HKCU\\Software\\BellumAeternum",
                UIName = "Bellum Æternum"
            };
            GameList.Add(game);

            // CnCNet chat.
            var otherGames = new DefaultCnCNetGame[]
            {
                new DefaultCnCNetGame("DTAClient.Icons.cncneticon.png")
                {
                    ChatChannel = "#cncnet",
                    InternalName = "cncnet",
                    UIName = "General CnCNet Chat".L10N("Client:ClientCore:GeneralCnCNetChat"),
                    AlwaysEnabled = true
                }
            };
            GameList.AddRange(otherGames);

            int gameIndex = GetGameIndexFromInternalName(ClientConfiguration.Instance.LocalGame);
            if (gameIndex == -1)
            {
                throw new ClientConfigurationException("Could not find a game in the game collection matching LocalGame value of " +
                    ClientConfiguration.Instance.LocalGame + ".");
            }
            else if (!GameList[gameIndex].Supported)
            {
                throw new ClientConfigurationException("The game specified in LocalGame value of " + ClientConfiguration.Instance.LocalGame +
                    " is marked as not supported.");
            }

            // Fire-and-forget background preloading of images.
            var gamesToPreload = GameList.ToList();
            _ = Task.Run(() =>
            {
                foreach (var game in gamesToPreload)
                    _ = game.Image;
            });
        }

        /// <summary>
        /// Gets the index of a CnCNet supported game based on its internal name.
        /// </summary>
        /// <param name="gameName">The internal name (suffix) of the game.</param>
        /// <returns>The index of the specified CnCNet game. -1 if the game is unknown or not supported.</returns>
        public int GetGameIndexFromInternalName(string gameName)
        {
            for (int gId = 0; gId < GameList.Count; gId++)
            {
                CnCNetGame game = GameList[gId];

                if (gameName.ToLowerInvariant() == game.InternalName)
                    return gId;
            }

            return -1;
        }

        /// <summary>
        /// Seeks the supported game list for a specific game's internal name and if found,
        /// returns the game's full name. Otherwise returns the internal name specified in the param.
        /// </summary>
        /// <param name="gameName">The internal name of the game to seek for.</param>
        /// <returns>The full name of a supported game based on its internal name.
        /// Returns the given parameter if the name isn't found in the supported game list.</returns>
        public string GetGameNameFromInternalName(string gameName)
        {
            CnCNetGame game = GameList.Find(g => g.InternalName == gameName.ToLowerInvariant());

            if (game == null)
                return gameName;

            return game.UIName;
        }

        /// <summary>
        /// Returns the full UI name of a game based on its index in the game list.
        /// </summary>
        /// <param name="gameIndex">The index of the CnCNet supported game.</param>
        /// <returns>The UI name of the game.</returns>
        public string GetFullGameNameFromIndex(int gameIndex)
        {
            return GameList[gameIndex].UIName;
        }

        /// <summary>
        /// Returns the internal name of a game based on its index in the game list.
        /// </summary>
        /// <param name="gameIndex">The index of the CnCNet supported game.</param>
        /// <returns>The internal name (suffix) of the game.</returns>
        public string GetGameIdentifierFromIndex(int gameIndex)
        {
            return GameList[gameIndex].InternalName;
        }

        public string GetGameBroadcastingChannelNameFromIdentifier(string gameIdentifier)
        {
            CnCNetGame game = GameList.Find(g => g.InternalName == gameIdentifier.ToLowerInvariant());
            if (game == null)
                return null;
            return game.GameBroadcastChannel;
        }

        public string GetGameChatChannelNameFromIdentifier(string gameIdentifier)
        {
            CnCNetGame game = GameList.Find(g => g.InternalName == gameIdentifier.ToLowerInvariant());
            if (game == null)
                return null;
            return game.ChatChannel;
        }
    }

    /// <summary>
    /// An exception that is thrown when configuration for a game to add to game collection
    /// contains invalid or unexpected settings / data or required settings / data are missing.
    /// </summary>
    class GameCollectionConfigurationException : Exception
    {
        public GameCollectionConfigurationException(string message) : base(message)
        {
        }
    }
}
