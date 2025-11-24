using UnityEngine;
using System;
using System.Collections.Generic;

public static class StudyStats
{
    // 產生某日期的 key，例如 "StudySeconds_20251123"
    static string GetKeyForDate(DateTime date)
    {
        string d = date.ToString("yyyyMMdd");
        return "StudySeconds_" + d;
    }

    public static string GetTodayKey()
    {
        return GetKeyForDate(DateTime.Now);
    }

    // ✅ 累積今天的秒數（整數秒）
    public static void AddStudySecondsInt(int seconds)
    {
        string key = GetTodayKey();
        int baseInt = Mathf.FloorToInt(PlayerPrefs.GetFloat(key, 0f));
        int total = baseInt + Mathf.Max(seconds, 0);
        PlayerPrefs.SetFloat(key, total);
        PlayerPrefs.Save();
    }

    // 取得今天累積秒數（float 但實際是整數）
    public static float GetTodayStudySeconds()
    {
        string key = GetTodayKey();
        return PlayerPrefs.GetFloat(key, 0f);
    }

    // 取得「某一天」的秒數
    public static float GetStudySecondsOnDate(DateTime date)
    {
        string key = GetKeyForDate(date);
        return PlayerPrefs.GetFloat(key, 0f);
    }

    // 取得「最近 N 天」的秒數列表（包含今天）
    // 例如 N = 7 → [6 天前, ..., 昨天, 今天] 共 7 個
    public static List<float> GetPastDaysSeconds(int days)
    {
        List<float> list = new List<float>();
        DateTime today = DateTime.Now;

        for (int i = days - 1; i >= 0; i--)
        {
            DateTime d = today.AddDays(-i);
            list.Add(GetStudySecondsOnDate(d));
        }

        return list;
    }
}
