using System;

[Serializable]
public class StickyNote
{
    public string senderUid;
    public string message;
    public string timestamp;
    public string sourceScene;   // Forest / Cafe / Classroom / Camp / Library / SwimmingPool
    public string key;           // Firebase push key（本地用）
}
