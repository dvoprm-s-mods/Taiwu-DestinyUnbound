using System;
using System.IO;
using System.Reflection;
using System.Text;

using GameData.Common;
using GameData.Domains;
using GameData.Domains.Story;

using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

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
            DestinyLogger.Log("[DestinyUnbound] This version only logs states. It does NOT modify archive data.");
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
            if (_fixed)
                return;

            _fixed = true;

            try
            {
                DestinyLogger.Log("[QiyuanWandaoFixer] Start fixing IronPlate and Ranshan Three Corpses.");

                //// 1. 解锁 IronPlate / 铁牌线
                //DomainManager.Story.SetIconPlateIsUnlocked(context, true);
                //DestinyLogger.Log("[QiyuanWandaoFixer] IronPlate unlocked.");

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

            // 修复前先打印一次
            QiyuanWandaoStateDumper.Dump("Before Fix");

            // 执行修复
            QiyuanWandaoFixer.FixOnce(context);

            // 修复后再打印一次
            QiyuanWandaoStateDumper.Dump("After Fix");
        }
    }
}