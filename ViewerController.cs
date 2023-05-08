using JetBrains.Annotations;
using SFB;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using Application = UnityEngine.Application;
using Label = UnityEngine.UIElements.Label;
using ProgressBar = UnityEngine.UIElements.ProgressBar;
using Screen = UnityEngine.Screen;

namespace AssetBundleViewer
{
    public class ViewerController : MonoBehaviour
    {

        #region defining fields
        private Camera _camera;
        private Label _cameraAzimuthLabel;
        private Label _cameraDistanceLabel;
        private Label _cameraElevationLabel;
        private GameObject _cameraTransformGuide;
        private VisualElement _controlArea;
        [CanBeNull] private GameObject _currentModel;
        [CanBeNull] private AssetBundle _currentModelAssetBundle;
        private Bounds _currentModelBounds;
        [CanBeNull] private Material _currentSkybox;
        [CanBeNull] private AssetBundle _currentSkyboxAssetBundle;
        private Label _lightAzimuthLabel;
        private Label _lightElevationLabel;
        private Label _lightIntensityLabel;
        private Label _load3DModelLabel;
        private Label _loadSkyboxLabel;
        private Label _objectXRotationLabel;
        private Label _objectYRotationLabel;
        private Label _objectZRotationLabel;
        private Label _resetLabel;
        private VisualElement _root;
        private Label _skyboxExposureLabel;
        private GameObject _sun;
        private VisualElement _viewport;
        private VisualElement _viewportContainer;
        private VisualElement _fullscreenVE;
        private VisualElement _fullscreenIcon;
        private VisualElement _imageCaptureVE;
        private bool _isCapturingImage = false;
        private SelectableLabel _panelViewControlSL;
        #endregion

        #region defining properties
        private float CameraElevation
        {
            get => float.Parse(_cameraElevationLabel.text);
            set => _cameraElevationLabel.text = value.ToString("n2");
        }

        private float CameraAzimuth
        {
            get => float.Parse(_cameraAzimuthLabel.text);
            set => _cameraAzimuthLabel.text = value.ToString("n2");
        }

        private float CameraDistance
        {
            get => float.Parse(_cameraDistanceLabel.text);
            set => _cameraDistanceLabel.text = value.ToString("n2");
        }

        private float ObjectXRotation
        {
            get => float.Parse(_objectXRotationLabel.text);
            set => _objectXRotationLabel.text = value.ToString("n2");
        }

        private float ObjectYRotation
        {
            get => float.Parse(_objectYRotationLabel.text);
            set => _objectYRotationLabel.text = value.ToString("n2");
        }

        private float ObjectZRotation
        {
            get => float.Parse(_objectZRotationLabel.text);
            set => _objectZRotationLabel.text = value.ToString("n2");
        }

        private float LightElevation
        {
            get => float.Parse(_lightElevationLabel.text);
            set => _lightElevationLabel.text = value.ToString("n2");
        }

        private float LightAzimuth
        {
            get => float.Parse(_lightAzimuthLabel.text);
            set => _lightAzimuthLabel.text = value.ToString("n2");
        }

        private float LightIntensity
        {
            get => float.Parse(_lightIntensityLabel.text);
            set => _lightIntensityLabel.text = value.ToString("n2");
        }

        private float SkyboxExposure
        {
            get => float.Parse(_skyboxExposureLabel.text);
            set => _skyboxExposureLabel.text = value.ToString("n2");
        }
        #endregion


        private void Awake()
        {
            Physics.gravity = Vector3.zero;
            Application.targetFrameRate = 25;
        }

        private void Start()
        {
            _sun = GameObject.Find("Directional Light");
            _camera = Camera.main;
            _cameraTransformGuide = new GameObject
            {
                transform =
                {
                    position = Vector3.zero
                }
            };
            _camera.transform.SetParent(_cameraTransformGuide.transform);

            _root = GetComponent<UIDocument>().rootVisualElement;

            _controlArea = _root.Q<VisualElement>("controlArea");

            _viewport = _root.Q<VisualElement>("viewport");
            _viewport.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                evt.StopPropagation();
                UpdateViewport();
            });
            UpdateViewport();
            _viewport.RegisterCallback<MouseDownEvent>(HandleMouseDownEvent);
            _viewport.RegisterCallback<MouseUpEvent>(HandleMouseUpEvent);
            _viewport.RegisterCallback<MouseOutEvent>(HandleMouseOutEvent);
            _viewport.RegisterCallback<WheelEvent>(HandleMouseWheelEvent);

            _viewportContainer = _root.Q<VisualElement>("viewportContainer");

            _loadSkyboxLabel = _root.Q<Label>("loadSkyboxLabel");
            _loadSkyboxLabel.RegisterCallback<ClickEvent>(BrowseAndLoadSkybox);

            _load3DModelLabel = _root.Q<Label>("load3DModelLabel");
            _load3DModelLabel.RegisterCallback<ClickEvent>(BrowseAndLoadModel);

            _resetLabel = _root.Q<Label>("resetLabel");
            _resetLabel.RegisterCallback<ClickEvent>(ResetViewer);

            _cameraElevationLabel = _root.Q<Label>("cameraElevationLabel");
            _cameraAzimuthLabel = _root.Q<Label>("cameraAzimuthLabel");
            _cameraDistanceLabel = _root.Q<Label>("cameraDistanceLabel");
            _objectXRotationLabel = _root.Q<Label>("objectXRotationLabel");
            _objectYRotationLabel = _root.Q<Label>("objectYRotationLabel");
            _objectZRotationLabel = _root.Q<Label>("objectZRotationLabel");
            _lightElevationLabel = _root.Q<Label>("lightElevationLabel");
            _lightAzimuthLabel = _root.Q<Label>("lightAzimuthLabel");
            _lightIntensityLabel = _root.Q<Label>("lightIntensityLabel");
            _skyboxExposureLabel = _root.Q<Label>("skyboxExposureLabel");
            _fullscreenVE = _root.Q<VisualElement>("fullscreenVE");
            _fullscreenVE.RegisterCallback<ClickEvent>(HandleWindowingMode);
            _fullscreenIcon = _fullscreenVE.ElementAt(0);
            _imageCaptureVE = _root.Q<VisualElement>("imageCaptureVE");
            _imageCaptureVE.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                _isCapturingImage = true;
            });
            _panelViewControlSL = _root.Q<SelectableLabel>("panelViewControlSL");
            _panelViewControlSL.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                _panelViewControlSL.ElementAt(1).style.backgroundImage = Resources.Load<Texture2D>(_panelViewControlSL.IsSelected ? "Icons/showPanelIcon" : "Icons/hidePanelIcon");
                _controlArea.style.display = _panelViewControlSL.IsSelected ? DisplayStyle.None : DisplayStyle.Flex;
            });
            SetDefaultViewParameters();
        }

        private void CaptureAndSaveImage()
        {
            _isCapturingImage = false;
            var filePath = StandaloneFileBrowser.SaveFilePanel
                (
                    title: "Save captured image to ...",
                    directory: "",
                    defaultName: "",
                    extension: "JPEG Image (.jpeg)|*.jpeg|PNG Image (.png)|*.png"
                    );
            if (filePath == null | filePath == string.Empty) return;
            var capturedTexture = ImageCaptureOps.CaptureCameraView(aCamera: _camera, width: Mathf.FloorToInt(_root.resolvedStyle.width));
            var viewportWidth = Mathf.FloorToInt(_viewport.resolvedStyle.width);
            var controlAreaWidth = Mathf.FloorToInt(_controlArea.resolvedStyle.width);
            var croppedTexture2D = ImageCaptureOps.CropTexture2D(
                aTexture2D: capturedTexture,
                startPixel: new Vector2Int(controlAreaWidth, 0),
                cropSize: new Vector2Int(capturedTexture.width - controlAreaWidth, capturedTexture.height));
            var bytes = croppedTexture2D.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            Destroy(capturedTexture);
            Destroy(croppedTexture2D);
        }

        private void HandleWindowingMode(ClickEvent evt)
        {
            evt.StopPropagation();
            if (Screen.fullScreen == false)
            {
                Screen.fullScreen = true;
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
                _fullscreenIcon.style.backgroundImage = Resources.Load<Texture2D>("Icons/exitFullscreenIcon");
            }
            else
            {
                Screen.fullScreen = false;
                _fullscreenIcon.style.backgroundImage = Resources.Load<Texture2D>("Icons/enterFullscreenIcon");
            }
        }

        private IEnumerator SetCurrentSkybox(string assetBundlePath)
        {
            if (_currentSkybox != null)
                DestroyImmediate(_currentSkybox, true);

            if (_currentSkyboxAssetBundle != null)
                _currentSkyboxAssetBundle.UnloadAsync(true);

            _viewportContainer.style.display = DisplayStyle.None;

            var bundleLoadRequest = AssetBundle.LoadFromFileAsync(assetBundlePath);
            var progressBar = CreateProgressBar();
            var aLabel = progressBar.parent.Q<Label>();
            aLabel.text = $"Loading Skybox {Path.GetFileName(assetBundlePath)}";
            while (!bundleLoadRequest.isDone && bundleLoadRequest.progress < 0.95)
            {
                var progress = bundleLoadRequest.progress * 100f + 10f;
                progressBar.title = progress.ToString("n1");
                progressBar.value = progress;
                yield return new WaitForEndOfFrame();
            }
            DeleteProgressBar(progressBar);
            yield return bundleLoadRequest;

            var loadedAssetBundle = bundleLoadRequest.assetBundle;
            if (loadedAssetBundle == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                yield break;
            }
            _currentSkyboxAssetBundle = loadedAssetBundle;
            var assetLoadRequest = _currentSkyboxAssetBundle.LoadAllAssetsAsync();
            _currentSkybox = assetLoadRequest.allAssets[0] as Material;
            RenderSettings.skybox = _currentSkybox;
            SkyboxExposure = RenderSettings.skybox.GetFloat("_Exposure");
        }

        private IEnumerator SetCurrentModel(string assetBundlePath)
        {
            if (_currentModel != null)
                Destroy(_currentModel);

            if (_currentModelAssetBundle != null)
                _currentModelAssetBundle.UnloadAsync(true);

            _viewportContainer.style.display = DisplayStyle.None;

            var bundleLoadRequest = AssetBundle.LoadFromFileAsync(assetBundlePath);

            var progressBar = CreateProgressBar();
            var aLabel = progressBar.parent.Q<Label>();
            aLabel.text = $"Loading 3D Model {Path.GetFileName(assetBundlePath)}";
            while (!bundleLoadRequest.isDone && bundleLoadRequest.progress < 0.95)
            {
                var progress = bundleLoadRequest.progress * 100f + 10f;
                progressBar.title = progress.ToString("n1");
                progressBar.value = progress;
                yield return new WaitForEndOfFrame();
            }
            DeleteProgressBar(progressBar);
            yield return bundleLoadRequest;

            var loadedAssetBundle = bundleLoadRequest.assetBundle;
            if (loadedAssetBundle == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                yield break;
            }

            _currentModelAssetBundle = loadedAssetBundle;
            var assetLoadRequest = loadedAssetBundle.LoadAllAssetsAsync();
            yield return assetLoadRequest;

            var prefab = assetLoadRequest.allAssets[0] as GameObject;
            _currentModel = Instantiate(prefab);

            AutoScaleCurrentModel();
            CenterCurrentModel();

            ObjectXRotation = 0;
            ObjectYRotation = 0;
            ObjectZRotation = 0;
        }

        private void SetDefaultViewParameters()
        {
            CameraDistance = 2;
            CameraElevation = 0;
            CameraAzimuth = 0;
            _cameraTransformGuide.transform.rotation = Quaternion.Euler(new Vector3(CameraElevation, CameraAzimuth, 0));
            // _camera.transform.position = new Vector3(0, 0, -1 * CameraDistance);
            ObjectXRotation = 0;
            ObjectYRotation = 0;
            ObjectZRotation = 0;
            LightElevation = 0;
            LightAzimuth = 0;
            _sun.transform.rotation = Quaternion.Euler(new Vector3(LightElevation, LightAzimuth, 0));
            LightIntensity = 1;
            _sun.GetComponent<Light>().intensity = 1;
            SkyboxExposure = 0f;
        }


        private void ResetViewer(ClickEvent evt)
        {
            evt.StopPropagation();

            if (_currentModel != null)
            {
                Destroy(_currentModel);
                _currentModelAssetBundle.UnloadAsync(true);
            }

            if (_currentSkybox != null)
            {
                RenderSettings.skybox = null;
                DestroyImmediate(_currentSkybox, true);
                _currentSkyboxAssetBundle.UnloadAsync(true);
            }
            SetDefaultViewParameters();
            _viewportContainer.style.display = DisplayStyle.Flex;

        }


        private void CalculateCurrentModelBounds()
        {
            var meshRenderers = _currentModel.GetComponentsInChildren<MeshRenderer>();
            var bounds = meshRenderers[0].bounds;
            var remainingBounds = new ArraySegment<MeshRenderer>(meshRenderers, 1, meshRenderers.Length - 1);
            foreach (var meshRenderer in remainingBounds) bounds.Encapsulate(meshRenderer.bounds);

            _currentModelBounds = bounds;
        }

        private void CenterCurrentModel()
        {
            CalculateCurrentModelBounds();
            if (Mathf.Abs(_currentModel.transform.position.magnitude - _currentModelBounds.center.magnitude) <
                0.001f) return;
            var anEmpty = new GameObject
            {
                transform =
                {
                    position = _currentModelBounds.center
                },
                name = _currentModel.name + "_parent"
            };
            _currentModel.transform.parent = anEmpty.transform;
            _currentModel = anEmpty;
            _currentModel.transform.position = Vector3.zero;
        }

        private void AutoScaleCurrentModel()
        {
            CalculateCurrentModelBounds();
            var maxLength = Mathf.Max(_currentModelBounds.size.x, _currentModelBounds.size.y,
                _currentModelBounds.size.z);

            var scaleFactor = 1 / maxLength;
            _currentModel.gameObject.transform.localScale *= scaleFactor;
        }

        private void BrowseAndLoadModel(ClickEvent evt)
        {
            evt.StopPropagation();
            var paths = StandaloneFileBrowser.OpenFilePanel("Please select an asset bundle containing a 3D Model",
                "", "", false);

            if (paths.Length != 0)
            {
                StartCoroutine(SetCurrentModel(paths[0]));
            }
        }

        private void BrowseAndLoadSkybox(ClickEvent evt)
        {
            evt.StopPropagation();

            var paths = StandaloneFileBrowser.OpenFilePanel("Please select an asset bundle containing a skybox material",
                "", "", false);

            if (paths.Length != 0)
                StartCoroutine(SetCurrentSkybox(paths[0]));
        }

        private void BrowseAndLoadNNModel(ClickEvent evt)
        {
            evt.StopPropagation();

            var paths = StandaloneFileBrowser.OpenFilePanel("Please select a pytorch or tensorflow model",
                "", "", false);

            if (paths.Length != 0)
                Debug.Log(paths[0]);
        }

        private void UpdateViewport()
        {
            var newX = _controlArea.resolvedStyle.width / Screen.width;
            Camera.main.rect = new Rect(newX, 0, 1, 1);
        }


        private void HandleMouseDownEvent(MouseDownEvent evt)
        {
            evt.StopPropagation();

            if ((_currentModel != null | _currentSkybox != null)
                & evt.button == 0)
            {
                _viewport.RegisterCallback<MouseMoveEvent>(HandleCameraOrbit);
                return;
            }

            if (_currentSkybox != null && evt.button == 1)
                _viewport.RegisterCallback<MouseMoveEvent>(HandleSkyboxExposure);

            if (_currentModel != null && evt.button == 1)
                _viewport.RegisterCallback<MouseMoveEvent>(HandleSunOrbitAndIntensity);
        }

        private void HandleSkyboxExposure(MouseMoveEvent evt)
        {
            if (!evt.ctrlKey) return;
            SkyboxExposure += evt.mouseDelta.y > 0 ? 0.05f : -0.05f;
            RenderSettings.skybox.SetFloat("_Exposure", SkyboxExposure);
        }

        private void HandleSunOrbitAndIntensity(MouseMoveEvent evt)
        {
            evt.StopPropagation();

            if (Mathf.Abs(Mathf.Abs(evt.mouseDelta.y) - Mathf.Abs(evt.mouseDelta.x)) < 1f) return;
            if (evt.ctrlKey) return;

            if (evt.altKey)
            {
                var increment = evt.mouseDelta.y > 0 ? 0.05f : -0.05f;
                _sun.GetComponent<Light>().intensity += increment;
                LightIntensity += increment;
            }

            else if (Mathf.Abs(evt.mouseDelta.y) > Mathf.Abs(evt.mouseDelta.x))
            {
                var newElevation = LightElevation + evt.mouseDelta.y;
                if (newElevation > 90)
                    LightElevation = 90;
                else if (newElevation < -90)
                    LightElevation = -90;
                else
                    LightElevation = newElevation;

                _sun.transform.rotation = Quaternion.Euler(new Vector3(LightElevation, LightAzimuth, 0));
            }

            else if (Mathf.Abs(evt.mouseDelta.y) < Mathf.Abs(evt.mouseDelta.x))
            {
                LightAzimuth += evt.mouseDelta.x;
                if (LightAzimuth > 360)
                    LightAzimuth = 0;
                if (LightAzimuth < 0)
                    LightAzimuth = 360;
                LightAzimuth += evt.mouseDelta.x;
                _sun.transform.rotation = Quaternion.Euler(new Vector3(LightElevation, LightAzimuth, 0));
            }
        }


        private void HandleCameraOrbit(MouseMoveEvent evt)
        {
            evt.StopPropagation();

            if (Mathf.Abs(Mathf.Abs(evt.mouseDelta.y) - Mathf.Abs(evt.mouseDelta.x)) < 1f) return;

            if (Mathf.Abs(evt.mouseDelta.y) > Mathf.Abs(evt.mouseDelta.x))
            {
                var newElevation = CameraElevation + evt.mouseDelta.y;
                if (newElevation > 90)
                    CameraElevation = 90;
                else if (newElevation < -90)
                    CameraElevation = -90;
                else
                    CameraElevation = newElevation;

                _cameraTransformGuide.transform.rotation =
                    Quaternion.Euler(new Vector3(CameraElevation, CameraAzimuth, 0));
            }

            else if (Mathf.Abs(evt.mouseDelta.y) < Mathf.Abs(evt.mouseDelta.x))
            {
                CameraAzimuth += evt.mouseDelta.x;
                if (CameraAzimuth > 360)
                    CameraAzimuth = 0;
                if (CameraAzimuth < 0)
                    CameraAzimuth = 360;
                _cameraTransformGuide.transform.rotation =
                    Quaternion.Euler(new Vector3(CameraElevation, CameraAzimuth, 0));
            }
        }

        private void HandleMouseUpEvent(MouseUpEvent evt)
        {
            evt.StopPropagation();
            _viewport.UnregisterCallback<MouseMoveEvent>(HandleCameraOrbit);
            _viewport.UnregisterCallback<MouseMoveEvent>(HandleSunOrbitAndIntensity);
            _viewport.UnregisterCallback<MouseMoveEvent>(HandleSkyboxExposure);
        }

        private void HandleMouseOutEvent(MouseOutEvent evt)
        {
            evt.StopPropagation();
            _viewport.UnregisterCallback<MouseMoveEvent>(HandleCameraOrbit);
            _viewport.UnregisterCallback<MouseMoveEvent>(HandleSunOrbitAndIntensity);
            _viewport.UnregisterCallback<MouseMoveEvent>(HandleSkyboxExposure);
        }

        private void HandleMouseWheelEvent(WheelEvent evt)
        {
            evt.StopPropagation();

            if (_currentModel == null) return;

            if (evt.ctrlKey)
            {
                var increment = evt.delta.y > 0 ? 1f : -1f;
                ObjectXRotation += increment;
                _currentModel.transform.rotation =
                    Quaternion.Euler(new Vector3(ObjectXRotation, ObjectYRotation, ObjectZRotation));
            }

            else if (evt.shiftKey)
            {
                var increment = evt.delta.y > 0 ? 1f : -1f;
                ObjectYRotation += increment;
                _currentModel.transform.rotation =
                    Quaternion.Euler(new Vector3(ObjectXRotation, ObjectYRotation, ObjectZRotation));
            }

            else if (evt.altKey)
            {
                var increment = evt.delta.y > 0 ? 1f : -1f;
                ObjectZRotation += increment;
                _currentModel.transform.rotation =
                    Quaternion.Euler(new Vector3(ObjectXRotation, ObjectYRotation, ObjectZRotation));
            }

            else
            {
                var increment = evt.delta.y > 0 ? 0.05f : -0.05f;
                var position = _camera.transform.position;
                position += position.normalized * increment;
                _camera.transform.position = position;
                CameraDistance += position.magnitude * increment;
            }
        }

        private ProgressBar CreateProgressBar()
        {
            var progressContainer = new VisualElement();
            var progressLabel = new Label();
            var radialProgress = new ProgressBar();
            progressContainer.Add(progressLabel);
            progressContainer.Add(radialProgress);
            progressContainer.name = "progressContainer";
            _viewport.Add(progressContainer);
            return radialProgress;
        }

        private void DeleteProgressBar(ProgressBar aRadialProgress)
        {
            _viewport.Remove(aRadialProgress.parent);
        }

        private void LateUpdate()
        {
            if (_isCapturingImage)
            {
                CaptureAndSaveImage();
            }
        }
    }
}