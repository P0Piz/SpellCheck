using UnityEngine;

public class FeedbackButton : MonoBehaviour
{
    [SerializeField] private string surveyURL = "https://forms.gle/At8oiS9LY1RThqLH6";

    public void OpenSurvey()
    {
        Application.OpenURL(surveyURL);
    }
}