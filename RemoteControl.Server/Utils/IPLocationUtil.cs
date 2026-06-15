using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace RemoteControl.Server.Utils
{
    /// <summary>
    /// IP地理位置解析工具（使用 ip-api.com 免费服务）
    /// </summary>
    static class IPLocationUtil
    {
        private static readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
        private static readonly object _cacheLock = new object();

        /// <summary>
        /// 异步查询IP地理位置，结果通过回调返回
        /// </summary>
        public static void QueryAsync(string ip, Action<string> callback)
        {
            if (string.IsNullOrEmpty(ip) || ip == "-")
            {
                callback("-");
                return;
            }

            // 检查缓存
            lock (_cacheLock)
            {
                if (_cache.ContainsKey(ip))
                {
                    callback(_cache[ip]);
                    return;
                }
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                string location = QueryLocation(ip);
                lock (_cacheLock)
                {
                    _cache[ip] = location;
                }
                callback(location);
            });
        }

        private static string QueryLocation(string ip)
        {
            try
            {
                // ip-api.com 免费API，返回JSON，限制45次/分钟
                string url = "http://ip-api.com/json/" + ip + "?fields=country,regionName,city&lang=zh-CN";
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    string json = client.DownloadString(url);
                    return ParseLocation(json);
                }
            }
            catch
            {
                return QueryLocationFallback(ip);
            }
        }

        private static string QueryLocationFallback(string ip)
        {
            try
            {
                // 备用：使用 ip.sb
                string url = "https://api.ip.sb/geoip/" + ip;
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers.Add("User-Agent", "Mozilla/5.0");
                    string json = client.DownloadString(url);
                    return ParseLocationSb(json);
                }
            }
            catch
            {
                return "-";
            }
        }

        /// <summary>
        /// 解析 ip-api.com 返回的JSON
        /// 格式: {"country":"中国","regionName":"广东","city":"深圳"}
        /// </summary>
        private static string ParseLocation(string json)
        {
            string country = ExtractJsonValue(json, "country");
            string region = ExtractJsonValue(json, "regionName");
            string city = ExtractJsonValue(json, "city");

            if (string.IsNullOrEmpty(country))
                return "-";

            StringBuilder sb = new StringBuilder();
            sb.Append(country);
            if (!string.IsNullOrEmpty(region) && region != country)
                sb.Append(" " + region);
            if (!string.IsNullOrEmpty(city) && city != region)
                sb.Append(" " + city);
            return sb.ToString();
        }

        /// <summary>
        /// 解析 ip.sb 返回的JSON
        /// </summary>
        private static string ParseLocationSb(string json)
        {
            string country = ExtractJsonValue(json, "country");
            string region = ExtractJsonValue(json, "region");
            string city = ExtractJsonValue(json, "city");

            if (string.IsNullOrEmpty(country))
                return "-";

            StringBuilder sb = new StringBuilder();
            sb.Append(country);
            if (!string.IsNullOrEmpty(region) && region != country)
                sb.Append(" " + region);
            if (!string.IsNullOrEmpty(city) && city != region)
                sb.Append(" " + city);
            return sb.ToString();
        }

        private static string ExtractJsonValue(string json, string key)
        {
            string pattern = "\"" + key + "\":\"";
            int start = json.IndexOf(pattern);
            if (start < 0) return "";
            start += pattern.Length;
            int end = json.IndexOf("\"", start);
            if (end < 0) return "";
            return json.Substring(start, end - start);
        }
    }
}
