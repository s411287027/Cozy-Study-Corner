using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StickyNoteItemUI : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text senderText;
    public TMP_Text messageText;
    public TMP_Text timeText;
    public TMP_Text sourceText; // 可不接

    [Header("Background")]
    public Image bgImage; // 便利貼底圖 Image（你白色那張）

    public void Set(StickyNote note)
    {
        if (senderText) senderText.text = note.senderUid ?? "";
        if (messageText) messageText.text = note.message ?? "";
        if (timeText) timeText.text = note.timestamp ?? "";
        if (sourceText) sourceText.text = note.sourceScene ?? "";

        // 其他字白色
        if (senderText) senderText.color = Color.white;
        if (timeText)   timeText.color   = Color.white;
        if (sourceText) sourceText.color = Color.white;

        ApplyTheme(note.sourceScene ?? "");
    }

    private void ApplyTheme(string scene)
    {
        Color bg  = Color.white;
        Color msg = Color.black;

        switch (scene)
        {
            case "Forest":        bg = Hex("#B0C3B9"); msg = Hex("#38513D"); break;
            case "Cafe":          bg = Hex("#ECDFC4"); msg = Hex("#706B61"); break;
            case "Classroom":     bg = Hex("#E5CDC7"); msg = Hex("#8A7675"); break;
            case "Camp":          bg = Hex("#D7E1C2"); msg = Hex("#6E755E"); break;
            case "Library":       bg = Hex("#C4D1EC"); msg = Hex("#747A8A"); break;
            case "SwimmingPool":  bg = Hex("#C7DFE4"); msg = Hex("#74838A"); break;
        }

        if (bgImage) bgImage.color = bg;
        if (messageText) messageText.color = msg;
    }

    private Color Hex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var c))
            return c;
        return Color.white;
    }
}
