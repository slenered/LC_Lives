using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using LCVR;

namespace LC_Lives;

public class Configs : SyncedConfig2<Configs> {
	
	[field: SyncedEntryField] public SyncedEntry<int> PartySize { get; private set; }
	[field: SyncedEntryField] public SyncedEntry<int> GlobalLives { get; private set; }
	[field: SyncedEntryField] public SyncedEntry<int> PlayerLives { get; private set; }
	[field: SyncedEntryField] public SyncedEntry<float> RespawnTimeSeconds { get; private set; }
	[field: SyncedEntryField] public SyncedEntry<bool> ReviveOnBodyCollectedBool { get; private set; }
	[field: SyncedEntryField] public SyncedEntry<bool> PreventShipFromLeaving { get; private set; }

	public Configs(ConfigFile configFile) : base(MyPluginInfo.PLUGIN_GUID) {
		PartySize = configFile.BindSyncedEntry("General", "Party Size", 4,
			"The number lives that the team shares. (Each player consumes a life by landing)");
		GlobalLives = configFile.BindSyncedEntry("General", "Global Lives", 0, "Additional lives that the team shares.");
		PlayerLives = configFile.BindSyncedEntry("General", "Player Lives", 0, "The number lives that each player has.");
		ReviveOnBodyCollectedBool = configFile.BindSyncedEntry("General", "Revive on body collection", true,
			"Force revive a player when their body is returned to the ship.");
		RespawnTimeSeconds = configFile.BindSyncedEntry("General", "Respawn Timer (Seconds)", 30f,
			"The amount of time before respawning a player.");
		PreventShipFromLeaving = configFile.BindSyncedEntry("General", "Prevent Ship From Leaving", false,
			"Stop the ship from leaving if there are lives left.");
		ConfigManager.Register(this);
		
		PartySize.Changed += (_, args) => {
			Logger.LogInfo($"(NOW) PartySize: {args.OldValue} -> {args.NewValue}");
		};
		GlobalLives.Changed += (_, args) => {
			Logger.LogInfo($"(NOW) GlobalLives: {args.OldValue} -> {args.NewValue}");
		};
		PlayerLives.Changed += (_, args) => {
			Logger.LogInfo($"(NOW) PlayerLives: {args.OldValue} -> {args.NewValue}");
		};
		PreventShipFromLeaving.Changed += (_, args) => {
			Logger.LogInfo($"(NOW) PreventShipFromLeaving: {args.OldValue} -> {args.NewValue}");
		};
		RespawnTimeSeconds.Changed += (_, args) => {
			if (args.NewValue > 0) {
				Logger.LogInfo("(NOW) On Timer!");
				LC_Lives.RevivePlayerMetric.RevivePlayerSystem.AddReviveCondition(new LC_Lives.RevivePlayerMetric.ReviveOnTimer());
			}
			else {
				Logger.LogInfo("(NOW) Off on Timer!");
				LC_Lives.RevivePlayerMetric.RevivePlayerSystem.RemoveReviveCondition(new LC_Lives.RevivePlayerMetric.ReviveOnTimer());
			}
		};
		ReviveOnBodyCollectedBool.Changed += (_, args) => {
			if (args.NewValue) {
				Logger.LogInfo("(NOW) On Body Collect!");
				LC_Lives.RevivePlayerMetric.RevivePlayerSystem.AddReviveCondition(
					new LC_Lives.RevivePlayerMetric.ReviveOnBodyCollected());
			}
			else {
				Logger.LogInfo("(NOW) Off on Body Collect!");
				LC_Lives.RevivePlayerMetric.RevivePlayerSystem.RemoveReviveCondition(
					new LC_Lives.RevivePlayerMetric.ReviveOnBodyCollected());
			}
		};
	} 
}

