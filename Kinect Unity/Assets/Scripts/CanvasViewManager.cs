using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;

public class CanvasViewManager : MonoBehaviour
{
    public Transform boneGroupObject;

    private int[] beforeSize = { 0, 0 };
    public RawImage image;

    public float scale = 1.64f;

    public float textureScale { get; private set; }

    public static readonly int MIN_WIDTH = 300;

    public Material lineMaterial;

    private ulong? TrackingId;

    Dictionary<JointType, LineRenderer> boneObjects = new Dictionary<JointType, LineRenderer>();

    private RectTransform rect;
    private Canvas canvas;

    private Vector2 lastScreenSize;



    private Dictionary<JointType, JointType> boneMap = new Dictionary<JointType, JointType>()
    {
        { JointType.FootLeft, JointType.AnkleLeft },
        { JointType.AnkleLeft, JointType.KneeLeft },
        { JointType.KneeLeft, JointType.HipLeft },
        { JointType.HipLeft, JointType.SpineBase },

        { JointType.FootRight, JointType.AnkleRight },
        { JointType.AnkleRight, JointType.KneeRight },
        { JointType.KneeRight, JointType.HipRight },
        { JointType.HipRight, JointType.SpineBase },

        { JointType.HandTipLeft, JointType.HandLeft },
        { JointType.ThumbLeft, JointType.HandLeft },
        { JointType.HandLeft, JointType.WristLeft },
        { JointType.WristLeft, JointType.ElbowLeft },
        { JointType.ElbowLeft, JointType.ShoulderLeft },
        { JointType.ShoulderLeft, JointType.SpineShoulder },

        { JointType.HandTipRight, JointType.HandRight },
        { JointType.ThumbRight, JointType.HandRight },
        { JointType.HandRight, JointType.WristRight },
        { JointType.WristRight, JointType.ElbowRight },
        { JointType.ElbowRight, JointType.ShoulderRight },
        { JointType.ShoulderRight, JointType.SpineShoulder },

        { JointType.SpineBase, JointType.SpineMid },
        { JointType.SpineMid, JointType.SpineShoulder },
        { JointType.SpineShoulder, JointType.Neck },
        { JointType.Neck, JointType.Head },
    };


    private void Start() {
        rect = GetComponent<RectTransform>();
        canvas = GetComponent<Canvas>();
        lastScreenSize = new Vector2(Screen.width, Screen.height);
    }

    void Update() {
        OnChangedTextureScale();
        UpdateKinectImage();
        DrawSkeleton();
        ScreenChangeCheck();
    }

    private void ScreenChangeCheck() {
        if (lastScreenSize.x == Screen.width && lastScreenSize.y == Screen.height) return;
        lastScreenSize.x = Screen.width;
        lastScreenSize.y = Screen.height;

        OnChangedTextureScale(true);
    }

    private void UpdateKinectImage() {
        if (KinectColorManager.instance == null || KinectColorManager.instance.texture == null) return;
        image.texture = KinectColorManager.instance.texture;
    }

    private void OnChangedTextureScale(bool force = false) {
        if (KinectColorManager.instance == null || KinectColorManager.instance.texture == null) return;
        Texture2D texture = KinectColorManager.instance.texture;

        if (beforeSize[0] != texture.width || beforeSize[1] != texture.height || force) {
            beforeSize[0] = texture.width;
            beforeSize[1] = texture.height;

            textureScale = this.rect.sizeDelta.x / texture.width;
            if (texture.height * textureScale < this.rect.sizeDelta.y) {
                textureScale = this.rect.sizeDelta.y / texture.height;
            }

            Vector2 rect = image.rectTransform.sizeDelta;

            rect.x = texture.width * textureScale;
            rect.y = texture.height * textureScale;

            image.rectTransform.sizeDelta = rect;

            Vector2 train = TrainingUI.instance.rect.sizeDelta;
            train.x = rect.x * 0.3f;
            if (train.x < 300) train.x = 300;
            TrainingUI.instance.rect.sizeDelta = train;

            Vector2 anchored = TrainingUI.instance.rect.anchoredPosition;
            anchored.x = -train.x / 2;
            TrainingUI.instance.rect.anchoredPosition = anchored;

            TrainingUI.TextData[] textDatas = TrainingUI.instance.textDatas;

            float height = this.rect.sizeDelta.y + TrainingUI.instance.rect.sizeDelta.y;

            //스쿼트 TITLE 크기
            RectTransform textRect = textDatas[0].text.rectTransform;
            Vector2 size = textRect.sizeDelta;
            size.y = height * 0.1f;
            textRect.sizeDelta = size;

            Vector2 pos = textRect.anchoredPosition;
            pos.y = -size.y / 2 - 10;
            textRect.anchoredPosition = pos;

            float tempHeight = size.y;

            //현재 갯수 텍스트
            textRect = textDatas[1].text.rectTransform;
            size = textRect.sizeDelta;
            size.y = height * 0.06f;
            textRect.sizeDelta = size;

            pos = textRect.anchoredPosition;
            pos.y = -tempHeight + -size.y / 2 - 10;
            textRect.anchoredPosition = pos;

            //상태 택스트
            textRect = textDatas[2].text.rectTransform;
            size = textRect.sizeDelta;
            size.y = height * 0.08f;
            textRect.sizeDelta = size;

            tempHeight = size.y;

            //완벽한 자세 텍스트
            textRect = textDatas[3].text.rectTransform;
            size = textRect.sizeDelta;
            size.y = height * 0.06f;
            textRect.sizeDelta = size;

            pos = textRect.anchoredPosition;
            pos.y = -tempHeight + -size.y / 2;
            textRect.anchoredPosition = pos;

            //Now Input Data 텍스트
            textRect = textDatas[4].text.rectTransform;
            size = textRect.sizeDelta;
            size.y = height * 0.05f;
            textRect.sizeDelta = size;

            pos = textRect.anchoredPosition;
            pos.y = height * 0.15f + 40;
            textRect.anchoredPosition = pos;

            //Status 텍스트
            textRect = textDatas[5].text.rectTransform;
            size = textRect.sizeDelta;
            size.y = height * 0.15f;
            textRect.sizeDelta = size;

            pos = textRect.anchoredPosition;
            pos.y = size.y / 2 + 25;
            textRect.anchoredPosition = pos;
        }
    }

    private void DrawSkeleton() {
        if (KinectManager.instance == null || KinectBodyManager.instance == null || KinectBodyManager.instance.data == null) return;

        bool isBodyDraw = false;
        foreach(Body body in KinectBodyManager.instance.data) {
            if (body == null || !body.IsTracked || TrackingId.HasValue && TrackingId.Value != body.TrackingId) continue;
            TrackingId = body.TrackingId;
            Vector2? drawBody = DrawBody(body);
            if(drawBody.HasValue) {
                isBodyDraw = true;
                TrainingUI.instance.TrainingCheck(body); //자세 확인
                //KinectBodyIndexManager.instance.DrawBodyOutline(drawBody.Value);
            }
            break;
        }

        if(!isBodyDraw) TrackingId = null;
        if (!isBodyDraw && boneGroupObject.gameObject.activeSelf) boneGroupObject.gameObject.SetActive(false);
        if (!isBodyDraw && TrainingUI.instance != null) TrainingUI.instance.Clear();
    }

    private LineRenderer CreateBodyObject(JointType type) {
        GameObject body = new GameObject();
        body.transform.parent = boneGroupObject;
        body.name = type.ToString();
        LineRenderer lr = body.AddComponent<LineRenderer>();
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.material = lineMaterial;

        boneObjects.Add(type, lr);
        return lr;
    }

    private Vector2? DrawBody(Body body) {
        if (!boneGroupObject.gameObject.activeSelf) boneGroupObject.gameObject.SetActive(true);

        Vector2? bodyPos = null;

        foreach (Windows.Kinect.Joint joint in body.Joints.Values) {
            Windows.Kinect.Joint? targetJoint = null;

            if (boneMap.ContainsKey(joint.JointType)) targetJoint = body.Joints[boneMap[joint.JointType]];

            LineRenderer lr;
            if (boneObjects.ContainsKey(joint.JointType)) lr = boneObjects[joint.JointType];
            else lr = CreateBodyObject(joint.JointType);


            ColorSpacePoint? point = KinectManager.instance.GetColoredSpacePoint(joint.Position);
            if (!point.HasValue) {
                lr.enabled = false;
                continue;
            }

            Vector3 pos = Point2Vector2(point.Value);
            if (!IsWithinColorFrame(pos)) {
                lr.enabled = false;
                continue;
            }
            bodyPos = new Vector2(point.Value.X, point.Value.Y);

            pos.x -= KinectColorManager.instance.texture.width / 2;
            pos.y += KinectColorManager.instance.texture.height / 2;

            pos *= textureScale;

            pos.x += rect.sizeDelta.x / 2;
            pos.y += rect.sizeDelta.y / 2;
            pos.z = canvas.planeDistance;

            pos = Camera.main.ScreenToWorldPoint(pos);
            pos.z = 0;

            lr.transform.position = pos;

            if (!targetJoint.HasValue) {
                lr.enabled = false;
                continue;
            }

            ColorSpacePoint? targetPoint = KinectManager.instance.GetColoredSpacePoint(targetJoint.Value.Position);
            if(!targetPoint.HasValue) {
                lr.enabled = false;
                continue;
            }

            Vector3 targetPos = Point2Vector2(targetPoint.Value);
            if (!IsWithinColorFrame(targetPos)) {
                lr.enabled = false;
                continue;
            }

            targetPos.x -= KinectColorManager.instance.texture.width / 2;
            targetPos.y += KinectColorManager.instance.texture.height / 2;

            targetPos *= textureScale;

            targetPos.x += rect.sizeDelta.x / 2;
            targetPos.y += rect.sizeDelta.y / 2;
            targetPos.z = canvas.planeDistance;

            targetPos = Camera.main.ScreenToWorldPoint(targetPos);
            targetPos.z = 0;

            lr.SetPosition(0, pos);
            lr.SetPosition(1, targetPos);
            lr.startColor = GetColorForState(joint.TrackingState);
            lr.endColor = GetColorForState(targetJoint.Value.TrackingState);
            if (joint.TrackingState == TrackingState.Tracked && targetJoint.Value.TrackingState == TrackingState.Tracked)
                lr.enabled = true;
            else lr.enabled = false;
        }

        return bodyPos;
    }

    public static Vector3 Joint2Vector3(Windows.Kinect.Joint joint) {
        return new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
    }

    public static Vector2 Point2Vector2(ColorSpacePoint point) {
        return new Vector2(point.X, -point.Y);
    }

    private static Color GetColorForState(TrackingState state) {
        switch (state) {
            case TrackingState.Tracked:
                return new Color(0.3333333f, 1f, 0.76862745f);

            case TrackingState.Inferred:
                return Color.red;

            default:
                return Color.black;
        }
    }

    public static bool IsWithinColorFrame(Vector2 pos) {
        return pos.x >= 0 && pos.x <= KinectColorManager.instance.texture.width
            && -pos.y >= 0 && -pos.y <= KinectColorManager.instance.texture.height;
    }

    /*
     * 업데이트 이전 버전
    private void OnChangedTextureScaleBefore() {
        if (KinectColorManager.instance == null || KinectColorManager.instance.texture == null) return;
        Texture2D texture = KinectColorManager.instance.texture;
        if (beforeSize[0] != texture.width || beforeSize[1] != texture.height) {
            beforeSize[0] = texture.width;
            beforeSize[1] = texture.height;

            int width;

            if (texture.width * scale >= texture.height) width = texture.width;
            else width = (int)(texture.height / scale);

            Vector2 vec = scaler.referenceResolution;
            vec.x = width;
            vec.y = width * scale;

            scaler.referenceResolution = vec;



            //이미지 크기 변경
            float sizeX = vec.x * imageXScale;

            float imageScaler = sizeX/texture.width;
            if (texture.height * imageScaler < vec.y) imageScaler = vec.y/texture.height;

            Vector2 rect = image.rectTransform.sizeDelta;

            rect.x = texture.width * imageScaler;
            rect.y = texture.height * imageScaler;

            image.rectTransform.sizeDelta = rect;
            
            //canvas 이미지 위치 조절
            Vector3 pos = image.rectTransform.anchoredPosition;
            pos.x = (vec.x - sizeX)/2;
            image.rectTransform.anchoredPosition = pos;
        }
    }
    */
}
