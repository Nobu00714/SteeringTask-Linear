using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SetAverageMTTextAccurate : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI MTText;
    void Start()
    {
        
    }
    void Update()
    {
        MTText.text = "ニュートラルでの平均操作時間："+(VariableManager.AllMTSumNeutral/(21*3*5))+"  "+
                        "正確さ重視での目標操作時間："+((VariableManager.AllMTSumNeutral/(21*3*5))*1.1f);
        
    }
}
