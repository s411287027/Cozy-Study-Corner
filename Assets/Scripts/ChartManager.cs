using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class ChartManager : MonoBehaviour
{
    [Header("Bar Settings")]
    public RectTransform barPrefab;        // 指向 Project 裡的 BarPrefab
    public RectTransform barContainer;     // 指向 Hierarchy 裡的 BarContainer
    public float horizontalPadding = 20f;  // 左右預留空間
    [Range(0f, 1f)]
    public float maxHeightRatio = 0.7f;    // 柱子最高佔容器高度比例（0~1）

    [Header("Colors (optional)")]
    public Color todayColor = Color.green;
    public Color normalColor = Color.cyan;

    [Header("Label Font Sizes")]
    public int todayFontSize = 28;
    public int weekFontSize = 20;
    public int monthFontSize = 18;

    // 內部標記是哪種模式
    enum LabelMode { Today, Week, Month }

    // ===== 給按鈕 OnClick 用 =====

    public void ShowTodayChart()
    {
        ClearBars();

        float todaySeconds = StudyStats.GetTodayStudySeconds();
        List<float> list = new List<float> { todaySeconds };

        CreateBars(list, LabelMode.Today);
    }

    public void ShowWeekChart()
    {
        ClearBars();

        List<float> secondsList = StudyStats.GetPastDaysSeconds(7);
        CreateBars(secondsList, LabelMode.Week);
    }

    public void ShowMonthChart()
    {
        ClearBars();

        List<float> secondsList = StudyStats.GetPastDaysSeconds(30);
        CreateBars(secondsList, LabelMode.Month);
    }

    // ===== 主要畫圖邏輯 =====

    void ClearBars()
    {
        for (int i = barContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(barContainer.GetChild(i).gameObject);
        }
    }

    void CreateBars(List<float> secondsList, LabelMode labelMode)
    {
        if (secondsList == null || secondsList.Count == 0)
            return;

        int n = secondsList.Count;
        if (n <= 0) return;

        // 1. 找最大秒數（高度比例用）
        float maxSeconds = 0f;
        foreach (var s in secondsList)
            if (s > maxSeconds) maxSeconds = s;
        if (maxSeconds <= 0f) maxSeconds = 1f;

        // 2. 取得容器寬高
        float containerWidth = barContainer.rect.width;
        float containerHeight = barContainer.rect.height;
        if (containerWidth <= 0f || containerHeight <= 0f)
        {
            Debug.LogWarning("ChartManager: barContainer rect size is zero, check RectTransform.");
            return;
        }

        // 3. 計算每一格寬度 & 每根柱子寬度
        float usableWidth = Mathf.Max(containerWidth - 2f * horizontalPadding, 10f);
        float cellWidth = usableWidth / n;        // 每一天的格子寬度
        float barWidth = cellWidth * 0.6f;        // 每根柱子實際寬度（60%）

        // 4. 決定柱子最大高度
        float maxBarHeight = containerHeight * maxHeightRatio;

        DateTime today = DateTime.Now;

        for (int i = 0; i < n; i++)
        {
            float sec = secondsList[i];

            DateTime day;
            string label;
            bool isToday;
            int fontSize;

            if (labelMode == LabelMode.Today)
            {
                day = today;
                label = TimeFormatter.FormatHMS(sec);    // 只顯示時間
                isToday = true;
                fontSize = todayFontSize;
            }
            else if (labelMode == LabelMode.Week)
            {
                // i = 0 → 最早那天，i = n-1 → 今天
                day = today.AddDays(i - (n - 1));
                string datePart = day.ToString("MM/dd");
                string timePart = TimeFormatter.FormatHMS(sec);
                label = datePart + "\n" + timePart;      // 日期 + 換行 + 時間
                isToday = (day.Date == today.Date);
                fontSize = weekFontSize;
            }
            else // Month
            {
                day = today.AddDays(i - (n - 1));
                label = day.ToString("dd");              // 只顯示「幾號」
                isToday = (day.Date == today.Date);
                fontSize = monthFontSize;
            }

            CreateSingleBar(
                valueSeconds: sec,
                maxSeconds: maxSeconds,
                maxBarHeight: maxBarHeight,
                labelText: label,
                isToday: isToday,
                index: i,
                cellWidth: cellWidth,
                barWidth: barWidth,
                totalCount: n,
                fontSize: fontSize
            );
        }
    }

    void CreateSingleBar(
        float valueSeconds,
        float maxSeconds,
        float maxBarHeight,
        string labelText,
        bool isToday,
        int index,
        float cellWidth,
        float barWidth,
        int totalCount,
        int fontSize)
    {
        // 建立一根柱子
        RectTransform bar = Instantiate(barPrefab, barContainer);
        bar.SetParent(barContainer, false);

        // 保證這個 RectTransform 是以左下為基準
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(0f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);

        // 設定 Bar 容器寬高（高只是容器，真正高度由 Fill 控制）
        bar.sizeDelta = new Vector2(barWidth, maxBarHeight);

        // 算 X 位置：平均分佈在 ChartArea 裡
        float containerWidth = barContainer.rect.width;
        float usableWidth = Mathf.Max(containerWidth - 2f * horizontalPadding, 10f);
        float startX = horizontalPadding;
        float cellCenterOffset = cellWidth * 0.5f;
        float x = startX + index * cellWidth + cellCenterOffset;

        bar.anchoredPosition = new Vector2(x, 0f);

        // 找 Fill & Label
        Image fill = bar.GetComponentInChildren<Image>();
        TextMeshProUGUI label = bar.GetComponentInChildren<TextMeshProUGUI>();

        // 高度比例
        float ratio = Mathf.Clamp01(valueSeconds / maxSeconds);

        if (fill != null)
        {
            RectTransform fillRect = fill.rectTransform;
            var size = fillRect.sizeDelta;
            size.y = maxBarHeight * ratio;
            fillRect.sizeDelta = size;

            fill.color = isToday ? todayColor : normalColor;
        }

        if (label != null)
        {
            label.text = labelText;
            label.fontSize = fontSize;
        }
    }
}
