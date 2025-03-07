using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using uWintab;
using UnityEngine.SceneManagement;
using TMPro;
using MathNet.Numerics.Statistics;

public class TaskController : MonoBehaviour
{
    [SerializeField] public int Participant;
    [SerializeField] public Bias bias;
    [SerializeField] private Team team;
    [SerializeField] public Device device;
    [SerializeField] private bool practice;
    [SerializeField] public int set;
    [SerializeField] public int taskNum;
    [SerializeField] public int biasNum;
    [SerializeField] private int nowTaskAmplitude;
    [SerializeField] private int nowTaskWidth;
    [SerializeField] GameObject start;
    [SerializeField] GameObject goal;
    [SerializeField] GameObject path;
    [SerializeField] GameObject startArea;
    [SerializeField] GameObject goalArea;
    [SerializeField] GameObject button;
    [SerializeField] private AudioClip correctAudio;
    [SerializeField] private AudioClip wrongAudio;
    [SerializeField] private GameObject cursor;
    [SerializeField] private GameObject MTTextObj;
    [SerializeField] private GameObject DevTextObj;
    [SerializeField] private TextMeshProUGUI MTText;
    [SerializeField] private TextMeshProUGUI DevText;
    private bool inPath;
    private bool inPathPrev;
    private int deviationCount;
    private List<float> cursorYarray = new List<float>();
    private Tablet tablet;
    private bool taskReady;
    private bool taskStarted;
    private bool startCrossed;
    private bool goalCrossed;
    private bool nextButtonPressed;
    private bool mousePress;
    public int[] taskAmplitude;
    public int[] taskWidth;
    private List<int> taskList;
    private StreamWriter swPos;
    private StreamWriter swMT;
    private float taskStartTime;
    private float taskFinishTime;
    private float cursorX;
    private float cursorY;   
    private bool falseStart = false;
    private SpriteRenderer startRenderer;
    private SpriteRenderer goalRenderer;
    private AudioSource audioSource;
    private float Ae;
    private float AeOld;
    private Vector2 StartPoint;
    private Vector2 EndPoint;
    private float cursorXprev;
    private float cursorYprev;
    private int taskClear;
    private int sameTaskNum;
    RectTransform rectTransform;
    private bool buttonPressable = true;
    GameObject trajectory;
    GameObject trajectoryLine;
    [SerializeField] GameObject cursorTrajectoryParent;
    [SerializeField] LineRenderer lineRenderer;
    GameObject LineInstance;
    private bool first = true;
    private bool CSVUpdated = false;
    private bool firstPenTouch =true;
    private bool firstPenRelease =true;
    private List<Vector3> trajectoryList = new List<Vector3>();
    public enum Bias
    {
        Fast,
        Neutral,
        Accurate
    }
    public enum Team
    {
        FastToAccurate,
        AccurateToFast
    }
    public enum Device
    {
        Mouse,
        Pen
    }
    void Start()
    {
        Application.targetFrameRate = 120;
        startRenderer = start.GetComponent<SpriteRenderer>();
        goalRenderer = goal.GetComponent<SpriteRenderer>();
        audioSource = this.GetComponent<AudioSource>();
        rectTransform = button.GetComponent<RectTransform>();
        tablet = this.GetComponent<Tablet>();

        taskAmplitude = new int[4];
        taskAmplitude[0] = 360;
        taskAmplitude[1] = 480;
        taskAmplitude[2] = 700;
        taskAmplitude[3] = 850;

        taskWidth = new int[4];
        taskWidth[0] = 16;
        taskWidth[1] = 22;
        taskWidth[2] = 36;
        taskWidth[3] = 90;

        taskList = new List<int>();
        for(int i=0; i<taskAmplitude.Length*taskWidth.Length; i++)
        {
            taskList.Add(i);
        }
        ShuffleList(taskList);

        taskUpdate();
        
        Cursor.visible = false;
        trajectory = (GameObject)Resources.Load("CursorTrajectory");
        trajectoryLine = (GameObject)Resources.Load("Line");

        bias = VariableManager.bias;
        biasNum = VariableManager.biasNum;
        Participant = VariableManager.Participant;
        taskNum = VariableManager.taskNum;
        VariableManager.MTSum = 0;
        swMT = VariableManager.swMT;
        VariableManager.ERSum = 0;
        VariableManager.WidthNum = taskWidth.Length;
        VariableManager.AmplitudeNum = taskAmplitude.Length;
    }
    void OnApplicationQuit()
    {
        swMT.Close();
    }
    void Update()
    {
        DecideCursorPos();
        DrawCursorTrajectory();
        if(taskNum == 0)
        {
            if(first)
            {
                makeMTCSV();
                first = false;
                VariableManager.swMT = swMT;
            }
        }

        float startX = nowTaskAmplitude/2;
        float startYUp = nowTaskWidth/2;
        float startYBottom = -nowTaskWidth/2;
        float goalX = -nowTaskAmplitude/2;
        float goalYUp = nowTaskWidth/2;
        float goalYBottom = -nowTaskWidth/2;

        //ネクストボタンが押され，マウスがスタートより左に戻ったら準備完了
        if(nextButtonPressed)
        {
            if(cursorX>startX)
            {
                nextButtonPressed = false;
                taskReady = true;
                CSVUpdated = false;
            }
        }
        if(taskReady && mousePress)
        {
            //スタート地点を超えたら
            if(cursorX<=startX && cursorXprev>=startX)
            {
                startCrossed = true;
                cursorYarray.Clear();
                Ae = 0;

                //スタート地点を2点（）現在位置と1フレーム前位置から計測
                float YDist = Mathf.Abs(cursorYprev-cursorY);
                float XDistToStart = Mathf.Abs(cursorX-startX);
                float XDist = Mathf.Abs(cursorX-cursorXprev);
                float PositiveNegative = Mathf.Sign(cursorYprev-cursorY);
                StartPoint = new Vector2(startX, cursorY+PositiveNegative*YDist*(XDistToStart/XDist));

                //Pathから逸脱しているかのフラグ管理
                inPath = true;
                inPathPrev = true;
                deviationCount = 0;

                if(!falseStart)
                {
                    startRenderer.color = new Color(0f,1f,0f,1f);   //緑色にする
                    taskStartTime = Time.time; //開始時刻を記録
                    taskStarted = true; //タスク開始のフラグをTrue
                    taskReady = false;  //タスク開始準備完了のフラグをFalse
                }
                
                /*Debug.Log("cursorX"+cursorX);
                Debug.Log("cursorY"+cursorY);
                Debug.Log("cursorXprev"+cursorXprev);
                Debug.Log("cursorYprev"+cursorYprev);
                Debug.Log("startX"+StartPoint.x);
                Debug.Log("startY"+StartPoint.y);*/
            }
            //スタート地点より右に戻ったら
            if(cursorX>startX)
            {
                startRenderer.color = new Color(0f,0.5f,1f,1f);   //青色にする
                falseStart = false;
            }
        } 
        if(taskStarted && mousePress)
        { 
            //タスク中ずっと
            //Aeに総距離を加算
            Ae += Mathf.Sqrt(Mathf.Pow(cursorX-cursorXprev,2)+Mathf.Pow(cursorY-cursorYprev,2));
            //カーソルYの座標を配列に保存
            cursorYarray.Add(cursorY);
            updatePosCSV();

            //タスクの幅から逸脱しているかをチェック
            if(cursorY>=-nowTaskWidth/2 && cursorY<=nowTaskWidth/2)
            {
                inPath = true;
            }
            else
            {
                inPath = false;
            }
            //逸脱したとき，逸脱した回数を加算
            if(inPath == false && inPathPrev == true)
            {
                deviationCount++;
                VariableManager.ERSum++;
            }
            inPathPrev = inPath;


            //ゴール地点を超えたら
            if(cursorX<goalX)
            {
                float YDist = Mathf.Abs(cursorYprev-cursorY);
                float XDistToStart = Mathf.Abs(cursorX-goalX);
                float XDist = Mathf.Abs(cursorX-cursorXprev);
                float PositiveNegative = Mathf.Sign(cursorYprev-cursorY);
                EndPoint = new Vector2(goalX, cursorY+PositiveNegative*YDist*(XDistToStart/XDist));
                /*Debug.Log("cursorX"+cursorX);
                Debug.Log("cursorY"+cursorY);
                Debug.Log("cursorXprev"+cursorXprev);
                Debug.Log("cursorYprev"+cursorYprev);
                Debug.Log("endX"+EndPoint.x);
                Debug.Log("endY"+EndPoint.y);*/

                taskFinishTime = Time.time;
                taskStarted = false;
                goalCrossed = true;

                goalRenderer.color = new Color(0f,1f,0f,1f);   //緑色にする
                //クリアを記録
                taskClear = 1;
                /*//ゴールターゲットを通過したら
                if(EndPoint.y>=goalYBottom && EndPoint.y<=goalYUp)
                {
                    goalRenderer.color = new Color(0f,1f,0f,1f);   //緑色にする
                    //クリアを記録
                    taskClear = 1;
                    audioSource.PlayOneShot(correctAudio);
                    
                }
                //ターゲット外を通過したら
                else
                {
                    goalRenderer.color = new Color(1f,0f,0f,1f);   //緑色にする
                    //エラーを記録
                    taskClear = 0;
                    audioSource.PlayOneShot(wrongAudio);
                }*/
                //ネクストボタン表示
                rectTransform.anchoredPosition = new Vector3(-750,-282,0);
            }
        }
        if(device == Device.Pen)
        {
            //ペンを付けた時
            if(tablet.pressure>0.001)
            {
                firstPenRelease = true;
                if(firstPenTouch)
                {
                    firstPenTouch = false;
                    //LineInstance = (GameObject)Instantiate(trajectoryLine, Vector3.zero, Quaternion.identity);
                    //LineInstance.transform.parent = cursorTrajectoryParent.transform;
                    if(taskReady)
                    {
                        mousePress = true;
                        makePosCSV();
                    }
                    if(buttonPressable && cursorX>=rectTransform.anchoredPosition.x-150 && cursorX<=rectTransform.anchoredPosition.x+150 && cursorY>=rectTransform.anchoredPosition.y-150 && cursorY<=rectTransform.anchoredPosition.y+150)
                    {
                        MTTextObj.SetActive(false);
                        DevTextObj.SetActive(false);
                        startRenderer.color = new Color(0f,0.5f,1f,1f);   //青色にする
                        goalRenderer.color = new Color(0f,0.5f,1f,1f);   //青色にする
                        if(taskNum%(taskWidth.Length*taskAmplitude.Length)==15)
                        {
                            ShuffleList(taskList);
                        }
                        if(goalCrossed)
                        {
                            taskNum++;
                            taskUpdate();
                        }
                        nextButtonPressed = true;
                        button.SetActive(false);
                        buttonPressable = false;
                        goalCrossed = false;
                        foreach ( Transform child in cursorTrajectoryParent.transform )
                        {
                            GameObject.Destroy(child.gameObject);
                        }
                    }
                }
                LineInstance = (GameObject)Instantiate(trajectoryLine, Vector3.zero, Quaternion.identity);
                LineInstance.transform.parent = cursorTrajectoryParent.transform;
            }
            //ペンを離したとき
            if(tablet.pressure<0.001)
            { 
                firstPenTouch = true;
                if(firstPenRelease)
                {
                    firstPenRelease =false;
                    //trajectoryList.Clear();
                    //ゴールしてから離したら
                    if(startCrossed && goalCrossed)
                    {       
                        if(deviationCount>0)
                        {
                            audioSource.PlayOneShot(wrongAudio);
                            DevText.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                        }
                        else
                        {
                            audioSource.PlayOneShot(correctAudio);
                            DevText.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                        }
                        //audioSource.PlayOneShot(correctAudio);
                        //クリアかエラーを記録
                        updateMTCSV(taskClear);
                        //ボタンを出現
                        MTTextObj.SetActive(true);
                        DevTextObj.SetActive(true);
                        MTText.text = "Time : " + (taskFinishTime-taskStartTime).ToString();
                        DevText.text = "Deviation Num : " + (deviationCount).ToString();
                        button.SetActive(true);
                        buttonPressable = true;

                        //マウスポジションの記録を終了
                        swPos.Close();
                        sameTaskNum = 0;
                        CSVUpdated = true;
                        if(taskClear == 0)
                        {
                            //VariableManager.ERSum++;
                            print(VariableManager.ERSum);
                        }
                        
                    }

                    //ゴールせずに離したら（スタートは成功）
                    if(startCrossed && !goalCrossed && !falseStart)
                    {
                        if(!CSVUpdated)
                        {
                            startRenderer.color = new Color(0f,0.5f,1f,1f);
                            //ゴール前に離したというデータを記録
                            updateMTCSV(2);
                            audioSource.PlayOneShot(wrongAudio);
                            //もう一度同じタスクを実行
                            sameTaskNum++;
                            nextButtonPressed = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            taskStarted = false;
                            CSVUpdated = true;
                            foreach ( Transform child in cursorTrajectoryParent.transform )
                            {
                                GameObject.Destroy(child.gameObject);
                            }
                        }
                    }
                    //ゴールせずに離したら（スタートも失敗）
                    if(startCrossed && !goalCrossed && falseStart)
                    {
                        if(!CSVUpdated)
                        {
                            startRenderer.color = new Color(0f,0.5f,1f,1f);
                            //フォールススタートしたというデータを記録
                            updateMTCSV(3);
                            audioSource.PlayOneShot(wrongAudio);
                            //もう一度同じタスクを実行
                            sameTaskNum++;
                            nextButtonPressed = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            taskStarted = false;
                            CSVUpdated = true;
                            foreach ( Transform child in cursorTrajectoryParent.transform )
                            {
                                GameObject.Destroy(child.gameObject);
                            }
                        }
                    }
                    //スタートせずに離したら
                    if(!startCrossed && taskReady)
                    {
                        if(!CSVUpdated)
                        {
                            startRenderer.color = new Color(0f,0.5f,1f,1f);
                            //フォールススタートしたというデータを記録
                            updateMTCSV(4);
                            audioSource.PlayOneShot(wrongAudio);
                            //もう一度同じタスクを実行
                            sameTaskNum++;
                            nextButtonPressed = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            taskStarted = false;
                            CSVUpdated = true;
                            foreach ( Transform child in cursorTrajectoryParent.transform )
                            {
                                GameObject.Destroy(child.gameObject);
                            }
                        }
                    }
                    
                    /*foreach ( Transform child in cursorTrajectoryParent.transform )
                    {
                        GameObject.Destroy(child.gameObject);
                    }*/
                    mousePress = false;
                    startCrossed = false;
                }
            }
        }
        if(device == Device.Mouse)
        {
            if(Input.GetMouseButtonDown(0))
            {
                //LineInstance = (GameObject)Instantiate(trajectoryLine, Vector3.zero, Quaternion.identity);
                //LineInstance.transform.parent = cursorTrajectoryParent.transform;
                if(taskReady)
                {
                    mousePress = true;
                    makePosCSV();
                }
                if(buttonPressable && cursorX>=rectTransform.anchoredPosition.x-250 && cursorX<=rectTransform.anchoredPosition.x+250 && cursorY>=rectTransform.anchoredPosition.y-250 && cursorY<=rectTransform.anchoredPosition.y+250)
                {
                    startRenderer.color = new Color(0f,0.5f,1f,1f);   //青色にする
                    goalRenderer.color = new Color(0f,0.5f,1f,1f);   //青色にする
                    if(goalCrossed)
                    {
                        taskNum++;
                        taskUpdate();
                    }
                    nextButtonPressed = true;
                    button.SetActive(false);
                    buttonPressable = false;
                    goalCrossed = false;
                    foreach ( Transform child in cursorTrajectoryParent.transform )
                    {
                        GameObject.Destroy(child.gameObject);
                    }
                }
            }
            if(Input.GetMouseButton(0))
            {
                LineInstance = (GameObject)Instantiate(trajectoryLine, Vector3.zero, Quaternion.identity);
                LineInstance.transform.parent = cursorTrajectoryParent.transform;
            }
            if(Input.GetMouseButtonUp(0))
            {  
                trajectoryList.Clear();
                //ゴールしてから離したら
                if(goalCrossed)
                {
                    if(!CSVUpdated)
                    {
                        //クリアかエラーを記録
                        updateMTCSV(taskClear);
                        //ボタンを出現
                        rectTransform.anchoredPosition = new Vector3(-750,-282,0);
                        MTText.text = "完了時間 : " + (taskFinishTime-taskStartTime).ToString();
                        DevText.text = "飛び出した回数 : " + (deviationCount).ToString();
                        button.SetActive(true);
                        buttonPressable = true;
                        //マウスポジションの記録を終了
                        swPos.Close();
                        sameTaskNum = 0;
                        CSVUpdated = true;
                    }
                }

                //ゴールせずに離したら
                if(startCrossed && !goalCrossed)
                {
                    if(!CSVUpdated)
                    {
                        startRenderer.color = new Color(0f,0.5f,1f,1f);
                        //ゴール前に離したというデータを記録
                        updateMTCSV(2);
                        audioSource.PlayOneShot(wrongAudio);
                        //もう一度同じタスクを実行
                        sameTaskNum++;
                        nextButtonPressed = true;
                        //マウスポジションの記録を終了
                        swPos.Close();
                        taskStarted = false;
                        CSVUpdated = true;
                        foreach ( Transform child in cursorTrajectoryParent.transform )
                        {
                            GameObject.Destroy(child.gameObject);
                        }
                    }
                }
                mousePress = false;
                startCrossed = false;
            }
        }
        
        if(mousePress)
        {
            //updatePosCSV();
        }
        ChangeBias();
        ShowSetResult();
        FinishExperiment();
        cursorXprev = cursorX;
        cursorYprev = cursorY;
    }
    //ボタンが押されたら
    public void OnClick()
    {
        startRenderer.color = new Color(0f,0.5f,1f,1f);   //青色にする
        goalRenderer.color = new Color(0f,0.5f,1f,1f);   //青色にする
        if(goalCrossed)
        {
            taskNum++;
            taskUpdate();
        }
        nextButtonPressed = true;
        button.SetActive(false);
        goalCrossed = false;
    }
    //カーソルの位置を決定
    private void DecideCursorPos()
    {
        if(device == Device.Pen)
        {
            cursorX = tablet.x * 1920 - 1920/2;
            cursorY = tablet.y * 1080 - 1080/2;
        }
        if(device == Device.Mouse)
        {
            cursorX = Input.mousePosition.x - 1920/2;
            cursorY = Input.mousePosition.y - 1080/2;
        }
        cursor.GetComponent<RectTransform>().anchoredPosition = new Vector3(cursorX, cursorY, 1);
        //Debug.Log("X:"+cursorX+"Y:"+cursorY);
    }
    //カーソルの軌跡を描画
    private void DrawCursorTrajectory()
    {
        if(mousePress)
        {
            //GameObject instance = (GameObject)Instantiate(trajectory, cursor.GetComponent<RectTransform>().anchoredPosition, Quaternion.identity);
            //instance.transform.parent = cursorTrajectoryParent.transform;
            LineRenderer lineRenderer = LineInstance.GetComponent<LineRenderer>();
            trajectoryList.Add(cursor.GetComponent<RectTransform>().anchoredPosition);
            var positions = new Vector3[2];
            positions[0] = new Vector3( cursorX, cursorY, -1 );
            positions[1] = new Vector3( cursorXprev, cursorYprev, -1 );
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            if(taskStarted)
            {
                if(cursorY<nowTaskWidth/2 && cursorY>-nowTaskWidth/2)
                {
                    lineRenderer.startColor = new Color(0,1,0,1);
                    lineRenderer.endColor = new Color(0,1,0,1);
                }
                else
                {
                    lineRenderer.startColor = new Color(1,0,0,1);
                    lineRenderer.endColor = new Color(1,0,0,1);
                }
            }
            else
            {
                lineRenderer.startColor = new Color(0,0,1,1);
                lineRenderer.endColor = new Color(0,0,1,1);
            }
            
            lineRenderer.SetPositions(positions);
        }
    }
    private void ShowSetResult()
    {
        if(taskNum%(taskWidth.Length*taskAmplitude.Length) == 0 && taskNum != 0 && !VariableManager.resultCheck)
        {
            VariableManager.taskNum = taskNum;
            //SceneManager.LoadScene("SetResultScene");
            //Debug.Log("aaa");
            
        }
        if(taskNum%(taskWidth.Length*taskAmplitude.Length) == 1)
        {
            VariableManager.resultCheck = false;
        }
    }
    //バイアスを変更
    private void ChangeBias()
    {
        if(taskNum>=set*taskWidth.Length*taskAmplitude.Length)
        {
            taskNum = 0;
            first = true;
            biasNum++;
            VariableManager.biasNum = biasNum;
            swMT.Close();
            if(team == Team.FastToAccurate)
            {
                if(biasNum == 1)
                {
                    bias = Bias.Fast;
                    VariableManager.bias = bias;
                    SceneManager.LoadScene("ToFastScene");
                }
                if(biasNum == 2)
                {
                    bias = Bias.Accurate;
                    VariableManager.bias = bias;
                    SceneManager.LoadScene("ToControlScene");
                }
            }
            if(team == Team.AccurateToFast)
            {
                if(biasNum == 1)
                {
                    bias = Bias.Accurate;
                    VariableManager.bias = bias;
                    SceneManager.LoadScene("ToControlScene");
                }
                if(biasNum == 2)
                {
                    bias = Bias.Fast;
                    VariableManager.bias = bias;
                    SceneManager.LoadScene("ToFastScene");
                }
            }
        }
    }
    //タスクがすべて終わったら終了
    private void FinishExperiment()
    {
        if(biasNum>2)
        {   
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;//ゲームプレイ終了
            #else
                Application.Quit();//ゲームプレイ終了
            #endif
        }
    }
    //タスクの順番をシャッフル
    private void ShuffleList(List<int> list)
    {
        int tmp;
        int rndNum;
        for(int i=list.Count-1; i>1; i--)
        {
            rndNum = UnityEngine.Random.Range(0,i);
            tmp = list[rndNum];
            list[rndNum] = list[i];
            list[i] = tmp;
        }
    }
    //タスクの更新
    private void taskUpdate()
    {
        nowTaskWidth = taskWidth[taskList[taskNum%(taskAmplitude.Length*taskWidth.Length)]%taskWidth.Length];
        nowTaskAmplitude = taskAmplitude[taskList[taskNum%(taskAmplitude.Length*taskWidth.Length)]/taskWidth.Length];

        //サイズ変更
        //start.transform.localScale = new Vector3( -3, nowTaskWidth, 1);
        //goal.transform.localScale = new Vector3( 3, nowTaskWidth, 1);
        path.transform.localScale = new Vector3( -nowTaskAmplitude, nowTaskWidth, 1);
        startArea.transform.localScale = new Vector3( 1000, nowTaskWidth, 1);
        goalArea.transform.localScale = new Vector3( -1000, nowTaskWidth, 1);

        //位置変更
        start.transform.position = new Vector3( (nowTaskAmplitude/2), nowTaskWidth/2, 0);
        goal.transform.position = new Vector3( -(nowTaskAmplitude/2), nowTaskWidth/2, 0);
        path.transform.position = new Vector3( (nowTaskAmplitude/2), nowTaskWidth/2, 0);
        startArea.transform.position = new Vector3( (nowTaskAmplitude/2), nowTaskWidth/2, 0);
        goalArea.transform.position = new Vector3( -(nowTaskAmplitude/2), nowTaskWidth/2, 0);
    }
    //マウス座標を保存するCSVを作成
    private void makePosCSV()
    {
        if(practice)
        {
            swPos = new StreamWriter(@"PracticePos"+Participant.ToString()+bias.ToString()+"No."+taskNum+"A"+nowTaskAmplitude+"W"+nowTaskWidth+".csv", true, Encoding.GetEncoding("UTF-8"));
        }
        else
        {
            swPos = new StreamWriter(@"Pos"+Participant.ToString()+bias.ToString()+"No."+taskNum+"A"+nowTaskAmplitude+"W"+nowTaskWidth+"Num"+sameTaskNum+".csv", true, Encoding.GetEncoding("UTF-8"));
        }
        string[] s1 = { "参加者", "長さ", "幅", "バイアス", "時間", "x座標", "y座標", "タスク中"};
        string s2 = string.Join(",", s1);
        swPos.WriteLine(s2);
    }
    //フレームごとにマウス座標を保存
    private void updatePosCSV()
    {
        string[] s1 = {Participant.ToString(),nowTaskAmplitude.ToString(),nowTaskWidth.ToString(),bias.ToString(), (Time.time-taskStartTime).ToString(),cursorX.ToString(),cursorY.ToString(),taskStarted.ToString()};
        string s2 = string.Join(",",s1);
        if(swPos!=null)
        {
            swPos.WriteLine(s2);
        }
    }
    //操作時間を保存するCSVを作成
    private void makeMTCSV()
    {
        if(practice)
        {
            swMT = new StreamWriter(@"PracticeMT"+Participant.ToString()+bias.ToString()+".csv", true, Encoding.GetEncoding("UTF-8"));
        }
        else
        {
            swMT = new StreamWriter(@"MT"+Participant.ToString()+bias.ToString()+".csv", true, Encoding.GetEncoding("UTF-8"));
        }
        string[] s1 = { "参加者", "セット", "試行", "長さ", "幅","バイアス", "操作時間", "We","軌跡総距離","クリア","逸脱回数"};
        string s2 = string.Join(",", s1);
        swMT.WriteLine(s2);
    }
    //タスクのクリアごとに操作時間を保存
    private void updateMTCSV(int clear)
    {
        AeOld = Mathf.Abs(StartPoint.x-EndPoint.x);
        double SDy = cursorYarray.PopulationStandardDeviation();
        double We = 4.133 * SDy;
        string[] s1 = {Participant.ToString(), (taskNum/(taskAmplitude.Length*taskWidth.Length)).ToString(), (taskNum%(taskAmplitude.Length*taskWidth.Length)).ToString(), nowTaskAmplitude.ToString(), nowTaskWidth.ToString(), bias.ToString(), (taskFinishTime-taskStartTime).ToString(), We.ToString(), Ae.ToString(), clear.ToString(), deviationCount.ToString()};
        string s2 = string.Join(",", s1);
        if(swMT!=null)
        {
            swMT.WriteLine(s2);
        }
        if(clear==1 || clear == 0)
        {
            VariableManager.MTSum += taskFinishTime-taskStartTime;
            if(bias == Bias.Neutral)
            {
                VariableManager.AllMTSumNeutral += taskFinishTime-taskStartTime;
            }
            if(bias == Bias.Fast)
            {
                VariableManager.AllMTSumFast += taskFinishTime-taskStartTime;
            }
            if(bias == Bias.Accurate)
            {
                VariableManager.AllMTSumAccurate += taskFinishTime-taskStartTime;
            }
        }
        
    }
}