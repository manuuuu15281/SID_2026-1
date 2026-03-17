using UnityEngine;

public class UIButtonSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip clickSound;

    public void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}