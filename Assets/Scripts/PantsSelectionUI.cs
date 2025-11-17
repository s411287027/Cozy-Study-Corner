using UnityEngine;
using System.Collections.Generic;

public class PantsSelectionUI : MonoBehaviour
{
    public GameObject pantsButtonPrefab;
    public Transform content;

    public Transform playerDisplayPosition;
    private GameObject player;
    private PantsController pantsController;

    public List<PantsData> pantsList;

    void Start()
    {
        player = PlayerManager.Instance.playerInstance;
        pantsController = player.GetComponentInChildren<PantsController>();

        Generate();
    }

    void Generate()
    {
        foreach (Transform c in content) Destroy(c.gameObject);

        foreach (var p in pantsList)
        {
            var obj = Instantiate(pantsButtonPrefab, content);
            obj.GetComponent<PantsButtonUI>().Setup(p, this);
        }
    }

    public void SelectPants(PantsData p)
    {
        pantsController.pantsUp = p.pantsUp;
        pantsController.pantsDown = p.pantsDown;
        pantsController.pantsLeft = p.pantsLeft;
        pantsController.pantsRight = p.pantsRight;

        pantsController.pantsUpFrames = p.pantsUpFrames;
        pantsController.pantsDownFrames = p.pantsDownFrames;
        pantsController.pantsLeftFrames = p.pantsLeftFrames;
        pantsController.pantsRightFrames = p.pantsRightFrames;

        pantsController.UpdatePantsDirection(0, -1);
    }
}
