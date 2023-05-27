using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.LogiX.Data;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Linq;

namespace EnvironmentVariable
{
    public class EnvironmentVariable : NeosMod
    {
        public override String Name => "EnvironmentVariable";
        public override String Author => "HinanoAira";
        public override String Version => "1.0.0";
        public override String Link => "https://github.com/HinanoAira/EnvironmentVariable";

        private static ModConfiguration Config;
        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<string> Key = new ModConfigurationKey<string>("Key", "EnvironmentVariable Key", () => GeneratePassword(32));

        static private string GeneratePassword(int length)
        {
            var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var builder = new System.Text.StringBuilder();
            Random r = new Random();
            for (int i = 0; i < length; i++)
            {
                builder.Append(chars[r.Next(chars.Length)]);
            }

            return builder.ToString();
        }

        public override void OnEngineInit()
        {
            Config = GetConfiguration();
            Config.Save(true);
            Harmony harmony = new Harmony("jp.hinasense.EnvironmentVariable");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Output<string>))]
        [HarmonyPatch("Value")]
        [HarmonyPatch(MethodType.Getter)]
        class DynamicVariableInput_Value_Patch
        {
            static void Postfix(Output<string> __instance, ref string __result)
            {
                string variableName;
                if(__instance.Parent is DynamicVariableInput<string> d)
                {
                    variableName = d.VariableName;
                }
                else if (__instance.Parent is ReadDynamicVariable<string> r)
                {
                    variableName = r.VariableName.EvaluateRaw();
                }
                else
                {
                    return;
                }
                if (__instance.Name == "Value")
                {
                    if (variableName != null && variableName.StartsWith("EnvironmentVariable/"))
                    {
                        var comments = __instance.Slot.GetComponentsInParents<Comment>().Where(e => (e.Text.Value??"").StartsWith("EnvironmentVariableKey:"));
                        foreach (var comment in comments)
                        {
                            var key = comment.Text.Value.Substring(23);
                            if (key == Config.GetValue(Key))
                            {
                                var env = Environment.GetEnvironmentVariable(variableName.Substring(20));
                                __result = env;
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Output<bool>))]
        [HarmonyPatch("Value")]
        [HarmonyPatch(MethodType.Getter)]
        class DynamicVariableInput_HasValue_Patch
        {
            static void Postfix(Output<string> __instance, ref bool __result)
            {

                string variableName;
                if (__instance.Parent is DynamicVariableInput<string> d)
                {
                    variableName = d.VariableName;
                }
                else if (__instance.Parent is ReadDynamicVariable<string> r)
                {
                    variableName = r.VariableName.EvaluateRaw();
                }
                else
                {
                    return;
                }
                if (__instance.Name == "HasValue")
                {
                    if (variableName != null && variableName.StartsWith("EnvironmentVariable/"))
                    {
                        var comments = __instance.Slot.GetComponentsInParents<Comment>().Where(e => (e.Text.Value ?? "").StartsWith("EnvironmentVariableKey:"));
                        foreach (var comment in comments)
                        {
                            var key = comment.Text.Value.Substring(23);
                            if (key == Config.GetValue(Key))
                            {
                                var env = Environment.GetEnvironmentVariable(variableName.Substring(20));
                                __result = env != null;
                            }
                        }
                    }
                }
            }
        }
    }
}
