using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public Action<Vector2> OnInputMove;
    public Action<bool> OnInputSprint;
    public Action OnInputJump;
    public Action OnInputClimb;
    public Action OnInputCancelClimb;
    public Action OnChangePOV;
    public Action OnInputCrouch;
    public Action OnInputGlide;
    public Action OnInputCancelGlide;
    public Action OnInputPunch;

    private void Update()
    {
        CheckMoveInput();
        CheckSprintInput();
        CheckJumpInput();
        CheckClimbInput();
        CheckCancelInput();
        CheckChangePOV();
        CheckCrouchInput();
        CheckGlideInput();
        CheckPunchInput();
    }

    void CheckMoveInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        OnInputMove?.Invoke(new Vector2(horizontal, vertical));
    }

    void CheckSprintInput()
    {
        bool isSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        OnInputSprint?.Invoke(isSprint);
    }

    void CheckJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnInputJump?.Invoke();
        }
    }

    void CheckClimbInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            OnInputClimb?.Invoke();
        }
    }

    void CheckCancelInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            OnInputCancelClimb?.Invoke();
            OnInputCancelGlide?.Invoke();
        }
    }

    void CheckChangePOV()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            OnChangePOV?.Invoke();
        }
    }

    void CheckCrouchInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            OnInputCrouch?.Invoke();
        }
    }

    void CheckGlideInput()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            OnInputGlide?.Invoke();
        }
    }

    void CheckPunchInput()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            OnInputPunch?.Invoke();
        }
    }
}
