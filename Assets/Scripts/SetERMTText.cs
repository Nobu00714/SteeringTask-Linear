using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SetERMTText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ER;
    [SerializeField] private TextMeshProUGUI MT;
    void Start()
    {
        
    }
    void Update()
    {
        ER.text = (VariableManager.ERSum).ToString();
        MT.text = (VariableManager.MTSum/(VariableManager.AmplitudeNum * VariableManager.WidthNum)).ToString();
    }
}
