using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class RythmInputs : MonoBehaviour
{
    [SerializeField]private HitWindow hitWindow1;
    [SerializeField]private HitWindow hitWindow2;
    [SerializeField]private HitWindow hitWindow3;
    [SerializeField]private HitWindow hitWindow4;

    private void Start()
    {
        hitWindow1.SetLane(0);
        hitWindow2.SetLane(1);
        hitWindow3.SetLane(2);
        hitWindow4.SetLane(3);
    }

    public void OnLane1(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        hitWindow1.OnPressHitWindow();
    }

    public void OnLane2(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        hitWindow2.OnPressHitWindow();
    }
    
    public void OnLane3(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        hitWindow3.OnPressHitWindow();
    }
    public void OnLane4(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        hitWindow4.OnPressHitWindow();
    }

}
