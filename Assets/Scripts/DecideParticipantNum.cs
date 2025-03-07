using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DecideParticipantNum : MonoBehaviour
{
    [SerializeField] private TMP_InputField input;
    public int participantNum;
    void Start()
    {
        
    }
    void Update()
    {
        if(input.text != "")
        {
            VariableManager.Participant = int.Parse(input.text);
        }
    }
}
