﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SimpleCore.Utilities;
using SmartImage.Engines;
using SmartImage.Utilities;
using Novus.Win32;
using SimpleCore.Cli;
using SimpleCore.Net;
using SmartImage.Configuration;
using SmartImage.Searching;

// ReSharper disable ArrangeAccessorOwnerBody

#pragma warning disable IDE0052, HAA0502, HAA0505, HAA0601, HAA0502, HAA0101, RCS1213, RCS1036, CS8602
#nullable enable

namespace SmartImage.Core
{
	/// <summary>
	///     User interface; contains <see cref="NConsoleInterface" /> and <see cref="NConsoleOption" /> for the main menu
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal static class Interface
	{
		// TODO: refactor, optimize

		private static NConsoleOption[] AllOptions
		{
			get
			{
				var fields = typeof(Interface).GetFields(
						BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Default)
					.Where(f => f.FieldType == typeof(NConsoleOption))
					.ToArray();


				var options = new NConsoleOption[fields.Length];

				for (int i = 0; i < fields.Length; i++) {
					options[i] = (NConsoleOption) fields[i].GetValue(null)!;
				}

				return options;
			}
		}

		/// <summary>
		///     Main menu console interface
		/// </summary>
		internal static NConsoleInterface MainMenu => new(AllOptions, Info.NAME_BANNER, null, false, null);

		/// <summary>
		///     Runs when no arguments are given (and when the executable is double-clicked)
		/// </summary>
		/// <remarks>
		///     User-friendly menu
		/// </remarks>
		internal static void Run() => NConsole.ReadOptions(MainMenu);


		/// <summary>
		/// Misc color
		/// </summary>
		internal static readonly Color ColorMisc2 = Color.White;

		/// <summary>
		/// Primary color
		/// </summary>
		internal static readonly Color ColorPrimary = Color.Red;

		/// <summary>
		/// Main color
		/// </summary>
		internal static readonly Color ColorMain = Color.Yellow;

		/// <summary>
		/// Config color
		/// </summary>
		internal static readonly Color ColorConfig = Color.DeepSkyBlue;

		/// <summary>
		/// Utility color
		/// </summary>
		internal static readonly Color ColorUtility = Color.DarkOrange;
	

		/// <summary>
		/// Version color
		/// </summary>
		internal static readonly Color ColorVersion = Color.LightGreen;


		/// <summary>
		/// Console window width (initial)
		/// </summary>
		internal const int ConsoleWindowWidth = 100;


		/// <summary>
		/// Console window height (initial)
		/// </summary>
		internal const int ConsoleWindowHeight = 35;

		/// <summary>
		/// Main option
		/// </summary>
		private static readonly NConsoleOption RunSelectImage = new()
		{
			Name  = ">>> Select image <<<",
			Color = ColorMain,
			Function = () =>
			{

				NConsole.WriteInfo("Drag and drop the image here");
				NConsole.WriteInfo("Or paste a direct image link");


				string? img = NConsole.ReadInput("Image", ColorMain);

				img = img?.CleanString();

				if (!ImageInputInfo.TryCreate(img, out _)) {

					NConsole.WriteError($"Invalid image!");
					NConsole.WaitForSecond();

					return null;
				}


				SearchConfig.Config.ImageInput = img;

				return true;
			}
		};

		private static readonly NConsoleOption ConfigSearchEnginesOption = new()
		{
			Name  = "Configure search engines",
			Color = ColorConfig,
			Function = () =>
			{
				if (ReadSearchEngineOptions(out var newValues)) {
					SearchConfig.Config.SearchEngines = newValues;
					SearchConfig.Config.EnsureConfig();

				}

				NConsole.WriteSuccess(SearchConfig.Config.SearchEngines);
				NConsole.WaitForSecond();
				return null;
			}
		};


		private static readonly NConsoleOption ConfigPriorityEnginesOption = new()
		{
			Name  = "Configure priority engines",
			Color = ColorConfig,
			Function = () =>
			{
				if (ReadSearchEngineOptions(out var newValues)) {
					SearchConfig.Config.PriorityEngines = newValues;
					SearchConfig.Config.EnsureConfig();

				}

				NConsole.WriteSuccess(SearchConfig.Config.PriorityEngines);
				NConsole.WaitForSecond();
				return null;
			}
		};


		private static bool ReadSearchEngineOptions(out SearchEngineOptions newValues)
		{
			var rgEnum = NConsoleOption.FromEnum<SearchEngineOptions>();
			var values = NConsole.ReadOptions(rgEnum, true);

			if (!values.Any()) {
				newValues = default;
				return false;
			}

			newValues = Enums.ReadFromSet<SearchEngineOptions>(values);

			Debug.WriteLine($"{values.Count} -> {newValues}");

			return true;
		}


		private static readonly NConsoleOption ConfigSauceNaoAuthOption = new()
		{
			Name  = $"Configure SauceNao API authentication",
			Color = ColorConfig,
			Function = () =>
			{
				SearchConfig.Config.SauceNaoAuth = NConsole.ReadInput("API key");

				NConsole.WaitForSecond();

				return null;
			}
		};


		private static readonly NConsoleOption ConfigImgurAuthOption = new()
		{
			Name  = "Configure Imgur API authentication",
			Color = ColorConfig,
			Function = () =>
			{

				SearchConfig.Config.ImgurAuth = NConsole.ReadInput("API key");

				NConsole.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ConfigAutoFilter = new()
		{
			Name  = GetAutoFilterString(),
			Color = ColorConfig,
			Function = () =>
			{

				SearchConfig.Config.FilterResults = !SearchConfig.Config.FilterResults;
				ConfigAutoFilter.Name             = GetAutoFilterString();
				return null;
			}
		};

		private static string GetAutoFilterString()
		{
			//var x = SearchConfig.Config.FilterResults ? NConsole.AddColor("#", Color.GreenYellow) : NConsole.AddColor("-",Color.Red);
			var x = SearchConfig.Config.FilterResults;
			return $"Filter results: {x}";
		}

		private static readonly NConsoleOption ConfigUpdateOption = new()
		{
			Name  = "Update configuration file",
			Color = ColorConfig,
			Function = () =>
			{
				SearchConfig.Config.SaveFile();

				NConsole.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ShowInfoOption = new()
		{
			Name  = "Show info and config",
			Color = ColorConfig,
			Function = () =>
			{
				Info.ShowInfo();

				NConsole.WaitForInput();
				return null;
			}
		};


		private static readonly NConsoleOption ContextMenuOption = new()
		{
			Name  = GetContextMenuString(Integration.IsContextMenuAdded),
			Color = ColorUtility,
			Function = () =>
			{
				bool ctx = Integration.IsContextMenuAdded;

				var io = !ctx ? IntegrationOption.Add : IntegrationOption.Remove;

				Integration.HandleContextMenu(io);
				bool added = io == IntegrationOption.Add;

				NConsole.WriteInfo($"Context menu integrated: {added}");

				ContextMenuOption.Name = GetContextMenuString(added);

				NConsole.WaitForSecond();
				return null;
			}
		};

		private static string GetContextMenuString(bool added) =>
			(!added ? "Add" : "Remove") + " context menu integration";


		private static readonly NConsoleOption CheckForUpdateOption = new()
		{
			Name  = "Check for updates",
			Color = ColorUtility,
			Function = () =>
			{
				UpdateInfo.AutoUpdate();

				NConsole.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ResetOption = new()
		{
			Name  = "Reset all configuration and integrations",
			Color = ColorUtility,
			Function = () =>
			{
				Integration.ResetIntegrations();

				NConsole.WaitForSecond();
				return null;
			}
		};


		private static readonly NConsoleOption CleanupLegacy = new()
		{
			Name  = "Legacy cleanup",
			Color = ColorUtility,

			Function = () =>
			{
				bool ok = LegacyIntegration.LegacyCleanup();

				NConsole.WriteInfo($"Legacy cleanup: {ok}");
				NConsole.WaitForInput();

				return null;
			}
		};

		private static readonly NConsoleOption UninstallOption = new()
		{
			Name  = "Uninstall",
			Color = ColorUtility,
			Function = () =>
			{
				Integration.ResetIntegrations();
				Integration.HandlePath(IntegrationOption.Remove);

				File.Delete(SearchConfig.ConfigLocation);

				Integration.Uninstall();

				// No return

				Environment.Exit(0);

				return null;
			}
		};


#if DEBUG
		private static readonly string[] TestImages =
		{
			@"..\..\..\..\Test1.jpg",
			@"..\..\..\..\Test2.jpg",
			@"..\..\..\..\Test3.png",
			@"C:\Users\Deci\Pictures\fucking_epic.jpg",
			"https://kemono.party/files/224580/46935364/beidou_800.jpg",
			"https://i.imgur.com/QtCausw.jpg"
		};

		private static readonly NConsoleOption DebugTestOption = new()
		{
			Name = "[DEBUG] Run test",
			Function = () =>
			{
				
				var     rgOption = NConsoleOption.FromArray(TestImages, s => s);
				
				string? testImg  = (string) NConsole.ReadOptions(rgOption).First();

				SearchConfig.Config.ImageInput = testImg;
				//SearchConfig.Config.PriorityEngines = SearchEngineOptions.None;

				return true;
			}
		};
#endif
	}
}