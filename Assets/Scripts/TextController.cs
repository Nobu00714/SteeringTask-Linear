using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextController : MonoBehaviour
{
    [SerializeField] private TaskController taskController;
    [SerializeField] private TextMeshProUGUI biasText;
    [SerializeField] private TextMeshProUGUI numText;
    void Start()
    {
    }
    void Update()
    {
        if(taskController.bias == TaskController.Bias.Fast)
        {
            biasText.text = "Fast : As fast as possible";
        }
        if(taskController.bias == TaskController.Bias.Neutral)
        {
            biasText.text = "Neutral : As fast and as accurate as possible";
        }
        if(taskController.bias == TaskController.Bias.Accurate)
        {
            biasText.text = "Accurate : As accurate as possible";
        }
        numText.text = "Task count : "+(taskController.taskNum+1)+" / "+taskController.taskWidth.Length * taskController.taskAmplitude.Length * taskController.set;
    }
}
