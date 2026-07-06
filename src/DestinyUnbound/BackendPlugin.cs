using GameData.Common;
using GameData.Domains;
using GameData.Domains.Story;
using GameData.Domains.TaiwuEvent;
using GameData.Domains.TaiwuEvent.EventOption;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using TaiwuModdingLib.Core.Plugin;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Config.EventConfig;
using GameData.Domains.TaiwuEvent;
using HarmonyLib;


namespace DestinyUnbound
{
    [PluginConfig("DestinyUnbound", "Dvoprm", "0.1.0")]
    public class BackendPlugin : TaiwuRemakePlugin
    {
        private Harmony _harmony;

        public override void Initialize()
        {
            DestinyLogger.Init();
            DestinyLogger.Log("========================================");
            DestinyLogger.Log("[DestinyUnbound] Diagnostic plugin loaded.");
            DestinyLogger.Log("[DestinyUnbound] ShenHuo skip enabled: this version writes DivineFlame prerequisite flags into archive data.");
            DestinyLogger.Log("========================================");

            _harmony = new Harmony("dvoprm.taiwu.destinyunbound");
            _harmony.PatchAll(typeof(BackendPlugin).Assembly);
        }

        public override void Dispose()
        {
            DestinyLogger.Log("[DestinyUnbound] Plugin unloaded.");
            _harmony?.UnpatchSelf();
        }
    }

    internal static class DestinyLogger
    {
        private static readonly object LockObj = new object();
        private static string _logPath;

        public static void Init()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _logPath = Path.Combine(baseDir, "DestinyUnbound_Diagnose.log");

                File.AppendAllText(
                    _logPath,
                    "\n\n===== DestinyUnbound Diagnose Start: " + DateTime.Now + " =====\n"
                );
            }
            catch
            {
                // 日志失败不能影响游戏启动
            }
        }

        public static void Log(string text)
        {
            try
            {
                lock (LockObj)
                {
                    if (string.IsNullOrEmpty(_logPath))
                    {
                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        _logPath = Path.Combine(baseDir, "DestinyUnbound_Diagnose.log");
                    }

                    File.AppendAllText(
                        _logPath,
                        DateTime.Now.ToString("HH:mm:ss.fff") + " " + text + "\n"
                    );
                }
            }
            catch
            {
                // 日志失败不能影响游戏
            }
        }

        public static void LogBlock(string title, string body)
        {
            Log("");
            Log("========== " + title + " ==========");
            foreach (string line in body.Split('\n'))
            {
                Log(line.TrimEnd('\r'));
            }
            Log("========== END " + title + " ==========");
            Log("");
        }
    }

    internal static class ReflectHelper
    {
        public static object GetMemberValue(object obj, string name)
        {
            if (obj == null)
                return null;

            Type type = obj.GetType();

            PropertyInfo prop = type.GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (prop != null)
            {
                try
                {
                    return prop.GetValue(obj);
                }
                catch
                {
                    return null;
                }
            }

            FieldInfo field = type.GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (field != null)
            {
                try
                {
                    return field.GetValue(obj);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public static object CallMethod(object obj, string methodName, params object[] args)
        {
            if (obj == null)
                return null;

            Type type = obj.GetType();

            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (method == null)
                return null;

            try
            {
                return method.Invoke(obj, args);
            }
            catch
            {
                return null;
            }
        }

        public static string ValueToString(object value)
        {
            return value == null ? "null" : value.ToString();
        }
    }

    internal static class QiyuanWandaoStateDumper
    {
        private static bool _dumped = false;

        public static void DumpOnce(string reason)
        {
            if (_dumped)
                return;

            _dumped = true;
            Dump(reason);
        }

        public static void Dump(string reason)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Reason = " + reason);
            sb.AppendLine("Time = " + DateTime.Now);

            DumpSectMainStoryStatus(sb);
            DumpIronPlateData(sb);
            DumpDivineFlameData(sb);
            DumpRanshanThreeCorpses(sb);

            DestinyLogger.LogBlock("QiyuanWandaoState", sb.ToString());
        }

        private static void DumpSectMainStoryStatus(StringBuilder sb)
        {
            sb.AppendLine("");
            sb.AppendLine("[SectMainStoryTaskStatus]");
            sb.AppendLine("说明：通常 0 = 未完成，1 = 好结局，2 = 坏结局。");

            try
            {
                StoryDomain story = DomainManager.Story;

                if (story == null)
                {
                    sb.AppendLine("DomainManager.Story = null");
                    return;
                }

                for (sbyte org = 1; org <= 15; org++)
                {
                    sbyte status = story.GetSectMainStoryTaskStatus(org);
                    sb.AppendLine(GetSectName(org) + " / Sect " + org + " = " + status);
                }
            }
            catch (Exception e)
            {
                sb.AppendLine("Failed: " + e.GetType().Name + " " + e.Message);
            }
        }

        private static void DumpIronPlateData(StringBuilder sb)
        {
            sb.AppendLine("");
            sb.AppendLine("[IronPlateData]");
            sb.AppendLine("说明：IronPlate / 铁牌线很可能与七元万道强相关。");

            try
            {
                object story = DomainManager.Story;
                object ironPlate = ReflectHelper.CallMethod(story, "GetIronPlateData");

                if (ironPlate == null)
                {
                    sb.AppendLine("GetIronPlateData() returned null or method not found.");
                    return;
                }

                sb.AppendLine("Type = " + ironPlate.GetType().FullName);
                sb.AppendLine("IsUnlocked = " + ReflectHelper.ValueToString(ReflectHelper.GetMemberValue(ironPlate, "IsUnlocked")));
                sb.AppendLine("FollowingCharId = " + ReflectHelper.ValueToString(ReflectHelper.GetMemberValue(ironPlate, "FollowingCharId")));
                sb.AppendLine("CooldownDate = " + ReflectHelper.ValueToString(ReflectHelper.GetMemberValue(ironPlate, "CooldownDate")));
            }
            catch (Exception e)
            {
                sb.AppendLine("Failed: " + e.GetType().Name + " " + e.Message);
            }
        }

        private static void DumpDivineFlameData(StringBuilder sb)
        {
            sb.AppendLine("");
            sb.AppendLine("[DivineFlameData]");
            sb.AppendLine("说明：DivineFlame / 神火线很可能与神火金身强相关。");

            try
            {
                object story = DomainManager.Story;
                object divineFlame = ReflectHelper.CallMethod(story, "GetDivineFlameData");

                if (divineFlame == null)
                {
                    sb.AppendLine("GetDivineFlameData() returned null or method not found.");
                    return;
                }

                sb.AppendLine("Type = " + divineFlame.GetType().FullName);
                sb.AppendLine("IsUnlocked = " + ReflectHelper.ValueToString(ReflectHelper.GetMemberValue(divineFlame, "IsUnlocked")));
                sb.AppendLine("CooldownDate = " + ReflectHelper.ValueToString(ReflectHelper.GetMemberValue(divineFlame, "CooldownDate")));
            }
            catch (Exception e)
            {
                sb.AppendLine("Failed: " + e.GetType().Name + " " + e.Message);
            }
        }

        private static void DumpRanshanThreeCorpses(StringBuilder sb)
        {
            sb.AppendLine("");
            sb.AppendLine("[RanshanThreeCorpses]");
            sb.AppendLine("说明：698=华居，699=玄质，700=迎娇。");

            try
            {
                object extra = DomainManager.Extra;

                if (extra == null)
                {
                    sb.AppendLine("DomainManager.Extra = null");
                    return;
                }

                DumpOneCorpse(sb, extra, 698, "华居 / RanshanHuaju");
                DumpOneCorpse(sb, extra, 699, "玄质 / RanshanXuanzhi");
                DumpOneCorpse(sb, extra, 700, "迎娇 / RanshanYingjiao");
            }
            catch (Exception e)
            {
                sb.AppendLine("Failed: " + e.GetType().Name + " " + e.Message);
            }
        }

        private static void DumpOneCorpse(StringBuilder sb, object extra, short templateId, string name)
        {
            sb.AppendLine("");
            sb.AppendLine("TemplateId = " + templateId + "  " + name);

            object corpse = ReflectHelper.CallMethod(
                extra,
                "GetRanshanThreeCorpsesCharacterByTemplateId",
                templateId
            );

            if (corpse == null)
            {
                sb.AppendLine("Data = null");
                sb.AppendLine("可能含义：该三尸数据不存在，或方法名/访问方式不同。");
                return;
            }

            sb.AppendLine("Type = " + corpse.GetType().FullName);

            object isGoodEnd = ReflectHelper.GetMemberValue(corpse, "IsGoodEnd");
            object isAroundTaiwu = ReflectHelper.GetMemberValue(corpse, "IsAroundTaiwu");
            object characterId = ReflectHelper.GetMemberValue(corpse, "CharacterId");
            object template = ReflectHelper.GetMemberValue(corpse, "TemplateId");

            sb.AppendLine("IsGoodEnd = " + ReflectHelper.ValueToString(isGoodEnd));
            sb.AppendLine("IsAroundTaiwu = " + ReflectHelper.ValueToString(isAroundTaiwu));
            sb.AppendLine("CharacterId = " + ReflectHelper.ValueToString(characterId));
            sb.AppendLine("TemplateIdField = " + ReflectHelper.ValueToString(template));
        }

        private static string GetSectName(sbyte org)
        {
            switch (org)
            {
                case 1: return "少林";
                case 2: return "峨眉";
                case 3: return "百花";
                case 4: return "武当";
                case 5: return "元山";
                case 6: return "狮相";
                case 7: return "然山";
                case 8: return "璇女";
                case 9: return "铸剑";
                case 10: return "空桑";
                case 11: return "金刚";
                case 12: return "五仙";
                case 13: return "界青";
                case 14: return "伏龙";
                case 15: return "血吼";
                default: return "未知";
            }
        }
    }

    internal static class QiyuanWandaoFixer
    {
        private static bool _fixed = false;

        public static void FixOnce(DataContext context)
        {
            //if (_fixed)
            //    return;

            _fixed = true;

            try
            {
                DestinyLogger.Log("[QiyuanWandaoFixer] Start fixing IronPlate and Ranshan Three Corpses.");

                //// 1. 解锁 IronPlate / 铁牌线
                //DomainManager.Story.SetIconPlateIsUnlocked(context, false);
                //DomainManager.Story.GmCmd_SetIconPlateIsUnlocked(context, false);
                //DestinyLogger.Log("[QiyuanWandaoFixer] IronPlate unlocked.");

                DomainManager.Story.GmCmd_SetDivineFlameIsUnlocked(context, true);

                // 1. 15 门派全部设为好结局 / 昌盛
                for (sbyte orgTemplateId = 1; orgTemplateId <= 15; orgTemplateId++)
                {
                    sbyte oldStatus = DomainManager.Story.GetSectMainStoryTaskStatus(orgTemplateId);

                    DestinyLogger.Log(
                        $"[QiyuanWandaoFixer] Sect {orgTemplateId} old status = {oldStatus}."
                    );

                    if (oldStatus != 1)
                    {
                        DomainManager.Story.SetSectMainStoryTaskStatus(context, orgTemplateId, 1);

                        DestinyLogger.Log(
                            $"[QiyuanWandaoFixer] Sect {orgTemplateId} set to GoodEnd / Prosperous."
                        );
                    }
                }

                // 2. 初始化/修复三尸基础数据
                DomainManager.Extra.GmCmd_AddThreeCorpses(context);
                DestinyLogger.Log("[QiyuanWandaoFixer] GmCmd_AddThreeCorpses called.");

                // 3. 三尸全部设置为好结局
                DomainManager.Extra.SetRanshanThreeCorpsesCharacterGoodEnd(context, 698);
                DomainManager.Extra.SetRanshanThreeCorpsesCharacterGoodEnd(context, 699);
                DomainManager.Extra.SetRanshanThreeCorpsesCharacterGoodEnd(context, 700);
                DestinyLogger.Log("[QiyuanWandaoFixer] Three corpses set GoodEnd = true.");

                // 4. 三尸全部设置为跟随太吾
                DomainManager.Extra.SetRanshanThreeCorpsesCharacterFollowing(context, 698, true);
                DomainManager.Extra.SetRanshanThreeCorpsesCharacterFollowing(context, 699, true);
                DomainManager.Extra.SetRanshanThreeCorpsesCharacterFollowing(context, 700, true);
                DestinyLogger.Log("[QiyuanWandaoFixer] Three corpses set IsAroundTaiwu = true.");

                DestinyLogger.Log("[QiyuanWandaoFixer] Fix done.");
            }
            catch (Exception e)
            {
                DestinyLogger.Log("[QiyuanWandaoFixer] Failed: " + e);
            }
        }
    }

    internal static class ShenHuoJinShenFixer
    {
        private static bool _fixed = false;

        // true：读档后自动写入神火线前置变量，使出神之地终点能进入紫无绡长绳分支。
        // false：只保留代码，不实际修改存档。
        private const bool EnableShenHuoSkip = true;

        public static void FixOnce(DataContext context)
        {
            if (_fixed)
                return;

            _fixed = true;

            if (!EnableShenHuoSkip)
            {
                DestinyLogger.Log("[ShenHuoJinShenFixer] Skip disabled by EnableShenHuoSkip = false.");
                return;
            }

            try
            {
                DestinyLogger.Log("[ShenHuoJinShenFixer] Start forcing DivineFlame / ShenHuo prerequisite flags.");

                object argBox = GlobalArgBoxReflection.GetGlobalEventArgumentBox();

                if (argBox == null)
                {
                    DestinyLogger.Log("[ShenHuoJinShenFixer] Failed: GlobalEventArgumentBox is null.");
                    return;
                }

                // 原流程：第 8 个紫竹化身神火前置故事完成后，会写入 MainStoryLineDivineFlame。
                // 这里直接伪造成“已完成 8 个前置故事”。
                GlobalArgBoxReflection.SetInt(argBox, "PreDivineFlameXiangshuImpersonatorStoriesFinished", 8);

                // 任务链/任务显示相关计数。不是出神之地长绳分支的直接判断，但一起补上更稳。
                GlobalArgBoxReflection.SetInt(argBox, "DivineflameTaskFinished", 8);

                // 出神之地入口脚本通过 CheckGlobalArgBox("MainStoryLineDivineFlame") 判断神火线资格。
                // 脚本只需要这个 key 存在；原事件中保存的值就是 0，因此这里也写 0。
                GlobalArgBoxReflection.SetInt(argBox, "MainStoryLineDivineFlame", 0);

                // 不要提前写 MainStoryEndingLine。
                // MainStoryEndingLine = 0 应由接受紫无绡救援后的 4f69f6a5... 事件自然写入。
                GlobalArgBoxReflection.SaveGlobalEventArgumentBox();

                // 额外打开 StoryDomain 里的 DivineFlame 解锁标记；这不是长绳分支的核心判断，
                // 但可以减少其它 UI/系统状态不一致的风险。
                try
                {
                    DomainManager.Story.GmCmd_SetDivineFlameIsUnlocked(context, true);
                    DestinyLogger.Log("[ShenHuoJinShenFixer] GmCmd_SetDivineFlameIsUnlocked(true) called.");
                }
                catch (Exception inner)
                {
                    DestinyLogger.Log("[ShenHuoJinShenFixer] GmCmd_SetDivineFlameIsUnlocked failed: " + inner.GetType().Name + " " + inner.Message);
                }

                DestinyLogger.Log("[ShenHuoJinShenFixer] Set PreDivineFlameXiangshuImpersonatorStoriesFinished = 8.");
                DestinyLogger.Log("[ShenHuoJinShenFixer] Set DivineflameTaskFinished = 8.");
                DestinyLogger.Log("[ShenHuoJinShenFixer] Set MainStoryLineDivineFlame = 0.");
                DestinyLogger.Log("[ShenHuoJinShenFixer] Fix done. Do NOT set MainStoryEndingLine here.");
            }
            catch (Exception e)
            {
                DestinyLogger.Log("[ShenHuoJinShenFixer] Failed: " + e);
            }
        }
    }

    internal static class GlobalArgBoxReflection
    {
        public static object GetGlobalEventArgumentBox()
        {
            try
            {
                object taiwuEventDomain = DomainManager.TaiwuEvent;
                if (taiwuEventDomain == null)
                {
                    DestinyLogger.Log("[GlobalArgBoxReflection] DomainManager.TaiwuEvent is null.");
                    return null;
                }

                MethodInfo method = taiwuEventDomain.GetType().GetMethod(
                    "GetGlobalEventArgumentBox",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (method == null)
                {
                    DestinyLogger.Log("[GlobalArgBoxReflection] GetGlobalEventArgumentBox method not found.");
                    return null;
                }

                return method.Invoke(taiwuEventDomain, null);
            }
            catch (Exception e)
            {
                DestinyLogger.Log("[GlobalArgBoxReflection] GetGlobalEventArgumentBox failed: " + e);
                return null;
            }
        }

        public static void SaveGlobalEventArgumentBox()
        {
            try
            {
                object taiwuEventDomain = DomainManager.TaiwuEvent;
                if (taiwuEventDomain == null)
                {
                    DestinyLogger.Log("[GlobalArgBoxReflection] DomainManager.TaiwuEvent is null when saving.");
                    return;
                }

                MethodInfo method = taiwuEventDomain.GetType().GetMethod(
                    "SaveGlobalEventArgumentBox",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (method == null)
                {
                    DestinyLogger.Log("[GlobalArgBoxReflection] SaveGlobalEventArgumentBox method not found.");
                    return;
                }

                method.Invoke(taiwuEventDomain, null);
            }
            catch (Exception e)
            {
                DestinyLogger.Log("[GlobalArgBoxReflection] SaveGlobalEventArgumentBox failed: " + e);
            }
        }

        public static void SetInt(object argBox, string key, int value)
        {
            if (argBox == null)
            {
                DestinyLogger.Log("[GlobalArgBoxReflection] SetInt failed: argBox is null. Key = " + key);
                return;
            }

            try
            {
                Type type = argBox.GetType();

                // 优先找普通 Set(string, int) 或 Set(string, object)
                MethodInfo[] methods = type.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "Set")
                        continue;

                    ParameterInfo[] ps = method.GetParameters();
                    if (ps.Length != 2)
                        continue;

                    if (ps[0].ParameterType != typeof(string))
                        continue;

                    if (method.IsGenericMethodDefinition)
                    {
                        MethodInfo generic = method.MakeGenericMethod(typeof(int));
                        generic.Invoke(argBox, new object[] { key, value });
                        DestinyLogger.Log("[GlobalArgBoxReflection] SetInt generic: " + key + " = " + value);
                        return;
                    }

                    object secondArg;

                    if (ps[1].ParameterType == typeof(int))
                    {
                        secondArg = value;
                    }
                    else if (ps[1].ParameterType == typeof(short))
                    {
                        secondArg = (short)value;
                    }
                    else if (ps[1].ParameterType == typeof(sbyte))
                    {
                        secondArg = (sbyte)value;
                    }
                    else if (ps[1].ParameterType == typeof(object))
                    {
                        secondArg = value;
                    }
                    else
                    {
                        continue;
                    }

                    method.Invoke(argBox, new object[] { key, secondArg });
                    DestinyLogger.Log("[GlobalArgBoxReflection] SetInt: " + key + " = " + value);
                    return;
                }

                DestinyLogger.Log("[GlobalArgBoxReflection] Set method not found. Key = " + key);
            }
            catch (Exception e)
            {
                DestinyLogger.Log("[GlobalArgBoxReflection] SetInt failed. Key = " + key + ", Error = " + e);
            }
        }
    }

    [HarmonyPatch(typeof(StoryDomain), nameof(StoryDomain.OnCurrWorldArchiveDataReady))]
    public static class Patch_StoryDomain_OnCurrWorldArchiveDataReady_Dump
    {
        [HarmonyPostfix]
        public static void Postfix(DataContext context, bool isNewWorld)
        {
            if (isNewWorld)
            {
                DestinyLogger.Log("[DestinyUnbound] New world detected. Skip diagnose dump.");
                return;
            }

            //// 修复前先打印一次
            //QiyuanWandaoStateDumper.Dump("Before Fix");

            //// 执行修复
            //QiyuanWandaoFixer.FixOnce(context);

            //// 修复后再打印一次
            //QiyuanWandaoStateDumper.Dump("After Fix");

            // 执行修复
            ShenHuoJinShenFixer.FixOnce(context);
        }
    }
}

[HarmonyPatch(typeof(EventScriptRuntime), nameof(EventScriptRuntime.LoadPackageScripts))]
internal static class LoadPackageScriptsDumpPatch
{
    private static void Postfix(EventPackage package, string packageScriptPath)
    {
        //TwesScriptDumper.DumpPackageScripts(package, packageScriptPath);
    }
}

internal static class TwesScriptDumper
{
    private const int MaxDepth = 16;
    private const int MaxEnumerableItems = 500;

    public static void DumpPackageScripts(EventPackage package, string packageScriptPath)
    {
        try
        {
            if (package == null || string.IsNullOrEmpty(packageScriptPath))
            {
                return;
            }

            string packageName = Path.GetFileNameWithoutExtension(packageScriptPath);

            // 只 dump 出神之地事件包
            if (!packageName.Contains("MainStory_LandOfTrance"))
            {
                return;
            }

            string dumpRoot = GetGameRootDumpDir(packageScriptPath);
            string dumpDir = Path.Combine(dumpRoot, packageName);
            Directory.CreateDirectory(dumpDir);

            List<TaiwuEventItem> events = package.GetAllEvents();

            var summary = new StringBuilder();
            summary.AppendLine("Package: " + packageName);
            summary.AppendLine("Path: " + packageScriptPath);
            summary.AppendLine("EventCount: " + events.Count);
            summary.AppendLine();

            foreach (TaiwuEventItem eventItem in events)
            {
                if (eventItem == null)
                {
                    continue;
                }

                // 只看出神之地组
                if (eventItem.EventGroup != "MainStory_LandOfTrance")
                {
                    continue;
                }

                string eventGuid = eventItem.Guid.ToString();

                summary.AppendLine("==================================================");
                summary.AppendLine("EventGuid: " + eventGuid);
                summary.AppendLine("EventGroup: " + eventItem.EventGroup);
                summary.AppendLine("MainRoleKey: " + eventItem.MainRoleKey);
                summary.AppendLine("TargetRoleKey: " + eventItem.TargetRoleKey);
                summary.AppendLine("EventContent: " + SafeText(eventItem.EventContent));
                summary.AppendLine("Options:");

                DumpOne(
                    Path.Combine(dumpDir, $"{eventGuid}_EventScript.txt"),
                    eventItem.Script
                );

                DumpOne(
                    Path.Combine(dumpDir, $"{eventGuid}_EventConditions.txt"),
                    eventItem.Conditions
                );

                TaiwuEventOption[] options = eventItem.EventOptions;
                if (options == null)
                {
                    summary.AppendLine("  <null>");
                    continue;
                }

                foreach (TaiwuEventOption option in options)
                {
                    if (option == null)
                    {
                        continue;
                    }

                    string optionGuid = option.OptionGuid ?? "null-option-guid";
                    string optionKey = option.OptionKey ?? "null-option-key";

                    summary.AppendLine($"  OptionKey: {optionKey}");
                    summary.AppendLine($"  OptionGuid: {optionGuid}");
                    summary.AppendLine($"  OptionContent: {SafeText(option.OptionContent)}");

                    string prefix = $"{eventGuid}_{optionGuid}";

                    DumpOne(
                        Path.Combine(dumpDir, $"{prefix}_OptionScript.txt"),
                        option.Script
                    );

                    DumpOne(
                        Path.Combine(dumpDir, $"{prefix}_OptionAvailableConditions.txt"),
                        option.AvailableConditions
                    );

                    DumpOne(
                        Path.Combine(dumpDir, $"{prefix}_OptionVisibleConditions.txt"),
                        option.VisibleConditions
                    );
                }

                summary.AppendLine();
            }

            File.WriteAllText(
                Path.Combine(dumpDir, "_summary.txt"),
                summary.ToString(),
                Encoding.UTF8
            );
        }
        catch (Exception ex)
        {
            try
            {
                string dumpRoot = GetGameRootDumpDir(packageScriptPath);
                Directory.CreateDirectory(dumpRoot);
                File.WriteAllText(
                    Path.Combine(dumpRoot, "DumpError.txt"),
                    ex.ToString(),
                    Encoding.UTF8
                );
            }
            catch
            {
                // 不再抛出，避免影响游戏启动
            }
        }
    }

    private static string GetGameRootDumpDir(string packageScriptPath)
    {
        try
        {
            string fullPath = Path.GetFullPath(packageScriptPath);
            string scriptDir = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrEmpty(scriptDir))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TwesDump");
            }

            DirectoryInfo dir = new DirectoryInfo(scriptDir);

            // packageScriptPath 通常类似：
            // ...\\The Scroll Of Taiwu\\Event\\EventScript\\Taiwu_EventPackage_MainStory_LandOfTrance.twes
            // 向上找到 Event 目录，然后取它的父目录作为游戏根目录。
            while (dir != null && !string.Equals(dir.Name, "Event", StringComparison.OrdinalIgnoreCase))
            {
                dir = dir.Parent;
            }

            if (dir != null && dir.Parent != null)
            {
                return Path.Combine(dir.Parent.FullName, "TwesDump");
            }

            // 兜底：如果没找到 Event 目录，就导出到 .twes 同级目录下。
            return Path.Combine(scriptDir, "TwesDump");
        }
        catch
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TwesDump");
        }
    }

    private static void DumpOne(string path, object obj)
    {
        string text = DumpObject(obj);
        File.WriteAllText(path, text, Encoding.UTF8);
    }

    private static string DumpObject(object obj)
    {
        var sb = new StringBuilder();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        DumpObjectRecursive(obj, sb, 0, visited);
        return sb.ToString();
    }

    private static void DumpObjectRecursive(
        object obj,
        StringBuilder sb,
        int depth,
        HashSet<object> visited)
    {
        string indent = new string(' ', depth * 2);

        if (depth > MaxDepth)
        {
            sb.AppendLine(indent + "<max depth>");
            return;
        }

        if (obj == null)
        {
            sb.AppendLine(indent + "null");
            return;
        }

        Type type = obj.GetType();

        if (IsSimpleType(type, obj))
        {
            sb.AppendLine(indent + obj);
            return;
        }

        if (obj is Delegate del)
        {
            sb.AppendLine(indent + $"<delegate {del.Method.DeclaringType?.FullName}.{del.Method.Name}>");
            return;
        }

        if (obj is Type t)
        {
            sb.AppendLine(indent + $"<type {t.FullName}>");
            return;
        }

        if (visited.Contains(obj))
        {
            sb.AppendLine(indent + "<visited>");
            return;
        }

        visited.Add(obj);

        sb.AppendLine(indent + type.FullName);

        if (obj is IEnumerable enumerable && !(obj is string))
        {
            int index = 0;
            foreach (object item in enumerable)
            {
                if (index >= MaxEnumerableItems)
                {
                    sb.AppendLine(indent + $"<more than {MaxEnumerableItems} items>");
                    break;
                }

                sb.AppendLine(indent + $"[{index}]");
                DumpObjectRecursive(item, sb, depth + 1, visited);
                index++;
            }

            return;
        }

        FieldInfo[] fields = type.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (FieldInfo field in fields)
        {
            object value;
            try
            {
                value = field.GetValue(obj);
            }
            catch
            {
                continue;
            }

            sb.AppendLine(indent + field.Name + ":");
            DumpObjectRecursive(value, sb, depth + 1, visited);
        }
    }

    private static bool IsSimpleType(Type type, object obj)
    {
        return type.IsPrimitive
               || type.IsEnum
               || obj is string
               || obj is decimal
               || obj is Guid
               || obj is DateTime;
    }

    private static string SafeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        return text.Replace("\r", "\\r").Replace("\n", "\\n");
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}