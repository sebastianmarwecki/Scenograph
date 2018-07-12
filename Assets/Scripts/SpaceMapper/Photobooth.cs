using System.Collections.Generic;
using Assets.Scripts.Petrinet;
using Assets.Scripts.SpaceMapper;
using UnityEngine;

namespace SpaceMapper
{
    [ExecuteInEditMode]
    public class Photobooth : MonoBehaviour
    {
        public Camera Cam;
        public Material PhotoMaterial;
        public float Height;
        private RenderTexture _renderTex;
        private Dictionary<PetrinetTransition, Texture2D> _picturesTaken;

        private void Awake()
        {
            _picturesTaken = new Dictionary<PetrinetTransition, Texture2D>();
        }

        public void SetRenderTexture(Vector2Int resolution)
        {
            _renderTex = new RenderTexture(resolution.x, resolution.y, 16, RenderTextureFormat.ARGB32);
            Cam.targetTexture = _renderTex;
        }

        public void TryTakePicture(Vector2 size, PetrinetTransition transition, bool forceRadius, bool takeNewPhoto)
        {
            if (_picturesTaken == null)
                _picturesTaken = new Dictionary<PetrinetTransition, Texture2D>();
            if (!_picturesTaken.ContainsKey(transition) || takeNewPhoto)
            {
                var go = transition.PreviewObject;
                
                var oldPos = go.transform.localPosition;
               // var oldRot = go.transform.localRotation;
                var oldActive = go.activeSelf;

                go.transform.position = transform.position;
               // go.transform.rotation = transition.GetPreviewRotation();
                go.SetActive(true);

                Cam.transform.position = transition.PhotoFrom.position;////localPosition = -transition.GetPreviewPosition();// new Vector3(-size.x, Height, -size.y);// * (1f + transition.PreviewExtraDistance);
                Cam.transform.LookAt(transition.PhotoTo);//transform.position);//transform.position + Vector3.up * 0.5f);
                //Cam.transform.localPosition = Cam.transform.localPosition - Cam.transform.forward * 1.0f;
                Cam.Render();

               go.transform.localPosition = oldPos;
              //  go.transform.localRotation = oldRot;
                go.SetActive(oldActive);

                var was = RenderTexture.active;
                RenderTexture.active = _renderTex;
                // width and height, chosen to match the source r.t.
                var tex = new Texture2D(_renderTex.width, _renderTex.height, TextureFormat.ARGB32, false);
                // Grab everything
                tex.ReadPixels(new Rect(0f, 0f, _renderTex.width, _renderTex.height), 0, 0);
                tex.Apply();
                RenderTexture.active = was;

                //PhotoMaterial

                var rend = transition.Preview.GetComponentInChildren<Renderer>();
                //rend.texture = null;
                var mat = new Material(PhotoMaterial);
                mat.SetTexture("_MainTex", tex);
                //mat.SetTexture("_Mask", tex);
                var scale = new Vector2(1f, 1f);//new Rect(0f, 0f, 1f, 1f));
                var offset = new Vector2(0f, 0f);
                if (!forceRadius)
                {
                    var h = transition.GetComponent<HardwareRequirements>();
                    offset = h.Requirement.x < h.Requirement.y
                        ? new Vector2((1f - h.Requirement.x / h.Requirement.y) / 2f, 0f)
                        : new Vector2(0f, (1f - h.Requirement.y / h.Requirement.x) / 2f);
                    scale = h.Requirement.x < h.Requirement.y
                        ? new Vector2(h.Requirement.x / h.Requirement.y, 1f)
                        : new Vector2(1f, h.Requirement.y / h.Requirement.x);
                }
                mat.SetTextureOffset("_MainTex", offset);
                mat.SetTextureScale("_MainTex", scale);
                rend.material = mat;

                if (_picturesTaken.ContainsKey(transition))
                    _picturesTaken.Remove(transition);
                _picturesTaken.Add(transition, tex);
            }
        }
    }
}
