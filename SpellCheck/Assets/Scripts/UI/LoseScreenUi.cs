using UnityEngine;

public class LoseScreenUi : MonoBehaviour
{
    public GameObject Feedback;
    public GameObject Restart;
    public GameObject Menu;
    public GameObject Quit;

    public void activate()
    {
        Feedback.SetActive(true);
        Restart.SetActive(true);
        Menu.SetActive(true);
        Quit.SetActive(true);
    }
}
