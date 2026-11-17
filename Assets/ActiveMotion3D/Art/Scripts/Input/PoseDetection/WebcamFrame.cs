using System;
using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Input.PoseDetection
{
    internal sealed class WebcamFrame : IDisposable
    {
        private Color32[] cameraPixels;
        private Color32[] uprightPixels;

        public Texture2D Texture { get; private set; }

        public void Capture(WebCamTexture webcam)
        {
            int width = webcam.width;
            int height = webcam.height;
            int rotation = webcam.videoRotationAngle;
            bool sideways = rotation == 90 || rotation == 270;
            int frameWidth = sideways ? height : width;
            int frameHeight = sideways ? width : height;
            if (Texture == null || Texture.width != frameWidth || Texture.height != frameHeight)
            {
                Dispose();
                Texture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);
                uprightPixels = new Color32[width * height];
            }

            cameraPixels = webcam.GetPixels32(cameraPixels);
            CopyUpright(cameraPixels, uprightPixels, width, height, rotation, webcam.videoVerticallyMirrored);
            Texture.SetPixels32(uprightPixels);
            Texture.Apply(false);
        }

        internal static void CopyUpright(Color32[] source, Color32[] destination, int width, int height,
            int rotation, bool mirrored)
        {
            // Unity's texture buffer starts at the bottom. MediaPipe reads its first row as the top.
            bool sideways = rotation == 90 || rotation == 270;
            int frameWidth = sideways ? height : width;
            int frameHeight = sideways ? width : height;
            int rowStep = mirrored ? width : -width;
            int topRow = mirrored ? 0 : (height - 1) * width;
            for (int y = 0; y < frameHeight; y++)
            {
                int start;
                int step;
                switch (rotation)
                {
                    case 90:
                        start = topRow + (height - 1) * rowStep + y;
                        step = -rowStep;
                        break;
                    case 180:
                        start = topRow + (height - 1 - y) * rowStep + width - 1;
                        step = -1;
                        break;
                    case 270:
                        start = topRow + width - 1 - y;
                        step = rowStep;
                        break;
                    default:
                        Array.Copy(source, topRow + y * rowStep, destination, y * frameWidth, frameWidth);
                        continue;
                }

                for (int x = 0; x < frameWidth; x++)
                    destination[y * frameWidth + x] = source[start + x * step];
            }
        }

        public void Dispose()
        {
            if (Texture != null) UnityEngine.Object.Destroy(Texture);
            Texture = null;
            cameraPixels = null;
            uprightPixels = null;
        }
    }
}
