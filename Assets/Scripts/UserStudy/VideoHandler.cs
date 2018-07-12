using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.SpaceMapper;
using Assets.Scripts.VirtualSpace.Unity_Specific;
using RenderHeads.Media.AVProMovieCapture;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.UserStudy
{
    public class VideoHandler : MonoBehaviour
    {
        public KeyCode StartStop;
        public RenderTexture RenderTex;
        public SpaceMapperRoot SpaceMapper;
        public IkTargetAdapt Goldilocks;

        private bool _recording;
        private List<CaptureBase> _captures;

        private List<Camera> _cams;
        //private RenderTexture _renderTex;

        internal bool KeyPressed;

        void Start()
        {
            //_renderTex = new RenderTexture(Resolution.x, Resolution.y, 16, RenderTextureFormat.ARGB32);
            //CapTex.SetSourceTexture(_renderTex);
            //CapTex.PrepareCapture();
            _cams = GetComponentsInChildren<Camera>(true).ToList();
            _captures = GetComponentsInChildren<CaptureBase>(true).ToList();
        }

        private void OnEnable()
        {
            SpaceMapper.KeyPressedInEditor.AddListener(KeyPressedHandler);
            SpaceMapper.RenderedCam.AddListener(MakeTex);
        }

        private void OnDisable()
        {
            SpaceMapper.KeyPressedInEditor.RemoveListener(KeyPressedHandler);
            SpaceMapper.RenderedCam.RemoveListener(MakeTex);
        }

        private void KeyPressedHandler(KeyCode arg0)
        {
            if (arg0 == StartStop)
            {
                KeyPressed = false;
                _recording = !_recording;

                if (_recording)
                {
                    Goldilocks.UpdateHeight();
                    foreach (var cam in _cams)
                        cam.gameObject.SetActive(true);
                    foreach (var cap in _captures)
                        cap.StartCapture();
                }
                else
                {
                    foreach (var cam in _cams)
                        cam.gameObject.SetActive(false);
                    foreach (var cap in _captures)
                        cap.StopCapture();
                }
            }
        }

        //void Update()
        //{
        //    if (KeyPressed)
        //    {
        //        KeyPressed = false;
        //        _recording = !_recording;

        //        if (_recording)
        //        {
        //            Goldilocks.UpdateHeight();
        //            foreach (var cam in _cams)
        //                cam.gameObject.SetActive(true);
        //            foreach (var cap in _captures)
        //                cap.StartCapture();
        //        }
        //        else
        //        {
        //            foreach (var cam in _cams)
        //                cam.gameObject.SetActive(false);
        //            foreach (var cap in _captures)
        //                cap.StopCapture();
        //        }
        //    }
        //}

        private bool _camSet;

        internal void TakeThisCam(Camera cam)
        {
            if (_camSet)
                return;
            var caps = GetComponentsInChildren<CaptureFromCamera>(true).ToList();
            foreach (var cap in caps)
                cap.SetCamera(cam);
            _camSet = true;

        }

        public void MakeTex()
        {
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                SceneView.lastActiveSceneView.camera.targetTexture = RenderTex;
                SceneView.lastActiveSceneView.camera.Render();
                SceneView.lastActiveSceneView.camera.targetTexture = null;
            }

            //var was = RenderTexture.active;
            //RenderTexture.active = RendTex;
            //var tex = new Texture2D(RendTex.width, RendTex.height, TextureFormat.ARGB32, false);
            //tex.ReadPixels(new Rect(0f, 0f, RendTex.width, RendTex.height), 0, 0);
            //tex.Apply();
            //RenderTexture.active = was;
        }
    }

    [CustomEditor(typeof(SpaceMapperRoot))]
    public class SpaceMapperRootEditor : Editor
    {
        public void OnSceneGUI()
        {
            var handler = (VideoHandler) target;
            var cam = SceneView.currentDrawingSceneView.camera;
            if (cam != null)
                handler.TakeThisCam(cam);
        }
    }
}