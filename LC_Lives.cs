using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Patches.Spectating;
using LCVR.Player;
using UnityEngine;
using Object = object;

// using LobbyCompatibility.Attributes;
// using LobbyCompatibility.Enums;

namespace LC_Lives;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("io.daxcess.lcvr", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.sigurd.csync", "5.0.1")]
// ReSharper disable once InconsistentNaming
public class LC_Lives : BaseUnityPlugin {

	public bool inVR;
	private static LC_Lives Instance { get; set; } = null!;
	private new static ManualLogSource Logger { get; set; } = null!;
	private static Harmony? Harmony { get; set; }
	private int GlobalLivesLeft { get; set; }

	private static Configs _config = null!;
	private static readonly int LimpAnimator = Animator.StringToHash("Limp");
	private static readonly int DeadAnimator = Animator.StringToHash("dead");
	private static readonly int GasEmittingAnimator = Animator.StringToHash("gasEmitting");
	private static readonly int ReviveAnimator = Animator.StringToHash("revive");

	private void Awake() {
		Logger = base.Logger;
		Instance = this;
		Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

		
		if(Chainloader.PluginInfos.ContainsKey("io.daxcess.lcvr")) {
			LCVRCompatibility();
		}
		else {
			Logger.LogInfo("LCVR was not found. Skipping...");
		}
		_config = new Configs(Config);
		
		_config.InitialSyncCompleted += (_, _) => {
			Logger.LogInfo("Initial sync complete!"); 
			if (_config.ReviveOnBodyCollectedBool.Value) {
				Logger.LogInfo("On Body Collect!");
				RevivePlayerMetric.RevivePlayerSystem.AddReviveCondition(new RevivePlayerMetric.ReviveOnBodyCollected());
			}

			if (_config.RespawnTimeSeconds.Value > 0) {
				Logger.LogInfo("On Timer!");
				RevivePlayerMetric.RevivePlayerSystem.AddReviveCondition(new RevivePlayerMetric.ReviveOnTimer());
			}
		};
		
		try {
			Harmony.PatchAll(typeof(RevivePlayerMetric.RevivePlayerSystem));
		}
		catch (Exception ex) {
			Logger.LogError("Failed to patch Revive System; '" + ex.Message + "'\n" + ex.StackTrace);
		}

		Logger.LogInfo($"PartySize: {_config.PartySize.Value}");
		Logger.LogInfo($"GlobalLives: {_config.GlobalLives.Value}");
		Logger.LogInfo($"PlayerLives: {_config.PlayerLives.Value}");
		Logger.LogInfo($"ReviveOnBodyCollectedBool: {_config.ReviveOnBodyCollectedBool.Value}");
		Logger.LogInfo($"RespawnTimeSeconds: {_config.RespawnTimeSeconds.Value}");
		Logger.LogInfo($"PreventShipFromLeaving: {_config.PreventShipFromLeaving.Value}");
		Logger.LogInfo("------------------------------------------");
		Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
	}
	
	// ReSharper disable once InconsistentNaming
	private void LCVRCompatibility() {
		Logger.LogInfo("LCVR Found!");
		// LCVR
		inVR = VRSession.InVR;
		
	}


	internal class RevivePlayerMetric {
		private PlayerControllerB Player { get; }
		private int LivesLeft { get; set; }

		private RevivePlayerMetric(PlayerControllerB player, int revives) {
			Player = player;
			LivesLeft = revives;
		}

		private bool CanRevive() {
			return LivesLeft + Instance.GlobalLivesLeft > 0;
		}

		private int GetPlayerIndex() {
			StartOfRound instance = StartOfRound.Instance;
			if ((Object)instance == null) {
				throw new NullReferenceException("Start of round is null");
			}

			for (int i = 0; i < instance.allPlayerScripts.Length; i++) {
				PlayerControllerB val = instance.allPlayerScripts[i];
				if (val.actualClientId == Player.actualClientId && (val.actualClientId != 0L || val.isHostPlayerObject)) {
					return i;
				}
			}

			throw new ArgumentException("Unknown Player Index: " + Player);
		}

		private void Revive(Vector3 revivePos) {
			if (!CanRevive()) {
				return;
			}
			StartOfRound instance = StartOfRound.Instance;

			if (Instance.inVR) {
				ReviveVRPlayer();
				// instance.ReviveDeadPlayers();
			}

			// Logger.LogInfo($"Reviving player {Player.actualClientId}");
			int playerIndex = GetPlayerIndex();
			instance.livingPlayers++;
			Player.isClimbingLadder = false;
			Player.clampLooking = false;
			Player.inVehicleAnimation = false;
			Player.disableMoveInput = false;
			Player.ResetZAndXRotation();
			Player.thisController.enabled = true;
			Player.health = 100;
			Player.hasBeenCriticallyInjured = false;
			Player.disableLookInput = false;
			Player.disableInteract = false;
			Player.isPlayerDead = false;
			Player.isPlayerControlled = true;
			Player.isInElevator = true;
			Player.isInHangarShipRoom = true;
			Player.isInsideFactory = false;
			Player.parentedToElevatorLastFrame = false;
			Player.overrideGameOverSpectatePivot = null;
			instance.SetPlayerObjectExtrapolate(false);
			Player.TeleportPlayer(revivePos);
			Player.setPositionOfDeadPlayer = false;
			Player.DisablePlayerModel(instance.allPlayerObjects[playerIndex], true, true);
			Player.helmetLight.enabled = false;
			Player.Crouch(false);
			Player.criticallyInjured = false;
			if (Player.playerBodyAnimator != null)
				Player.playerBodyAnimator.SetBool(LimpAnimator, false);
			Player.bleedingHeavily = false;
			Player.activatingItem = false;
			Player.twoHanded = false;
			Player.inShockingMinigame = false;
			Player.inSpecialInteractAnimation = false;
			Player.freeRotationInInteractAnimation = false;
			Player.disableSyncInAnimation = false;
			Player.inAnimationWithEnemy = null;
			Player.holdingWalkieTalkie = false;
			Player.speakingToWalkieTalkie = false;
			Player.isSinking = false;
			Player.isUnderwater = false;
			Player.sinkingValue = 0.0f;
			Player.statusEffectAudio.Stop();
			Player.DisableJetpackControlsLocally();
			Player.mapRadarDotAnimator.SetBool(DeadAnimator, false);
			Player.externalForceAutoFade = Vector3.zero;
			if (Player.IsOwner) {
				HUDManager.Instance.gasHelmetAnimator.SetBool(GasEmittingAnimator, false);
				Player.hasBegunSpectating = false;
				HUDManager.Instance.RemoveSpectateUI();
				HUDManager.Instance.gameOverAnimator.SetTrigger(ReviveAnimator);
				Player.hinderedMultiplier = 1f;
				Player.isMovementHindered = 0;
				Player.sourcesCausingSinking = 0;
				Player.reverbPreset = instance.shipReverb;
				HUDManager.Instance.UpdateHealthUI(100, false);
				HUDManager.Instance.audioListenerLowPass.enabled = false;
				HUDManager.Instance.HideHUD(false);
			}
			SoundManager.Instance.earsRingingTimer = 0.0f;
			Player.voiceMuffledByEnemy = false;
			SoundManager.Instance.playerVoicePitchTargets[playerIndex] = 1f;
			SoundManager.Instance.SetPlayerPitch(1f, playerIndex);
			if (Player.currentVoiceChatIngameSettings == null)
				instance.RefreshPlayerVoicePlaybackObjects();
			if (Player.currentVoiceChatIngameSettings != null) {
				if (Player.currentVoiceChatIngameSettings.voiceAudio == null)
					Player.currentVoiceChatIngameSettings.InitializeComponents();
				if (Player.currentVoiceChatIngameSettings.voiceAudio == null)
					return;
				Player.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
			}

			Player.spectatedPlayerScript = null;
			instance.SetSpectateCameraToGameOverMode(false, Player);
			instance.UpdatePlayerVoiceEffects();

			Logger.LogInfo($"LivesLeft: {LivesLeft} | GlobalLivesLeft: {Instance.GlobalLivesLeft}");
			if (LivesLeft > 0) {
				int livesLeft = LivesLeft - 1;
				LivesLeft = livesLeft;
				if (HUDManager.Instance && Player.IsOwner)
					HUDManager.Instance.DisplayTip("Lives", $"You have {LivesLeft} live(s) left.", LivesLeft <= 1);
			}
			else {
				int livesLeft = Instance.GlobalLivesLeft - 1;
				Instance.GlobalLivesLeft = livesLeft;
				if (HUDManager.Instance)
					HUDManager.Instance.DisplayTip("Lives", $"There are {Instance.GlobalLivesLeft} team live(s) left.",
						Instance.GlobalLivesLeft <= 1);
			}
		}

		private void ReviveVRPlayer() {
			SpectatorPlayerPatches.OnPlayerRevived(); //running a private statement with publicizer
		}

		// [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ReviveDeadPlayers))]
		// [HarmonyAfter("LCVR.Patches.Spectating.SpectatorPlayerPatches.OnPlayerRevived")]
		// private static bool IsRoundOver() {// Workaround OnPlayerRevived() being private.
		// 	// Logger.LogInfo("Is Round Over?");
		// 	StartOfRound instance = StartOfRound.Instance;
		// 	if (instance.shipIsLeaving)
		// 		return true;
		// 	return false;
		// }

		internal static class RevivePlayerSystem {
			private static readonly List<IReviveCondition> SReviveActions = new List<IReviveCondition>();

			private static readonly Dictionary<ulong, RevivePlayerMetric> SPlayerMetrics =
				new Dictionary<ulong, RevivePlayerMetric>();

			public static void AddReviveCondition(IReviveCondition condition) {
				SReviveActions.Add(condition);
			}
			public static void RemoveReviveCondition(IReviveCondition condition) {
				SReviveActions.Remove(condition);
			}

			public static void ClearReviveActions() {
				SReviveActions.Clear();
			}

			[HarmonyPatch(typeof(StartOfRound), "ShipLeaveAutomatically")]
			[HarmonyPrefix]
			// ReSharper disable once InconsistentNaming
			private static bool ShipLeaveAutomaticallyPatch(StartOfRound __instance) {
				bool livesRemain = false;
				foreach (RevivePlayerMetric playerMetric in SPlayerMetrics.Values) {
					if (playerMetric.CanRevive()) {
						livesRemain = true;
						break;
					}
				}

				// Logger.LogInfo(TimeOfDay.Instance.totalTime);
				// Logger.LogInfo(TimeOfDay.Instance.globalTime);
				// Logger.LogInfo(TimeOfDay.Instance.shipLeavingAlertCalled);
				Logger.LogInfo(TimeOfDay.Instance.shipLeavingOnMidnight);
				// Logger.LogInfo(TimeOfDay.Instance.shipLeaveAutomaticallyTime);
				if (_config.PreventShipFromLeaving.Value && livesRemain && !(TimeOfDay.Instance.shipLeavingOnMidnight)) {
					__instance.allPlayersDead = false;
					return false;
				}

				// __instance.allPlayersDead = true;
				return true;
			}

			[HarmonyPatch(typeof(RoundManager), "FinishGeneratingNewLevelClientRpc")]
			[HarmonyPrefix]
			private static void ResetMetricsOnRoundStart() {
				Logger.LogError("Round Manager Finished Level Generation!");
				StartOfRound instance = StartOfRound.Instance;
				SPlayerMetrics.Clear();
				if ((Object)instance == null) {
					Logger.LogError("Start of Round is null at 'FinishGeneratingNewLevelClientRpc'");
					return;
				}

				SReviveActions.ForEach(delegate(IReviveCondition x) { x.Reset(); });
				int value = _config.PlayerLives.Value;
				PlayerControllerB[] allPlayerScripts = instance.allPlayerScripts;
				foreach (PlayerControllerB val in allPlayerScripts) {
					ulong actualClientId = val.actualClientId;
					string playerUsername = val.playerUsername;
					if (actualClientId == 0L && !val.isHostPlayerObject) {
						break;
					}

					Logger.LogInfo(
						$"Starting Revive Tracking for: '{actualClientId}' => '{playerUsername}'");
					SPlayerMetrics.Add(val.actualClientId, new RevivePlayerMetric(val, value));
				}

				Instance.GlobalLivesLeft = _config.GlobalLives.Value +
				                           Math.Max(_config.PartySize.Value - instance.livingPlayers, 0);
				if (HUDManager.Instance)
					HUDManager.Instance.DisplayTip("Lives",
						$"{Instance.GlobalLivesLeft} Global lives,\n{value} Personal lives.");
			}

			[HarmonyPatch(typeof(StartOfRound), "Update")]
			[HarmonyPrefix]
			// ReSharper disable once InconsistentNaming
			private static void TryRevivePlayers(ref StartOfRound __instance) {
				if (!__instance.shipHasLanded || __instance.inShipPhase) {
					return;
				}

				if (SPlayerMetrics.Count <= 0 || SReviveActions.Count <= 0) {
					Logger.LogInfo("No revive actions or players available to revive");
					return;
				}

				foreach (KeyValuePair<ulong, RevivePlayerMetric> sPlayerMetric in SPlayerMetrics) {
					sPlayerMetric.Deconstruct(out var key, out var value);
					ulong num = key;
					RevivePlayerMetric revivePlayerMetric = value;
					PlayerControllerB player = revivePlayerMetric.Player;
					if (!player.isPlayerDead || player is { actualClientId: 0L, isHostPlayerObject: false }) {
						continue;
					}

					foreach (IReviveCondition sReviveAction in SReviveActions) {
						if (revivePlayerMetric.CanRevive() && sReviveAction.ShouldRevivePlayer(revivePlayerMetric)) {
							SReviveActions.ForEach(delegate(IReviveCondition x) { x.SoftReset(revivePlayerMetric); });
							Logger.LogInfo($"Reviving Player '{num}' => '{player.playerUsername}'");
							revivePlayerMetric.Revive(sReviveAction.GetRevivePosition(revivePlayerMetric));
						}
					}
				}
			}
		}


		internal class ReviveOnBodyCollected : IReviveCondition {
			private readonly Dictionary<ulong, float> _mTimers = new Dictionary<ulong, float>();

			public void Reset() {
				_mTimers.Clear();
			}

			public void SoftReset(RevivePlayerMetric metric) {
				ulong actualClientId = metric.Player.actualClientId;
				_mTimers[actualClientId] = 0f;
			}

			public bool ShouldRevivePlayer(RevivePlayerMetric metric) {
				if (_config.ReviveOnBodyCollectedBool.Value && metric.Player.isPlayerDead) {
					ulong actualClientId = metric.Player.actualClientId;
					float value = 5;
					if (!_mTimers.TryGetValue(actualClientId, out var value2)) {
						_mTimers.Add(actualClientId, 0f);
						return false;
					}

					_mTimers[actualClientId] = value2 + Time.deltaTime;
					value2 = _mTimers[actualClientId];
					if (value2 > value) {
						_mTimers[actualClientId] = 0f;
						return metric.Player.deadBody.isInShip;
					}
				}

				return false;
			}
		}

		internal class ReviveOnTimer : IReviveCondition {
			private readonly Dictionary<ulong, float> _mTimers = new Dictionary<ulong, float>();

			public void Reset() {
				_mTimers.Clear();
			}

			public void SoftReset(RevivePlayerMetric metric) {
				ulong actualClientId = metric.Player.actualClientId;
				_mTimers[actualClientId] = 0f;
			}

			public bool ShouldRevivePlayer(RevivePlayerMetric metric) {
				if (!metric.Player.isPlayerDead) {
					return false;
				}

				ulong actualClientId = metric.Player.actualClientId;
				float value = _config.RespawnTimeSeconds.Value;
				if (!_mTimers.TryGetValue(actualClientId, out var value2)) {
					_mTimers.Add(actualClientId, 0f);
					return false;
				}

				_mTimers[actualClientId] = value2 + Time.deltaTime;
				value2 = _mTimers[actualClientId];
				if (value2 > value) {
					_mTimers[actualClientId] = 0f;
					return true;
				}

				return false;
			}
		}

	}
	// public static class MyPluginInfo
	// {
	// 	public const string PLUGIN_GUID = "slenered.LC_Lives";
	// 	public const string PLUGIN_NAME = "slenered.LC_Lives";
	// 	public const string PLUGIN_VERSION = "1.0.2";
	// }

	internal interface IReviveCondition {
		void Reset();
		void SoftReset(RevivePlayerMetric metric);

		bool ShouldRevivePlayer(RevivePlayerMetric metric);

		Vector3 GetRevivePosition(RevivePlayerMetric _) {
			return StartOfRound.Instance.middleOfShipNode.position;
		}
	}
}