using MinecraftClient.Inventory;
using System.Linq;
using System.Text.RegularExpressions;

namespace MinecraftClient.ChatBots
{

	public class FarmBot : ChatBot
	{

		int[] sellAttempts = new int[2];
		int totalExperience;

		Regex soldItemsValue = new Regex(@"^Shop > You sold \d+ items for a total of \$((?:\d{1,3},?)+).$");
		Regex balanceFormat = new Regex(@"^⇨ Balance: \$((?:\d{1,3},?)+)");
		Regex personalCommand = new Regex(@"^\[\w+\] \w+ ([a-zA-Z0-9_]+): ([a-zA-Z0-9_]+) (.+)$");

		bool balanceForPay;

		public override void GetText(string text)
		{
			string message = "";
			string username = "";
			text = GetVerbatim(text);

			if (!IsPrivateMessage(text, ref message, ref username) && !IsChatMessage(text, ref message, ref username))
			{
				if (text.Contains("!status"))
					SendStatus();
				else if (text.Contains("!reload"))
				{
					UnloadBot();
					PerformInternalCommand("script FarmBot");
					SendText("/gc Reloaded");
				}
				else if (text.Contains("!average"))
					if (sellAttempts[1] > 0)
						SendText(string.Format("/gc Sum: ${0} | Average: ${1} per minute", sellAttempts[0], (sellAttempts[0] / sellAttempts[1]) * 6));
					else
						SendText("/gc No sells yet.");
				else if (soldItemsValue.IsMatch(text))
				{
					var value = soldItemsValue.Match(text).Groups[1].Value.Replace(",", "");

					sellAttempts[0] += int.Parse(value);
					sellAttempts[1]++;
				}
				else if (text == "Shop > You don't have any items you can sell.")
					sellAttempts[1]++;
				else if (text.Contains("!exp"))
				{
					SendText(string.Format("/gc Total: {0} | Heroic Books: {1} | Legendary Books: {2}", totalExperience, totalExperience / 50000, totalExperience / 25000));
				}
				else if (balanceFormat.IsMatch(text))
				{
					if (GetOnlinePlayers().Contains("luizhrios"))
					{
						var balance = balanceFormat.Match(text).Groups[1].Value;
						if (balanceForPay)
						{
							balance = balance.Replace(",", "");

							var pay = int.Parse(balance.Substring(0, 1).PadRight(balance.Length, '0')) / 2;

							SendText(string.Format("/pay luizhrios {0}", pay));
							SendText(string.Format("/g deposit {0}", pay));
						}
						else
						{
							SendText(string.Format("/gc ${0}", balance));
						}
					}
					else
					{
						SendText(string.Format("/gc luizhrios is not online."));
					}
				}
				else if (text.Contains("!pay") || text.Contains("!balance"))
				{
					balanceForPay = text.Contains("!pay");
					SendText("/balance");
				}
				else if (text.Contains("!respawn"))
				{
					if (!isAlive)
					{
						PerformInternalCommand("respawn");
						SendText("/home");
						isAlive = true;
						SendStatus();
					}
				}
				else if (personalCommand.IsMatch(text))
				{
					var matches = personalCommand.Match(text).Groups;
					if (Settings.Username.Contains(matches[2].Value))
						if (matches[3].Value == "tpa")
							SendText(string.Format("/tpahere {0}", matches[1].Value));
				}
				else if (text == string.Format("[+] {0}", Settings.Username)) BotJoined();
			}
		}

		private void BotJoined()
		{
		}

		private void SendStatus()
		{
			var saveRods = GetPlayerInventory().Items
				.Where(item => item.Value.Type == ItemType.BlazeRod)
				.Select(item => item.Value.Count)
				.Aggregate(0, (count, stackCount) => count += stackCount);

			var potatosUsable = GetPlayerInventory().Items
				.Where(item => item.Value.Type == ItemType.Potato && item.Key >= 36 && item.Key <= 44)
				.Select(item => item.Value.Count)
				.Aggregate(0, (count, stackCount) => count += stackCount);

			var potatos = GetPlayerInventory().Items
				.Where(item => item.Value.Type == ItemType.Potato)
				.Select(item => item.Value.Count)
				.Aggregate(0, (count, stackCount) => count += stackCount);

			var heldhandItem = "Hand";
			if (GetPlayerInventory().Items.ContainsKey(GetCurrentSlot() + 36))
				heldhandItem = GetPlayerInventory().Items[GetCurrentSlot() + 36].DisplayName;

			if (GetPlayerInventory().Items.ContainsKey(5) && GetVerbatim(GetPlayerInventory().Items[5].DisplayName) == "DragonCry")
			{
				SendText(string.Format("/gc {0} SaveRods | DragonCry | Using {1}", saveRods, GetVerbatim(heldhandItem)));
				return;
			}

			SendText(string.Format("/gc {0} SaveRods | {1}/{2} Potatoes total/usable | Using {3}", saveRods, potatos, potatosUsable, GetVerbatim(heldhandItem)));
		}

		bool isAlive = true;

		public override void OnDeath()
		{
			var saveRods = GetPlayerInventory().Items
				.Where(item => item.Value.Type == ItemType.BlazeRod)
				.Select(item => item.Value.Count)
				.Aggregate(0, (count, stackCount) => count += stackCount);

			if (saveRods > 1)
			{
				SendMessage(string.Format("{0} died. {1} SaveRods remaining. Respawning.", Settings.Username, saveRods));
				PerformInternalCommand("respawn");
				SendText("/home");
				isAlive = true;
			}
			else
			{
				isAlive = false;
				SendMessage(string.Format("{0} died.", Settings.Username));
			}
		}

		float lastSentHealth;
		bool greaterHealthSent;

		public override void OnHealthUpdate(float health, int food)
		{
			if (health > 6 && health < 12)
			{
				if (health < lastSentHealth)
				{
					var saveRods = GetPlayerInventory().Items
						.Where(item => item.Value.Type == ItemType.BlazeRod)
						.Select(item => item.Value.Count)
						.Aggregate(0, (count, stackCount) => count += stackCount);

					if (greaterHealthSent)
						SendMessage(string.Format("{0} is almost dying, {1} hearts and {2} food", Settings.Username, health, food));
					SendText(string.Format("/gc I'm almost dying, I have {0} hearts and {1} food", health, food));
					if (saveRods < 1)
						SendText("/warp church");
					greaterHealthSent = false;
				}
				else if (health > lastSentHealth)
				{
					if (!greaterHealthSent)
						SendMessage(string.Format("{0} is recovering, {1} hearts and {2} food", Settings.Username, health, food));
					SendText(string.Format("/gc I'm recovering, {0} hearts and {1} food", health, food));
					greaterHealthSent = true;
				}
			}
			lastSentHealth = health;
		}

		public override void OnSetExperience(float Experiencebar, int Level, int TotalExperience)
		{
			totalExperience = TotalExperience;
		}

	}

}