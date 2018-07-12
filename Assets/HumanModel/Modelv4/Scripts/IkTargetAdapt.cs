using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.SpaceMapper;
using UnityEngine;

namespace Assets.Scripts.VirtualSpace.Unity_Specific
{
    public class IkTargetAdapt : MonoBehaviour
    {
        public float HeightMin, HeightMax, LocalSizeMin, LocalSizeMax;
        public Transform Headmount;
        public Transform Ground;
        public float AllowedCameraUpOffsetForMeasurement = 30;
        public float HistoryBuffer = 10;
        public SpaceMapperRoot SpaceMapper;
        private float _startLocalScale, _goalLocalScale;

        public float MeasureHeightEvery = .1f;
        public int NumMeasurementsToRemember = 10;
        private float _nextMeasurementIn;
        private List<float> _measurements;

        public float UpdateHeightIn = -1;
        private float _heightUpdatingFor;

        void Start () {
            _startLocalScale = LocalSizeMin;
            _goalLocalScale = LocalSizeMin;

            _nextMeasurementIn = 0;
            _measurements = new List<float>();
        }
        
        void Update ()
        {
            _nextMeasurementIn -= Time.unscaledDeltaTime;
            if (_nextMeasurementIn < 0 && Measure())
            {
                _nextMeasurementIn = MeasureHeightEvery;
            }

            UpdateHeight();
        }

        private void OnEnable()
        {
            SpaceMapper.RestartExperience.AddListener(SpaceMapperListener);
        }

        private void OnDisable()
        {
            SpaceMapper.RestartExperience.RemoveListener(SpaceMapperListener);
        }

        private void SpaceMapperListener(SpaceMapperRoot.Command arg0)
        {
            UpdateHeight();
        }

        private bool Measure()
        {
            var angle = Vector3.Angle(Vector3.up, Headmount.up);
            //Debug.Log("angle is " + angle);

            if (angle > AllowedCameraUpOffsetForMeasurement)
                return false;

            var delta = Headmount.transform.position.y - Ground.transform.position.y;

            while (_measurements.Count >= NumMeasurementsToRemember)
            {
                _measurements.RemoveAt(0);
            }

            _measurements.Add(delta);

            //Debug.Log("Measured " + delta);

            return true;
        }

        [EasyButtons.Button("Update Height", EasyButtons.ButtonMode.EnabledInPlayMode)]
        public void UpdateHeightValue()
        {
            var median = Median(_measurements);

            Debug.Log("median : " + median);

            median = Mathf.Clamp(median, HeightMin, HeightMax);

            var relativeHeight = (median - HeightMin) / (HeightMax - HeightMin);

            _startLocalScale = _goalLocalScale;
            _goalLocalScale = LocalSizeMin + (LocalSizeMax - LocalSizeMin) * relativeHeight;

            _heightUpdatingFor = 0;
        }

        private float Median(IEnumerable<float> xs)
        {
            var ys = xs.OrderBy(x => x).ToList();
            var mid = (ys.Count - 1) / 2.0f;
            return (ys[(int)(mid)] + ys[(int)(mid + 0.5f)]) / 2f;
        }

        public void UpdateHeight()
        {
            if (UpdateHeightIn < 0 && _heightUpdatingFor > UpdateHeightIn)
            {
                transform.localScale = Vector3.one * _goalLocalScale;

                _heightUpdatingFor = 2 * UpdateHeightIn;
            }

            if (UpdateHeightIn > 0 && _heightUpdatingFor < UpdateHeightIn)
            {
                transform.localScale = Vector3.Lerp(Vector3.one * _goalLocalScale, Vector3.one * _startLocalScale, _heightUpdatingFor / UpdateHeightIn);

                _heightUpdatingFor += Time.unscaledDeltaTime;
            }
        }
    }
}
