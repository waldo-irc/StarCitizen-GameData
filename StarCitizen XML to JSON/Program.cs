﻿using StarCitizen_XML_to_JSON.Cry;
using StarCitizen_XML_to_JSON.JsonObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StarCitizen_XML_to_JSON
{
    class Program
	{
		public static bool debug { get; internal set; } = false;
		public static DateTime starttime = DateTime.Now;
		public static string assembly_directory =  AppContext.BaseDirectory;

		private static bool useCache = false;

		static void Main(string[] args)
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			bool hasException = false;

			if (args.Length < 1 || args.Contains("-h") || args.Contains("--help"))
			{
				Logger.LogEmpty("Usage: dotnet StarCitizen_XML_to_JSON.dll [source] <destination> [CONFIG] [FILTER(S)]");
				Logger.LogEmpty("Convert any StarCitizen XML files to JSON");
				Logger.LogEmpty();
				Logger.LogEmpty("[Required]");
				Logger.LogEmpty("\tsource: \tthe folder to extract XML data.");
				Logger.LogEmpty();
				Logger.LogEmpty("[Config]");
				Logger.LogEmpty("\tdestination\twrite all JSON in the destination, respecting source hierarchy.");
				Logger.LogEmpty("\t\t\tdefault: current working directory.");
				Logger.LogEmpty("\t--debug\t\tprint all Debug infos.");
				Logger.LogEmpty("\t\t\tdefault: no.");
				Logger.LogEmpty("\t--cache\t\tuse a local cache to speed up the process.");
				Logger.LogEmpty("\t\t\tdefault: do not use the cache.");
				Logger.LogEmpty("\t--help, -h\tprint this message.");
				Logger.LogEmpty();
				Logger.LogEmpty("[Filters]");
				Logger.LogEmpty("\t--ships, -s\t\tConvert Ships.");
				Logger.LogEmpty("\t--weapons, -w\t\tConvert Weapons.");
				Logger.LogEmpty("\t--weapons-magazine, -wm\tConvert Weapons Magazines.");
				Logger.LogEmpty("\t--commodities, -c\tConvert Commodities.");
				Logger.LogEmpty("\t--tags, -t\t\tConvert Tags.");
				Logger.LogEmpty("\t--shops, -S\t\tConvert Shops.");
				Logger.LogEmpty("\t--manufacturers, -m\tConvert Manufacturers.");
				Logger.LogEmpty("\t--starmap, -sh\t\tConvert Starmap.");
				return;
			}

			string working_dir = Environment.CurrentDirectory;

			string source = new DirectoryInfo(args[0]).FullName + "\\";
			string destination = new DirectoryInfo((args.Length >= 2) ? args[1] : ".").FullName + "\\";
			SCType filters = FindParameters(args);

			Logger.LogEmpty("Process has started.");
			Logger.LogDebug("DEBUG MODE ENABLED");
			Logger.LogDebug("Arguments: " + String.Join(' ', args));

			Logger.LogEmpty("Parameters:");
			Logger.LogEmpty($"\tSource:\t\t{source}");
			Logger.LogEmpty($"\tDestination:\t{destination}");
			Logger.LogEmpty($"Filter:");
			Logger.LogEmpty("\tShips: " + ((filters & SCType.Ship) == SCType.None ? "No" : "Yes"));
			Logger.LogEmpty("\tWeapons: " + ((filters & SCType.Weapon) == SCType.None ? "No" : "Yes"));
			Logger.LogEmpty("\tStations: " + ((filters & SCType.None) == SCType.None ? "No" : "Yes"));
			Logger.LogEmpty("\tCommodities: " + ((filters & SCType.Commoditie) == SCType.None ? "No" : "Yes"));
			Logger.LogEmpty();

			if (filters == SCType.None)
			{
				Logger.LogInfo("No filter(s) entered,  try to add a least one filter.");
				Logger.LogInfo("Type '--help' for help.");
				return;
			}

			Logger.Log("Loading directory.. ", end: "");
			Tuple<string, SCType>[] files = null;
			try
			{
				files = (Tuple<string, SCType>[])Progress.Process(() => GetFiles(source, filters), "Done");
			}
			catch (Exception ex)
			{
				Logger.LogError("Loading directory.. FAILED", ex, start: "\r");
				Exit(true, saveCache: false);
			}

			Logger.Log("Preparing resources.. ", end: "");
			CryXML cryXml = null;
			try
			{
				cryXml = (CryXML)Progress.Process(() => new CryXML(source, destination), "Done");
			}
			catch (Exception ex)
			{
				Logger.LogError("Preparing resources.. FAILED", ex, start: "\r");
				Exit(true, saveCache: false);
			}

			if (useCache)
			{
				try
				{
					Logger.Log("Loading cache.. ", end: "");
					var exist = (bool)Progress.Process(() => CryXML.game.LoadCache(), "Done");
					if (!exist)
						Logger.Log("Cache is empty");
				}
				catch (System.Runtime.Serialization.SerializationException ex)
				{
					Logger.LogError("Loading cache.. Format error", ex, start: "\r");
					Logger.LogWarning("Loading cache failed, the cache will rebuild.");
					CryXML.game.DeleteCache();
				}
				catch (Exception ex)
				{
					Logger.LogError("Loading cache.. FAILED", ex, start: "\r");
					Exit(true, saveCache: false);
				}
			}

			Logger.LogInfo($"Files to be converted: {files.Length}");
			Logger.LogEmpty();
			Logger.LogInfo("Starting..");

			var category = SCType.None;
			foreach (Tuple<string, SCType> file in new ProgressBar(files, "Converting", true))
			{
				FileInfo f = new FileInfo(file.Item1);
				if (category != file.Item2)
				{
					category = file.Item2;
					Logger.LogEmpty();
					Logger.LogInfo($"Category [{category.ToString()}]", clear_line: true);
				}

				Logger.Log($"Converting {f.Name}..  ", end: "");

				// catch exception on Release build
#if RELEASE
				try
				{
#endif
					Progress.Process(() => cryXml.ConvertJSON(f, file.Item2), "Done");
#if RELEASE
				}
				catch (Exception ex)
				{
					Logger.LogError($"Converting {f.Name}.. FAILED 🔥", start: "\r", exception: ex);
					hasException = true;
				}
#endif
			}

			Exit(hasException);
		}

		/// <summary>
		/// Exit the application
		/// </summary>
		/// <param name="hasException"></param>
		private static void Exit(bool hasException, bool saveCache = true)
		{
			Logger.LogEmpty();
			if (saveCache)
			{
				Logger.LogInfo("Saving cache..  ", end: "");
				Progress.Process(() => CryXML.game.SaveCache(), "Done");
				Logger.LogEmpty();
			}
			
			Logger.LogInfo($"Output files: {JObject.converted_count}");
			Logger.LogInfo($"Execution time: {(DateTime.Now-starttime).TotalSeconds.ToString("00.00s")}");

			if (hasException)
			{
				Logger.LogEmpty();
				Logger.LogEmpty("=====================================");
				Logger.LogError("Something went wrong!");
				Logger.LogError($"More details can be found in: '{Path.Combine(assembly_directory, Logger.filename)}'");
				Logger.LogEmpty("=====================================");
			}
			Logger.LogEmpty();
			Logger.WriteLog();
			Environment.Exit(hasException ? 1 : 0);
		}

		/// <summary>
		/// Get all xml files in de directory and subdirectory
		/// </summary>
		/// <param name="source">Where to search for XML</param>
		/// <returns></returns>
		private static Tuple<string, SCType>[] GetFiles(string source, SCType filter)
		{
			try
			{
				return Directory.GetFiles(source, "*.xml", SearchOption.AllDirectories)
					.Where(f =>
						!f.ToLower().EndsWith("game.xml") &&
						(filter & CryXML.DetectType(f)) != SCType.None)
					.ToList()
					.ConvertAll(f => new Tuple<string, SCType>(f, CryXML.DetectType(f)))
					.OrderBy(f => f.Item2)
					.ToArray();
			}
			catch (Exception ex)
			{
				Logger.LogError(ex.Message);
			}
			return new Tuple<string, SCType>[0];
		}

		/// <summary>
		/// Parse all args from terminal
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		private static SCType FindParameters(string[] args)
		{
			SCType parameters = SCType.None;

			foreach (string arg in args)
			{
				switch (arg)
				{
					case "--cache":
						useCache = true;
						break;

					case "--debug":
						debug = true;
						break;


					case "--ships":
					case "-s":
						parameters |= SCType.Ship;
						break;

					case "--weapons":
					case "-w":
						parameters |= SCType.Weapon;
						break;

					case "--weapons-magazine":
					case "-wm":
						parameters |= SCType.Weapon_Magazine;
						break;

					case "--commodities":
					case "-c":
						parameters |= SCType.Commoditie;
						break;
				}
			}

#if DEBUG
			debug = true;
#endif
			return parameters;
		}
    }
}
