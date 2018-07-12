using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Petrinet;
using SpaceMapper;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.PostProcessing;

namespace Assets.Scripts.SpaceMapper
{
    [Serializable]
    public class RestartExperienceEvent : UnityEvent<SpaceMapperRoot.Command>
    {
    }

    [Serializable]
    public class KeyPressedInEditorEvent : UnityEvent<KeyCode>
    {
    }

    [Serializable]
    public class RebuildExperienceEvent : UnityEvent<bool>
    {
    }

    public class SpaceMapperRoot : MonoBehaviour
    {
        public TrackingSpaceRoot Ram;
        public Texture Rebuild, Restart, DestroyBuild, TogglePlay, ToggleDraw, ToggleMatrix, Bot;
        public bool ShowLayersElseHide;
        public List<string> SpaceOsLayers, SpaceManagementUnitLayersBuildOn, SpaceManagementUnitLayersBuildOff;
        public Camera SpaceEditorCam;
        public PostProcessingProfile WithEffects, WithoutEffects;
        public Material EditorBackground;
        public Photobooth Photobooth;
        public float SizeFactorTransitionsConditions = 2f;
        public Vector2Int ResolutionTransitions, ResolutionConditions;
        public float SpaceOsCameraSize;
        public GUISkin ActiveButtonSkin, InactiveButtonSkin, ToggleButtonSkin, ModeButtonSkin, BoxSkin;
        public double BotUpdate;
        public Transform PlaceMarker;
        public float FixedRadiusPlaces, FixedRadiusObjects, RadiusStates, ConeSize, ConeOffset, LineThin, LineThick, PictureFactor;
        public bool UseFixedRadiusPlaces, UseFixedRadiusObjects;
        public Rect Menu;
        public Color StateColor, PlaceColor;
        public ForceLayoutSettings ForceLayoutSettings;
        public List<GameObject> AllowedSwitch;
        public Vector3 PositionCam, PositionCamRecording, RotationCam, RotationCamRecording;
        public float FovCam, FovCamRecording, ForwardCam, ForwardCamRecording;
        public Font LabelFont;
        public float LabelPlaceOffsetFactor;
        public float AnimationTimeCam, AnimationTimeRearrange;
        public bool LeaveOutWallsForView;
        public LookTowardsCenter SpaceOsCam;
        [HideInInspector]
        public RestartExperienceEvent RestartExperience;
        [HideInInspector]
        public RebuildExperienceEvent RebuildExperience;
        [HideInInspector]
        public KeyPressedInEditorEvent KeyPressedInEditor;
        [HideInInspector]
        public UnityEvent RenderedCam;
        //public List<PetrinetCondition> StartTokens;

        private Dictionary<PetrinetCondition, int> _startTokens;

        internal List<PetrinetCondition> Conditions;
        internal List<PetrinetTransition> Transitions;
        internal bool BuildAvailable;
        internal List<PetrinetTransition> AddedTransitions;
        internal List<PetrinetCondition> AddedConditions;

        private Dictionary<PetrinetCondition, PetrinetCondition> _revokeTo;
        //private List<HardwareRequirements> _placedHardwareRequirements;
        private List<GameObject> _deleteOnRevoke;
        private Dictionary<PetrinetCondition, string> _oldName;
        private bool _init;

        public enum Command
        {
            StopRecordingStartAgain,
            StopRecording,
            SaveRecordStartAgain,
            SaveRecordContinueRecordingIfRecording,
            ContinueRecordingIfRecording
        }

        internal struct Interaction
        {
            internal PetrinetTransition Transition;
            internal bool Success;
        }

        internal List<Interaction> InteractionList;
        
        void Start ()
        {
           TryInit();
        }

        public void TryInit()
        {
            if(!Application.isPlaying || _init)
                return;
            _init = true;
            _deleteOnRevoke = new List<GameObject>();
            // _placedHardwareRequirements= new List<HardwareRequirements>();
            _revokeTo = new Dictionary<PetrinetCondition, PetrinetCondition>();
            Conditions = GetComponentsInChildren<PetrinetCondition>().ToList();
            Transitions = GetComponentsInChildren<PetrinetTransition>().ToList();
            _oldName = new Dictionary<PetrinetCondition, string>();
            AddedTransitions = new List<PetrinetTransition>();
            AddedConditions = new List<PetrinetCondition>();
            _startTokens = Conditions.ToDictionary(c => c, c => c.Tokens);
            InteractionList = new List<Interaction>();

            Ram.RequestRebuild.AddListener(TryRebuild);
            //TryRebuild();
        }

        public bool IsBuildAvailable()
        {
            return BuildAvailable;
        }

        private void OnEnable()
        {
            PetrinetTransition.TriedToFire.AddListener(RegisterInteraction);
        }
        private void OnDisable()
        {
            PetrinetTransition.TriedToFire.RemoveListener(RegisterInteraction);
        }

        private void RegisterInteraction(PetrinetTransition arg0, bool arg1)
        {
            if(InteractionList != null)
                InteractionList.Add(new Interaction {Transition = arg0, Success = arg1});
        }

        internal void TryRebuild()
        {
            Debug.Log("Petrinet Root: Updated Space to " + Ram.GetSpaceAvailable() + "m2");
            RestartPetrinet(false, Command.ContinueRecordingIfRecording);
            Revoke();
            //assign hardware (cluster & pack)
            BuildAvailable = AssignHardware();
            if (BuildAvailable)
            {
                //set up linkers
                AbstractLinker.LeaveOutWallsLeftLowerWallsAndCornersForView = LeaveOutWallsForView;
                //place all visuals etc. (compile & link)
                CreateExe();
            }
            if (RebuildExperience != null)
                RebuildExperience.Invoke(BuildAvailable);
        }

        internal void RestartPetrinet(bool resetConditions, Command command)
        {
            if (resetConditions)
            {
                //restart petrinet
                for (var i = 0; i < Conditions.Count; i++)
                {
                    //_conditions[i].Tokens = StartTokens.ContainsKey(_conditions[i]) ? StartTokens[_conditions[i]] : 0;// _startTokens[i];
                    var cond = Conditions[i];
                    Conditions[i].Tokens = _startTokens[Conditions[i]];//StartTokens.Count(s => s == cond);
                    Conditions[i].TokenUpdate();
                }
                //reset interaction list
                if(InteractionList != null)
                    InteractionList.Clear();
            }
            else
            {
                //reset onto last room
                var keys = _revokeTo.Keys;
                foreach (var k in keys)
                {
                    if (k.Tokens > 0)
                    {
                        _revokeTo[k].Tokens += k.Tokens;
                        _revokeTo[k].TokenUpdate();
                        k.Tokens = 0;
                        k.TokenUpdate();
                    }
                }
            }

            //setup visualizations / compile 
            var thingCompilers = gameObject.GetComponentsInChildren<AbstractCompiler>(true);
            foreach (var compiler in thingCompilers)
                compiler.Reset();

            if (RestartExperience != null)
                RestartExperience.Invoke(command);

            //change added conditions always
            for (var i = 0; i < AddedConditions.Count; i++)
            {
                AddedConditions[i].Tokens = 0;
                AddedConditions[i].TokenUpdate();
            }
        }

        internal void Revoke()
        {
            //delete transitions objects in revoke list
            var delete = new List<GameObject>(_deleteOnRevoke);
            foreach (var d in delete)
            {
                DestroyImmediate(d);
            }
            _deleteOnRevoke.Clear();
            //go through all other transitions and relink to old conditions
            foreach (var c in Transitions)
            {
                if (c == null)
                    return;
                c.In = c.In.Select(cin => _revokeTo.ContainsKey(cin) ? _revokeTo[cin] : cin).ToList();
                c.Out = c.Out.Select(cout => _revokeTo.ContainsKey(cout) ? _revokeTo[cout] : cout).ToList();
            }
            //free up hardware
            //_placedHardwareRequirements.Clear();
            //handle generated conditions
            var keys = _revokeTo.Keys;
            foreach (var k in keys)
            {
                //reparent conditions
                foreach (var t in Transitions)
                {
                    if (t.transform.parent != k.transform)
                        continue;
                    t.transform.parent = _revokeTo[k].transform;
                }
                //delete generated conditions
                DestroyImmediate(k.gameObject);
            }
            //destroy all environments
            var linkers = gameObject.GetComponentsInChildren<AbstractLinker>(true);
            foreach (var l in linkers)
                l.DeleteKids();
            //clear lists
            _revokeTo.Clear();
            AddedTransitions.Clear();
            AddedConditions.Clear();
            //reset names
            foreach (var t in Conditions)
                if (_oldName.ContainsKey(t))
                    t.gameObject.name = _oldName[t];
            _oldName.Clear();
            BuildAvailable = false;
        }

        private bool AssignHardware()
        {
            //get all physical places
            var places = Conditions.Where(c => c.Type == PetrinetCondition.ConditionType.Place).ToList();

            //get all permutations for room clusterings (each place needs to be clustered in pn steps, find n (cluster), then go through permutations)
            var clusterings = new Dictionary<PetrinetCondition, List<int[]>>();
            var hardwareAtPlace = new Dictionary<PetrinetCondition, List<HardwareRequirements>>();
            var otherHardware = GetUnparentedHardware();
            var ignorePlacesNoCluster = new List<PetrinetCondition>();
            foreach (var t in places)
            {
                var placeHardware = t.GetComponentsInChildren<HardwareRequirements>().ToList();
                var clusters = ClusterHardware(placeHardware).ToList();
                //for iteration, need at least one cluster, gets ignored later
                if (clusters.Count == 0)
                {
                    ignorePlacesNoCluster.Add(t);
                    clusters.Add(new[] { 0 });
                    placeHardware = new List<HardwareRequirements>();
                }
                hardwareAtPlace.Add(t, placeHardware);
                clusterings.Add(t, clusters);
            }

            //build loop
            var total = clusterings.Select(c => c.Value.Count).ToArray();
            //var current = total.Select(c => 0).ToArray();
            //var continueClustering = true;
            var mixedRadixCheckSumIterator = GetMixedRadixIteratorCrossSumSorted(total);

            //var index = 0;;
            for(var index = 0; index < mixedRadixCheckSumIterator.Count; index++)//while (true)//continueClustering)
            {
                //int count;
                var current = mixedRadixCheckSumIterator[index];//NumberToSet(index++, total, out count);
                //Debug.Log("evaluate set " + current.Aggregate("", (c, t) => c + (t + " ")));

                //get next clustering option
                var evaluatedClustering = new Dictionary<PetrinetCondition, int[]>();
                foreach (var place in places)
                {
                    var placeIndex = places.IndexOf(place);
                    var clusterIndexForPlaceIndex = current[placeIndex];
                    var cluster = !ignorePlacesNoCluster.Contains(place)
                        ? clusterings[place][clusterIndexForPlaceIndex]
                        : new int[0];
                    evaluatedClustering.Add(place, cluster);
                }
                //evaluate clustering option
                var packed = TryPack(places, evaluatedClustering, hardwareAtPlace, otherHardware);
                if (!packed)
                    continue;
                Debug.Log("packing successful at step " + index);
                return true;
                //if (index == count)
            }
            return false;
        }

        private static List<int[]> GetMixedRadixIteratorCrossSumSorted(IList<int> total)
        {
            var setDict = new List<int[]>();
            var index = 0;
            while (true) 
            {
                int count;
                var current = NumberToSet(index++, total, out count);
                setDict.Add(current);

                if (index == count)
                    break;
            }
            var sortedByCheckSum = setDict.OrderBy(rd => rd.Sum()).ToList();
            return sortedByCheckSum;
        }

        private static int[] NumberToSet(int number, IList<int> factors, out int maxValue)
        {
            //var list = new List<int>();
            //for (var i = 0; i < factors.Length; i++)
            //{
            //    list.Add(value & factors[i]);
            //    value = value / factors[i];
            //}
            //return list.ToArray();


            //var number = 313732097;

            //var factors = new[] { 1000, 60, 60, 24, 365 };
            var currPlaceValue = 1;
            var values = new List<int>();

            for (var i = 0; i < factors.Count; ++i)
            {
                values.Add(currPlaceValue);
                currPlaceValue *= factors[i];
                //Debug.Log(currPlaceValue);
            }
            // this isn't relevant for this specific problem, but might
            // be useful in related problems.
            maxValue = currPlaceValue - 1;

            var output = new int[values.Count];

            for (var v = values.Count - 1; v >= 0; --v)
            {
                output[v] = Math.DivRem(number, values[v], out number); //FloorToInt(theNumber / values[v]); 
                number %= values[v];
            }

            return output;
            // [97, 52, 8, 15, 3] --> 3 days, 15 hours, 8 minutes, 52 seconds, 97 milliseconds
        }

        private void CreateExe()
        {
            //setup visualizations / compile 
            var thingCompilers = gameObject.GetComponentsInChildren<AbstractCompiler>(true);
            foreach (var compiler in thingCompilers)
                compiler.StartCompile(Ram);

            //rebuild room / link objects
            var linkers = gameObject.GetComponentsInChildren<AbstractLinker>(true);
            var allTransitions = new List<PetrinetTransition>();
            allTransitions.AddRange(Transitions);
            allTransitions.AddRange(AddedTransitions);
            foreach (var linker in linkers)
            {
                var relevantTransitions = allTransitions.Where(t => 
                    t.In.Contains(linker.Condition) || 
                    t.Out.Contains(linker.Condition)
                    ).ToList();
                var hardware = relevantTransitions.Select(rt => rt.HardwareRequirements).Distinct().ToList();
                var compilers = thingCompilers.Where(tc => hardware.Contains(tc.HardwareRequirements)).ToList();
                linker.ActivateLinker(Ram, compilers);
            }
        }

        #region clustering

        private static IEnumerable<int[]> ClusterHardware(List<HardwareRequirements> hardware)
        {
            if(!hardware.Any())
                return new List<int[]>();

            //cluster transitions and create sub-spaces => split petrinet
            var distanceMatrix = new double[hardware.Count, hardware.Count];

            //helper
            var hardware2Transitions = hardware.Select(h => h.GetComponents<PetrinetTransition>().ToList()).ToArray();

            //init values
            for (var i = 0; i < hardware.Count; i++)
            {
                var transitionsI = hardware2Transitions[i];

                //just upper matrix
                for (var j = i + 1; j < hardware.Count; j++)
                {
                    var value = 5.0;

                    var transitionsJ = hardware2Transitions[j];

                    //step 1: transitions are close if interactions in story before/after this interaction 
                    // var outsJ = transitions[j].Out;
                    var orderIthenJ =
                        transitionsJ.Any(tj => transitionsI.Any(ti => ti.Out.All(tj.In.Contains)));//.TrueForAll(tiOut => tiOutx)Contains(tjIn))));//outsI.Contains(rtJin))// insJ.Any(insJx => insJx.All(inJx => outsI.Any()))  //transitions[j].In.All(rtJin => outsI.Contains(rtJin));
                    var orderJthenI =
                        transitionsJ.Any(tj => transitionsI.Any(ti => tj.Out.All(ti.In.Contains)));
                    if (orderIthenJ && orderJthenI)
                        value = 1;
                    else if (orderIthenJ || orderJthenI)
                        value = 2;
                    
                    //Debug.Log(hardware[i].gameObject.name + "/" + hardware[j].gameObject.name + "= " + value);
                    distanceMatrix[i, j] = value;
                }
            }

            //perform agglomerative hierarchical clustering on transitions
            var rep = TryCluster(distanceMatrix);
            if (rep == null)
                Debug.LogError("clustering error");
            var retList = new List<int[]>();
            var hashList = new List<int>();
            for (var k = 1; k <= hardware.Count; k++)
            {
                var clustering = GetClustering(rep, k);

                //also get all simpler splits (leave out older splits)
                var clusterSimplerCombinations = GetSimplerCombinations(clustering, ref hashList);
                retList.AddRange(clusterSimplerCombinations);
            }

            //order from least splits first, maintain other order when same amount of splits
            var orderedItems = OrderLeastSplitsFirst(retList);

            //set order according to interaction sequences
            var orderedArrangedItems = orderedItems.Select(oi => ArrangeInteractionOrder(oi, hardware)).ToArray();
            
            return orderedArrangedItems;
        }

        private static List<int[]> OrderLeastSplitsFirst(List<int[]> clusterings)
        {
            var newList = clusterings.OrderBy(c => c.Max(x => x)).ToList();
            return newList;
        }

        private static int[] ArrangeInteractionOrder(IList<int> clustering, IList<HardwareRequirements> hardware)
        {
            //cluster hardware first
            var k = clustering.Max(x => x) + 1;
            var clusteredHardware = new List<HardwareRequirements>[k];
            for (var i = 0; i < k; i++)
                clusteredHardware[i] = new List<HardwareRequirements>();
            for (var c = 0; c < clustering.Count; c++)
                clusteredHardware[clustering[c]].Add(hardware[c]);

            //now need to order clustering
            var orderedClustering = new int[clustering.Count];
            //init random order
            var all = new List<int>();
            for (var i = 0; i < k; i++)
                all.Add(i);
            //init empty list
            var order = new List<int>();
            //order
            while (all.Any())
            {
                var next = all.First();
                all.Remove(next);
                int insertPosition;
                if (!order.Any())
                {
                    //put first anywhere
                    insertPosition = 0;
                }
                else
                {
                    //find best insert position
                    insertPosition = 0;

                    var h1List = clusteredHardware[next];
                    var h1Trans = GetAllTransitionsFromHardwareList(h1List); //GetAllLogicalInsOrOuts(h1List, false);
                    var bestValue = -1;
                    //iterate through sorted order
                    for (var o = 0; o < order.Count; o++)
                    {
                        var orderLast = order[o];
                        var h2List = clusteredHardware[orderLast];
                        var h2Trans = GetAllTransitionsFromHardwareList(h2List);// GetAllLogicalInsOrOuts(h2List, true);                           
                        var value = h2Trans.Count(t2 => h1Trans.Any(t1 => TransitionLeadsToTransition(t1, t2)));
                        if (value > bestValue)
                        {
                            insertPosition = o;
                            bestValue = value;
                        }
                        if (o == order.Count - 1)
                        {
                            var valueRev = h1Trans.Count(t1 => h2Trans.Any(t2 => TransitionLeadsToTransition(t2, t1)));
                            if (valueRev > bestValue)
                            {
                                insertPosition = o + 1;
                                bestValue = value;
                            }
                        }
                    }

                }
                order.Insert(insertPosition, next);
            }

            for (var i = 0; i < clustering.Count; i++)
                orderedClustering[i] = order.IndexOf(clustering[i]);

            return orderedClustering;
        }

        private static IEnumerable<int[]> GetSimplerCombinations(int[] currentClustering, ref List<int> hashes)
        {
            var returnList = new List<int[]>();

            //go through linearly
            var splitIndices = new List<int>();
            for (var c = 1; c < currentClustering.Length; c++)
            {
                //check where change is, save index
                if (currentClustering[c] != currentClustering[c - 1])
                    splitIndices.Add(c);
            }

            //check if there is no cluster
            if (currentClustering.Max(c => c) == 0)
            {
                //add clustering
                var hashCurrentClustering = GetSequenceHashCode(splitIndices);
                if (hashes.Contains(hashCurrentClustering)) return returnList;
                hashes.Add(hashCurrentClustering);
                returnList.Add(currentClustering);
                return returnList;
            }

            //create all combinations of indices
            var combinations = GetCombination(splitIndices);

            //foreach combination re-create clustering 
            foreach (var combination in combinations)
            {
                //Debug.Log("__");
                //re-create clustering from index split
                var thisCluster = new int[currentClustering.Length];
                var tmp = 0;
                for (var c = 0; c < thisCluster.Length; c++)
                {
                    if (combination.Contains(c))
                        tmp++;
                    thisCluster[c] = tmp;
                   // Debug.Log(tmp);
                }
                //check if hash is already saved
                var thisHash = GetSequenceHashCode(combination);
                if (hashes.Contains(thisHash))
                    continue;
                //if not add to solutions
                returnList.Add(thisCluster);
                hashes.Add(thisHash);
            }
            
            return returnList;
        }

        private static List<int[]> GetCombination(IList<int> list)
        {
            var combinations = new List<int[]>();
            var count = Mathf.Pow(2, list.Count);
            for (var i = 1; i <= count - 1; i++)
            {
                var str = Convert.ToString(i, 2).PadLeft(list.Count, '0');
                var combination = new List<int>();
                for (var j = 0; j < str.Length; j++)
                {
                    if (str[j] == '1')
                    {
                        combination.Add(list[j]);
                    }
                }
                combinations.Add(combination.ToArray());
            }
            return combinations;
        }

        private static int GetSequenceHashCode<T>(IList<T> sequence)
        {
            const int seed = 487;
            const int modifier = 31;

            unchecked
            {
                return sequence.Aggregate(seed, (current, item) =>
                    (current * modifier) + item.GetHashCode());
            }
        }

        private static bool TransitionLeadsToTransition(PetrinetTransition from, PetrinetTransition to)
        {
            return to.In.All(from.Out.Contains);
        }

        private static List<PetrinetTransition> GetAllTransitionsFromHardwareList(List<HardwareRequirements> hardware)
        {
            var retList = new List<PetrinetTransition>();
            foreach (var h in hardware)
                retList.AddRange(h.GetComponentsInChildren<PetrinetTransition>());
            return retList;
        }

        private static int[] GetClustering(alglib.ahcreport rep, int k)
        {
            int[] clustering, cz;
            alglib.clusterizergetkclusters(rep, k, out clustering, out cz);
            return clustering;
        }

        private static alglib.ahcreport TryCluster(double[,] distanceMatrix)
        {
            alglib.clusterizerstate s;
            alglib.ahcreport rep;
            alglib.clusterizercreate(out s);
            alglib.clusterizersetdistances(s, distanceMatrix, true);
            alglib.clusterizerrunahc(s, out rep);
            return rep;
        }

        #endregion

        #region placement
        
        private bool TryPack(List<PetrinetCondition> places, Dictionary<PetrinetCondition, int[]> evaluatedClusteringAtPlace, 
            Dictionary<PetrinetCondition, List<HardwareRequirements>> hardwareAtPlace, List<HardwareRequirements> otherHardwareToPlace)
        {
            //abstraction layer, hardware, conditions => ids
            //assign id to places
            var places2Id = places.ToDictionary(p => p, p => places.IndexOf(p) * 1000);
            //get all hardware requirements and find connections to place(s)
            var otherHardware2Places = GetHardware2Places(otherHardwareToPlace);
            
            //build request to packer
            var requestToPacker = new List<AbstractPacker.PackingRequest>();
            
            //build lists for later assignment (in case packing gives a result)
            var buildObjects = new List<GameObject>();
            var instantiate = new List<bool>();
            var placeFrom = new List<PetrinetCondition>();
            var placeToId = new List<int>();
            var buildPosition = new List<Vector3>();

            //add hardware for transition objects
            //add other hardware
            foreach (var oh in otherHardware2Places)
            {
                requestToPacker.Add(
                    new AbstractPacker.PackingRequest
                    {
                        Size = oh.Key.Requirement,
                        Places = oh.Value.Select(v => places2Id[v]).ToList(),
                        WallX = oh.Key.WallPrefsX,
                        WallY = oh.Key.WallPrefsY,
                        //ValueWallX = oh.Key.ValueX,
                        //ValueWallY = oh.Key.ValueY,
                        IsSemiWall = oh.Key.IsSemiWall,
                        PlaceFarAway = oh.Key.PlaceFarAway
                    });
                buildObjects.Add(oh.Key.gameObject);
                buildPosition.Add(Vector3.zero);
                instantiate.Add(false);
                placeFrom.Add(null);
                placeToId.Add(-1);
            }
            //add place hardware, iterate through places
            foreach (var place in places)
            {
                var clustering = evaluatedClusteringAtPlace[place];
                var hardware = hardwareAtPlace[place];
                for (var i = 0; i < clustering.Length; i++)
                {
                    var requirement = hardware[i].Requirement;
                    var placeId = places2Id[place] + clustering[i];
                    buildObjects.Add(hardware[i].gameObject);
                    instantiate.Add(false);
                    buildPosition.Add(Vector3.zero);
                    placeFrom.Add(place);
                    placeToId.Add(placeId);
                    requestToPacker.Add(
                        new AbstractPacker.PackingRequest
                        {
                            Size = requirement,
                            Places = new List<int> { placeId },
                            WallX = hardware[i].WallPrefsX,
                            WallY = hardware[i].WallPrefsY,
                            //ValueWallX = hardware[i].ValueX,
                            //ValueWallY = hardware[i].ValueY,
                            IsSemiWall = hardware[i].IsSemiWall,
                            PlaceFarAway = hardware[i].PlaceFarAway
                        });
                }

               // Debug.Log("evaluate clusterresult " + clustering.Aggregate("", (current, t) => current + (t + " ")));
               // Debug.Log("with " + hardware.Aggregate("", (current, t) => current + t.gameObject.name + " "));

                //get extra space needed - the transition objects between each cluster/room
                var k = clustering.Any() ? clustering.Max() + 1 : 0;
                for (var i = 1; i < k; i++)
                {
                    var switchObject = place.gameObject.GetComponent<AbstractLinker>().GetSwitchObject();
                    var extra = switchObject.GetComponentInChildren<HardwareRequirements>();
                    var requirement = extra.Requirement;
                    buildObjects.Add(switchObject);
                    instantiate.Add(true);
                    //var position = MaxAlgo.WhereDoIPutThis(place.transform.position, Dictionary<_conditions + _transitions, float radius> fixed);
                    buildPosition.Add(place.transform.position);//TODO transition position
                    placeFrom.Add(place);
                    var placeId = places2Id[place] + i;
                    placeToId.Add(-1);
                    requestToPacker.Add(
                        new AbstractPacker.PackingRequest
                        {
                            Size = requirement,
                            Places = new List<int>
                            {
                                placeId - 1,
                                placeId
                            },
                            WallX = extra.WallPrefsX,
                            WallY = extra.WallPrefsY,
                            //ValueWallX = extra.ValueX,
                            //ValueWallY = extra.ValueY,
                            IsSemiWall = extra.IsSemiWall,
                            PlaceFarAway = extra.PlaceFarAway
                        });//amount maybe: k - 1; TODO nice tradeoff here - use for paper? extra transitions cost space - sweet middle, also: optimize which transitions can be left out!
                }
            }
            
            Dictionary<AbstractPacker.PackingRequest , AbstractPacker.PackingResult> result;
            var spaceReqFulfilled = Ram.GetPacking(requestToPacker, out result);
            if (!spaceReqFulfilled)
                return false;
            Debug.Log("CHECK");

            //from packing result, build new transition objects and assign hardware
            //empty added transition list
            AddedConditions.Clear();
            //iterate through rooms
            foreach (var place in places)
            {
                var hardware = hardwareAtPlace[place];
                var clustering = evaluatedClusteringAtPlace[place];
                var k = clustering.Any() ? clustering.Max() + 1 : 0;
                var linker = place.GetComponent<AbstractLinker>();

                //first, make room empty
                foreach (var h in hardware)
                    h.transform.parent = null;

                //copy k-1 times the condition as new rooms, with same parent (this.transform)
                for (var i = 0; i < k; i++)
                {
                    //i = 0 is original room/condition
                    var conditionI = i == 0 ? place : Instantiate(place);
                    if (i > 0)
                    {
                        conditionI.gameObject.name = place.gameObject.name + (i + 1);
                        conditionI.transform.position = place.transform.position; //TODO condition position
                        conditionI.VisualReferenceInEditor.position = place.VisualReferenceInEditor.position; //TODO condition position
                        _revokeTo.Add(conditionI, place);
                        places2Id.Add(conditionI, places2Id[place] + i);
                        AddedConditions.Add(conditionI);

                        //set right amount of tokens
                        //conditionI.Tokens = _startTokens[_conditions.IndexOf(place)];
                        conditionI.Tokens = _startTokens[place];//.Count(s => s == conditionI);

                        //set linker offset
                        var newUnfocusPosition = linker.GetCopyUnfocusIncrement() + linker.UnfocusPosition;
                        var newLinker = conditionI.GetComponent<AbstractLinker>();
                        newLinker.UnfocusPosition = newUnfocusPosition;

                        //create new dual transition between new condition and last
                        //addedLooseTransitions.AddRange(
                        //    CreateLooseTransitions(condition.GeneratedTransitionObject, last2Connect2, conditionI));
                        // last2Connect2 = conditionI;
                    }

                    //parenting and connecting transitions
                    //foreach (var c in transitionClusters[i])
                    //{
                    //    //parent all transitions underneath new condition
                    //    if (c.transform.parent == null)
                    //        c.transform.parent = conditionI.transform;
                    //    //set all transitions inside each cluster to match new condition
                    //    c.In = c.In.Select(cin => cin == condition ? conditionI : cin).ToList();
                    //    c.Out = c.Out.Select(cout => cout == condition ? conditionI : cout).ToList();
                    //}

                    conditionI.transform.parent = transform;

                   // conditions.Add(conditionI);
                }
                if (k > 1)
                {
                    _oldName.Add(place, place.gameObject.name);
                    place.gameObject.name = place.gameObject.name + "1";
                }
                    
            }

            //create id to room dictionary
            var id2Place = places2Id.ToDictionary(p => p.Value, p => p.Key);

            //empty added transition list
            AddedTransitions.Clear();
            AddedTransitions = new List<PetrinetTransition>();

            //build new objects
            var requestIds = requestToPacker.Select(rtp => rtp.Places).ToList();
            for (var i = 0; i < result.Count; i++)
            {
                //instantiate build object if ref to prefab
                var buildObject = buildObjects[i];

                if (i >= otherHardware2Places.Count)
                {
                    //if result is prefab ref, need to build generated transition object
                    if (instantiate[i])
                    {
                        var placesI = requestIds[i].Select(ri => id2Place[ri]).ToList();
                        var from = placesI[0];
                        var to = placesI[1];
                        buildObject = Instantiate(buildObjects[i]);
                        _deleteOnRevoke.Add(buildObject);
                        buildObject.name = "trans" + from.name + "And" + to.name;
                        buildObject.transform.parent = transform;
                        buildObject.transform.position = buildPosition[i];
                        var added = buildObject.GetComponentsInChildren<PetrinetTransition>();
                        added[0].In = new List<PetrinetCondition> { from };
                        added[0].Out = new List<PetrinetCondition> { to };
                        added[0].Name = "goTo" + to.name;
                        added[1].In = new List<PetrinetCondition> { to };
                        added[1].Out = new List<PetrinetCondition> { from };
                        added[1].Name = "goTo" + from.name;
                        added[0].Preview.transform.position = buildPosition[i];
                        added[1].Preview.transform.position = buildPosition[i];
                        buildObject.GetComponentInChildren<HardwareRequirements>().VisualReferenceInEditor.position = buildPosition[i];

                        AddedTransitions.AddRange(added);
                    }
                    //if result instead is in place, need to relink
                    else
                    {
                        //set all transitions inside each cluster to match new condition
                        var placeFromI = placeFrom[i];
                        var transitions = buildObject.GetComponentsInChildren<PetrinetTransition>();
                        var placeToI = id2Place[placeToId[i]];
                        foreach (var transition in transitions)
                        {
                            transition.In = transition.In.Select(cin => cin == placeFromI ? placeToI : cin).ToList();
                            transition.Out = transition.Out.Select(cout => cout == placeFromI ? placeToI : cout).ToList();
                        }
                    }

                    //parent underneath new condition
                    var resultParent = placeToId[i] >= 0 ? id2Place[placeToId[i]].transform : transform;
                    if (buildObject.transform.parent == null)
                        buildObject.transform.parent = resultParent;
                }
                
                //assign hardware
                var hardware = buildObject.GetComponentInChildren<HardwareRequirements>();
                var resultI = result[requestToPacker[i]];
                hardware.Assign(Ram, resultI);
            }

            /* old implementation
            //place loose transitions first, then conditions
            var allLooseTransitions = new List<PetrinetTransition>();
            allLooseTransitions.AddRange(addedLooseTransitions);
            allLooseTransitions.AddRange(looseTransitions);
            //set up conditions now
            //get all hardware reqs from parented condition
            var conditions2Hardware = conditions.ToDictionary(GetAllTransitionsFromChildren, t => t);
            //get all conditions from loose / unparented hardware reqs
            var hardware2Conditions = allLooseTransitions.ToDictionary(
                a => a.gameObject.GetComponent<HardwareRequirements>(),
                a => GetAllConditionsFromTransitions(a, conditions));

            //step 1: place loose transitions and get placements (need this for placing the conditions after)
            PlaceLooseTransitions(
                allLooseTransitions,
                conditions);
            //step 2: place transitions within clusters according to loose placements
            PlaceParentedTransitions(
                parentedTransitions,
                allLooseTransitions);
            */

            return true;

        }

        /* old implementation
        private void PlaceLooseTransitions(
            Dictionary<PetrinetCondition, List<PetrinetTransition>> conditionWithTransitions)
        {
            //browse through all conditions
            var conditions = conditionWithTransitions.Keys.ToList();
            //iterate through all clusters (generated rooms)
            foreach (var condition in conditions)
            {
                //get all transitions regarding this cluster
                var transitionsOnCondition = conditionWithTransitions[condition];
                //place transitions
                var reqs = transitionsOnCondition
                    .Select(ct => ct.gameObject.GetComponent<HardwareRequirements>()).Distinct().ToList();
                Ram.AssignHardware(reqs, new List<HardwareRequirements>());
            }
        }

        private void PlaceParentedTransitions(
            Dictionary<PetrinetCondition, List<PetrinetTransition>> parentedTransitions,
            List<PetrinetTransition> allLooseTransitions)
        {
            //browse through all conditions
            var conditions = parentedTransitions.Keys.ToList();
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < conditions.Count; i++)
            {
                //get condition
                var condition = conditions[i];

                //check which loose transitions occupying space (transitions between room clusters)
                var transitionsToConsiderAlreadyPlaced = allLooseTransitions.Where(k => k.In.Contains(condition) || k.Out.Contains(condition));
                //include all transitions underneath that condition (transitions/interaction in room)
                var transitionsToPlaceOrCheck = parentedTransitions[condition].Distinct().ToList();

                //place transition onto recommended position
                var reqs2Place = transitionsToPlaceOrCheck.Select(ct => ct.gameObject.GetComponent<HardwareRequirements>()).Distinct().ToList();
                var reqs2Consider = transitionsToConsiderAlreadyPlaced.Select(ct => ct.gameObject.GetComponent<HardwareRequirements>()).Distinct().ToList();
                Ram.AssignHardware(reqs2Place, reqs2Consider);
            }
        }
        
        private GameObject CreateLooseTransitions(out PetrinetTransition[] kids, GameObject generatedTransitionObject, List<PetrinetCondition> conditions)
        {
            var from = conditions[0];
            var to = conditions[1];
            var newTransition = Instantiate(generatedTransitionObject);
            _deleteOnRevoke.Add(newTransition);
            newTransition.gameObject.name = "_trans" + from.name + "And" + to.name;
            newTransition.transform.parent = transform;
            kids = newTransition.GetComponentsInChildren<PetrinetTransition>();
            kids[0].In = new List<PetrinetCondition> { from };
            kids[0].Out = new List<PetrinetCondition> { to };
            kids[0].Name = "_trans" + from.name + "To" + to.name;
            kids[1].In = new List<PetrinetCondition> { to };
            kids[1].Out = new List<PetrinetCondition> { from };
            kids[1].Name = "_trans" + to.name + "To" + from.name;
            return newTransition;
        }

        */
        
        #endregion

        #region helper funct

        private List<HardwareRequirements> GetUnparentedHardware()
        {
            var all = gameObject.GetComponentsInChildren<HardwareRequirements>();//.Where(hw => hw.transform.parent == transform).ToList()
            return (from hw in all let condition = hw.GetComponentInParent<PetrinetCondition>() where condition == null select hw).ToList();
        }

        private static Dictionary<HardwareRequirements, List<PetrinetCondition>> GetHardware2Places(
            IEnumerable<HardwareRequirements> allHardware)
        {
            var hardware2Places = new Dictionary<HardwareRequirements, List<PetrinetCondition>>();
            foreach (var hardware in allHardware)
            {
                var transitions = hardware.GetComponents<PetrinetTransition>();
                var transitions2Places = new List<PetrinetCondition>();
                foreach (var transition in transitions)
                {
                    var transition2Places = transition.In.Where(i => i.Type == PetrinetCondition.ConditionType.Place).ToList();
                    transition2Places.AddRange(transition.Out.Where(o => o.Type == PetrinetCondition.ConditionType.Place).ToList());
                    transitions2Places.AddRange(transition2Places);
                }
                var distinctPlaces = transitions2Places.Distinct().ToList();
                hardware2Places.Add(hardware, distinctPlaces);
            }
            return hardware2Places;
        }

        /* not needed anymore
         * 
         * 
        private List<HardwareRequirements> GetAllTransitionsFromCondition(PetrinetCondition condition, List<PetrinetTransition> transitions)
        {
            var t = transitions.Where(lt => lt.In.Contains(condition) || lt.Out.Contains(condition)).ToList();
            var tt = t.Select(ct => ct.gameObject.GetComponent<HardwareRequirements>()).Distinct().ToList();
            return tt;
        }
        private List<HardwareRequirements> GetAllTransitionsFromChildren(PetrinetCondition condition)
        {
            var t = condition.gameObject.GetComponentsInChildren<PetrinetTransition>(true).ToList();
            return GetAllTransitionsFromCondition(condition, t);
        }

        private static List<PetrinetCondition> GetAllConditionsFromTransitions(PetrinetTransition transition, List<PetrinetCondition> conditionsRelevant)
        {
            return conditionsRelevant.Where(lt => transition.In.Contains(lt) || transition.Out.Contains(lt)).ToList();
        }
        */
        #endregion
    }

    public struct Valuation
    {
        public float Radius;
        public float Weight;
    }

    [CustomEditor(typeof(SpaceMapperRoot))]
    public class SpaceMapperRootEditor : Editor
    {
        //private bool Run;
        private Vector3 _lastMousePosition;
        private Vector2 _lastMousePointer;
        private GameObject _lastSelected;
        private PetrinetTransition _lastSelectedTransition;
        private PetrinetCondition _lastSelectedCondition;
        private Vector3 _lastSelectedPosition;
        private List<PetrinetCondition> _conditions;
        private List<PetrinetTransition> _transitions;
        private List<ConditionCam> _conditionCams;
        //private Dictionary<SceneView, Camera> _sceneToCameras;
        //private bool _effectsEnabled = true;
        //private bool _skipDestruction = false;
        private DateTime _time, _timeGoto;
        //private double _updateInterval = 5d;
        //private SceneView _spaceManagementView, _spaceOsView;
        //private bool _isBuildAvailable = true;
        private bool _botToggle = false;
        private Mode _currentMode;
        private bool _resetCam;
        private float _lastOsSize;
        private Vector3 _lastLookAtPos;
        private Quaternion _lastLookAtRot;
        private Dictionary<HardwareRequirements, Valuation> _hardwareDict;
        private Dictionary<PetrinetCondition, Valuation> _conditionDict;
        private Vector3 _upperLeft;
        private Vector3 _lowerRight;
        private Vector3 _gotoCam, _goFromPos;
        private float _goFromSize;
        private bool _animateCam;
        private PetrinetCondition _focusedCondition;

        private enum Mode
        {
            SpaceOsLink,
            SpaceOsPlay,
            SpaceManagement
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying)
            {
                return;
            }

            if (_transitions == null || !_transitions.Any())
                return;

            GUILayout.Label("Transitions");
            var canFire = false;
            foreach (var transition in _transitions)
            {
                //i < Transitions.Count
                //    ? Transitions[i]
                //    : myScript.AddedTransitions[i - Transitions.Count];
                canFire = canFire || transition.CanFire();
                if (transition.CanFire() && GUILayout.Button(transition.Name))
                    transition.TryFire();
            }
            if (!canFire)
                GUILayout.Label("--- no transitions available ---");
        }

        private void UsePhotobooth(bool newPhotos)
        {
            var myScript = (SpaceMapperRoot) target;
            var photobooth = myScript.Photobooth;
            photobooth.gameObject.SetActive(true);
            photobooth.SetRenderTexture(myScript.ResolutionTransitions);
            var hardware = _transitions.Select(t => t.GetComponent<HardwareRequirements>()).Distinct();
            foreach (var h in hardware)
            {
                var transitions = h.GetComponents<PetrinetTransition>();
                foreach (var t in transitions)
                    photobooth.TryTakePicture(h.Requirement, t, myScript.UseFixedRadiusObjects, newPhotos);
            }
            photobooth.gameObject.SetActive(false);
        }
        
        private void FindAllConditionsAndTransitions()
        {
            var myScript = (SpaceMapperRoot)target;
            if(_conditions != null && _conditions.Any())
                _conditions.Clear();
            if (_transitions != null && _transitions.Any())
                _transitions.Clear();
            if (_conditionCams != null && _conditionCams.Any())
                _conditionCams.Clear();
            _conditions = myScript.GetComponentsInChildren<PetrinetCondition>().ToList();
            _transitions = myScript.GetComponentsInChildren<PetrinetTransition>().ToList();
            _conditionCams = myScript.GetComponentsInChildren<ConditionCam>().ToList();
            foreach (var condCam in _conditionCams)
                condCam.Init(myScript.ResolutionConditions);

            myScript.TryInit();
            _conditionDict = new Dictionary<PetrinetCondition, Valuation>();
            _hardwareDict = new Dictionary<HardwareRequirements, Valuation>();
            if (myScript.Conditions == null)
            {
                foreach (var transition in _transitions)
                {
                    var h = transition.HardwareRequirements;
                    if(_hardwareDict.ContainsKey(h))
                        continue;
                    _hardwareDict.Add(h, new Valuation { Radius = myScript.FixedRadiusObjects, Weight = 1f });
                }
                foreach (var condition in _conditions)
                {
                    _conditionDict.Add(condition, new Valuation
                    {
                        Radius = condition.Type == PetrinetCondition.ConditionType.Place ? myScript.FixedRadiusPlaces : myScript.RadiusStates,
                        Weight = 1f
                    });
                }
            }
            else
            {
                //var radiusDictionary = new Dictionary<PetrinetCondition, float>();
                var allConditionsFromTransitions = new List<PetrinetCondition>();
                foreach (var transition in _transitions)
                {
                    allConditionsFromTransitions.AddRange(transition.In);
                    allConditionsFromTransitions.AddRange(transition.Out);
                    var h = transition.HardwareRequirements;
                    if (_hardwareDict.ContainsKey(h))
                        continue;
                    var radius = myScript.UseFixedRadiusObjects
                        ? myScript.FixedRadiusObjects
                        : Mathf.Sqrt(h.Requirement.x * h.Requirement.y / Mathf.PI);
                    var weight = 4f;
                    _hardwareDict.Add(h, new Valuation { Radius = radius, Weight = weight });
                }
                allConditionsFromTransitions = allConditionsFromTransitions.Distinct().ToList();
                foreach (var condition in allConditionsFromTransitions)
                {
                    var radius = myScript.RadiusStates;
                    var weight = 6f;

                    if (condition.Type == PetrinetCondition.ConditionType.Place)
                    {
                        if (!myScript.UseFixedRadiusPlaces)
                        {
                            var condition1 = condition;
                            var relTransition = _transitions
                                .Where(t => t.In.Contains(condition1) || t.Out.Contains(condition1)).Distinct().ToList();
                            var hardware = relTransition.Select(t => t.GetComponent<HardwareRequirements>()).Distinct()
                                .ToList();
                            radius = myScript.SizeFactorTransitionsConditions *
                                     Mathf.Sqrt(hardware.Sum(t => t.Requirement.x * t.Requirement.y) / Mathf.PI);
                        }
                        else
                            radius = myScript.FixedRadiusPlaces;
                        weight = myScript.Conditions.Contains(condition) ? 10f : 1f;
                    }
                    _conditionDict.Add(condition, new Valuation { Radius = radius, Weight = weight });
                }
            }
        }

        private void DrawSpaceManagementUnitSceneView()
        {
            var myScript = (SpaceMapperRoot)target;
            
            //MyRenderSettings renderSettings = (MyRenderSettings)EditorWindow.GetWindow(typeof(MyRenderSettings)
            
            //SceneView.currentDrawingSceneView.camera.giz; = true;
            var ram = myScript.Ram;
            var kids = ram.transform.GetComponentsInChildren<Transform>()
                .Where(t => t.parent == ram.transform);
            //var invokes = Selection.gameObjects.Select(go => go.GetComponent<InvokeDeActivateEvent>()).ToList();
            var size = Mathf.Min(ram.TileSize.x, ram.TileSize.y) / 2f;
            //var pressed = false;
            //var available = false;
            Handles.color = new Color(1f, 1f, 1f, 0f);
            foreach (var kid in kids)
            {
                if (Handles.Button(kid.position, Quaternion.LookRotation(Vector3.up), size, size,
                    Handles.RectangleHandleCap))
                {
                    var invoke = kid.GetComponent<InvokeDeActivateEvent>();
                    invoke.MakeAvailable(!invoke.Available);
                    ram.CheckTilesChanged(myScript.BuildAvailable, false);
                   // FindAllConditionsAndTransitions();
                    SceneView.RepaintAll();
                    break;
                }
            }
        }

        private void DrawSpaceOsSceneView()
        {
            var myScript = (SpaceMapperRoot)target;

            var e = Event.current;

            var guiStyle = new GUIStyle
            {
                alignment = TextAnchor.LowerCenter,
                font = myScript.LabelFont
            };
            var guiStyleBold = new GUIStyle
            {
                alignment = TextAnchor.LowerCenter,
                font = myScript.LabelFont,
                fontStyle = FontStyle.Bold
            };
            
            _transitions = _transitions.Where(t => t != null).ToList();
            _conditions = _conditions.Where(t => t != null).ToList();

            var mouseDown = false;
            if (e.type == EventType.MouseMove)
            {
                _lastMousePointer = e.mousePosition;
                _lastMousePosition = HandleUtility.GUIPointToWorldRay(_lastMousePointer).origin;
                _lastMousePosition.y = 0f;
                e.Use();  //Eat the event so it doesn't propagate through the editor.
            } else if (e.type == EventType.MouseDown)
            {
                mouseDown = true;
            }


            var mouseOverMenu =
                _lastMousePointer.y > myScript.Menu.y &&
                _lastMousePointer.y < myScript.Menu.y + myScript.Menu.height &&
                _lastMousePointer.x > myScript.Menu.x &&
                _lastMousePointer.x < myScript.Menu.x + myScript.Menu.width;
            
            //draw buttons and shapes
            GameObject currentSelected = null;
            PetrinetTransition currentSelectedTransition = null;
            var currentSelectedPosition = Vector3.zero;
            PetrinetCondition currentSelectedCondition = null;
            var allHardware = _transitions.Select(t => t.GetComponent<HardwareRequirements>()).Distinct().ToList();
            foreach (var hardware in allHardware)
            {
                Handles.color = new Color(1f, 1f, 1f, 0f);
                var transitions = hardware.GetComponents<PetrinetTransition>();
                //draw connections
                for (var i = 0; i < transitions.Length; i++)
                {
                    var requirement = myScript.UseFixedRadiusObjects ? Vector2.one * myScript.FixedRadiusObjects *2f : myScript.SizeFactorTransitionsConditions * hardware.Requirement;
                    var yOffset = (Mathf.Max(requirement.y, requirement.x) + 0.3f) * i * 1.5f;
                    var pos = hardware.VisualReferenceInEditor.position;//transitions[i].transform.position;
                    pos.z += yOffset;
                    var alreadyInteractedWith = myScript.InteractionList != null && myScript.InteractionList.Any(inter => inter.Success && inter.Transition == transitions[i]);

                    if (transitions[i].Preview != null)
                    {
                        pos = transitions[i].Preview.transform.position;
                    }

                    var closestDict = new Dictionary<PetrinetCondition, Vector3>();
                    foreach (var iIn in transitions[i].In)
                    {
                        Handles.color = iIn.Type == PetrinetCondition.ConditionType.LogicalCondition
                            ? myScript.StateColor
                            : myScript.PlaceColor;

                        var iInPosition = iIn.PictureObject.position;
                        if (iIn.Type == PetrinetCondition.ConditionType.LogicalCondition)
                        {
                            var v = new Vector3[4];
                            iIn.SizeRef.GetWorldCorners(v);
                            var rectBoundPoint = ClosestToRectangle(
                                pos * 0.5f + 0.5f * iInPosition,
                                new Vector2(v[0].x, v[0].z),
                                new Vector2(v[2].x, v[2].z));
                            iInPosition = rectBoundPoint;
                        }

                        Vector3 posClosest;
                        Vector3 dir;
                        if (!myScript.UseFixedRadiusObjects)
                        {
                            var closest = ClosestToRectangle(
                                iInPosition - pos,
                            new Vector2(-0.5f * requirement.x, -0.5f * requirement.y),
                            new Vector2(0.5f * requirement.x, 0.5f * requirement.y));
                            dir = (pos + closest - iInPosition).normalized;
                            closestDict.Add(iIn, closest);
                            posClosest = pos + closest;
                        }
                        else
                        {
                            dir = (pos - iInPosition).normalized;
                            posClosest = pos - dir * myScript.FixedRadiusObjects;
                        }
                        posClosest -= dir * myScript.ConeOffset;

                        var radius = _conditionDict[iIn].Radius;
                        var otherpos = iIn.Type == PetrinetCondition.ConditionType.LogicalCondition
                            ? iInPosition
                            : iInPosition + dir * (radius + myScript.ConeOffset);
                        if (dir.magnitude > 0f)
                            Handles.ConeHandleCap(0, posClosest , Quaternion.LookRotation(dir), myScript.ConeSize, EventType.Repaint);
                        Handles.DrawAAPolyLine(alreadyInteractedWith ? myScript.LineThick : myScript.LineThin, otherpos,
                            posClosest);
                    }
                    foreach (var iOut in transitions[i].Out)
                    {
                        Handles.color = iOut.Type == PetrinetCondition.ConditionType.LogicalCondition
                            ? myScript.StateColor
                            : myScript.PlaceColor;

                        var iOutPosition = iOut.PictureObject.position;
                        if (iOut.Type == PetrinetCondition.ConditionType.LogicalCondition)
                        {
                            var v = new Vector3[4];
                            iOut.SizeRef.GetWorldCorners(v);
                            var rectBoundPoint = ClosestToRectangle(
                                pos * 0.5f + 0.5f * iOutPosition,
                                new Vector2(v[0].x, v[0].z),
                                new Vector2(v[2].x, v[2].z));
                            iOutPosition = rectBoundPoint;
                        }

                        Vector3 posClosest;
                        Vector3 dir;
                        if (!myScript.UseFixedRadiusObjects)
                        {
                            Vector3 closest;
                            if (closestDict.ContainsKey(iOut))
                            {
                                closest = closestDict[iOut];
                            }
                            else
                            {
                                closest = ClosestToRectangle(
                                    iOut.PictureObject.position - pos,
                                    new Vector2(-0.5f * requirement.x, -0.5f * requirement.y),
                                    new Vector2(0.5f * requirement.x, 0.5f * requirement.y));
                            }
                            dir = (iOutPosition - (pos + closest)).normalized;
                            posClosest = pos + closest;
                        }
                        else
                        {
                            dir = (iOutPosition - pos).normalized;
                            posClosest = pos + dir * myScript.FixedRadiusObjects;
                        }
                        posClosest += dir * myScript.ConeOffset;

                        var radius = _conditionDict[iOut].Radius;
                        var otherpos = iOut.Type == PetrinetCondition.ConditionType.LogicalCondition
                            ? iOutPosition
                            : iOutPosition - (radius + myScript.ConeOffset) * dir;
                        //Handles.DrawLine(iOut.VisualReferenceInEditor.position - dir * radius, pos + closest);
                        Handles.DrawAAPolyLine(alreadyInteractedWith ? myScript.LineThick : myScript.LineThin, otherpos, posClosest);
                        if (dir.magnitude > 0f)
                            Handles.ConeHandleCap(0, otherpos, Quaternion.LookRotation(dir), myScript.ConeSize, EventType.Repaint);
                        //Handles.DrawDottedLine(midPoint, pos, 4f);
                    }
                    var size = myScript.UseFixedRadiusObjects ? myScript.FixedRadiusObjects : Mathf.Max(requirement.x, requirement.y);
                    transitions[i].HardwareRequirements.VisualReferenceInEditor.localScale = size * Vector3.one;
                    if (transitions[i].Preview != null)
                    {
                        var softFollow = transitions[i].Preview.GetComponent<SoftFollow>();
                        if (softFollow != null)
                            softFollow.MoveOffset = yOffset * Vector3.forward;
                        //TODO transitions[i].Preview.transform.position = Vector3.Slerp(transitions[i].Preview.transform.position, pos, lerp);
                        //TODO transitions[i].Preview.transform.localScale = Vector3.Slerp(transitions[i].Preview.transform.localScale, new Vector3(requirement.x, 1f, requirement.y), lerp);//Vector3.one * size);
                    }
                    else
                    {
                        var verts = new Vector3[4];
                        verts[0] = new Vector3(transitions[i].transform.position.x - requirement.x / 2f, 0f,
                            transitions[i].transform.position.z - requirement.y / 2f + yOffset);
                        verts[1] = new Vector3(transitions[i].transform.position.x - requirement.x / 2f, 0f,
                            transitions[i].transform.position.z + requirement.y / 2f + yOffset);
                        verts[2] = new Vector3(transitions[i].transform.position.x + requirement.x / 2f, 0f,
                            transitions[i].transform.position.z + requirement.y / 2f + yOffset);
                        verts[3] = new Vector3(transitions[i].transform.position.x + requirement.x / 2f, 0f,
                            transitions[i].transform.position.z - requirement.y / 2f + yOffset);
                        Handles.color = new Color(1f, 1f, 1f, 0f); ;
                        //Handles.DrawSolidRectangleWithOutline(verts, new Color(1, 1, 1, 1f), new Color(0, 0, 0, 0));
                    }

                    //Handles.color = new Color(0f, 1f, 1f, 0f);
                    if (!mouseOverMenu)
                    {
                        var itIsOver = false;
                        if (myScript.UseFixedRadiusObjects)
                        {
                            itIsOver = Vector3.Distance(_lastMousePosition, pos) < myScript.FixedRadiusObjects;
                            //itIsOn = Handles.Button(pos, Quaternion.LookRotation(Vector3.up), myScript.FixedRadiusObjects, myScript.FixedRadiusObjects,
                            //    Handles.CircleHandleCap);
                        }
                        else
                        {
                            itIsOver = Vector3.Distance(_lastMousePosition, pos) < size / 2f;
                            //itIsOn = Handles.Button(pos, Quaternion.LookRotation(Vector3.up), size / 2f, size / 2f,
                            //    Handles.RectangleHandleCap);
                        }
                        if (mouseDown && itIsOver)
                        {
                            currentSelectedTransition = transitions[i];
                            currentSelected = hardware.VisualReferenceInEditor.gameObject;
                            currentSelectedPosition = pos;
                        }

                        var canFire = transitions[i].GetConditionsFulfilled().Values.All(v => v);
                        if (_currentMode == Mode.SpaceOsPlay && canFire)
                        {
                            var goName = transitions[i].Name;
                            goName = goName.Replace("_", "");
                            var labelPos = pos;
                            labelPos.z -= 0.5f * requirement.y;
                            Handles.Label(labelPos, goName, canFire ? guiStyleBold : guiStyle);
                        }
                        else
                        {
                            if (itIsOver)
                            {
                                var goName = transitions[i].Name;
                                goName = goName.Replace("_", "");
                                var labelPos = HandleUtility.GUIPointToWorldRay(_lastMousePointer - myScript.LabelPlaceOffsetFactor * Vector2.up).origin;
                                Handles.Label(labelPos, goName, canFire ? guiStyleBold : guiStyle);
                            }
                        }
                    }
                }
            }

            foreach (var condition in _conditions)
            {
                var color = condition.Type == PetrinetCondition.ConditionType.Place ? myScript.PlaceColor : myScript.StateColor;
                Handles.color = color;

                var radius = _conditionDict.ContainsKey(condition) ? _conditionDict[condition].Radius : 1f;
                var conditionCam = condition.GetComponentInChildren<ConditionCam>();
                if (condition.Type == PetrinetCondition.ConditionType.Place && conditionCam != null)
                {
                    //TODOconditionCam.PictureObject.transform.position = Vector3.Slerp(conditionCam.PictureObject.transform.position, condition.VisualReferenceInEditor.position, lerp);
                    condition.VisualReferenceInEditor.localScale = myScript.PictureFactor * Vector3.one * radius / 5f;// conditionCam.PictureObject.transform.localScale = Vector3.Slerp(conditionCam.PictureObject.transform.localScale,, lerp);
                    //conditionCam.PictureObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                }
                else
                {
                    //Handles.DrawSolidDisc(condition.PictureObject.position, Vector3.up, radius);
                }

                if (condition.Tokens > 0)
                {
                    switch (condition.Type)
                    {
                        case PetrinetCondition.ConditionType.LogicalCondition:
                            //Handles.color = Color.black;
                            //Handles.DrawSolidDisc(condition.PictureObject.position, Vector3.up, 0.2f * myScript.RadiusStates);
                            break;
                        case PetrinetCondition.ConditionType.Place:
                            myScript.PlaceMarker.position = condition.PictureObject.position;
                            myScript.PlaceMarker.localScale = Vector3.one * (radius + 0.3f) / 5f;
                            break;
                    }
                    //Handles.color = Color.white;
                    //Handles.DrawSolidDisc(condition.VisualReferenceInEditor.position, Vector3.up, 0.26f);

                }


                //color.a = 0f;
                //Handles.color = color;
                if (!mouseOverMenu)//Handles.Button(condition.VisualReferenceInEditor.position, Quaternion.LookRotation(Vector3.up), radius, radius, Handles.CircleHandleCap))
                {
                    
                    var isItOver = false;
                    if (condition.Type == PetrinetCondition.ConditionType.LogicalCondition)
                    {
                        var v = new Vector3[4];
                        condition.SizeRef.GetWorldCorners(v);
                        isItOver = _lastMousePosition.x > v[0].x &&
                                   _lastMousePosition.z > v[0].z &&
                                   _lastMousePosition.x < v[2].x &&
                                   _lastMousePosition.z < v[2].z;
                    }
                    else
                    {
                        var condPos = condition.VisualReferenceInEditor.position;
                        condPos.y = 0f;
                        var mouse2Thing = Vector3.Distance(_lastMousePosition, condPos);
                        isItOver = mouse2Thing < radius;
                    }

                    if (mouseDown && isItOver)
                    {
                        currentSelected = condition.VisualReferenceInEditor.gameObject;
                        currentSelectedCondition = condition;
                        currentSelectedPosition = condition.VisualReferenceInEditor.position;
                    }
                    
                    var goName = condition.gameObject.name;
                    goName = goName.Replace("_", "");

                    if (condition.FloatingText != null)
                    {
                        //const char dot = '\u2022';
                        if (condition.Tokens == 0)
                            goName = goName + "?";
                        for (var t = 0; t < condition.Tokens; t++)
                            goName = goName + "!";//dot.ToString();
                        condition.FloatingText.text = goName;
                        condition.FloatingText.fontStyle = condition.Tokens == 0 ? FontStyle.Normal : FontStyle.Bold;
                    }
                    else
                    {
                        if (isItOver)
                        {
                            //var labelPos = _lastMousePosition;//condPos;
                            //labelPos.z -= myScript.LabelPlaceOffsetFactor;//* radius;
                            var labelPos = HandleUtility.GUIPointToWorldRay(_lastMousePointer - myScript.LabelPlaceOffsetFactor * Vector2.up).origin;
                            Handles.Label(labelPos, new GUIContent(goName), condition.Tokens == 0 ? guiStyle : guiStyleBold);
                        }
                    }
                }

                
            }

            //draw current user selection
            if (_lastSelected != null)
            {
                Handles.color = Color.white;
                var posL = _lastSelectedPosition;
                if (_lastSelectedCondition != null)
                {
                    if (_lastSelectedCondition.Type == PetrinetCondition.ConditionType.LogicalCondition)
                    {
                        var v = new Vector3[4];
                        _lastSelectedCondition.SizeRef.GetWorldCorners(v);
                        var rectBoundPoint = ClosestToRectangle(
                            _lastMousePosition * 0.5f + 0.5f * _lastSelectedPosition,
                            new Vector2(v[0].x, v[0].z),
                            new Vector2(v[2].x, v[2].z));
                        posL = rectBoundPoint;
                    }
                    else
                    {
                        posL = _lastSelectedPosition + (_lastMousePosition - _lastSelectedPosition).normalized *
                               _conditionDict[_lastSelectedCondition].Radius;
                    }
                } else if (_lastSelectedTransition != null)
                {
                    posL = _lastSelectedPosition + (_lastMousePosition - _lastSelectedPosition).normalized *
                           _hardwareDict[_lastSelectedTransition.HardwareRequirements].Radius;
                }
                Handles.DrawLine(posL, _lastMousePosition);
            }
            if (currentSelected != null)
            {
                if (_currentMode == Mode.SpaceOsLink)
                {
                    if (_lastSelected != null)
                    {
                        if (_lastSelectedTransition != null && currentSelectedCondition != null)
                        {
                            if (!_lastSelectedTransition.Out.Contains(currentSelectedCondition))
                            {
                                _lastSelectedTransition.Out.Add(currentSelectedCondition);
                            }
                            else
                            {
                                _lastSelectedTransition.Out.Remove(currentSelectedCondition);
                            }
                        }
                        else if (_lastSelectedCondition != null && currentSelectedTransition != null)
                        {
                            if (!currentSelectedTransition.In.Contains(_lastSelectedCondition))
                            {
                                currentSelectedTransition.In.Add(_lastSelectedCondition);
                            }
                            else
                            {
                                currentSelectedTransition.In.Remove(_lastSelectedCondition);
                            }
                        }

                        ReparentAllTransitions();

                        _lastSelected = null;
                        _lastSelectedTransition = null;
                        _lastSelectedCondition = null;
                        _lastSelectedPosition = Vector3.zero;
                    }
                    else 
                    {
                        _lastSelected = currentSelected;
                        _lastSelectedTransition = currentSelectedTransition;
                        _lastSelectedPosition = currentSelectedPosition;
                        _lastSelectedCondition = currentSelectedCondition;
                    }
                }
                else if(_currentMode == Mode.SpaceOsPlay)
                {
                    if (currentSelectedTransition)
                        currentSelectedTransition.TryFire();
                    //else
                    //    GoToSpaceManagementView();
                }
            } else if (mouseDown)
            {
                if (_lastSelected != null)
                {
                    var pos = _lastMousePosition;
                    pos.y = 0f;
                    _lastSelected.transform.position = pos;
                    _lastSelected = null;
                }
            }
            //else if (mouseDown &&  _currentMode != Mode.SpaceManagement)
            //{
            //    ResetCamTo(_lastMousePosition, null);
            //}
        }

        private void DrawInterface()
        {
            var myScript = (SpaceMapperRoot)target;
            
            // Create style for a button
            var myButtonStyle = new GUIStyle(myScript.ActiveButtonSkin.button);
            var myModeStyle = new GUIStyle(myScript.ModeButtonSkin.button);
            var myInactiveButtonStyle = new GUIStyle(myScript.InactiveButtonSkin.button);
            //var myToggleButtonStyle = new GUIStyle(myScript.ToggleButtonSkin.button);
            var myBoxStyle = new GUIStyle(myScript.BoxSkin.box);
            Handles.BeginGUI();

            //GUILayout.Label("Other options", textStyle);
            GUILayout.BeginArea(myScript.Menu, myBoxStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(myScript.Rebuild, myButtonStyle, GUILayout.Width(50), GUILayout.Height(50)) && Application.isPlaying)
            {
                EnableDisableCompilers(true);
                // myScript.Ram.CheckTilesChanged(_isBuildAvailable);
                myScript.TryRebuild();
                //FindAllConditionsAndTransitions();
                SceneView.RepaintAll();
            }
            if (GUILayout.Button(myScript.DestroyBuild, myScript.BuildAvailable ? myButtonStyle : myInactiveButtonStyle, GUILayout.Width(50), GUILayout.Height(50)) && myScript.BuildAvailable && Application.isPlaying)
            {
                _botToggle = false;
                myScript.SpaceEditorCam.GetComponent<PostProcessingBehaviour>().profile = myScript.WithoutEffects;
                myScript.RestartPetrinet(false, SpaceMapperRoot.Command.StopRecording);
                myScript.Revoke();
                //FindAllConditionsAndTransitions();
                EnableDisableCompilers(false);
                SceneView.RepaintAll();
            }
            if (GUILayout.Button(myScript.Restart, myScript.BuildAvailable ? myButtonStyle : myInactiveButtonStyle, GUILayout.Width(50), GUILayout.Height(50)) && myScript.BuildAvailable && Application.isPlaying)
            {
                //ToggleBuildMode(true);
                _botToggle = false;
                myScript.RestartPetrinet(true, SpaceMapperRoot.Command.SaveRecordStartAgain);
                //FindAllConditionsAndTransitions();
                SceneView.RepaintAll();
            }
            if (GUILayout.Button(myScript.Bot, _botToggle ? myButtonStyle : myModeStyle, GUILayout.Width(50), GUILayout.Height(50)) && Application.isPlaying)
            {
                _botToggle = !_botToggle;
            }
            GUILayout.Space(20);
            if (GUILayout.Button(myScript.ToggleDraw,
                (_currentMode == Mode.SpaceOsLink ? myModeStyle : myButtonStyle),
                GUILayout.Width(50), GUILayout.Height(50)))
            {
                myScript.SpaceEditorCam.GetComponent<PostProcessingBehaviour>().profile = myScript.WithoutEffects;
                _currentMode = Mode.SpaceOsLink;
                _lastSelected = null;
                myScript.SpaceOsCam.CloseView = false;
                SceneView.RepaintAll();
            }
            if (GUILayout.Button(myScript.TogglePlay, 
                (_currentMode == Mode.SpaceOsPlay ? myModeStyle : myButtonStyle), 
                GUILayout.Width(50), GUILayout.Height(50)))
            {
                myScript.SpaceEditorCam.GetComponent<PostProcessingBehaviour>().profile = myScript.WithoutEffects;
                _currentMode = Mode.SpaceOsPlay;
                myScript.SpaceOsCam.CloseView = false;
                _lastSelected = null;
                SceneView.RepaintAll();
            }
            if (GUILayout.Button(myScript.ToggleMatrix,
                    (_currentMode == Mode.SpaceManagement ? myModeStyle : myButtonStyle),
                GUILayout.Width(50), GUILayout.Height(50)))
            {
                GoToSpaceManagementView();
                myScript.SpaceOsCam.CloseView = true;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        public void OnSceneGUI()
        {
            //var myScript = (SpaceMapperRoot)target;
            //if(myScript.RenderedCam != null)
            //    myScript.RenderedCam.Invoke();
            //SceneView.lastActiveSceneView.camera.targetTexture = myScript.RenderTex;

            var myScript = (SpaceMapperRoot)target;

            if (_currentMode == Mode.SpaceManagement)//SceneView.currentDrawingSceneView == _spaceManagementView)
                DrawSpaceManagementUnitSceneView();
            else //if (SceneView.currentDrawingSceneView == _spaceOsView)
                DrawSpaceOsSceneView();

            if (Application.isPlaying && _botToggle)
                RunBot();
            
            DrawInterface();

            RenderCam();

            CheckKeyEvents();

            // for live debugging the layouter, also disable SoftFollow when using this
            //if (_layouter != null)
            //    _layouter.Update(Time.fixedUnscaledDeltaTime * myScript.ForceLayoutSettings.LiveSimulationSpeed);

            //SceneView.lastActiveSceneView.camera.Render();
            //SceneView.lastActiveSceneView.camera.targetTexture = null;
            //SceneView.lastActiveSceneView.camera.Render();
        }

        private void CheckKeyEvents()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;
            var myScript = (SpaceMapperRoot)target;
            if(myScript.KeyPressedInEditor != null)
                myScript.KeyPressedInEditor.Invoke(e.keyCode);
            e.Use();
        }

        private void GoToSpaceManagementView()
        {
            var myScript = (SpaceMapperRoot)target;
            myScript.SpaceEditorCam.GetComponent<PostProcessingBehaviour>().profile = myScript.BuildAvailable ? myScript.WithEffects : myScript.WithoutEffects;
            _currentMode = Mode.SpaceManagement;
            _resetCam = true;
            SceneView.RepaintAll();
        }

        private void RenderCam()
        {
            var myScript = (SpaceMapperRoot) target;
            
            Material sky = myScript.EditorBackground;
            if (_currentMode == Mode.SpaceManagement && myScript.BuildAvailable)
            {
                foreach (var c in _conditions)
                {
                    if (c.Type == PetrinetCondition.ConditionType.LogicalCondition || c.Tokens <= 0)
                        continue;
                    var condCam = c.GetComponentInChildren<ConditionCam>();
                    if(condCam != null)
                        sky = condCam.EditorSkybox;
                    break;
                }
            }
            RenderSettings.skybox = sky;

            List<string> layers;
            Vector3 lookAtPos;
            Quaternion lookAtRot;
            bool orthographic, sceneLighting;
            float size;
            if (_currentMode == Mode.SpaceManagement)
            {
                orthographic = false;
                sceneLighting = myScript.BuildAvailable;
                var trans = myScript.SpaceEditorCam.transform;
                lookAtPos = trans.position;
                lookAtRot = trans.rotation;
                //pivot = SceneView.lastActiveSceneView.pivot;
                size = 1f;

                layers = myScript.BuildAvailable ?
                            myScript.SpaceManagementUnitLayersBuildOn :
                            myScript.SpaceManagementUnitLayersBuildOff;
            }
            else
            {
                orthographic = true;
                sceneLighting = false;

                if (!_resetCam)
                {
                    if (_animateCam)
                    {
                        var scaledTimeDiff = (float) (DateTime.Now- _timeGoto).TotalSeconds / myScript.AnimationTimeCam;
                        var factor = Mathf.Clamp01(SoftFollow.Ease(scaledTimeDiff, SoftFollow.Easing.Quadratic));
                        _lastLookAtPos = Vector3.Lerp(_goFromPos, _gotoCam, factor);
                        //var sizeFactor = Mathf.Abs((factor - 0.5f) * 2f);
                        //_lastOsSize = _goFromSize * sizeFactor + (1f - sizeFactor) * _goFromSize * 2f;
                        _animateCam = scaledTimeDiff < 1f;
                    }
                    else
                    {
                        _lastLookAtPos = SceneView.currentDrawingSceneView.pivot;
                    }

                    _lastOsSize = SceneView.currentDrawingSceneView.size;
                    _lastLookAtRot = SceneView.currentDrawingSceneView.rotation;
                    
                    var lastViewRect = SceneView.currentDrawingSceneView.camera.pixelRect;
                    _upperLeft = SceneView.currentDrawingSceneView.camera.ScreenToWorldPoint(Vector2.zero);
                    _lowerRight = SceneView.currentDrawingSceneView.camera.ScreenToWorldPoint(new Vector2(lastViewRect.width, lastViewRect.height));
                }

                _resetCam = false;
                size = _lastOsSize;
                lookAtPos = _lastLookAtPos;
                lookAtRot = _lastLookAtRot;

                layers = myScript.SpaceOsLayers;
            }
            ToolLayers(layers, myScript.ShowLayersElseHide);
            //SceneView.currentDrawingSceneView.LookAtDirect(lookAt);//);LookAtDirect(lookAt);
            SceneView.currentDrawingSceneView.m_SceneLighting = sceneLighting;
            SceneView.currentDrawingSceneView.orthographic = orthographic;
            //SceneView.currentDrawingSceneView.pivot = pivot;
            SceneView.currentDrawingSceneView.LookAtDirect(
                lookAtPos, 
                lookAtRot, size);
            //var ppb = SceneView.currentDrawingSceneView.camera.GetComponent<PostProcessingBehaviour>() ?? SceneView.currentDrawingSceneView.camera.gameObject.AddComponent<PostProcessingBehaviour>();
            //ppb.profile = _currentMode == Mode.SpaceManagement ? myScript.WithEffects : myScript.WithoutEffects;
            //myScript.ddf SpaceEditorCam.GetComponent<PostProcessingBehaviour>().profile = _isBuildAvailable ? myScript.WithEffects : myScript.WithoutEffects;
            
            SceneView.currentDrawingSceneView.Repaint();
            
        }

        private void RunBot()
        {
            if (DateTime.Now.CompareTo(_time) <= 0)
                return;

            // set time to wait until the next update
            var myScript = (SpaceMapperRoot)target;
            _time = DateTime.Now.AddSeconds(myScript.BotUpdate);

            //FindAllConditionsAndTransitions();
            var fulfilled = _transitions.Where(t => t.GetConditionsFulfilled().Values.All(v => v)).ToList();
            if (fulfilled.Any())
            {
                var randomIndex = UnityEngine.Random.Range(0, fulfilled.Count);
                fulfilled[randomIndex].TryFire();
            }
            else
            {
                myScript.TryRebuild();
                myScript.RestartPetrinet(true, SpaceMapperRoot.Command.StopRecording);
            }
        }

        private void ResetCamTo(Vector3 position, PetrinetCondition condition)
        {
            if (condition != null && _focusedCondition != null && _focusedCondition == condition) return;
            _gotoCam = position;// + SpaceOsCameraReference.position;
            _goFromPos = _lastLookAtPos;
            _goFromSize = _lastOsSize;
            _timeGoto = DateTime.Now;
            _animateCam = true;
            _focusedCondition = condition;
        }

        void OnEnable()
        {
            FindAllConditionsAndTransitions();
            UsePhotobooth(true);

            var sceneView = (SceneView)SceneView.sceneViews[0];
            sceneView.Focus();

            var myScript = (SpaceMapperRoot)target;

            _lastOsSize = myScript.SpaceOsCameraSize;
            var activePlace =
                _conditions.FirstOrDefault(c => c.Type == PetrinetCondition.ConditionType.Place && c.Tokens > 0);
            var resetPosition = activePlace != null
                ? activePlace.VisualReferenceInEditor.position
                : myScript.transform.position;
            ResetCamTo(resetPosition, null);
            _resetCam = true;
            _lastLookAtRot = Quaternion.Euler(90f, 0f, 0f);//myScript.SpaceOsCameraReference.rotation);

            Reposition();
            
            PetrinetTransition.TriedToFire.AddListener(TransitionFire);
            _time = DateTime.Now;
            SoftFollow.AnimTime = myScript.AnimationTimeRearrange;
            //FindAllConditionsAndTransitions();
            Tools.hidden = true;
            Tools.visibleLayers = ~1;
            SceneView.RepaintAll();
            //EnableDisableCompilers(false);
            EditorApplication.update += Refocus;
            myScript.RebuildExperience.AddListener(Rebuild);
            SceneView.onSceneGUIDelegate += GuiDelegate;
            EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
        }

        void OnDisable()
        {
            var myScript = (SpaceMapperRoot)target;
            Tools.hidden = false;
            Tools.visibleLayers = ~0;
            //FindAllConditionsAndTransitions();
            SceneView.RepaintAll();
            //EnableDisableCompilers(true);
            PetrinetTransition.TriedToFire.RemoveListener(TransitionFire);
            EditorApplication.update -= Refocus;
            myScript.RebuildExperience.RemoveListener(Rebuild);
            SceneView.onSceneGUIDelegate -= GuiDelegate;
            EditorApplication.playModeStateChanged -= HandleOnPlayModeChanged;
        }

        private void TransitionFire(PetrinetTransition arg0, bool arg1)
        {
            if (!arg1 || _currentMode != Mode.SpaceOsPlay)
                return;
            var condition = arg0.Out.FirstOrDefault(c => c.Type == PetrinetCondition.ConditionType.Place);
            if (condition == null)
                return;
            var pos = condition.VisualReferenceInEditor.position;
            ResetCamTo(pos, condition);
        }

        private void GuiDelegate(SceneView sceneview)
        {
            if (SceneView.lastActiveSceneView == null)
                return;
            var myscript = (SpaceMapperRoot) target;
            myscript.SpaceOsCameraSize = SceneView.lastActiveSceneView.size;
            Selection.activeObject = target;
        }

        private void Rebuild(bool arg0)
        {
            FindAllConditionsAndTransitions();
            UsePhotobooth(false);

            var myScript = (SpaceMapperRoot)target;
            if(_currentMode == Mode.SpaceManagement)
                myScript.SpaceEditorCam.GetComponent<PostProcessingBehaviour>().profile = myScript.IsBuildAvailable() ? myScript.WithEffects : myScript.WithoutEffects;

            Reposition();
        }

        // ForceDirectedPetri _layouter;
        private void Reposition()
        {
            var myScript = (SpaceMapperRoot)target;
            
            //var conditionDict = _conditions.ToDictionary(c => c, c => 1f);
            //var hardware = _transitions.Select(t => t.HardwareRequirements).Distinct();
            //var hardwareDict = hardware.ToDictionary(h => h, h => 10f);
            //new Vector2(-150, -50), new Vector2(25, 50)
            var _layouter = new ForceDirectedPetri(_conditionDict, _hardwareDict, myScript.ForceLayoutSettings, _lowerRight, _upperLeft, myScript.transform);

            _layouter.SimulateAhead(10, .1f);
            _layouter.UpdateVisuals();
        }

        private void Refocus()
        {
            if (Selection.activeGameObject != null && Selection.activeObject == target)
                return;
            if (Application.isPlaying && !((SpaceMapperRoot) target).AllowedSwitch.Contains(Selection.activeGameObject))
                Selection.activeObject = target;
            //else if ()
            //{
            //    Selection.activeObject = target;
            //    //var refocus = myScript.Transitions.Any(t => Selection.gameObjects.Contains(t.gameObject));
            //    // refocus = refocus || myScript.Conditions.Any(t => Selection.gameObjects.Contains(t.gameObject));
            //    // if(refocus)
            //    //     Selection.activeGameObject = go;
            //}
        }

        private void HandleOnPlayModeChanged(PlayModeStateChange obj)
        {
            FindAllConditionsAndTransitions();
        }

        private void ToolLayers(IEnumerable<string> layers, bool showElseHide)
        {
            var visible = showElseHide ? ~1 : ~0;
            foreach (var layer in layers)
            {
                if (showElseHide)
                    Show(ref visible, layer);
                else
                    Hide(ref visible, layer);
            }
            Tools.visibleLayers = visible;
            SceneView.currentDrawingSceneView.camera.cullingMask = visible;
        }

        private void ReparentAllTransitions()
        {
            var myScript = (SpaceMapperRoot)target;
            foreach (var transition in _transitions)
            {
                var allConditions = new List<PetrinetCondition>();
                allConditions.AddRange(transition.In);
                allConditions.AddRange(transition.Out);
                allConditions = allConditions.Distinct().ToList();
                var placeCount = allConditions.Count(i => i.Type == PetrinetCondition.ConditionType.Place);
                transition.transform.parent = placeCount == 1
                    ? allConditions.First(i => i.Type == PetrinetCondition.ConditionType.Place).transform
                    : myScript.transform;
            }
        }

        private void EnableDisableCompilers(bool enable)
        {
            if (Application.isPlaying)
                return;
            var myScript = (SpaceMapperRoot)target;
            var kids = myScript.GetComponentsInChildren<AbstractCompiler>(true);//.Where(x => x.parent == t.transform);
            foreach (var k in kids)
            {
                k.gameObject.SetActive(enable);
            }
            //foreach (var transition in _transitions)
            //{
            //    if (transition == null)
            //        continue;
            //    var t = transition.transform;
            //    var kids = transition.GetComponentsInChildren<Transform>(true).Where(x => x.parent == t.transform);
            //    foreach (var k in kids)
            //    {
            //        k.gameObject.SetActive(enable);
            //    }
            //}
        }

        #region bit operations

        // Turn on the bit using an OR operation:
        public void Show(ref int cullingMask, string layerName)
        {
            cullingMask |= 1 << LayerMask.NameToLayer(layerName);
        }
        // Turn off the bit using an AND operation with the complement of the shifted int:
        public void Hide(ref int cullingMask, string layerName)
        {
            cullingMask &= ~(1 << LayerMask.NameToLayer(layerName));
        }
        // Toggle the bit using a XOR operation:
        public void Toggle(ref int cullingMask, string layerName)
        {
            cullingMask ^= 1 << LayerMask.NameToLayer(layerName);
        }

        #endregion

        #region polygon math helper

        private Vector3 ClosestToRectangle(Vector3 point, Vector2 min, Vector2 max)
        {
            var nearest = new Vector3(point.x, 0f, point.z);
            if (point.x < min.x)
                nearest.x = min.x;
            else if (point.x > max.x)
                nearest.x = max.x;

            if (point.z < min.y)
                nearest.z = min.y;
            else if (point.z > max.y)
                nearest.z = max.y;

            return nearest;
        }

        private static float DistanceFromLine(Vector2 p, Vector2 l1, Vector2 l2)
        {
            float xDelta = l2.x - l1.x;
            float yDelta = l2.y - l1.y;

            //	final double u = ((p3.getX() - p1.getX()) * xDelta + (p3.getY() - p1.getY()) * yDelta) / (xDelta * xDelta + yDelta * yDelta);
            float u = ((p.x - l1.x) * xDelta + (p.y - l1.y) * yDelta) / (xDelta * xDelta + yDelta * yDelta);

            Vector2 closestPointOnLine;
            if (u < 0)
            {
                closestPointOnLine = l1;
            }
            else if (u > 1)
            {
                closestPointOnLine = l2;
            }
            else
            {
                closestPointOnLine = new Vector2(l1.x + u * xDelta, l1.y + u * yDelta);
            }


            var d = p - closestPointOnLine;
            return Mathf.Sqrt(d.x * d.x + d.y * d.y); // distance
        }

        public float DistanceFromPoly(Vector3 pp, bool insideIsZero, List<Vector3> points)
        {
            if (points.Count < 3)
                return 0f;
            var inside = PointInPolygon(pp, points);
            if (insideIsZero && inside)
                return 0f;
            var p = new Vector2(pp.x, pp.z);
            var poly = points.Select(mp => new Vector2(mp.x, mp.z)).ToList();
            float result = 10000;

            // check each line
            for (int i = 0; i < poly.Count; i++)
            {
                int previousIndex = i - 1;
                if (previousIndex < 0)
                {
                    previousIndex = poly.Count - 1;
                }

                Vector2 currentPoint = poly[i];
                Vector2 previousPoint = poly[previousIndex];

                float segmentDistance = DistanceFromLine(new Vector2(p.x, p.y), previousPoint, currentPoint);

                if (segmentDistance < result)
                {
                    result = segmentDistance;
                }
            }
            if (inside)
                result *= -1;

            return result;
        }

        private static bool PointInPolygon(Vector3 point, List<Vector3> polygon)
        {
            var rev = new List<Vector3>(polygon);
            point.y = 0f;
            // Get the angle between the point and the
            // first and last vertices.
            var maxPoint = rev.Count - 1;
            var totalAngle = Vector3.Angle(rev[maxPoint] - point, rev[0] - point);

            // Add the angles from the point
            // to each other pair of vertices.
            for (var i = 0; i < maxPoint; i++)
            {
                totalAngle += Vector3.Angle(rev[i] - point, rev[i + 1] - point);
            }
            // The total angle should be 2 * PI or -2 * PI if
            // the point is in the polygon and close to zero
            // if the point is outside the polygon.
            totalAngle %= 360f;
            if (totalAngle > 359)
                totalAngle -= 360f;
            return (Mathf.Abs(totalAngle) < 0.001f);
        }


        #endregion
    }
}