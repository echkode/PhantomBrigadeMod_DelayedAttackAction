// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;
using HarmonyLib;
using PhantomBrigade;
using PBData = PhantomBrigade.Data;
using PBEquipmentActionSystem = PhantomBrigade.Combat.Systems.EquipmentActionSystem;

using UnityEngine;

namespace EchKode.PBMods.DelayedAttackAction
{
	public sealed class EquipmentActionSystem : PBEquipmentActionSystem
	{
		public static void Install(Systems feature, Contexts contexts) =>
			SystemInstaller.Replace<PBEquipmentActionSystem, EquipmentActionSystem>(
				feature,
				new EquipmentActionSystem(contexts));

		private readonly ActionContext action;
		private readonly CombatContext combat;
		private readonly IGroup<ActionEntity> equipmentActions;
		private readonly HashSet<int> partIDsActivatedThisTurn;

		public EquipmentActionSystem(Contexts contexts)
			: base(contexts)
		{
			var fi = AccessTools.Field(typeof(PBEquipmentActionSystem), nameof(action));
			action = (ActionContext)fi.GetValue(this);

			fi = AccessTools.Field(typeof(PBEquipmentActionSystem), nameof(combat));
			combat = (CombatContext)fi.GetValue(this);

			fi = AccessTools.Field(typeof(PBEquipmentActionSystem), nameof(equipmentActions));
			equipmentActions = (IGroup<ActionEntity>)fi.GetValue(this);

			fi = AccessTools.Field(typeof(PBEquipmentActionSystem), nameof(partIDsActivatedThisTurn));
			partIDsActivatedThisTurn = (HashSet<int>)fi.GetValue(null);
		}

		protected override void Execute(List<CombatEntity> entities)
		{
			var simulationTime = combat.simulationTime.f;

			foreach (var equipmentAction in equipmentActions.GetEntities())
			{
				if (equipmentAction == null)
				{
					continue;
				}
				if (equipmentAction.CompletedAction)
				{
					continue;
				}

				if (!TimeUtility.ContainsTime(simulationTime, equipmentAction.startTime.f, equipmentAction.duration.f))
				{
					continue;
				}

				equipmentAction.CompletedAction = true;

				var equipmentEntity = IDUtility.GetEquipmentEntity(equipmentAction.activeEquipmentPart.equipmentID);
				if (equipmentEntity == null)
				{
					continue;
				}

				if (!PBData.DataHelperAction.IsValid(equipmentAction))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) invalid equipment action",
						ModLink.modIndex,
						ModLink.modId);
					continue;
				}

				if (!equipmentEntity.hasPrimaryActivationSubsystem)
				{
					continue;
				}

				var subsystemEntity = IDUtility.GetEquipmentEntity(equipmentEntity.primaryActivationSubsystem.equipmentID);
				var activationProcessed = subsystemEntity?.dataLinkSubsystem.data?.activationProcessed;
				if (activationProcessed == null)
				{
					continue;
				}

				var count = Mathf.RoundToInt(PBData.DataHelperStats.GetCachedStatForPart("act_count", equipmentEntity));
				if (count <= 0)
				{
					Debug.LogWarning($"Part {equipmentEntity.ToLog()} has action count at 0, no actions would be performed");
					continue;
				}

				if (!equipmentAction.hasActionOwner)
				{
					continue;
				}

				var targetedActionBuffer = PBData.DataShortcuts.anim.targetedActionBuffer;
				var combatEntity = IDUtility.GetCombatEntity(equipmentAction.actionOwner.combatID);
				var persistentEntity = IDUtility.GetLinkedPersistentEntity(combatEntity);
				if (persistentEntity != null && persistentEntity.hasFaction && persistentEntity.faction.s == "Phantoms")
				{
					var newActivations = equipmentEntity.hasPartUsage ? equipmentEntity.partUsage.activations + 1 : 1;
					var newTurns = equipmentEntity.hasPartUsage ? equipmentEntity.partUsage.turns : 0;
					if (!partIDsActivatedThisTurn.Contains(equipmentEntity.id.id))
					{
						newTurns += 1;
						partIDsActivatedThisTurn.Add(equipmentEntity.id.id);
					}
					equipmentEntity.ReplacePartUsage(newTurns, newActivations);
				}

				var activationHeat = PBData.DataHelperStats.GetCachedStatForPart("act_heat", equipmentEntity);
				var hasAudio = activationProcessed.audio != null;
				var actionStartTime = equipmentAction.startTime.f + targetedActionBuffer;
				var equipmentDuration = equipmentAction.duration.f - targetedActionBuffer * 2f;
				var spacing = equipmentDuration / Mathf.Max(count - 1f, 1f);
				if (count == 1)
				{
					actionStartTime = SingleActionStart(subsystemEntity.dataLinkSubsystem.data.customProcessed, spacing, actionStartTime);
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Attack))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) delayed single shot | start time: {2}",
							ModLink.modIndex,
							ModLink.modId,
							actionStartTime);
					}
				}
				else if (subsystemEntity.dataLinkSubsystem.data.customProcessed != null)
				{
					var pct = CustomActionStartTime(subsystemEntity.dataLinkSubsystem.data.customProcessed);
					actionStartTime += equipmentDuration * pct;
					spacing *= 1f - pct;
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Attack))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) delayed multi-shot | start time: {2} | spacing: {3}",
							ModLink.modIndex,
							ModLink.modId,
							actionStartTime,
							spacing);
					}
				}

				var newHeatChange = activationHeat / count;
				for (var i = 0; i < count; i += 1)
				{
					var attackAction = action.CreateEntity();
					attackAction.ReplaceStartTime(actionStartTime);
					attackAction.ReplaceScheduledAttack(equipmentAction.id.id);
					attackAction.ReplaceSubActionIndex(i);
					if (i == 0)
					{
						attackAction.ScheduledAttackStart = true;
						if (hasAudio && !string.IsNullOrEmpty(activationProcessed.audio.onActivationFirst))
						{
							attackAction.ReplaceActivationSound(activationProcessed.audio.onActivationFirst);
						}
					}
					else if (i == count - 1)
					{
						attackAction.ScheduledAttackEnd = true;
						if (hasAudio && !string.IsNullOrEmpty(activationProcessed.audio.onActivationLast))
						{
							attackAction.ReplaceActivationSound(activationProcessed.audio.onActivationLast);
						}
					}
					else if (hasAudio && !string.IsNullOrEmpty(activationProcessed.audio.onActivationMid))
					{
						attackAction.ReplaceActivationSound(activationProcessed.audio.onActivationMid);
					}
					attackAction.ReplaceChangeHeat(newHeatChange);
					actionStartTime += spacing;
				}
			}
		}

		private static float CustomActionStartTime(PBData.DataBlockPartCustom custom)
		{
			if (custom.floats == null)
			{
				return 0f;
			}
			if (!custom.floats.TryGetValue("action_start_time", out var pct))
			{
				return 0f;
			}
			if (pct > 1f)
			{
				// This is a misconfigured subsystem; fallback to standard behavior.
				return 0f;
			}
			if (pct < 0f)
			{
				return 0f;
			}
			return pct;
		}

		private static float SingleActionStart(PBData.DataBlockPartCustom custom, float spacing, float actionStartTime)
		{
			if (custom == null)
			{
				return actionStartTime;
			}

			if (custom.strings == null)
			{
				return actionStartTime;
			}

			if (!custom.strings.TryGetValue("action_start_time", out var start))
			{
				return actionStartTime;
			}

			switch (start)
			{
				case "end":
					return actionStartTime + spacing;
				case "middle":
					return actionStartTime + spacing / 2f;
				case "percentage":
					return actionStartTime + spacing * CustomActionStartTime(custom);
			}

			return actionStartTime;
		}
	}
}
