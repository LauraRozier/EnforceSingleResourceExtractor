/* --- Contributor information ---
 * Please follow the following set of guidelines when working on this plugin,
 * this to help others understand this file more easily.
 * 
 * NOTE: On Authors, new entries go BELOW the existing entries. As with any other software header comment.
 *
 * -- Authors --
 * Thimo (ThibmoRozier) <thibmorozier@live.nl> 2021-03-15 +
 *
 * -- Naming --
 * Avoid using non-alphabetic characters, eg: _
 * Avoid using numbers in method and class names (Upgrade methods are allowed to have these, for readability)
 * Private constants -------------------- SHOULD start with a uppercase "C" (PascalCase)
 * Private readonly fields -------------- SHOULD start with a uppercase "C" (PascalCase)
 * Private fields ----------------------- SHOULD start with a uppercase "F" (PascalCase)
 * Arguments/Parameters ----------------- SHOULD start with a lowercase "a" (camelCase)
 * Classes ------------------------------ SHOULD start with a uppercase character (PascalCase)
 * Methods ------------------------------ SHOULD start with a uppercase character (PascalCase)
 * Public properties (constants/fields) - SHOULD start with a uppercase character (PascalCase)
 * Variables ---------------------------- SHOULD start with a lowercase character (camelCase)
 *
 * -- Style --
 * Max-line-width ------- 160
 * Single-line comments - // Single-line comment
 * Multi-line comments -- Just like this comment block!
 */
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("EnforceSingleResourceExtractor", "ThibmoRozier", "1.0.0")]
    [Description("Enforce players only being able to use a single quarry and/or pump jack.")]
    public class EnforceSingleResourceExtractor : RustPlugin
    {
        #region Types
        private enum ExtractorType
        {
            Invalid,
            PumpJack,
            Quarry
        }

        private struct QuarryState
        {
            public ulong PlayerId;
            public uint ExtractorId;
            public ExtractorType Type;

            public QuarryState(ulong aPlayerId, uint aExtractorId, ExtractorType aType)
            {
                PlayerId = aPlayerId;
                ExtractorId = aExtractorId;
                Type = aType;
            }
        }
        #endregion Types

        #region Constants
        // Not sure what the placable prefab is called, just playing safe
        private static readonly string[] PumpJackPrefabs = { "pumpjack", "pump_jack", "pump-jack", "pumpjack-static" };
        private static readonly string[] QuarryPrefabs = { "mining_quarry", "miningquarry_static" };
        #endregion Constants

        #region Variables
        private ConfigData FConfigData;
        private Timer FCleanupTimer;
        private readonly List<QuarryState> FPlayerExtractorList = new List<QuarryState>();
        #endregion Variables

        #region Config
        /// <summary>
        /// The config type class
        /// </summary>
        private class ConfigData
        {
            [DefaultValue(true)]
            [JsonProperty("Ignore Extractor Type", DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool IgnoreExtractorType { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try {
                FConfigData = Config.ReadObject<ConfigData>();

                if (FConfigData == null)
                    LoadDefaultConfig();
            } catch {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            FConfigData = new ConfigData {
                IgnoreExtractorType = true
            };
        }

        protected override void SaveConfig() => Config.WriteObject(FConfigData);
        #endregion Config

        #region Script Methods
        private void CheckExtractorIsOff()
        {
            foreach (BaseNetworkable extractor in BaseNetworkable.serverEntities.Where(x => FPlayerExtractorList.Exists(y => x.net.ID == y.ExtractorId))) {
                // Skip anything we don't care about and if the enine is still running
                if (
                    !(PumpJackPrefabs.Contains(extractor.ShortPrefabName) || QuarryPrefabs.Contains(extractor.ShortPrefabName)) ||
                    (extractor as MiningQuarry).IsEngineOn()
                )
                    continue;

                FPlayerExtractorList.RemoveAll(x => extractor.net.ID == x.ExtractorId);
            }
        }

        private void Enforce(MiningQuarry aExtractor, BasePlayer aPlayer)
        {
            // Turn engine OFF
            aExtractor.EngineSwitch(false);
            // Warn the player
            aPlayer.ChatMessage(lang.GetMessage("Warning Message Text", this, aPlayer.UserIDString));
        }
        #endregion Script Methods

        #region Hooks
        void Loaded()
        { 
            LoadConfig();
            FCleanupTimer = timer.Every(1f, CheckExtractorIsOff);
        }

        void Unload()
        {
            if (FCleanupTimer != null && !FCleanupTimer.Destroyed)
                FCleanupTimer.Destroy();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(
                new Dictionary<string, string> {
                    { "Warning Message Text", "<color=#FF7900>You can only run a single resource extractor at any given time.</color>" }
                }, this, "en"
            );
        }

        void OnQuarryToggled(MiningQuarry aExtractor, BasePlayer aPlayer)
        {
            ExtractorType type = ExtractorType.Invalid;

            if (PumpJackPrefabs.Contains(aExtractor.ShortPrefabName)) {
                type = ExtractorType.PumpJack;
            } else if (QuarryPrefabs.Contains(aExtractor.ShortPrefabName)) {
                type = ExtractorType.Quarry;
            }

            // Skip anything we don't care about
            if (type == ExtractorType.Invalid)
                return;

            bool engineState = aExtractor.IsEngineOn();

            // Extractor was turned off, don't care about player ID, just remove
            if (!engineState) {
                FPlayerExtractorList.RemoveAll(x => x.ExtractorId == aExtractor.net.ID);
                return;
            }

            IEnumerable<QuarryState> states = FPlayerExtractorList.Where(x => aPlayer.userID == x.PlayerId);

            if (states.Count(x => FConfigData.IgnoreExtractorType || type == x.Type) > 0) {
                Enforce(aExtractor, aPlayer);
                return;
            }

            FPlayerExtractorList.Add(new QuarryState(aPlayer.userID, aExtractor.net.ID, type));
        }
        #endregion Hooks
    }
}