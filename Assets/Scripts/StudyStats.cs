using UnityEngine;
using System;
using System.Collections.Generic;

public static class StudyStats
{
    static string GetKeyForDate(DateTime date)
    {
        string d = date.ToString("yyyyMMdd");
        return "StudySeconds_" + d;
    }

    public static string GetTodayKey()
    {
        return GetKeyForDate(DateTime.Now);
    }

    // 原本的（如果你已經有就留著）
    public static void AddStudySeconds(float seconds)
    {
        string key = GetTodayKey();
        float total = PlayerPrefs.GetFloat(key, 0f);
        total += seconds;
        PlayerPrefs.SetFloat(key, total);
        PlayerPrefs.Save();
    }

    // ✅ 新增整數版，之後計時器都用這個
    public static void AddStudySecondsInt(int seconds)
    {
        string key = GetTodayKey();
        int baseInt = Mathf.FloorToInt(PlayerPrefs.GetFloat(key, 0f));
        int total = baseInt + Mathf.Max(seconds, 0);
        PlayerPrefs.SetFloat(key, total);
        PlayerPrefs.Save();
    }

    public static float GetTodayStudySeconds()
    {
        string key = GetTodayKey();
        return PlayerPrefs.GetFloat(key, 0f);
    }

    public static float GetStudySecondsOnDate(DateTime date)
    {
        string key = GetKeyForDate(date);
        return PlayerPrefs.GetFloat(key, 0f);
    }

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
