using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Inventory;

namespace MinecraftClient.ChatBots
{
	class TelegramInventory : ChatBot
	{
		public string CmdName { get { return "telegram-inventory"; } }
		public string CmdUsage { get { return GetBasicUsage(); } }
		public string CmdDesc { get { return "cmd.inventory.desc"; } }

		public override void Initialize()
		{
			RegisterChatBotCommand(CmdName, CmdDesc, CmdUsage, Run);
		}

		public string Run(string command, string[] args)
		{
			if (GetInventoryEnabled())
			{
				if (args.Length < 1) {
					args = new string[] { "0" };
				}
				try
				{
					int inventoryId;
					if (args[0].ToLower().StartsWith("p"))
					{
						// player inventory is always ID 0
						inventoryId = 0;
					}
					else if (args[0].ToLower().StartsWith("c"))
					{
						List<int> availableIds = GetInventories().Keys.ToList();
						availableIds.Remove(0); // remove player inventory ID from list
						if (availableIds.Count > 0)
							inventoryId = availableIds.Max(); // use foreground container
						else return Translations.Get("cmd.inventory.container_not_found");
					}
					else if (args[0].ToLower() == "help")
					{
						if (args.Length >= 2)
						{
							return GetSubCommandHelp(args[1]);
						}
						else return GetHelp();
					}
					else if (args[0].ToLower() == "off")
					{
						UnloadBot();
						return "Ok!";
					}
					else inventoryId = int.Parse(args[0]);
					Container inventory = GetInventory(inventoryId);
					if (inventory == null)
						return Translations.Get("cmd.inventory.not_exist", inventoryId);
					SortedDictionary<int, Item> itemsSorted = new SortedDictionary<int, Item>(inventory.Items);
					List<string> response = new List<string>();
					response.Add(Translations.Get("cmd.inventory.inventory") + " #" + inventoryId + " - " + inventory.Title + " - " + Settings.Username);
					string asciiArt = inventory.Type.GetAsciiArt();
					if (asciiArt != null && Settings.DisplayInventoryLayout)
						SendMessage("```" + asciiArt + "```");
					int selectedHotbar = GetCurrentSlot() + 1;
					foreach (KeyValuePair<int, Item> item in itemsSorted)
					{
						int hotbar;
						bool isHotbar = inventory.IsHotbar(item.Key, out hotbar);
						string hotbarString = isHotbar ? (hotbar + 1).ToString() : " ";
						if ((hotbar + 1) == selectedHotbar)
							hotbarString = ">" + hotbarString;
						response.Add(String.Format("{0,2} | #{1,-2}: {2}", hotbarString, item.Key, item.Value.ToString()));
					}
					if (inventoryId == 0)
						response.Add(Translations.Get("cmd.inventory.hotbar", (GetCurrentSlot() + 1)));
					response.ForEach(text =>
					{
						LogToConsole(text);
						SendMessage(string.Format("{0}", text.Replace("§", "&")));
					});
					return "";
				}
				catch (FormatException) { return GetCommandDesc(); }
			}
			else return Translations.Get("extra.inventory_required");
		}

		public Container GetInventory(int inventoryID)
		{
			if (GetInventories().ContainsKey(inventoryID))
				return GetInventories()[inventoryID];
			return null;
		}

		#region Methods for commands help
		private string GetCommandDesc()
		{
			return GetBasicUsage() + " Type \"/inventory help\" for more help";
		}

		private string GetAvailableActions()
		{
			return Translations.Get("cmd.inventory.help.available") + ": list, close, click, drop, creativegive, creativedelete.";
		}

		private string GetBasicUsage()
		{
			return Translations.Get("cmd.inventory.help.basic") + ": /inventory <player|container|<id>> <action>.";
		}

		private string GetHelp()
		{
			return Translations.Get("cmd.inventory.help.help", GetAvailableActions());
		}

		private string GetSubCommandHelp(string cmd)
		{
			switch (cmd)
			{
				case "list":
					return Translations.Get("cmd.inventory.help.list") + ' ' + Translations.Get("cmd.inventory.help.usage") + ": /inventory <player|container|<id>> list";
				case "close":
					return Translations.Get("cmd.inventory.help.close") + ' ' + Translations.Get("cmd.inventory.help.usage") + ": /inventory <player|container|<id>> close";
				case "click":
					return Translations.Get("cmd.inventory.help.click") + ' ' + Translations.Get("cmd.inventory.help.usage") + ": /inventory <player|container|<id>> click <slot> [left|right|middle]. \nDefault is left click";
				case "drop":
					return Translations.Get("cmd.inventory.help.drop") + ' ' + Translations.Get("cmd.inventory.help.usage") + ": /inventory <player|container|<id>> drop <slot> [all]. \nAll means drop full stack";
				case "creativegive":
					return Translations.Get("cmd.inventory.help.creativegive") + ' ' + Translations.Get("cmd.inventory.help.usage") + ": /inventory creativegive <slot> <itemtype> <amount>";
				case "creativedelete":
					return Translations.Get("cmd.inventory.help.creativedelete") + ' ' + Translations.Get("cmd.inventory.help.usage") + ": /inventory creativedelete <slot>";
				case "help":
					return GetHelp();
				default:
					return Translations.Get("cmd.inventory.help.unknown") + GetAvailableActions();
			}
		}
		#endregion
	}
}
