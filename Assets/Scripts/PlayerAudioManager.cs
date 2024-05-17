using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [SerializeField] AudioSource footstepSFX;
    [SerializeField] AudioSource landingSFX;
    [SerializeField] AudioSource punchSFX;
    [SerializeField] AudioSource glideSFX;

    void PlayFootstepSFX()
    {
        footstepSFX.volume = Random.Range(0.8f, 1f);
        footstepSFX.pitch = Random.Range(0.8f, 1.5f);
        footstepSFX.Play();
    }

    void PlayLandingSFX()
    {
        landingSFX.Play();
    }

    void PlayPunchSFX()
    {
        punchSFX.volume = Random.Range(0.8f, 1f);
        punchSFX.pitch = Random.Range(0.8f, 1.5f);
        punchSFX.Play();
    }

    public void PlayGlideSFX()
    {
        glideSFX.Play();
    }

    public void StopGlideSFX()
    {
        glideSFX.Stop();
    }
}
