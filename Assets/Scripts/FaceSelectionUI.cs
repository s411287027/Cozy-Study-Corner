using UnityEngine;
using System.Collections.Generic;

public class FaceSelectionUI : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject faceButtonPrefab;    // prefab 需包含 FaceButtonUI
    public Transform content;

    [Header("Player 設定")]
    public Transform playerDisplayPosition;
    private GameObject player;
    private FaceController faceController;

    [Header("所有可選臉部")]
    public List<FaceData> faceList = new List<FaceData>();

    void Start()
    {
        player = PlayerManager.Instance.playerInstance;
        if (player == null)
        {
            Debug.LogError("找不到 Player!");
            return;
        }

        faceController = player.GetComponentInChildren<FaceController>();

        if (playerDisplayPosition != null)
            player.transform.position = playerDisplayPosition.position;

        GenerateFaceUI();
    }

    void GenerateFaceUI()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var face in faceList)
        {
            GameObject obj = Instantiate(faceButtonPrefab, content);
            FaceButtonUI ui = obj.GetComponent<FaceButtonUI>();
            ui.Setup(face, this);
        }
    }

    public void SelectFace(FaceData face)
    {
        if (faceController == null) return;

        faceController.faceUp = face.faceUp;
        faceController.faceDown = face.faceDown;
        faceController.faceLeft = face.faceLeft;
        faceController.faceRight = face.faceRight;

        faceController.UpdateFaceDirection(0f, -1f); // 顯示朝下
        Debug.Log($"成功替換臉部：{face.faceName}");
    }
}
