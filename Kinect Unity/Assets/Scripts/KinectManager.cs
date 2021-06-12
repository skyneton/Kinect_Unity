using UnityEngine;
using Windows.Kinect;

public class KinectManager : MonoBehaviour
{
    public static KinectManager instance { get; private set; }
    public KinectSensor sensor { get; private set; }

    private void Awake() {
        instance = this;
        KinectSensorLoad();
    }

    void Update() {
        KinectSensorLoad();
    }

    public ColorSpacePoint? GetColoredSpacePoint(CameraSpacePoint point) {
        if (sensor == null) return null;
        return sensor.CoordinateMapper.MapCameraPointToColorSpace(point);
    }

    private void KinectSensorLoad() {
        if (sensor == null || !sensor.IsAvailable) sensor = KinectSensor.GetDefault();
        if (sensor == null) return;
        if (!sensor.IsOpen) sensor.Open();
    }

    private void OnApplicationQuit() {
        if (sensor != null && sensor.IsOpen) sensor.Close();
    }
}
