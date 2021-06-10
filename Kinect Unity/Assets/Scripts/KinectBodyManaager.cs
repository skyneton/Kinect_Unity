using UnityEngine;
using Windows.Kinect;

public class KinectBodyManaager : MonoBehaviour
{
    public static KinectBodyManaager instance { get; private set; }
    private KinectSensor sensor;
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

        if(data == null) data = new Body[sensor.BodyFrameSource.BodyCount];
        frame.GetAndRefreshBodyData(data);

        frame.Dispose();
    }

    public ColorSpacePoint? GetColoredSpacePoint(CameraSpacePoint point) {
        if (sensor == null) return null;
        return sensor.CoordinateMapper.MapCameraPointToColorSpace(point);
    }

    private void KinectSensorLoad() {
        if (sensor != null && bodyReader != null) return;
        if (sensor == null) sensor = KinectSensor.GetDefault();
        if (sensor == null) return;

        if (bodyReader == null) bodyReader = sensor.BodyFrameSource.OpenReader();
        if (bodyReader == null) return;

        if (!sensor.IsOpen) sensor.Open();
    }

    private void OnApplicationQuit() {
        if (bodyReader != null) bodyReader.Dispose();
        if (sensor != null && sensor.IsOpen) sensor.Close();
    }
}
