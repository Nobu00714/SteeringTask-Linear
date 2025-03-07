using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VariableManager : MonoBehaviour
{
    public static int Participant;
    public static TaskController.Bias bias = TaskController.Bias.Neutral;
    public static int AmplitudeNum;
    public static int WidthNum;
    public static int biasNum = 0;
    public static float MTSum;
    public static float AllMTSumNeutral;
    public static float AllMTSumFast;
    public static float AllMTSumAccurate;
    public static float ERSum;
    public static int taskNum;
    public static bool resultCheck;
    public static StreamWriter swMT;
}
