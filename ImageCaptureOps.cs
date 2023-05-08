using UnityEngine;

namespace AssetBundleViewer
{
    public class ImageCaptureOps : MonoBehaviour
    {
        public static Texture2D MakeScreenshot()
        {
            return ScreenCapture.CaptureScreenshotAsTexture();
        }

        public static Texture2D CaptureCameraView(Camera aCamera, int width = 0, int height = 0, int depth = 24)
        {
            width = width == 0 ? aCamera.pixelWidth : width;
            height = height == 0 ? aCamera.pixelHeight : height;

            aCamera.targetTexture = new RenderTexture(width, height, depth);
            RenderTexture.active = aCamera.targetTexture;
            aCamera.Render();

            var targetTexture = aCamera.targetTexture;
            var aTexture2D = new Texture2D(targetTexture.width, targetTexture.height);
            aTexture2D.ReadPixels(new Rect(0, 0, targetTexture.width, targetTexture.height), 0, 0);
            aTexture2D.Apply();
            aCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(targetTexture);
            return aTexture2D;
        }

        public static Texture2D ResizeTexture2D(Texture2D aTexture2D, int width, int height)
        {
            aTexture2D.Reinitialize(width, height);
            return aTexture2D;
        }


        public static Texture2D CropTexture2D(Texture2D aTexture2D, Vector2Int startPixel, Vector2Int cropSize)
        {
            var cropRect = new RectInt(position: startPixel, size: cropSize);
            var resultTexture2D = new Texture2D(cropSize.x, cropSize.y);
            var colors = aTexture2D.GetPixels(cropRect.xMin, cropRect.yMin, cropRect.width, cropRect.height);
            resultTexture2D.SetPixels(colors);
            resultTexture2D.Apply();
            return resultTexture2D;
        }


    }
}
