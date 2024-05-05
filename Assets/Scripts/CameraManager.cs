using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CameraState
{
    FirstPerson,
    ThirdPerson
}

public class CameraManager : MonoBehaviour
{
    [SerializeField] InputManager inputManager;
    [SerializeField] CinemachineVirtualCamera fppCamera;
    [SerializeField] CinemachineFreeLook tppCamera;

    public CameraState state;

    CinemachinePOV fppPOV;

    private void Start()
    {
        fppPOV = fppCamera.GetCinemachineComponent<CinemachinePOV>();

        if (inputManager != null )
        {
            inputManager.OnChangePOV += SwitchCamera;
        }

        SwitchCamera();
    }

    private void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.OnChangePOV -= SwitchCamera;
        }
    }

    public void SetFPPClampedCamera(bool isClamped, Vector3 rotation)
    {
        if (isClamped)
        {
            fppPOV.m_HorizontalAxis.m_Wrap = false;
            fppPOV.m_HorizontalAxis.m_MinValue = rotation.y - 45;
            fppPOV.m_HorizontalAxis.m_MinValue = rotation.y + 45;
        }
        else
        {
            fppPOV.m_HorizontalAxis.m_Wrap = true;
            fppPOV.m_HorizontalAxis.m_MinValue = -180;
            fppPOV.m_HorizontalAxis.m_MaxValue = 180;
        }
    }

    public void SetTPPFieldOfView(float fov)
    {
        tppCamera.m_Lens.FieldOfView = fov;
    }

    void SwitchCamera()
    {
        if (state == CameraState.ThirdPerson)
        {
            state = CameraState.FirstPerson;
            fppCamera.gameObject.SetActive(true);
            tppCamera.gameObject.SetActive(false);
        }
        else
        {
            state = CameraState.ThirdPerson;
            fppCamera.gameObject.SetActive(false);
            tppCamera.gameObject.SetActive(true);
        }
    }
}
