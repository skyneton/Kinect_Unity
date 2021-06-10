using UnityEngine;
using Windows.Kinect;

public class KinectColorManager : MonoBehaviour
{
    public static KinectColorManager instance { get; private set; }
    private KinectSensor sensor;
    private ColorFrameReader colorReader;
    private byte[] data;

    public Texture2D texture { get; private set; }

    private void Awake() {
        instance = this;
    }

    void Start() {
        KinectSensorLoad();
        sensor = KinectSensor.GetDefault();
    }

    // Update is called once per frame
    void Update() {
        KinectSensorLoad();
        GetKinectColorCamera();
    }

    private void GetKinectColorCamera() {
        //키넥트가 연결 안됐을 경우
        if (colorReader == null) return;

        ColorFrame frame = colorReader.AcquireLatestFrame();
        if (frame == null) return;

        frame.CopyConvertedFrameDataToArray(data, ColorImageFormat.Rgba);
        texture.LoadRawTextureData(data);
        texture.Apply();

        frame.Dispose();
    }

    public void KinectSensorLoad() {
        if (sensor != null && colorReader != null) return;
        if (sensor == null) sensor = KinectSensor.GetDefault();
        if (sensor == null) return;
        if (colorReader == null) colorReader = sensor.ColorFrameSource.OpenReader();
        if (colorReader == null) return;

        FrameDescription description = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
        texture = new Texture2D(description.Width, description.Height, TextureFormat.RGBA32, false);
        data = new byte[description.BytesPerPixel * description.LengthInPixels];

        if (!sensor.IsOpen) sensor.Open();
    }

    private void OnApplicationQuit() {
        if (colorReader != null) colorReader.Dispose();
        if (sensor != null && sensor.IsOpen) sensor.Close();
    }
}
