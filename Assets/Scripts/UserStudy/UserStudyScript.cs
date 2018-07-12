using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Petrinet;
using Assets.Scripts.SpaceMapper;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.UserStudy
{
    public class UserStudyScript : MonoBehaviour
    {
        public enum StudyCondition
        {
            FullSpace,
            SpaceMapperL,
            SpaceMapperQs,
            ControlLSevenLeague,
            ControlQsSevenLeague,
            ControlLTeleport,
            ControlQsTeleport
        }

        [Header("References")]
        public SpaceMapperRoot SpaceMapper;
        public TrackingSpaceRoot SpaceManagementUnit;
        public Transform TrackingReferenceHead, TrackingSpace;
        public GameObject TeleportIndicator;
        public LineRenderer TeleportLineRenderer;
        public SteamVR_TrackedController Controller;
        public Chaperones Chaperones;
        public KeyCode UserStudyStartStop;
        public Logging Logger;
        [Header("Settings - Control Seven League")]
        public float MotionMappingForControlLSevenLeague = 1f;
        public float MotionMappingForControlQsSevenLeague = 1f;
        public Vector3 OffsetForControlLSevenLeague;
        public Vector3 OffsetForControlQsSevenLeague;
        [Header("Settings - Control Teleport")]
        public List<Vector3> ChaperonesForControlLTeleport;
        public List<Vector3> ChaperonesForControlQsTeleport;
        public bool ReverseOrder;
        public float HeightTeleport;
        [Header("Settings - Assignment Matrix")]
        public List<Vector2Int> AvailableTilesL;
        public List<Vector2Int> AvailableTilesQs;
        public List<Vector2Int> AvailableTilesFull;

        [Header("Settings - Study Meta Information")]
        public StudyCondition NextStudyCondition;
        public string NextParticipantId;
        public char Separator = ';';
        public float TravelOffsetForCount = 0.4f;

        private Vector3 _addedOffsetCondition;
        private Vector3 _oldPos;
        private float _nextDistanceTravelled;
        private List<PetrinetTransition> _nextInteraction;
        private List<bool> _nextInteractionsValid;
        private Vector3 _initialTrackingSpaceOffset;
        private float _motionMapping;
        private bool _teleportEnabled, _motionMappingEnabled;
        private bool _tryStart;

        internal bool Recording;
        internal string ConditionToCompare;
        internal string Baseline;
        
        void Awake ()
        {
            _nextInteraction = new List<PetrinetTransition>();
            _nextInteractionsValid = new List<bool>();
            _initialTrackingSpaceOffset = TrackingSpace.localPosition;
            Debug.Log("awake");
        }

        private void OnEnable()
        {
            Controller.PadClicked += PadClicked;
            PetrinetTransition.TriedToFire.AddListener(RegisterInteraction);
            SpaceMapper.RestartExperience.AddListener(RestartExperience);
            SpaceMapper.RebuildExperience.AddListener(RebuildExperience);
        }

        private void OnDisable()
        {
            Controller.PadClicked -= PadClicked;
            PetrinetTransition.TriedToFire.RemoveListener(RegisterInteraction);
            SpaceMapper.RestartExperience.RemoveListener(RestartExperience);
            SpaceMapper.RebuildExperience.AddListener(RebuildExperience);

        }

        private void OnApplicationQuit()
        {
            if (Recording)
                EndUserStudy();
        }

        private void PadClicked(object sender, ClickedEventArgs e)
        {
            if (!_teleportEnabled)
                return;
            var teleport = TeleportIndicator.transform.position - TrackingReferenceHead.position;
            teleport.y = 0f;
            TrackingSpace.position = TrackingSpace.position + teleport;
        }

        private void IndicateTeleportPosition()
        {
            if (!_teleportEnabled)
                return;
            var dir = Controller.transform.forward;
            var orig = Controller.transform.position;
            if (orig.y <= HeightTeleport)
                return;
            var x =  (HeightTeleport - orig.y)/ dir.y;
            var endPos = orig + x * dir;
            TeleportIndicator.transform.position = endPos;
            TeleportLineRenderer.SetPositions(new[]{ orig, endPos });
        }

        private void RebuildExperience(bool success)
        {
            if (_tryStart && success)
            {
                Recording = true;
                _tryStart = false;
                Logger.StartRecording();
                TeleportLineRenderer.gameObject.SetActive(_teleportEnabled);
                TeleportIndicator.SetActive(_teleportEnabled);
                Chaperones.gameObject.SetActive(_teleportEnabled);
            }
            else if(Recording)
            {
                EndUserStudy();
            }
        }

        private void RestartExperience(SpaceMapperRoot.Command command)
        {
            //switch (command)
            //{
            //    case SpaceMapperRoot.Command.SaveRecordStartAgain:
            //        Print();
            //        Reset(true);
            //        break;
            //    case SpaceMapperRoot.Command.ContinueRecordingIfRecording:
            //        break;
            //    case SpaceMapperRoot.Command.SaveRecordContinueRecordingIfRecording:
            //        Print();
            //        break;
            //    case SpaceMapperRoot.Command.StopRecording:
            //        Reset(false);
            //        break;
            //    case SpaceMapperRoot.Command.StopRecordingStartAgain:
            //        Reset(true);
            //        break;
            //}
        }
        
        void Update () {
            if (Recording)
            {
                var refLocal = TrackingSpace.InverseTransformPoint(TrackingReferenceHead.position);

                if (_motionMappingEnabled)
                {
                    var trackingSpaceOffset = refLocal * (_motionMapping - 1f);
                    trackingSpaceOffset.y = 0f;
                    TrackingSpace.localPosition = _addedOffsetCondition + _initialTrackingSpaceOffset + trackingSpaceOffset;
                }

                var pos = refLocal;
                pos.y = 0f;
                var dist = Vector3.Distance(pos, _oldPos);
                if (dist > TravelOffsetForCount)
                {
                    _nextDistanceTravelled += dist;
                    _oldPos = pos;
                }

                IndicateTeleportPosition();
            }
        }


        internal void TryStartUserStudy()
        {
            if (_tryStart)
                return;
            TrackingSpace.localPosition = _initialTrackingSpaceOffset;
            _tryStart = true;

            //set study condition params
            List<Vector2Int> tiles = null;
            _teleportEnabled = false;
            _motionMapping = 1f;
            _addedOffsetCondition = Vector3.zero;
            _motionMappingEnabled = false;
            switch (NextStudyCondition)
            {
                case StudyCondition.ControlLSevenLeague:
                    _teleportEnabled = false;
                    _motionMapping = MotionMappingForControlLSevenLeague;
                    _addedOffsetCondition = OffsetForControlLSevenLeague;
                    _motionMappingEnabled = true;
                    tiles = AvailableTilesFull;
                    break;
                case StudyCondition.ControlQsSevenLeague:
                    _teleportEnabled = false;
                    _motionMapping = MotionMappingForControlQsSevenLeague;
                    _addedOffsetCondition = OffsetForControlQsSevenLeague;
                    _motionMappingEnabled = true;
                    tiles = AvailableTilesFull;
                    break;
                case StudyCondition.ControlLTeleport:
                    _teleportEnabled = true;
                    _motionMapping = 1f;
                    Chaperones.SetNewPositionsWithOffset(ChaperonesForControlLTeleport, ReverseOrder);
                    _addedOffsetCondition = Vector3.zero;
                    _motionMappingEnabled = false;
                    tiles = AvailableTilesFull;
                    break;
                case StudyCondition.ControlQsTeleport:
                    _teleportEnabled = true;
                    _motionMapping = 1f;
                    Chaperones.SetNewPositionsWithOffset(ChaperonesForControlQsTeleport, ReverseOrder);
                    _addedOffsetCondition = Vector3.zero;
                    _motionMappingEnabled = false;
                    tiles = AvailableTilesFull;
                    break;
                case StudyCondition.FullSpace:
                    tiles = AvailableTilesFull;
                    break;
                case StudyCondition.SpaceMapperL:
                    tiles = AvailableTilesL;
                    break;
                case StudyCondition.SpaceMapperQs:
                    tiles = AvailableTilesQs;
                    break;
            }


            _nextDistanceTravelled = 0f;
            _oldPos = transform.position;
            _oldPos.y = 0f;
            _nextInteraction.Clear();
            _nextInteractionsValid.Clear();

            SpaceManagementUnit.SetAvailableTiles(tiles, true);
            SpaceMapper.RestartPetrinet(true, SpaceMapperRoot.Command.SaveRecordStartAgain);
        }

        internal void EndUserStudy()
        {
            Logger.StopRecording();
            Recording = false;
            TrackingSpace.localPosition = _initialTrackingSpaceOffset;
                TeleportLineRenderer.gameObject.SetActive(false);
                TeleportIndicator.SetActive(false);
                TrackingSpace.localPosition = _initialTrackingSpaceOffset;

            var interactionString = "";
            for (var i = 0; i < _nextInteraction.Count; i++)
            {
                interactionString += (i == 0 ? "" : "%") + _nextInteraction[i].Name + "$" + (_nextInteractionsValid[i] ? "T" : "F");
            }

            var assignedMatrixString = "";
            assignedMatrixString = SpaceManagementUnit.TileAvailable.Cast<bool>().Aggregate(assignedMatrixString, (current, x) => current + (x ? "1" : "0"));

            var print = "";
            print += NextParticipantId + "" + Separator;
            print += NextStudyCondition + "" + Separator;
            print += assignedMatrixString + "" + Separator;
            print += _motionMapping + "" + Separator;
            print += (_teleportEnabled ? "1" : "0") + "" + Separator;
            print += _nextDistanceTravelled + "" + Separator;
            print += interactionString + "" + Separator;

            var now = DateTime.UtcNow.ToLocalTime();
            var time = now.Hour + "-" + now.Minute + "-" + now.Second + "_" + now.Day + "-" + now.Month + "-" + now.Year;
            File.WriteAllText(Application.persistentDataPath + "/" + "logData_" + time + ".txt", print);

            Debug.Log(Application.persistentDataPath + "/" + "logData_" + time + ".txt");
            Debug.Log(print);

            _nextDistanceTravelled = 0f;
            _oldPos = transform.position;
            _oldPos.y = 0f;
            _nextInteraction.Clear();
            _nextInteractionsValid.Clear();

            SpaceMapper.RestartPetrinet(false, SpaceMapperRoot.Command.StopRecording);
            SpaceMapper.Revoke();
        }

        public void RegisterInteraction(PetrinetTransition obj, bool valid)
        {
            _nextInteraction.Add(obj);
            _nextInteractionsValid.Add(valid);
        }

        public void Compare(string condition, string baseline)
        {
            //get all interactions
            var conditionInteractions = condition.Split('%').ToList();
            var baselineInteractions = baseline.Split('%').ToList();

            //remove switch object interactions
            conditionInteractions = conditionInteractions.Where(i => i.Contains("_")).ToList();
            baselineInteractions = baselineInteractions.Where(i => i.Contains("_")).ToList();

            //remove valid/invalid tags (for now)
            conditionInteractions = conditionInteractions.Select(ci => ci.Split('$')[0]).ToList();
            baselineInteractions = baselineInteractions.Select(ci => ci.Split('$')[0]).ToList();

            //remove duplicates (not distinct, but duplicat immediately after)
            var conditionInteractionsCleaned = new List<string>();
            var baselineInteractionsCleaned = new List<string>();
            for (var i = 1; i < conditionInteractions.Count; i++)
                if(conditionInteractions[i] != conditionInteractions[i-1])
                    conditionInteractionsCleaned.Add(conditionInteractions[i]);
            for (var i = 1; i < baselineInteractions.Count; i++)
                if (baselineInteractions[i] != baselineInteractions[i - 1])
                    baselineInteractionsCleaned.Add(baselineInteractions[i]);
            conditionInteractions = conditionInteractionsCleaned;
            baselineInteractions = baselineInteractionsCleaned;

            //get measure 1 - ratio of baseline interactions covered in condition to baseline interactions
            var distinctBase = baselineInteractions.Distinct().ToList();
            var distinctCondition = conditionInteractions.Distinct().ToList();
            var m1 = (float) distinctBase.Count(v => conditionInteractions.Contains(v)) / distinctBase.Count;

            //get measure 2 - condition interactions not in baseline
            var m2 = (float) distinctCondition.Count(c => !baselineInteractions.Contains(c)) / distinctCondition.Count;

            //get measure 3 - number of baseline sequences covered in condition
            //for (var i = 1; i < baselineInteractions.Count; i++)
            //{

            //}
            //    if (baselineInteractions[i] != baselineInteractions[i - 1])
            //        baselineInteractionsCleaned.Add(baselineInteractions[i]);
            //var m2 = conditionInteractions.Count(c => !baselineInteractions.Contains(c));

            Debug.Log("Result: " + m1 + " " + m2);
        }

        public void CompareAll(string str)
        {
            var lines = str.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            for (var i = 1; i < lines.Length; i++)
            {
                Compare(lines[i], lines[0]);
            }
        }
    }

    [CustomEditor(typeof(UserStudyScript))]
    public class DistanceTravelledCounterEditor : Editor
    {
        public override void OnInspectorGUI()
        {

            var myScript = (UserStudyScript)target;
            
            GUILayout.Label("GENERATING OUTPUT TO: " + Application.persistentDataPath + "/" + "logData_<time>.txt");
            if (Application.isPlaying)
            {
                if(!myScript.Recording)
                    DrawDefaultInspector();
                if (!myScript.Recording && GUILayout.Button("(RE-)START USER STUDY"))
                {
                    myScript.TryStartUserStudy();
                }
                else if (myScript.Recording && GUILayout.Button("END USER STUDY"))
                {
                    myScript.EndUserStudy();
                }
            } else
            {
                DrawDefaultInspector();
                myScript.ConditionToCompare = EditorGUILayout.TextField("Condition:", myScript.ConditionToCompare);
                myScript.Baseline = EditorGUILayout.TextField("Baseline:", myScript.Baseline);
                //var ret = "Compare result: ";
                if (GUILayout.Button("Compare"))
                {
                    myScript.Compare(myScript.ConditionToCompare, myScript.Baseline);
                }
                else if (GUILayout.Button("CompareAll"))
                {
                    myScript.CompareAll(myScript.ConditionToCompare);
                }
            }
        }
    }
}