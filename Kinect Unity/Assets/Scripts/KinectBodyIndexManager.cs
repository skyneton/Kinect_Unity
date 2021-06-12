using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;

public class KinectBodyIndexManager : MonoBehaviour
{
    public static KinectBodyIndexManager instance { get; private set; }
    private BodyIndexFrameReader indexReader;
    public byte[] data { get; private set; }

    private void Awake() {
        instance = this;
    }
    void Start() {
        KinectSensorLoad();
    }

    void Update() {
        KinectSensorLoad();
        GetKinectBodyIndexData();
    }

    private void GetKinectBodyIndexData() {
        if (indexReader == null || data == null) return;
        BodyIndexFrame frame = indexReader.AcquireLatestFrame();
        if (frame == null) return;

        frame.CopyFrameDataToArray(data);
        FrameDescription description = frame.BodyIndexFrameSource.FrameDescription;
        frame.Dispose();
    }

    private void KinectSensorLoad() {
        if (KinectManager.instance == null || KinectManager.instance.sensor == null || indexReader != null) return;
        if (indexReader == null) {
            indexReader = KinectManager.instance.sensor.BodyIndexFrameSource.OpenReader();
            if (indexReader == null) return;

            FrameDescription description = indexReader.BodyIndexFrameSource.FrameDescription;
            data = new byte[description.BytesPerPixel * description.LengthInPixels];
        }
    }

    private static byte[] Color = { 120, 216, 237, 255 };
    public void DrawBodyOutline(Vector2 pos) {
        byte[] buf = KinectColorManager.instance.data;
        FrameDescription description = indexReader.BodyIndexFrameSource.FrameDescription;
        int width = description.Width;
        int height = description.Height;

        int startX = (int)pos.x;
        int startY = (int)pos.y;

        int perPixel = KinectColorManager.instance.perPixel;

        float sizeScale = buf.Length / perPixel;
        sizeScale /= data.Length;

        print(Mathf.CeilToInt((startY * width + startX) * sizeScale));
        byte color = buf[Mathf.CeilToInt((startY * width + startX) * sizeScale)];
        if (color == 255) return;

        int x = startX;

        //위쪽 그리기
        for(int y = startY; y >= 0; y--) {
            if (buf[y * width + x] == color) {
                for(int tempX = x; tempX < width; tempX++) {
                    if (buf[y * width + tempX] != color) {
                        for(int i = y * width + tempX; i < perPixel * 2 && i < buf.Length; i++) buf[i] = Color[i % perPixel];
                        break;
                    }
                }
                for(int tempX = x; tempX >= 0; tempX--) {
                    if (buf[y * width + tempX] != color) {
                        for (int i = y * width + tempX; i < perPixel * 2 && i < buf.Length; i++) buf[i] = Color[i % perPixel];
                        x = tempX;
                        break;
                    }
                }
                continue;
            }

            bool isHas = false;
            for(; x < width; x++) {
                if(buf[y * width + x] == color) {
                    isHas = true;
                    y--;
                    break;
                }
            }

            if (!isHas) break;
        }

        x = startX;
        for(int y = startY - 1; y <= height; y++) {
            if (buf[y * width + x] == color) {
                for (int tempX = x; tempX < width; tempX++) {
                    if (buf[y * width + tempX] != color) {
                        for (int i = y * width + tempX; i < perPixel * 2 && i < buf.Length; i++) buf[i] = Color[i % perPixel];
                        break;
                    }
                }
                for (int tempX = x; tempX >= 0; tempX--) {
                    if (buf[y * width + tempX] != color) {
                        for (int i = y * width + tempX; i < perPixel * 2 && i < buf.Length; i++) buf[i] = Color[i % perPixel];
                        x = tempX;
                        break;
                    }
                }
                continue;
            }

            bool isHas = false;
            for (; x < width; x++) {
                if (buf[y * width + x] == color) {
                    isHas = true;
                    y++;
                    break;
                }
            }

            if (!isHas) break;
        }

        KinectColorManager.instance.UpdateTexture();
    }

    private void OnApplicationQuit() {
        if (indexReader != null) indexReader.Dispose();
    }
}
