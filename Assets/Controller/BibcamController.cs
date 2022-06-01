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
    [SerializeField] Text _recordLabel = null;
    [SerializeField] Toggle _recordToggle = null;
    [SerializeField] NdiSender _ndiSender;

    #endregion

    #region Editable parameters

    [Space]

    [SerializeField] int _targetFPS = 30; // For archival recording, 60 is within capability of a LiDAR iPhone. But generally for streaming to app etc. 30 probably a good compromise.
    [SerializeField] float _minDepth = 0.01f;
    [SerializeField] float _maxDepth = 20.0f;

    #endregion

    #region Private members

    Matrix4x4 _projection;

    VideoRecorder _recorder => GetComponent<VideoRecorder>();

    RcamMetadata MakeMetadata()
      => new RcamMetadata
      {
          CameraPosition = _camera.transform.position,
          CameraRotation = _camera.transform.rotation,
          ProjectionMatrix = _projection,
          DepthRange = new Vector2(_minDepth, _maxDepth)
      };

    const string _recordOffMessage = "Record to device";
    const string _recordOnMessage = "Stop";

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

    public void OnRecordButton(bool isOn)
    {
        if (isOn)
        {
            if (_recorder.IsRecording)
            {
                Debug.LogWarning("Record button toggled off but not already recording");
                return;
            }

            // Reset the camera position.
            _camera.transform.parent.position
              = -_camera.transform.localPosition;

            // Start recording
            _recorder.StartRecording();
            _recordLabel.text = _recordOnMessage;
            _recordLabel.color = Color.red;
        }
        else
        {
            if (!_recorder.IsRecording)
            {
                Debug.LogWarning("Record button toggled on but already recording");
                return;
            }

            // Stop recording
            _recorder.EndRecording();
            _recordLabel.text = _recordOffMessage;
            _recordLabel.color = Color.white;
        }
    }

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        Application.targetFrameRate = _targetFPS;

        // Encoder setup
        (_encoder.minDepth, _encoder.maxDepth) = (_minDepth, _maxDepth); // This is no longer dynamic, need to instead set in Update if so.

        _recordToggle.onValueChanged.AddListener((isOn) => OnRecordButton(isOn));
        _recordLabel.text = _recordOffMessage;
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
