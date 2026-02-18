using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using Den.Tools;
using HarmonyLib;
using SemanticVersioning;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine;

namespace BitchlandPickupItemsFromVendingMachinesAndTrashPilesBepInEx
{
    [BepInPlugin("com.wolfitdm.BitchlandPickupItemsFromVendingMachinesAndTrashPilesBepInEx", "BitchlandPickupItemsFromVendingMachinesAndTrashPilesBepInEx Plugin", "1.0.0.0")]
    public class BitchlandPickupItemsFromVendingMachinesAndTrashPilesBepInEx : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private ConfigEntry<bool> configEnableMe;

        public BitchlandPickupItemsFromVendingMachinesAndTrashPilesBepInEx()
        {
        }

        public static Type MyGetType(string originalClassName)
        {
            return Type.GetType(originalClassName + ",Assembly-CSharp");
        }

        private static string pluginKey = "General.Toggles";

        public static bool enableThisMod = false;

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;

            configEnableMe = Config.Bind(pluginKey,
                                              "EnableThisMod",
                                              true,
                                             "Whether or not you want enable this mod (default true also yes, you want it, and false = no)");


            enableThisMod = configEnableMe.Value;

            PatchAllHarmonyMethods();

            Logger.LogInfo($"Plugin BitchlandPickupItemsFromVendingMachinesAndTrashPilesBepInEx BepInEx is loaded!");
        }
        public static void PatchAllHarmonyMethods()
        {
            if (!enableThisMod)
            {
                return;
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(int_VendingMachine), "Interact", "Interact_VendingMachine", true, false);
                PatchHarmonyMethodUnity(typeof(int_SearchTrash), "Interact", "Interact_SearchTrash", true, false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
        }

        public static void PatchHarmonyMethodUnity(Type originalClass, string originalMethodName, string patchedMethodName, bool usePrefix, bool usePostfix, Type[] parameters = null)
        {
            string uniqueId = "com.wolfitdm.BitchlandPickupItemsFromVendingMachinesAndTrashPilesBepInEx";
            Type uniqueType = typeof(BitchlandPickupItemsFromVendingMachinesAndTrashPilesBepInEx);

            // Create a new Harmony instance with a unique ID
            var harmony = new Harmony(uniqueId);

            if (originalClass == null)
            {
                Logger.LogInfo($"GetType originalClass == null");
                return;
            }

            MethodInfo patched = null;

            try
            {
                patched = AccessTools.Method(uniqueType, patchedMethodName);
            }
            catch (Exception ex)
            {
                patched = null;
            }

            if (patched == null)
            {
                Logger.LogInfo($"AccessTool.Method patched {patchedMethodName} == null");
                return;

            }

            // Or apply patches manually
            MethodInfo original = null;

            try
            {
                if (parameters == null)
                {
                    original = AccessTools.Method(originalClass, originalMethodName);
                }
                else
                {
                    original = AccessTools.Method(originalClass, originalMethodName, parameters);
                }
            }
            catch (AmbiguousMatchException ex)
            {
                Type[] nullParameters = new Type[] { };
                try
                {
                    if (patched == null)
                    {
                        parameters = nullParameters;
                    }

                    ParameterInfo[] parameterInfos = patched.GetParameters();

                    if (parameterInfos == null || parameterInfos.Length == 0)
                    {
                        parameters = nullParameters;
                    }

                    List<Type> parametersN = new List<Type>();

                    for (int i = 0; i < parameterInfos.Length; i++)
                    {
                        ParameterInfo parameterInfo = parameterInfos[i];

                        if (parameterInfo == null)
                        {
                            continue;
                        }

                        if (parameterInfo.Name == null)
                        {
                            continue;
                        }

                        if (parameterInfo.Name.StartsWith("__"))
                        {
                            continue;
                        }

                        Type type = parameterInfos[i].ParameterType;

                        if (type == null)
                        {
                            continue;
                        }

                        parametersN.Add(type);
                    }

                    parameters = parametersN.ToArray();
                }
                catch (Exception ex2)
                {
                    parameters = nullParameters;
                }

                try
                {
                    original = AccessTools.Method(originalClass, originalMethodName, parameters);
                }
                catch (Exception ex2)
                {
                    original = null;
                }
            }
            catch (Exception ex)
            {
                original = null;
            }

            if (original == null)
            {
                Logger.LogInfo($"AccessTool.Method original {originalMethodName} == null");
                return;
            }

            HarmonyMethod patchedMethod = new HarmonyMethod(patched);
            var prefixMethod = usePrefix ? patchedMethod : null;
            var postfixMethod = usePostfix ? patchedMethod : null;

            harmony.Patch(original,
                prefix: prefixMethod,
                postfix: postfixMethod);
        }

        private static GameObject getItemByName(string name)
        {
            List<GameObject> Prefabs = Main.Instance.AllPrefabs;
            if (Prefabs == null)
            {
                return null;
            }
            int length = Prefabs.Count;
            for (int i = 0; i < length; i++)
            {
                if (Prefabs[i].IsNull())
                {
                    continue;
                }

                if (Prefabs[i].name == name)
                {
                    return Prefabs[i];
                }
            }
            return null;
        }

        private static void prepareBackpack(Person person)
        {
            if (Main.Instance.AllPrefabs == null)
            {
                return;
            }

            GameObject backpack2 = getItemByName("backpack2");
            
            if (backpack2 == null)
            {
                backpack2 = getItemByName("backpack");
            }

            if (backpack2 == null)
            {
                return;
            }

            if (Main.Instance.Player.CurrentBackpack == null)
            {
                Main.Instance.Player.DressClothe(Main.Spawn(backpack2));
            }

            if (Main.Instance.Player.CurrentBackpack != null)
            {
                try
                {
                    Main.Instance.Player.CurrentBackpack.ThisStorage.StorageMax = int.MaxValue;
                }
                catch (Exception ex)
                {
                }
            }
        }

        private static void addItemToPerson(Person person, GameObject item)
        {
            if (person != null && person.CurrentBackpack != null)
            {
                person.CurrentBackpack.ThisStorage.AddItem(item);
            }
        }
        public static bool Interact_VendingMachine(Person person, object __instance)
        {
            if (!enableThisMod)
            {
                return true;
            }

            prepareBackpack(person);

            int_VendingMachine _this = (int_VendingMachine)__instance;

            if (Main.Instance.Player.CurrentBackpack == null)
            {
                return true;
            }

            if (person.Money >= _this.ItemCost)
            {
                person.Money -= _this.ItemCost;
                GameObject item = Main.Spawn(_this.ItemPrefabs[UnityEngine.Random.Range(0, _this.ItemPrefabs.Length)]);
                if (!person.IsPlayer)
                    return false;
                addItemToPerson(Main.Instance.Player, item) ;
                Main.Instance.GameplayMenu.ShowNotification($"Paid {_this.ItemCost.ToString()} Bitch Notes");
            }
            else
            {
                if (!person.IsPlayer)
                    return false;
                Main.Instance.GameplayMenu.ShowNotification($"You don't have {_this.ItemCost.ToString()} Bitch Notes");
            }

            return false;
        }
        public static bool Interact_SearchTrash(Person person, object __instance)
        {
            if (!enableThisMod)
            {
                return true;
            }

            if (!person.IsPlayer)
            {
                return true;
            }
            else
            {
                int_SearchTrash _this = (int_SearchTrash)__instance;

                if (!_this.CanSearch)
                    return true;

                prepareBackpack(person);

                _this.Audio.PlayOneShot(Main.Instance.SearchTrashSounds[UnityEngine.Random.Range(0, Main.Instance.SearchTrashSounds.Length)]);

                if (!_this.Infinite)
                {
                    if (_this.Items <= 0)
                    {
                        Main.Instance.GameplayMenu.ShowNotification("It's empty, wait some time");
                        if ((double)_this.EmptyTimer == 0.0)
                        {
                            _this.EmptyTimer = 10f;
                            goto label_9;
                        }
                        goto label_9;
                    }
                    --_this.Items;
                }
                GameObject item = Main.Spawn(_this.RandomItems[UnityEngine.Random.Range(0, _this.RandomItems.Length)]);
                addItemToPerson(Main.Instance.Player, item);
            label_9:
                _this.InteractText = "(Searching)";
                _this.Timer = 1f;
                _this.enabled = true;
                _this.CanSearch = false;
                return false;
            }
        }
    }
}
