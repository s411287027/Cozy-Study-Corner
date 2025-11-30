using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class StudyChartController : MonoBehaviour
{
    // 最大與最小柱子高度（配合你 200x120 的 UI）
    private const float MAX_BAR_HEIGHT = 50f;   // 柱子最高大約 50
    private const float MIN_BAR_HEIGHT = 1f;    // 再怎麼少至少有一點高度

    [Header("Bar Settings")]
    public RectTransform barContainer;      // 放所有 Bar 的父物件
    public GameObject barPrefab;            // 一根 bar 的 Prefab

    [Header("UI")]
    public TextMeshProUGUI totalTitleText;  // 顯示標題："Today", "Last 7 Days", "This Month"
    public TextMeshProUGUI totalTimeText;   // 顯示總時間："01:23:45"

    [Header("Navigation")]
    public StudyUIManager uiManager;        // 在 Inspector 拖進來

    // ChartPanel 啟用時，預設顯示 Today
    void OnEnable()
    {
        ShowTodayChart();
    }

    // ===== 給 Button 用的函式 =====

    public void OnClickTodayTab()
    {
        ShowTodayChart();
    }

    public void OnClickWeekTab()
    {
        ShowWeekChart();
    }

    public void OnClickMonthTab()
    {
        ShowMonthChart();
    }

    // 回 Mode 的按鈕
    public void OnBackToModeButton()
    {
        if (uiManager != null)
        {
            uiManager.OnBackToMode();
        }
        else
        {
            Debug.LogWarning("StudyChartController: uiManager is null, cannot go back to Mode.");
        }
    }

    // 回 HomePanel 的按鈕
    public void OnBackToHomeButton()
    {
        if (uiManager != null)
        {
            uiManager.OnBackToHome();
        }
        else
        {
            Debug.LogWarning("StudyChartController: uiManager is null, cannot go back to Home.");
        }
    }

    // ===== 三種模式 =====

    void ShowTodayChart()
    {
        int todaySec = StudyStats.GetTodayStudySecondsInt();

        List<int> values = new List<int> { todaySec };
        List<string> labels = new List<string> { "Today" };

        BuildChart(values, labels, "Today");
    }

    void ShowWeekChart()
    {
        // 最近 7 天（包含今天）：[6天前,...,昨天,今天]
        List<int> values = StudyStats.GetPastDaysSecondsInt(7);

        List<string> labels = new List<string>();
        DateTime today = DateTime.Now;

        for (int i = 7 - 1; i >= 0; i--)
        {
            DateTime d = today.AddDays(-i);
            labels.Add(d.ToString("MM/dd"));
        }

        BuildChart(values, labels, "Last 7 Days");
    }

    void ShowMonthChart()
    {
        List<int> values = StudyStats.GetThisMonthDailySeconds();
        List<string> labels = new List<string>();

        DateTime today = DateTime.Now;
        int year = today.Year;
        int month = today.Month;
        int daysInMonth = DateTime.DaysInMonth(year, month);

        for (int day = 1; day <= daysInMonth; day++)
        {
            labels.Add(day.ToString());  // "1", "2", "3" ...
        }

        BuildChart(values, labels, "This Month");
    }

    // ===== 核心：畫長條圖（不依賴 HorizontalLayoutGroup）=====

    void BuildChart(List<int> values, List<string> labels, string title)
    {
        if (values == null || labels == null)
        {
            Debug.LogWarning("BuildChart: values or labels is null.");
            return;
        }
        if (values.Count != labels.Count)
        {
            Debug.LogWarning($"BuildChart: values.Count({values.Count}) != labels.Count({labels.Count}).");
            return;
        }
        if (barContainer == null || barPrefab == null)
        {
            Debug.LogWarning("BuildChart: barContainer or barPrefab not assigned.");
            return;
        }

        // 清掉舊的 bar
        ClearBars();

        if (totalTitleText != null) totalTitleText.text = title;

        // 1. 總時間 / 最大值
        int totalSec = 0;
        int maxValue = 0;
        for (int i = 0; i < values.Count; i++)
        {
            int v = Mathf.Max(0, values[i]);
            values[i] = v;
            totalSec += v;
            if (v > maxValue) maxValue = v;
        }

        if (totalTimeText != null)
        {
            totalTimeText.text = TimeFormatter.FormatHMS(totalSec);
        }

        if (maxValue <= 0) maxValue = 1;

        // 2. 容器大小
        RectTransform containerRT = barContainer;
        float containerWidth = containerRT.rect.width;
        float containerHeight = containerRT.rect.height;

        int count = Mathf.Max(1, values.Count);

        // bar 之間的空隙
        float spacing = 2f;

        float maxAvailableWidth = containerWidth - spacing * (count - 1);
        if (maxAvailableWidth < 10f)
        {
            maxAvailableWidth = containerWidth;
            spacing = 0f;
        }

        float barWidth = maxAvailableWidth / count;

        // 寬度限制
        float minBarWidth = 2f;
        float maxBarWidth = 20f;
        barWidth = Mathf.Clamp(barWidth, minBarWidth, maxBarWidth);

        // 🔥 高度不用寫死，改用容器高度的 80%
        float maxBarHeight = containerHeight * 0.8f;
        if (maxBarHeight <= 0f) maxBarHeight = 50f;

        float minBarHeight = 4f;

        // 置中整排
        float totalBarsWidth = barWidth * count + spacing * (count - 1);
        float startX = -totalBarsWidth / 2f + barWidth / 2f;

        Debug.Log($"[Chart-Build] {title} count={count}, containerWidth={containerWidth}, containerHeight={containerHeight}, barWidth={barWidth}, maxValue={maxValue}, totalSec={totalSec}");

        // 3. 逐一建立 bar
        for (int i = 0; i < count; i++)
        {
            int v = values[i];
            string label = labels[i];

            GameObject barGO = GameObject.Instantiate(barPrefab, barContainer);
            barGO.SetActive(true);

            // --- Root 設寬 & 底部對齊 ---
            RectTransform rootRT = barGO.GetComponent<RectTransform>();
            if (rootRT != null)
            {
                // 底部中間當基準
                rootRT.anchorMin = new Vector2(0.5f, 0f);
                rootRT.anchorMax = new Vector2(0.5f, 0f);
                rootRT.pivot     = new Vector2(0.5f, 0f);

                // 寬度用 barWidth
                Vector2 size = rootRT.sizeDelta;
                size.x = barWidth;
                rootRT.sizeDelta = size;

                // X：每一根條的位置，Y：整排往上抬一點
                float offsetY = 5f;  // 想要整個圖表更靠上就加大這個
                float x = startX + i * (barWidth + spacing);
                rootRT.anchoredPosition = new Vector2(x, offsetY);
            }

            // --- Label 文字 ---
            Transform labelT = barGO.transform.Find("BarLabelText");
            if (labelT != null)
            {
                TextMeshProUGUI labelText = labelT.GetComponent<TextMeshProUGUI>();
                RectTransform labelRT = labelT.GetComponent<RectTransform>();

                if (labelText != null)
                {
                    labelText.text = label;      // "1" ~ "31" 或 "MM/dd"
                    // ⭐ 這句很重要：字元以中間為基準，才會跟 bar 對中
                    labelText.alignment = TextAlignmentOptions.Center;
                }

                if (labelRT != null)
                {
                    // 以「BarPrefab 的中心點」為基準
                    labelRT.anchorMin = new Vector2(0.5f, 0f);
                    labelRT.anchorMax = new Vector2(0.5f, 0f);
                    // pivot 在「下邊中間」，這樣往上拉就剛好貼在 bar 底下
                    labelRT.pivot     = new Vector2(0.5f, 0f);

                    // 給它一個固定寬度，確保顯示得清楚
                    float labelWidth = Mathf.Max(14f, barWidth * 2f); // 想更寬可以調這裡
                    Vector2 size = labelRT.sizeDelta;
                    size.x = labelWidth;
                    size.y = size.y;  // 保留原本高度
                    labelRT.sizeDelta = size;

                    // 往下貼在 bar 的底部附近（負的就是向下）
                    float labelOffsetY = -3.5f;    // 不夠靠近就改成 -1，太貼就改成 -3
                    labelRT.anchoredPosition = new Vector2(0f, labelOffsetY);
                }
            }
            else
            {
                Debug.LogWarning("BuildChart: BarLabelText NOT found in prefab (name must be 'BarLabelText').");
            }

            // --- Fill 高度 ---
            Transform fillT = barGO.transform.Find("BarFillImage");
            if (fillT != null)
            {
                RectTransform fillRT = fillT.GetComponent<RectTransform>();
                if (fillRT != null)
                {
                    // 🔥 強制把柱子 anchor/pivot 設成「底部」，往上長
                    fillRT.anchorMin = new Vector2(0.5f, 0f);
                    fillRT.anchorMax = new Vector2(0.5f, 0f);
                    fillRT.pivot = new Vector2(0.5f, 0f);
                    fillRT.anchoredPosition = Vector2.zero;

                    Vector2 fillSize = fillRT.sizeDelta;

                    // 寬度略小於 root
                    fillSize.x = Mathf.Max(1f, barWidth * 0.8f);

                    float normalized = (float)v / (float)maxValue;
                    float barHeight = normalized * maxBarHeight;
                    if (barHeight < minBarHeight) barHeight = minBarHeight;

                    fillSize.y = barHeight;
                    fillRT.sizeDelta = fillSize;
                }
                else
                {
                    Debug.LogWarning("BuildChart: BarFillImage has no RectTransform.");
                }
            }
            else
            {
                Debug.LogWarning("BuildChart: BarFillImage NOT found in prefab (name must be 'BarFillImage').");
            }
        }

        Debug.Log("BuildChart: finished creating bars.");
    }

    // 清掉所有舊 bar（只在遊戲執行時動手）
    void ClearBars()
    {
        // 避免在編輯模式 / Prefab 模式刪到資產
        if (!Application.isPlaying) return;
        if (barContainer == null) return;

        for (int i = barContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = barContainer.GetChild(i);

            if (child == null) continue;

            // 這裡刪掉的是「場景中的實例」，不會動到 Project 裡的 Prefab
            Destroy(child.gameObject);
        }
    }

}
