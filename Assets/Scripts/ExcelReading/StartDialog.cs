using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.EventSystem;

public class StartDialog : MonoBehaviour
{
    public int StartID;

    void Start()
    {
        EventManager.Instance.TriggerEvent(GameEvents.DIALOG_START, StartID);
    }
}
