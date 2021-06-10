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

    private CanvasScaler scaler;

    public float textureScale { get; private set; }

    public static readonly int MIN_WIDTH = 300;

    Dictionary<JointType, LineRenderer> boneObjects = new Dictionary<JointType, LineRenderer>();



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
        scaler = GetComponent<CanvasScaler>();
    }

    void Update() {
        OnChangedTextureScale();
        UpdateKinectImage();
        DrawSkeleton();
    }

    private void UpdateKinectImage() {
        if (KinectColorManager.instance == null || KinectColorManager.instance.texture == null) return;
        image.texture = KinectColorManager.instance.texture;
    }

    private void OnChangedTextureScale() {
        if (KinectColorManager.instance == null || KinectColorManager.instance.texture == null) return;
        Texture2D texture = KinectColorManager.instance.texture;

        if (beforeSize[0] != texture.width || beforeSize[1] != texture.height) {
            beforeSize[0] = texture.width;
            beforeSize[1] = texture.height;

            Vector2 vec = scaler.referenceResolution;

            textureScale = 1;
            if (texture.height / scale < MIN_WIDTH) {
                textureScale = MIN_WIDTH / (texture.height / scale);
            }

            vec.x = texture.width * textureScale / scale;
            vec.y = texture.height * textureScale;

            scaler.referenceResolution = vec;

            Vector2 rect = image.rectTransform.sizeDelta;

            rect.x = texture.width * textureScale;
            rect.y = texture.height * textureScale;

            image.rectTransform.sizeDelta = rect;
        }
    }

    private void DrawSkeleton() {
        if (KinectBodyManaager.instance == null || KinectBodyManaager.instance.data == null) return;
        foreach(Body body in KinectBodyManaager.instance.data) {
            if (body == null || !body.IsTracked) continue;
            DrawBody(body);
        }
    }

    private LineRenderer CreateBodyObject(JointType type) {
        GameObject body = new GameObject();
        body.transform.parent = boneGroupObject;
        body.name = type.ToString();
        LineRenderer lr = body.AddComponent<LineRenderer>();
        lr.startWidth = 0.5f;
        lr.endWidth = 0.5f;

        boneObjects.Add(type, lr);
        return lr;
    }

    private void DrawBody(Body body) {
        for(JointType type = JointType.SpineBase; type <= JointType.ThumbRight; type++) {
            Windows.Kinect.Joint joint = body.Joints[type];
            Windows.Kinect.Joint? target = null;

            if (boneMap.ContainsKey(type)) {
                target = body.Joints[boneMap[type]];
            }

            ColorSpacePoint? point = KinectBodyManaager.instance.GetColoredSpacePoint(joint.Position);


            LineRenderer lr;
            boneObjects.TryGetValue(type, out lr);
            if (lr == null) lr = CreateBodyObject(type);

            if (point.HasValue) {
                Vector2 pos = Camera.main.ScreenToWorldPoint(Point2Vector2(point.Value));

                print(textureScale);

                lr.transform.localPosition = pos;

                if (target.HasValue) {
                    ColorSpacePoint? targetPoint = KinectBodyManaager.instance.GetColoredSpacePoint(target.Value.Position);
                    if (targetPoint.HasValue) {
                        lr.SetPosition(0, pos);
                        lr.SetPosition(1, Camera.main.ScreenToWorldPoint(Point2Vector2(targetPoint.Value)));
                        lr.startColor = GetColorForState(joint.TrackingState);
                        lr.endColor = GetColorForState(target.Value.TrackingState);
                    }
                }
                else lr.enabled = false;
            }
        }
    }

    public static Vector3 Joint2Vector3(Windows.Kinect.Joint joint) {
        return new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
    }

    public static Vector2 Point2Vector2(ColorSpacePoint point) {
        return new Vector2(point.X, point.Y);
    }

    private static Color GetColorForState(TrackingState state) {
        switch (state) {
            case TrackingState.Tracked:
                return Color.green;

            case TrackingState.Inferred:
                return Color.red;

            default:
                return Color.black;
        }
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
