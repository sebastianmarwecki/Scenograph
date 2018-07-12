using UnityEngine;

namespace Assets.Scripts.SpaceMapper
{
    [ExecuteInEditMode]
    public class LookTowardsCenter : MonoBehaviour
    {
        public SpaceMapperRoot SpaceMapper;
        public TrackingSpaceRoot SpaceManagementUnit;
        public Camera Cam;
        public View CamView;
       // public KeyCode ApplyForwardKeyCode;
        public enum View
        {
            LookToCenterFollowPosition,
            FollowPositionAndRotation
        }

        public bool CloseView;
        private Vector3 _lookAt;

        void Start ()
        {
            SpaceUpdated();
        }
	
        void Update ()
        {
            var forward = SpaceMapper.ForwardCam;
            var rotation = SpaceMapper.RotationCam;
            var position = SpaceMapper.PositionCam;
            var fov = SpaceMapper.FovCam;
            if (CloseView)
            {
                forward = SpaceMapper.ForwardCamRecording;
                rotation = SpaceMapper.RotationCamRecording;
                position = SpaceMapper.PositionCamRecording;
                fov = SpaceMapper.FovCamRecording;
            }

            transform.position = position;
            Cam.fieldOfView = fov;
            switch (CamView)
            {
                case View.FollowPositionAndRotation:
                    transform.rotation = Quaternion.Euler(rotation);
                    break;
                case View.LookToCenterFollowPosition:
                    transform.LookAt(_lookAt);
                    break;
            }
            transform.position = transform.position + forward * transform.forward;
        }

        private void OnEnable()
        {
            SpaceManagementUnit.RequestRebuild.AddListener(SpaceUpdated);
           // SpaceMapper.KeyPressedInEditor.AddListener(KeyPressed);
        }

        private void OnDisable()
        {
            SpaceManagementUnit.RequestRebuild.RemoveListener(SpaceUpdated);
           // SpaceMapper.KeyPressedInEditor.RemoveListener(KeyPressed);
        }

        //private void KeyPressed(KeyCode arg0)
        //{
        //    if (arg0 == ApplyForwardKeyCode)
        //        _keyPressed = !_keyPressed;
        //}

        private void SpaceUpdated()
        {
            var tilesAvailable = SpaceManagementUnit.TileAvailable;
            if (tilesAvailable == null)
                return;
            //if (tilesAvailable == null)
            //    return;
            var pos = Vector2.zero;
            for (var i = 0; i < tilesAvailable.GetLength(0); i++)
            for (var j = 0; j < tilesAvailable.GetLength(1); j++)
                    if(tilesAvailable[i,j])
                pos += SpaceManagementUnit.GetSpaceFromPosition(i, j);
            pos /= SpaceManagementUnit.GetTilesAvailable();

            _lookAt = new Vector3(pos.x, 0f, pos.y);
        }
    }
}
