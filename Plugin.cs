using BepInEx;
using BepInEx.Configuration;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System;

namespace PickupFilter
{
    [BepInPlugin(MID, "PickupFilter", VERSION)]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string MID = "ravatar.pickupfilter";
        public const string VERSION = "1.0.2";
        public const string EXPECTED_VALHEIM_VERSION = "0.146.11";

        public static readonly string VALHEIM_VERSION = (string)Assembly.GetAssembly(typeof(Player)).GetType("Version").GetMethod("GetVersionString", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);

        public static ConfigFile Configuration = new ConfigFile(Path.Combine(Paths.ConfigPath, $"{MID}.cfg"), true);
    
        private static Harmony harmony = new Harmony(MID);

        private void Awake()
        {
            Debug.Log($"Valheim version: {VALHEIM_VERSION}");
            Debug.Log("PickupFilter plugin initialized.");
            Configuration.SaveOnConfigSet = true;
            PickupFilter.Initialize();

            // harmony.UnpatchSelf();
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    public static class PickupFilter
    {
        private static HashSet<string> FilterSet = new HashSet<string>();
        private static ConfigEntry<string> IgnoreList = Plugin.Configuration.Bind("PickupFilter", "ignoreList", "", "Comma separated array of items to ignore");

        public static bool ContainsItem(string name)
        {
            return FilterSet.Contains(name);
        }

        public new static string ToString()
        {
            return FilterSet.Join(null, ", ");
        }

        public static bool Empty
        {
            get
            {
                return FilterSet.Count > 0;
            }
        }

        public static void Clear()
        {
            FilterSet.Clear();
            IgnoreList.Value = "";
        }

        public static bool ToggleItem(string name)
        {
            bool willPickup;

            if (ContainsItem(name))
            {
                FilterSet.Remove(name);
                willPickup = true;
            }
            else
            {
                FilterSet.Add(name);
                willPickup = false;
            }

            IgnoreList.Value = ToString();

            return willPickup;
        }

        public static void Initialize()
        {
            string itemList = IgnoreList.Value;
            if (itemList != null && itemList.Length > 0)
            {
                foreach (var name in itemList.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    FilterSet.Add(name);
                }
            }
        }
    }

    // Add the extensions methods to simplify binding private or protected methods
    [HarmonyPatch]
    public static class MethodPatches
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Chat), "AddString", new Type[] { typeof(string) })]
        public static void AddString(this Chat instance, string text)
        {
            throw new NotImplementedException();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(InventoryGrid), "GetButtonPos")]
        public static Vector2i GetButtonPos(this InventoryGrid instance, GameObject go)
        {
            throw new NotImplementedException();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Chat), "Awake")]
        static void ChatAwakePrefix(ref Chat __instance)
        {
            __instance.AddString("/pickupfilter - displays PickupFilter commands");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Chat), "Awake")]
        static void ChatAwakePostfix(ref Chat __instance)
        {
            if(Plugin.EXPECTED_VALHEIM_VERSION != Plugin.VALHEIM_VERSION)
            {
                __instance.AddString($"PickupFilter expected game version {Plugin.EXPECTED_VALHEIM_VERSION} but found {Plugin.VALHEIM_VERSION}.");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Inventory), "CanAddItem", typeof(ItemDrop.ItemData), typeof(int))]
        private static bool InventoryCanAddItem(Inventory __instance, ItemDrop.ItemData item, int stack)
        {
            // Is this necessary? Do NPCs auto pickup?
            if (Player.m_localPlayer.GetInventory() == __instance)
            {
                return !PickupFilter.ContainsItem(item.m_shared.m_name);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Chat), "InputText")]
    public static class ChatInputTextPatch
    {
        static AccessTools.FieldRef<Chat, InputField> m_input = AccessTools.FieldRefAccess<Chat, InputField>("m_input");

        static bool Prefix(ref Chat __instance)
        {
            string text = m_input(__instance).text;

            if (text.StartsWith("/pickupfilter"))
            {
                string[] command = text.Split(' ');

                if (command.Length > 1)
                {
                    switch (command[1])
                    {
                        case "list":
                            {
                                __instance.AddString(PickupFilter.Empty ? $"Ignoring {Localization.instance.Localize(PickupFilter.ToString())} on the ground" : "Not ignoring any items");
                                return false;
                            }
                        case "clear":
                            {
                                PickupFilter.Clear();
                                __instance.AddString(PickupFilter.Empty ? "Ignored item list is cleared" : "Not ignoring any items");
                                return false;
                            }
                    }
                }

                __instance.AddString("");
                __instance.AddString("PickupFilter commands:");
                __instance.AddString("- list: displays a list of items that are being ignored");
                __instance.AddString("- clear: clears the ignored item list");
                __instance.AddString("");
                __instance.AddString("Alt + LeftClick item in inventory to toggle ignoring it");

                return false;
            }

            return true;
        }
    }


    [HarmonyPatch(typeof(InventoryGrid), "OnLeftClick", typeof(UIInputHandler))]
    public static class OnLeftClickPatch
    {
        private static AccessTools.FieldRef<InventoryGrid, Inventory> m_inventory = AccessTools.FieldRefAccess<InventoryGrid, Inventory>("m_inventory");

        private static bool Prefix(InventoryGrid __instance, UIInputHandler clickHandler)
        {
            // Toggle the item filter if the item is Alt + LeftClicked
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                GameObject go = clickHandler.gameObject;
                Vector2i buttonPos = __instance.GetButtonPos(go);
                ItemDrop.ItemData itemAt = m_inventory(__instance).GetItemAt(buttonPos.x, buttonPos.y);

                if (itemAt != null)
                {
                    string itemName = itemAt.m_shared.m_name;
                    bool willPickupItem = PickupFilter.ToggleItem(itemName);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"I'll {(willPickupItem ? "start" : "stop")} picking up {itemName}...");
                }

                return false;
            }

            return true;
        }
    }
}
