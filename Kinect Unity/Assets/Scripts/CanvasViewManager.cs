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

    Dictionary<JointType, LineRenderer> boneObjects = new Dictionary<JointType, LineRenderer>();

    private RectTransform rect;
    private Canvas canvas;



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

            textureScale = this.rect.sizeDelta.x / texture.width;
            if (texture.height * textureScale < this.rect.sizeDelta.y) {
                textureScale = this.rect.sizeDelta.y / texture.height;
            }

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
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.material = lineMaterial;

        boneObjects.Add(type, lr);
        return lr;
    }

    private void DrawBody(Body body) {
        foreach (Windows.Kinect.Joint joint in body.Joints.Values) {
            Windows.Kinect.Joint? targetJoint = null;

            if (boneMap.ContainsKey(joint.JointType)) targetJoint = body.Joints[boneMap[joint.JointType]];

            LineRenderer lr;
            if (boneObjects.ContainsKey(joint.JointType)) lr = boneObjects[joint.JointType];
            else lr = CreateBodyObject(joint.JointType);


            ColorSpacePoint? point = KinectBodyManaager.instance.GetColoredSpacePoint(joint.Position);
            if (!point.HasValue) {
                lr.enabled = false;
                continue;
            }

            Vector3 pos = Point2Vector2(point.Value);
            if (!IsWithinColorFrame(pos)) {
                lr.enabled = false;
                continue;
            }
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

            ColorSpacePoint? targetPoint = KinectBodyManaager.instance.GetColoredSpacePoint(targetJoint.Value.Position);
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
        }
        /*
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
                Vector2 pos = Point2Vector2(point.Value) * textureScale;

                if(!IsWithinColorFrame(pos)) {
                    lr.enabled = false;
                    return;
                }

                pos.x -= scaler.referenceResolution.x / 2;
                pos.y -= scaler.referenceResolution.y / 2;

                lr.transform.localPosition = Camera.main.ScreenToWorldPoint(pos);

                if (target.HasValue) {
                    ColorSpacePoint? targetPoint = KinectBodyManaager.instance.GetColoredSpacePoint(target.Value.Position);
                    if (targetPoint.HasValue) {
                        Vector2 targetPos = Point2Vector2(targetPoint.Value) * textureScale;

                        targetPos.x -= scaler.referenceResolution.x / 2;
                        targetPos.y -= scaler.referenceResolution.y / 2;

                        if (!IsWithinColorFrame(targetPos)) {
                            lr.enabled = false;
                            return;
                        }

                        lr.SetPosition(0, Camera.main.ScreenToWorldPoint(pos));
                        lr.SetPosition(1, Camera.main.ScreenToWorldPoint(targetPos));
                        lr.startColor = GetColorForState(joint.TrackingState);
                        lr.endColor = GetColorForState(target.Value.TrackingState);
                    }
                }
                else lr.enabled = false;
            }
        }
        */
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
                return Color.green;

            case TrackingState.Inferred:
                return Color.red;

            default:
                return Color.black;
        }
    }

    private bool IsWithinColorFrame(Vector2 pos) {
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
