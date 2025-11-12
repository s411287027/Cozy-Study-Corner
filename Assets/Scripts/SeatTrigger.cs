using UnityEngine;

public class SeatTrigger : MonoBehaviour
{
    public GameObject sitButton;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            sitButton.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            sitButton.SetActive(false);
        }
    }
}
