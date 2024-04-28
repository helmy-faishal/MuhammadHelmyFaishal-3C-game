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

    private void Update()
    {
        CheckMoveInput();
        CheckSprintInput();
        CheckJumpInput();
        CheckClimbInput();
        CheckCancelClimbInput();
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

    void CheckCancelClimbInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            OnInputCancelClimb?.Invoke();
        }
    }
}
