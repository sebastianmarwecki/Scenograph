using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.SpaceMapper
{
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class ConditionCam : MonoBehaviour
    {
        public bool GlobalElseLocal;
        public Camera FollowCam;
        public Shader MaterialShader;
        public Texture Mask;
        public Renderer PictureObject;
        public Material EditorSkybox;
        public SpaceMapperRoot SpaceMapper;
        public string IfNoBuildLayer;
        public List<string> IfBuildLayers;

        private RenderTexture _rtCam;
        private RenderTexture _rtTo;
        private Camera _cam;
        private Material _mat;
        private GameObject _buildObject;
        public int _cullingMask, _notVisible;

        private void Update()
        {
            if (FollowCam != null)
            {
                if (!SpaceMapper.BuildAvailable)
                {
                    transform.position = FollowCam.transform.position;
                    transform.rotation = FollowCam.transform.rotation;
                }
                else
                {
                    var offsetPosition = _buildObject != null ? _buildObject.transform.position : Vector3.zero;
                    var offsetRotation = _buildObject != null ? _buildObject.transform.rotation : Quaternion.identity;
                    transform.position = offsetPosition + FollowCam.transform.position;
                    transform.rotation = offsetRotation * FollowCam.transform.rotation;
                }
                if (_cam != null)
                {
                    _cam.fieldOfView = FollowCam.fieldOfView;
                    _cam.cullingMask = SpaceMapper.BuildAvailable ? _cullingMask : _notVisible;
                }
            }
        }
        
        public void Init(Vector2Int resolution)
        {
            _cam = gameObject.GetComponent<Camera>();
            _cullingMask = 0;
            foreach (var bl in IfBuildLayers)
                _cullingMask ^= 1 << LayerMask.NameToLayer(bl);
            _notVisible = 0;
            _notVisible |= 1 << LayerMask.NameToLayer(IfNoBuildLayer);
            _rtCam = new RenderTexture(resolution.x, resolution.y, 16, RenderTextureFormat.ARGB32);
            _rtTo = new RenderTexture(resolution.x, resolution.y, 16, RenderTextureFormat.ARGB32);
            _rtCam.Create();
            _rtTo.Create();
            _cam.targetTexture = _rtCam;
            _cam.cullingMask = FollowCam.cullingMask;
            _mat = new Material(MaterialShader);
            //test.material = mat;
            _mat.SetTexture("_MainTex", _rtCam);
            _mat.SetTexture("_Mask", Mask);
            PictureObject.material = _mat;
            var linker = GetComponentInParent<AbstractLinker>();
            _buildObject = linker.GetBuildObject();
        }
        
    }

    //[CustomEditor(typeof(SpaceMapperRoot))]
    //public class ConditionCamEditor : Editor
    //{
    //}
}
