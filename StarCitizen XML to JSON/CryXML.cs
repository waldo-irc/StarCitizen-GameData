﻿using System;
using System.IO;
using System.Xml;
using StarCitizen_XML_to_JSON.JsonObjects.Commodities;
using StarCitizen_XML_to_JSON.JsonObjects.Ship;
using StarCitizen_XML_to_JSON.JsonObjects.Weapon;

namespace StarCitizen_XML_to_JSON
{
	/// <summary>
	/// Convert all types of XML
	/// </summary>
	public class CryXML
	{
		public static CryGame game { get; internal set; } = null;

		public string source{ get; internal set; } = null;
		public string destination { get; internal set; } = null;

		public CryXML(string source, string destination)
		{
			this.source = source ?? throw new ArgumentNullException(nameof(source));
			this.destination = destination ?? throw new ArgumentNullException(nameof(destination));

			game = new CryGame(source);
			Directory.CreateDirectory(this.destination);
		}
		/// <summary>
		/// Convert a file into a JSON representation
		/// </summary>
		/// <param name="file">FileInfo: the file to convert</param>
		/// <param name="filter">SCType: filter</param>
		public void ConvertJSON(FileInfo file, SCType type)
		{
			// Load the XML
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(File.ReadAllText(file.FullName));

			// Cast JObject to the right type
			JObject jObject = null;
			switch (type)
			{
				case SCType.Ship:
					jObject = new JShip(doc, file, destination, source);
					break;
				case SCType.Weapon:
					jObject = new JWeapon(doc, file, destination, source);
					break;
				case SCType.Commoditie:
					jObject = new JCommodity(doc, file, destination, source);
					break;
				case SCType.Tag:
					break;
				case SCType.Shop:
					break;
				case SCType.Manufacturer:
					break;
				case SCType.Starmap:
					break;
				case SCType.Every:
					break;
				case SCType.None:
				default:
					throw new Exception($"Unknow CSType: '{file.FullName}', cannot determine the SCType");
			}
			// Start processing
			jObject?.Process();
		}

		/// <summary>
		/// Detect the type of the file
		/// </summary>
		/// <param name="filename">string: the targeted file</param>
		/// <returns></returns>
		public static SCType DetectType(string filename)
		{
			XmlDocument xml = new XmlDocument();
			xml.Load(filename);
			return DetectType(xml);
		}

		/// <summary>
		/// Detect the type of the file
		/// </summary>
		/// <param name="filename">XmlDocument: the targeted file</param>
		/// <returns></returns>
		private static SCType DetectType(XmlDocument xfile)
		{
			if(xfile.SelectSingleNode("/*").Name.Equals("Vehicle"))
				return SCType.Ship;

			if (new FileInfo(xfile.BaseURI).Directory.Name.ToLower() == "fps_weapons")
				return SCType.Weapon;

			if (new FileInfo(xfile.BaseURI).Directory.FullName.ToLower().Contains("commodities"))
				return SCType.Commoditie;

			return SCType.None;
		}
	}
}
