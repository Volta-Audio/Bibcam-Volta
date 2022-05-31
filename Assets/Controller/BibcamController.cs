using UnityEngine;
using UnityEngine.UI;
using Bibcam.Decoder;
using Bibcam.Encoder;
using Avfi;
using UnityEngine.XR.ARFoundation;
using Klak.Ndi;

sealed class BibcamController : MonoBehaviour
{
    #region Scene object references

    [Space]
    [SerializeField] BibcamEncoder _encoder = null;
    [SerializeField] Camera _camera = null;
    [SerializeField] ARCameraManager _cameraManager = null;
    [SerializeField] AROcclusionManager _occlusionManager = null;
    [Space]
    [SerializeField] BibcamMetadataDecoder _decoder = null;
    [SerializeField] BibcamTextureDemuxer _demuxer = null;
    [Space]
    [SerializeField] Slider _depthSlider = null;
    [SerializeField] Text _depthLabel = null;
    [SerializeField] Text _recordLabel = null;
    [SerializeField] GameObject _recordSign = null;
    [SerializeField] NdiSender _ndiSender;

    const int _width = 2048;
    const int _height = 1024;
    Matrix4x4 _projection;

    #endregion

    #region Editable parameters

    [Space]
    [SerializeField] float _minDepth = 0.2f;
    [SerializeField] float _maxDepth = 3.2f;

    #endregion

    #region Private members

    VideoRecorder Recorder => GetComponent<VideoRecorder>();

    RcamMetadata MakeMetadata()
      => new RcamMetadata
      {
          CameraPosition = _camera.transform.position,
          CameraRotation = _camera.transform.rotation,
          ProjectionMatrix = _projection,
          DepthRange = new Vector2(_minDepth, _maxDepth)
      };

    #endregion


    #region Camera callbacks

    void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        // We expect there is at least one texture.
        if (args.textures.Count == 0) return;

        // Try receiving the projection matrix.
        if (args.projectionMatrix.HasValue)
        {
            _projection = args.projectionMatrix.Value;

            // Aspect ratio compensation (camera vs. 16:9)
            _projection[1, 1] *= (16.0f / 9) / _camera.aspect;
        }
    }

    #endregion
    #region Public members (exposed for UI)

    public void OnRecordButton()
    {
        if (Recorder.IsRecording)
        {
            // Stop recording
            Recorder.EndRecording();
            _recordLabel.text = "Record";
            _recordLabel.color = Color.white;
            _recordSign.SetActive(false);
        }
        else
        {
            // Reset the camera position.
            _camera.transform.parent.position
              = -_camera.transform.localPosition;

            // Start recording
            Recorder.StartRecording();
            _recordLabel.text = "Stop";
            _recordLabel.color = Color.red;
            _recordSign.SetActive(true);
        }
    }

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        // We have a good phone. Crank it up to 60 fps.
        Application.targetFrameRate = 60;

        // Recorder setup
        Recorder.source = (RenderTexture)_encoder.EncodedTexture;

        // UI setup
        _depthSlider.value = PlayerPrefs.GetFloat("DepthSlider", 5);

        // NDI sender instantiation
        _ndiSender.sourceTexture = (RenderTexture)_encoder.EncodedTexture;
    }

    void Update()
    {
        // TOBY UPDATE: temporarily turning off any dynamic updating of depth. All depth ranges are set 0.2-5 in the Unity inspector. This matches the Volta Create sliders.
        // Next steps: check the re-mapping of range in Volta Create, as the depth map image is bounded by the depth range metadata?

        // Depth range settings update
        // var maxDepth = _depthSlider.value;
        // var minDepth = maxDepth / 50;
        // (_encoder.minDepth, _encoder.maxDepth) = (minDepth, maxDepth);

        // Monitor update
        _decoder.Decode(_encoder.EncodedTexture);
        _demuxer.Demux(_encoder.EncodedTexture, _decoder.Metadata);

        // UI update
        // _depthLabel.text = $"Depth Range: {minDepth:0.0}m - {maxDepth:0.0}m";
        // PlayerPrefs.SetFloat("DepthSlider", maxDepth);
    }

    void OnRenderObject()
      => _ndiSender.metadata = MakeMetadata().Serialize();


    void OnEnable()
    {
        // Camera callback setup
        _cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        // Camera callback termination
        _cameraManager.frameReceived -= OnCameraFrameReceived;
    }
    #endregion
}
