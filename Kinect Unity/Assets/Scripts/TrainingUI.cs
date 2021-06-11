using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;
using Kinect = Windows.Kinect;

public class TrainingUI : MonoBehaviour
{
    public TextData[] textDatas = new TextData[6];
    private TrainingData training = new TrainingData();

    public static TrainingUI instance;

    private void Awake() {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Clear();
    }

    // Update is called once per frame
    void Update()
    {
        if(training.isSquarting) {
            training.squartTime -= Time.deltaTime;
            TextMessage(string.Format("{0:0}초 동안 유지하세요", training.squartTime));

            if (training.squartTime <= 0) {
                if(++training.num >= 10) {
                    training.num = 0;
                    training.set++;
                }
                UpdateSquartInfo();
                SetSquartMode(false);
                training.beforeSquarting = true;
                TextMessage("일어나세요");
            }
        }
    }

    public void UpdateSquartInfo() {
        textDatas[0].text.text = string.Format("스쿼트 {0}세트 ˅", training.set);
        textDatas[1].text.text = string.Format("현재 {0} / 10개", training.num);
    }

    public void TrainingCheck(Body body) {
        //자세 확인
        Dictionary<JointType, Kinect.Joint> Joints = body.Joints;
        Vector3 ankleAvg = GetAverageVector(Joints[JointType.AnkleLeft], Joints[JointType.AnkleRight]);
        Vector3 kneeAvg = GetAverageVector(Joints[JointType.KneeRight], Joints[JointType.KneeLeft]);
        Vector3 hipAvg = GetAverageVector(Joints[JointType.HipLeft], Joints[JointType.HipRight]);
        Vector3 shoulder = GetAverageVector(Joints[JointType.ShoulderLeft], Joints[JointType.ShoulderRight]);
        Vector3 nect = CanvasViewManager.Joint2Vector3(Joints[JointType.Neck]) * 100;

        bool isAnkleCheck = WithInColorFrame(Joints[JointType.AnkleLeft].Position, Joints[JointType.AnkleRight].Position);
        bool isKneeCheck = WithInColorFrame(Joints[JointType.KneeLeft].Position, Joints[JointType.KneeRight].Position);
        bool isHipCheck = WithInColorFrameSingle(Joints[JointType.SpineBase].Position);
        bool isShoulderCheck = WithInColorFrameSingle(Joints[JointType.SpineShoulder].Position);
        bool isNectCheck = WithInColorFrameSingle(Joints[JointType.Head].Position);

        bool isWarning = false;

        string str = "";

        if(!isKneeCheck || !isHipCheck || !isShoulderCheck) {
            SetSquartMode(false);
            TextWarningMessage("몸 전체가 나와야 합니다.");
            training.beforeSquarting = false;
            isWarning = true;
        }

        if(isAnkleCheck) {
            //발목 -> 무릎 체크
            float distance = Vector2.Distance(Vector3ToXZ(ankleAvg), Vector3ToXZ(kneeAvg));
            float y = Distance(ankleAvg.y, kneeAvg.y);

            float degree = Mathf.Atan2(y, distance) * Mathf.Rad2Deg;
            str += string.Format("발목-무릎={0:00.00} (84 ~ 90)", degree);

            if ((degree < 84f || degree > 90f || ankleAvg.y >= kneeAvg.y) && !isWarning) {
                SetSquartMode(false);
                TextWarningMessage("자세가 잘못되었습니다.\n발목 - 무릎");
                training.beforeSquarting = false;
                isWarning = true;
            }
        }

        {
            //무릎 -> 골반 체크
            float distance = Vector2.Distance(Vector3ToXZ(kneeAvg), Vector3ToXZ(hipAvg));
            float y = Distance(kneeAvg.y, hipAvg.y);

            float degree = Mathf.Atan2(y, distance) * Mathf.Rad2Deg;
            str += string.Format("\n무릎-골반={0:00.00} (20 ~ 80)", degree);

            if ((kneeAvg.y >= hipAvg.y || degree < 20f || degree > 80f) && !isWarning) {
                SetSquartMode(false);
                TextWarningMessage("자세가 잘못되었습니다.\n무릎 - 골반");
                training.beforeSquarting = false;
                isWarning = true;
            }
        }

        {
            //골반 -> 어깨 체크
            float distance = Vector2.Distance(Vector3ToXZ(hipAvg), Vector3ToXZ(shoulder));
            float y = Distance(hipAvg.y, shoulder.y);

            float degree = Mathf.Atan2(y, distance) * Mathf.Rad2Deg;
            str += string.Format("\n골반-어깨={0:00.00} (70 ~ 90)", degree);

            if ((degree < 70f || degree > 90f) && !isWarning) {
                SetSquartMode(false);
                TextWarningMessage("자세가 잘못되었습니다.\n골반 - 어깨");
                training.beforeSquarting = false;
                isWarning = true;
            }
        }

        if(isNectCheck) {
            //어깨 -> 목 체크
            float distance = Vector2.Distance(Vector3ToXZ(shoulder), Vector3ToXZ(nect));
            float y = Distance(shoulder.y, nect.y);

            float degree = Mathf.Atan2(y, distance) * Mathf.Rad2Deg;
            str += string.Format("\n어깨-목={0:00.00} (0 ~ 10)", degree);

            if ((degree < 0f || degree > 10f) && !isWarning) {
                SetSquartMode(false);
                TextWarningMessage("자세가 잘못되었습니다.\n어깨 - 목");
                training.beforeSquarting = false;
                isWarning = true;
            }
        }

        if (!textDatas[4].text.gameObject.activeSelf) textDatas[4].text.gameObject.SetActive(true);
        if (!textDatas[5].text.gameObject.activeSelf) textDatas[5].text.gameObject.SetActive(true);
        textDatas[5].text.text = str;
        if(!isWarning) SetSquartMode(true);
    }

    public Vector2 GetAverageVector(Kinect.Joint j1, Kinect.Joint j2) {
        if (j1.TrackingState == TrackingState.Tracked && j2.TrackingState == TrackingState.Tracked)
            return JointAverage(j1.Position, j2.Position);

        else if (j1.TrackingState == TrackingState.Tracked)
            return CanvasViewManager.Joint2Vector3(j1) * 100;

        return CanvasViewManager.Joint2Vector3(j2) * 100;
    }

    public static Vector3 JointAverage(CameraSpacePoint p1, CameraSpacePoint p2) {
        return new Vector3((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, (p1.Z + p2.Z) / 2) * 100;
    }

    public static Vector2 Vector3ToXZ(Vector3 pos) {
        return new Vector2(pos.x, pos.z);
    }

    public static float Distance(float x1, float x2) {
        return Mathf.Abs(x1 - x2);
    }

    public static bool WithInColorFrame(CameraSpacePoint p1, CameraSpacePoint p2) {
        if (!WithInColorFrameSingle(p1)) return false;
        if (!WithInColorFrameSingle(p2)) return false;

        return true;
    }

    public static bool WithInColorFrameSingle(CameraSpacePoint p) {
        ColorSpacePoint? point = KinectBodyManaager.instance.GetColoredSpacePoint(p);
        if (!point.HasValue) return false;
        if (!CanvasViewManager.IsWithinColorFrame(CanvasViewManager.Point2Vector2(point.Value))) return false;

        return true;
    }

    public void Clear() {
        training.num = 0;
        training.set = 0;
        training.beforeSquarting = false;
        training.isSquarting = false;
        textDatas[2].text.gameObject.SetActive(false);
        textDatas[3].text.gameObject.SetActive(false);
        textDatas[4].text.gameObject.SetActive(false);
        textDatas[5].text.gameObject.SetActive(false);
        UpdateSquartInfo();
    }

    private void TextWarningMessage(string text) {
        TextData textData = textDatas[2];
        textData.text.text = text;

        SetColor(textData.text, 1f, 0f, 0f);
        SetColor(textData.outline, 0.73f, 0.0705f, 0.0705f);

        if (!textData.text.gameObject.activeSelf) textData.text.gameObject.SetActive(true);
    }

    private void TextMessage(string text) {
        TextData textData = textDatas[2];
        textData.text.text = text;

        SetColor(textData.text, 0.141f, 1f, 0f);
        SetColor(textData.outline, 0f, 0.73f, 0.0705f);

        if (!textData.text.gameObject.activeSelf) textData.text.gameObject.SetActive(true);
    }

    public static void SetColor(Text text, float r, float g, float b) {
        Color textColor = text.color;
        textColor.r = r;
        textColor.g = g;
        textColor.b = b;
        text.color = textColor;
    }

    public static void SetColor(Outline outline, float r, float g, float b) {
        Color textColor = outline.effectColor;
        textColor.r = r;
        textColor.g = g;
        textColor.b = b;
        outline.effectColor = textColor;
    }

    private void SetSquartMode(bool squart) {
        if (training.isSquarting == squart || training.beforeSquarting && squart) return;
        training.squartTime = 5;
        training.isSquarting = squart;

        if (squart) {
            training.beforeSquarting = false;
            if (!textDatas[2].text.gameObject.activeSelf) textDatas[2].text.gameObject.SetActive(true);
            if (!textDatas[3].text.gameObject.activeSelf) textDatas[3].text.gameObject.SetActive(true);
        }
        else {
            if (textDatas[2].text.gameObject.activeSelf) textDatas[2].text.gameObject.SetActive(false);
            if (textDatas[3].text.gameObject.activeSelf) textDatas[3].text.gameObject.SetActive(false);
        }
    }

    [Serializable]
    public class TextData {
        public Text text;
        public Outline outline;
    }

    public class TrainingData {
        public int set = 0; //세트
        public int num = 0; //스쿼트 세트 개수
        public bool isSquarting = false;
        public bool beforeSquarting = false;
        public float squartTime = 5f;
    }
}
