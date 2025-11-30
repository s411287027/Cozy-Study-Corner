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

    // 取得「今天」用的 key
    public static string GetTodayKey()
    {
        return GetKeyForDate(DateTime.Now);
    }

    // ✅ 累積今天的秒數（整數秒）
    public static void AddStudySecondsInt(int seconds)
    {
        string key = GetTodayKey();
        // 原本已存的秒數（用 float 存，但實際上是整數）
        int baseInt = Mathf.FloorToInt(PlayerPrefs.GetFloat(key, 0f));
        // 新的總秒數 = 舊的 + 這次增加的（至少 0）
        int total = baseInt + Mathf.Max(seconds, 0);
        PlayerPrefs.SetFloat(key, total);
        PlayerPrefs.Save();
    }

    // 取得今天累積秒數（float，但實際內容是整數）
    public static float GetTodayStudySeconds()
    {
        string key = GetTodayKey();
        return PlayerPrefs.GetFloat(key, 0f);
    }

    // 方便一點：直接用 int 拿今天秒數
    public static int GetTodayStudySecondsInt()
    {
        return Mathf.FloorToInt(GetTodayStudySeconds());
    }

    // 取得「某一天」的秒數
    public static float GetStudySecondsOnDate(DateTime date)
    {
        string key = GetKeyForDate(date);
        return PlayerPrefs.GetFloat(key, 0f);
    }

    // 同上，但回傳 int
    public static int GetStudySecondsOnDateInt(DateTime date)
    {
        return Mathf.FloorToInt(GetStudySecondsOnDate(date));
    }

    // 取得「最近 N 天」的秒數列表（包含今天）
    // 例如 N = 7 → [6 天前, ..., 昨天, 今天] 共 7 個（float 版）
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

    // （方便畫圖用）取得「最近 N 天」的秒數列表（int 版）
    public static List<int> GetPastDaysSecondsInt(int days)
    {
        List<int> list = new List<int>();
        DateTime today = DateTime.Now;

        for (int i = days - 1; i >= 0; i--)
        {
            DateTime d = today.AddDays(-i);
            list.Add(GetStudySecondsOnDateInt(d));
        }

        return list;
    }

    // ✅ 取得「最近 N 天」的總秒數（包含今天）
    public static int GetLastNDaysTotalSeconds(int days)
    {
        int total = 0;
        DateTime today = DateTime.Now;

        for (int i = 0; i < days; i++)
        {
            DateTime d = today.AddDays(-i);  // i=0 → 今天, i=1 → 昨天 ...
            total += GetStudySecondsOnDateInt(d);
        }

        return total;
    }

    // ✅ 最近 7 天總秒數（可以當作「這週」的概念）
    public static int GetLast7DaysTotalSeconds()
    {
        return GetLastNDaysTotalSeconds(7);
    }

    // ✅ 本月總秒數
    public static int GetThisMonthTotalSeconds()
    {
        DateTime today = DateTime.Now;
        int year = today.Year;
        int month = today.Month;

        int daysInMonth = DateTime.DaysInMonth(year, month);
        int total = 0;

        for (int day = 1; day <= daysInMonth; day++)
        {
            DateTime d = new DateTime(year, month, day);
            total += GetStudySecondsOnDateInt(d);
        }

        return total;
    }

    // 本月每天的秒數列表，[1 號, 2 號, ..., 最後一天]
    public static List<int> GetThisMonthDailySeconds()
    {
        List<int> list = new List<int>();

        DateTime today = DateTime.Now;
        int year = today.Year;
        int month = today.Month;
        int daysInMonth = DateTime.DaysInMonth(year, month);

        for (int day = 1; day <= daysInMonth; day++)
        {
            DateTime d = new DateTime(year, month, day);
            list.Add(GetStudySecondsOnDateInt(d));
        }

        return list;
    }
}
