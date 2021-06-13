using UnityEngine;
using Windows.Kinect;

public class KinectColorManager : MonoBehaviour
{
    public static KinectColorManager instance { get; private set; }
    private ColorFrameReader colorReader;
    public byte[] data { get; private set; }

    public Texture2D texture { get; private set; }
    public int perPixel { get; private set; } = 0;

    private void Awake() {
        instance = this;
    }

    void Start() {
        KinectSensorLoad();
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

    private void KinectSensorLoad() {
        if (KinectManager.instance == null || KinectManager.instance.sensor == null || colorReader != null) return;
        if (colorReader == null) colorReader = KinectManager.instance.sensor.ColorFrameSource.OpenReader();
        if (colorReader == null) return;

        FrameDescription description = KinectManager.instance.sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
        texture = new Texture2D(description.Width, description.Height, TextureFormat.RGBA32, false);
        perPixel = (int)description.BytesPerPixel;
        data = new byte[description.BytesPerPixel * description.LengthInPixels];
    }

    public void UpdateTexture() {
        texture.LoadRawTextureData(data);
        texture.Apply();
    }

    private void OnApplicationQuit() {
        if (colorReader != null) colorReader.Dispose();
    }
}
