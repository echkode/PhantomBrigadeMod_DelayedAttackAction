using System.Collections.Generic;

using Entitas;

using PhantomBrigade;

namespace EchKode.PBMods.DelayedAttackAction
{
	static class PatchFeature
	{
		private static readonly List<System.Action<Systems, Contexts>> combatSystems = new List<System.Action<Systems, Contexts>>()
		{
			EquipmentActionSystem.Install,
		};

		internal static void Install(GameController gc)
		{
			InstallCombatSystems(gc);
		}

		static void InstallCombatSystems(GameController gc)
		{
			var gcs = gc.m_stateDict[GameStates.combat];
			var combatFeature = gcs.m_systems[0];
			foreach (var install in combatSystems)
			{
				install(combatFeature, Contexts.sharedInstance);
			}
		}
	}
}
