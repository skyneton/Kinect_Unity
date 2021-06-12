using UnityEngine;
using Windows.Kinect;

public class KinectBodyManager : MonoBehaviour
{
    public static KinectBodyManager instance { get; private set; }
    private BodyFrameReader bodyReader;
    public Body[] data { get; private set; }

    private void Awake() {
        instance = this;
    }

    void Start() {
        KinectSensorLoad();
    }

    // Update is called once per frame
    void Update() {
        KinectSensorLoad();
        GetKinectBodySkeleton();
    }

    private void GetKinectBodySkeleton() {
        //키넥트가 연결 안됐을 경우
        if (bodyReader == null) return;

        BodyFrame frame = bodyReader.AcquireLatestFrame();
        if (frame == null) return;

        data = new Body[frame.BodyCount];
        frame.GetAndRefreshBodyData(data);

        frame.Dispose();
    }

    private void KinectSensorLoad() {
        if (KinectManager.instance == null || KinectManager.instance.sensor != null && bodyReader != null) return;

        if (bodyReader == null) bodyReader = KinectManager.instance.sensor.BodyFrameSource.OpenReader();
        if (bodyReader == null) return;
    }

    private void OnApplicationQuit() {
        if (bodyReader != null) bodyReader.Dispose();
    }
}
