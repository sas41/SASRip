using SASRip.Helpers;
using SASRip.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Timers;

namespace SASRip.Services;

public class LoggerService : ILogger
{
    private struct LogLine
    {
        public LogLine(string url, string source, string type, string status)
        {
            URL = url;
            Source = source;
            Type = type;
            Status = status;
            EventTime = DateTime.Now;
        }

        public string URL { get; set; }
        public string Source { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }

        public DateTime EventTime { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(EventTime.ToString(DateFormat, CultureInfo.InvariantCulture));
            sb.Append(CSVDelimeter);
            sb.Append(EventTime.ToString(TimeFormat, CultureInfo.InvariantCulture));
            sb.Append(CSVDelimeter);
            sb.Append(Status.Replace(CSVDelimeter, '-'));
            sb.Append(CSVDelimeter);
            sb.Append(URL.Replace(CSVDelimeter, '-'));
            sb.Append(CSVDelimeter);
            sb.Append(Type.Replace(CSVDelimeter, '-'));
            sb.Append(CSVDelimeter);
            sb.Append(Source.Replace(CSVDelimeter, '-'));
            sb.Append(CSVDelimeter);
            return sb.ToString();
        }
    }

    private static string DateFormat = "yyyy-MM-dd";
    private static string TimeFormat = "HH:mm:ss.FFFFFFF";
    private static char CSVDelimeter = ';';
    private static string loggingPath;
    private static Dictionary<string, DateTime> LastHashHitTime = new Dictionary<string, DateTime>();
    private static Dictionary<string, List<LogLine>> WriteCache = new Dictionary<string, List<LogLine>>();
    private static Timer WriteTimer;
    private static bool Locked = false;

    public LoggerService(IConfigurationService cfg)
    {
        loggingPath = cfg.LogPath;
        WriteTimer = new Timer(1000);
        WriteTimer.AutoReset = true;
        WriteTimer.Elapsed += OnWriteTimerElapsed;
        WriteTimer.Enabled = true;
    }

    private static void Write()
    {
        Locked = true;
        StringBuilder sb = new StringBuilder();
        List<string> toRemove = new List<string>();
        foreach (KeyValuePair<string, List<LogLine>> kvp in WriteCache)
        {
            sb.Clear();
            string date = LastHashHitTime[kvp.Key].ToString(DateFormat, CultureInfo.InvariantCulture);
            string time = LastHashHitTime[kvp.Key].ToString(TimeFormat, CultureInfo.InvariantCulture);
            string filePath = $"{loggingPath}/{date}.csv";

            if (!Directory.Exists(loggingPath))
            {
                Directory.CreateDirectory(loggingPath);
            }

            if (!File.Exists(filePath))
            {
                FileStream file = File.Create(filePath);
                file.Close();
                File.AppendAllText(filePath, $"sep={CSVDelimeter}{Environment.NewLine}");
            }

            if (IsStale(kvp.Key))
            {
                foreach (LogLine line in kvp.Value)
                {
                    sb.Append(kvp.Key);
                    sb.Append(CSVDelimeter);
                    sb.Append(date);
                    sb.Append(CSVDelimeter);
                    sb.Append(time);
                    sb.Append(CSVDelimeter);
                    sb.Append(line.ToString());
                    sb.Append(CSVDelimeter);
                    sb.AppendLine();
                }
                sb.AppendLine();
                File.AppendAllText(filePath, sb.ToString());
                toRemove.Add(kvp.Key);
            }
        }

        foreach (string key in toRemove)
        {
            WriteCache.Remove(key);
            LastHashHitTime.Remove(key);
        }
        Locked = false;
    }

    private static bool IsStale(string hash)
    {
        DateTime lastActivity = LastHashHitTime[hash];
        return DateTime.Now.Subtract(lastActivity) > TimeSpan.FromMinutes(0.5);
    }

    private static void OnWriteTimerElapsed(Object source, ElapsedEventArgs e)
    {
        if (!Locked)
        {
            Write();
        }
    }

    public void Log(string hash, string url, string requestSource, bool isVideo, RequestStatus status)
    {
        if (LastHashHitTime.ContainsKey(hash))
        {
            LastHashHitTime[hash] = DateTime.Now;
        }
        else
        {
            LastHashHitTime.Add(hash, DateTime.Now);
        }

        if (!WriteCache.ContainsKey(hash))
        {
            WriteCache[hash] = new List<LogLine>();
        }

        WriteCache[hash].Add(new LogLine(url, requestSource, isVideo ? "Video" : "Audio", status.ToString()));
    }

    public void LogError(string hash, string url, string requestSource, bool isVideo, string error)
    {
        if (LastHashHitTime.ContainsKey(hash))
        {
            LastHashHitTime[hash] = DateTime.Now;
        }
        else
        {
            LastHashHitTime.Add(hash, DateTime.Now);
        }

        if (!WriteCache.ContainsKey(hash))
        {
            WriteCache[hash] = new List<LogLine>();
        }

        WriteCache[hash].Add(new LogLine(url, requestSource, isVideo ? "Video" : "Audio", error));
    }
}
