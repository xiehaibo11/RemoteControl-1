using System;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private static string GetSkinFamilyKey(string skinFile)
        {
            string dir = System.IO.Path.GetDirectoryName(skinFile);
            if (string.IsNullOrEmpty(dir))
                return System.IO.Path.GetFileNameWithoutExtension(skinFile);
            return System.IO.Path.GetFileName(dir);
        }

        private static string GetSkinFamilyDisplayName(string familyKey)
        {
            string key = (familyKey ?? "").ToLowerInvariant();
            switch (key)
            {
                case "carlmness":
                case "calmness":
                    return "宁静";
                case "deep":
                    return "深色";
                case "diamond":
                    return "钻石";
                case "eighteen":
                    return "十八号";
                case "emerald":
                    return "翡翠";
                case "glass":
                    return "玻璃";
                case "longhorn":
                    return "长角风格";
                case "macos":
                    return "苹果风格";
                case "midsummer":
                    return "仲夏";
                case "mp10":
                    return "媒体播放器10";
                case "msn":
                    return "MSN风格";
                case "office2007":
                    return "Office 2007";
                case "one":
                    return "简约";
                case "page":
                    return "页面";
                case "realone":
                    return "RealOne风格";
                case "silver":
                    return "银色";
                case "sports":
                    return "运动";
                case "steel":
                    return "钢铁";
                case "vista1":
                    return "Vista 一";
                case "vista2":
                    return "Vista 二";
                case "warm":
                    return "暖色";
                case "wave":
                    return "波浪";
                case "winxp":
                    return "Windows XP";
                default:
                    return familyKey;
            }
        }

        private static string GetSkinVariantDisplayName(string skinFile, string familyKey)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(skinFile);
            string variant = RemoveSkinFamilyPrefix(name, familyKey);
            if (string.IsNullOrEmpty(variant))
                return "默认";

            variant = variant.Trim('_', '-', ' ');
            if (variant.StartsWith("color", StringComparison.OrdinalIgnoreCase))
            {
                string number = variant.Substring("color".Length);
                return "颜色" + ToChineseNumber(number);
            }
            if (variant.StartsWith("_color", StringComparison.OrdinalIgnoreCase))
            {
                string number = variant.Substring("_color".Length);
                return "颜色" + ToChineseNumber(number);
            }
            return TranslateSkinColor(variant);
        }

        private static string RemoveSkinFamilyPrefix(string name, string familyKey)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            string result = name;
            if (!string.IsNullOrEmpty(familyKey) && result.StartsWith(familyKey, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(familyKey.Length);
            }
            else if (result.StartsWith("XP", StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(2);
            }

            if (result.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(familyKey) &&
                name.Equals(familyKey, StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            return result;
        }

        private static string TranslateSkinColor(string value)
        {
            string key = (value ?? "").Trim('_', '-', ' ').ToLowerInvariant();
            switch (key)
            {
                case "":
                    return "默认";
                case "blue":
                    return "蓝色";
                case "green":
                    return "绿色";
                case "orange":
                    return "橙色";
                case "olive":
                    return "橄榄色";
                case "purple":
                    return "紫色";
                case "red":
                    return "红色";
                case "cyan":
                    return "青色";
                case "brown":
                    return "棕色";
                case "black":
                    return "黑色";
                case "maroon":
                    return "栗色";
                case "mulberry":
                    return "桑葚色";
                case "pink":
                    return "粉色";
                case "silver":
                    return "银色";
                default:
                    if (key.StartsWith("color"))
                        return "颜色" + ToChineseNumber(key.Substring("color".Length));
                    return value;
            }
        }

        private static string ToChineseNumber(string number)
        {
            switch ((number ?? "").Trim())
            {
                case "1":
                    return "一";
                case "2":
                    return "二";
                case "3":
                    return "三";
                case "4":
                    return "四";
                case "5":
                    return "五";
                case "6":
                    return "六";
                case "7":
                    return "七";
                default:
                    return number;
            }
        }
    }
}
