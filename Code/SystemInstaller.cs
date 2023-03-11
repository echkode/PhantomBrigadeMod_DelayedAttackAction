// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

using HarmonyLib;

using UnityEngine;

namespace EchKode.PBMods.DelayedAttackAction
{
	internal static class SystemInstaller
	{
		internal static void InstallAtEnd(Systems feature, ISystem installee)
		{
			feature.Add(installee);
			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.System))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) installed system {2}",
					ModLink.modIndex,
					ModLink.modId,
					installee.GetType().FullName);
			}
		}

		internal static void InstallBefore<T>(Systems feature, ISystem installee)
			where T : ISystem
		{
			var installed = false;

			if (installee is IInitializeSystem init)
			{
				InstallBefore<IInitializeSystem, T>(feature, "initialize", init);
				installed = true;
			}
			if (installee is IExecuteSystem exec)
			{
				InstallBefore<IExecuteSystem, T>(feature, "execute", exec);
				installed = true;
			}
			if (installee is ICleanupSystem cleanup)
			{
				InstallBefore<ICleanupSystem, T>(feature, "cleanup", cleanup);
				installed = true;
			}
			if (installee is ITearDownSystem tearDown)
			{
				InstallBefore<ITearDownSystem, T>(feature, "tearDown", tearDown);
				installed = true;
			}
			if (installee is IEnableSystem enable)
			{
				InstallBefore<IEnableSystem, T>(feature, "enable", enable);
				installed = true;
			}
			if (installee is IDisableSystem disable)
			{
				InstallBefore<IDisableSystem, T>(feature, "disable", disable);
				installed = true;
			}

			if (!installed)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) InstallBefore unable to install system -- new system doesn't implement installable interface | installee: {2}",
					ModLink.modIndex,
					ModLink.modId,
					installee.GetType().FullName);
			}
		}

		static void InstallBefore<S, T>(Systems feature, string kind, S installee)
			where S : ISystem
			where T : ISystem
		{
			var fi = AccessTools.Field(feature.GetType(), $"_{kind}Systems");
			if (fi == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) InstallBefore attempted to install a system kind that the feature doesn't support | feature: {2} | kind: {3} | installee: {4}",
					ModLink.modIndex,
					ModLink.modId,
					feature.GetType().Name,
					kind,
					installee.GetType().FullName);
				return;
			}

			var systems = (List<S>)fi.GetValue(feature);
			var i = 0;
			for (; i < systems.Count; i += 1)
			{
				if (systems[i] is T)
				{
					break;
				}
			}

			var insert = i != systems.Count;
			if (insert)
			{
				systems.Insert(i, installee);
			}
			else
			{
				systems.Add(installee);
			}

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.System))
			{
				var fmt = insert
					? "Mod {0} ({1}) InstallBefore inserted system {2} ({3}) before {4}"
					: "Mod {0} ({1}) InstallBefore did not find system {4} so appended system {2} ({3})";
				Debug.LogFormat(
					fmt,
					ModLink.modIndex,
					ModLink.modId,
					installee.GetType().FullName,
					typeof(S).Name,
					typeof(T).Name);
			}
		}

		internal static void Replace<T, U>(Systems feature, U replacement)
			where T : ISystem
			where U : ISystem
		{
			var installed = false;

			if (replacement is IInitializeSystem init)
			{
				ReplaceSystem<IInitializeSystem, T>(feature, "initialize", init);
				installed = true;
			}
			if (replacement is IExecuteSystem exec)
			{
				ReplaceSystem<IExecuteSystem, T>(feature, "execute", exec);
				installed = true;
			}
			if (replacement is ICleanupSystem cleanup)
			{
				ReplaceSystem<ICleanupSystem, T>(feature, "cleanup", cleanup);
				installed = true;
			}
			if (replacement is ITearDownSystem tearDown)
			{
				ReplaceSystem<ITearDownSystem, T>(feature, "tearDown", tearDown);
				installed = true;
			}
			if (replacement is IEnableSystem enable)
			{
				ReplaceSystem<IEnableSystem, T>(feature, "enable", enable);
				installed = true;
			}
			if (replacement is IDisableSystem disable)
			{
				ReplaceSystem<IDisableSystem, T>(feature, "disable", disable);
				installed = true;
			}

			if (!installed)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) unable to replace system -- new system doesn't implement installable interface | replacement: {2}",
					ModLink.modIndex,
					ModLink.modId,
					replacement.GetType().FullName);
			}
		}

		private static void ReplaceSystem<S, T>(Systems feature, string kind, S replacement)
			where S : ISystem
			where T : ISystem
		{
			var fi = AccessTools.Field(feature.GetType(), $"_{kind}Systems");
			var systems = (List<S>)fi.GetValue(feature);
			var i = 0;
			for (; i < systems.Count; i += 1)
			{
				if (systems[i] is T)
				{
					systems[i] = replacement;
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.System))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) replaced system {2} ({3}) with {4}",
							ModLink.modIndex,
							ModLink.modId,
							typeof(T).Name,
							typeof(S).Name,
							replacement.GetType().FullName);
					}
					return;
				}
			}

			Debug.LogWarningFormat(
				"Mod {0} ({1}) unable to replace system | name: {2} | kind: {3}",
				ModLink.modIndex,
				ModLink.modId,
				typeof(T).Name,
				kind);
		}
	}
}
