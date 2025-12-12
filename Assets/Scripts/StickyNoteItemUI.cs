using UnityEngine;
using TMPro;

public class StickyNoteItemUI : MonoBehaviour
{
    public TMP_Text senderText;
    public TMP_Text messageText;
    public TMP_Text timeText;

    public void Set(string sender, string message, string time)
    {
        if (senderText)  senderText.text  = sender;
        if (messageText) messageText.text = message;
        if (timeText)    timeText.text    = time;
    }
}
