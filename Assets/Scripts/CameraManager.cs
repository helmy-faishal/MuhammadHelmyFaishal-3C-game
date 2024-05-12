using Cinemachine;
using System;
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
    public Action OnChangePerspective;

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
            fppPOV.m_HorizontalAxis.m_MaxValue = rotation.y + 45;
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

    // UPDATE:
    // Mengganti kamera dengan mengubah Priority
    // Untuk kamera yang tidak aktif akan melakukan Recentering
    void SwitchCamera()
    {
        if (state == CameraState.ThirdPerson)
        {
            state = CameraState.FirstPerson;
            fppCamera.Priority = 10;
            tppCamera.Priority = 1;
            tppCamera.m_YAxisRecentering.m_enabled = true;
            tppCamera.m_RecenterToTargetHeading.m_enabled = true;
            fppPOV.m_HorizontalRecentering.m_enabled = false;
            fppPOV.m_VerticalRecentering.m_enabled = false;
        }
        else
        {
            state = CameraState.ThirdPerson;
            fppCamera.Priority = 1;
            tppCamera.Priority = 10;
            tppCamera.m_YAxisRecentering.m_enabled = false;
            tppCamera.m_RecenterToTargetHeading.m_enabled = false;
            fppPOV.m_HorizontalRecentering.m_enabled = true;
            fppPOV.m_VerticalRecentering.m_enabled = true;
        }

        OnChangePerspective?.Invoke();
    }
}
