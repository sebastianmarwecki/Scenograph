using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Petrinet;
using Assets.Scripts.SpaceMapper;
using UnityEngine;

namespace Assets.Scripts.Game
{
    public class Door2DoorTransitionCompiler : AbstractCompiler
    {
        public Collider Collider1To2Trigger, Collider2To1Trigger;
        public GameObject Viz1, Viz2;
        public GameObject FloorObject;
        public string PlayerObject;

        private Transform _playerTransform;
        private Dictionary<Collider, PetrinetTransition> _colliderToTransition;
        private Dictionary<PetrinetTransition, GameObject> _transitionToViz;
        private Dictionary<PetrinetTransition, bool> _readyToFire;
        private bool _compiled;

        public override void Recompile(TrackingSpaceRoot ram)
        {
            var memoryPointer = HardwareRequirements.MemoryPointer.Value;
            var allocatedMemory = HardwareRequirements.AllocatedMemory;

            _readyToFire = new Dictionary<PetrinetTransition, bool>();


            var tilesAvailable = ram.TileAvailable;
            var dimensions = ram.GetTileAmountTotal();

            //debug floor object
            var pos = Vector2.zero;
            for (var i = 0; i < allocatedMemory.x; i++)
            for (var j = 0; j < allocatedMemory.y; j++)
                pos += ram.GetSpaceFromPosition(memoryPointer.x + i, memoryPointer.y + j);
            pos /= allocatedMemory.x * allocatedMemory.y;
            //pos += ram.GetSpaceFromPosition(memoryPointer);
            if (FloorObject != null)
            {
                var go = CompileLibraryObject(FloorObject, pos, 0f, "floor_" + gameObject.name);
                go.transform.localScale = new Vector3(allocatedMemory.x, 1f, allocatedMemory.y);
            }

            //check where there is first 2m wall, place there
            //check all walls
            var upperWall = allocatedMemory.x > 1;
            var rightWall = allocatedMemory.y > 1;
            var lowerWall = allocatedMemory.x > 1;
            var leftWall = allocatedMemory.y > 1;
            for (var x = memoryPointer.x; x < memoryPointer.x + allocatedMemory.x; x++)
            for (var z = memoryPointer.y; z < memoryPointer.y + allocatedMemory.y; z++)
            {
                if (x == memoryPointer.x)
                    leftWall = leftWall && (x == 0 || !tilesAvailable[x - 1, z]);
                if (z == memoryPointer.y)
                    lowerWall = lowerWall && (z == 0 || !tilesAvailable[x, z - 1]);
                if (z == memoryPointer.y + allocatedMemory.y - 1)
                    upperWall = upperWall && (z + 1 == dimensions.y || !tilesAvailable[x, z + 1]);
                if (x == memoryPointer.x + allocatedMemory.x - 1)
                    rightWall = rightWall && (x + 1 == dimensions.x || !tilesAvailable[x + 1, z]);
            }
            //set position and rotation
            var position2 = ram.GetSpaceFromPosition(memoryPointer);
            var position3 = new Vector3(position2.x, 0f, position2.y);
            if (upperWall)
            {
                transform.position = position3 + (allocatedMemory.y-1) * Vector3.forward;
                transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            } else if (rightWall)
            {
                transform.position = position3 + Vector3.forward + (allocatedMemory.x - 1) * Vector3.right;
                transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            } else if (lowerWall)
            {
                transform.position = position3 + Vector3.right;
                transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            } else 
            {
                if (!leftWall)
                    Debug.LogError(gameObject.name + "/" + transform.parent.gameObject.name + " no wall");
                transform.position = position3;
                transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            } 
                

            //init vars
            _colliderToTransition = new Dictionary<Collider, PetrinetTransition>
            {
                {Collider1To2Trigger, Transitions[0]},
                {Collider2To1Trigger, Transitions[1]}
            };
            _transitionToViz = new Dictionary<PetrinetTransition, GameObject>
            {
                {Transitions[0], Viz1},
                {Transitions[1], Viz2}
            };
            
            _playerTransform = GameObject.Find(PlayerObject).transform;

            _compiled = true;
        }

        private void Update()
        {
            if (!_compiled)
                return;
            var transitionToFire = GetReadyToFireTransitionsStandingOn();
            if(transitionToFire != null)
                transitionToFire.TryFire();
        }

        private PetrinetTransition GetReadyToFireTransitionsStandingOn()
        {
            var down = Vector3.down; //-Camera.main.transform.up;
            var relevantPairs = _colliderToTransition.Where(k => _readyToFire != null && _readyToFire.ContainsKey(k.Value) && _readyToFire[k.Value]);
            foreach (var rp in relevantPairs)
            {
                RaycastHit info;
                if (rp.Key.Raycast(new Ray(_playerTransform.position, down), out info, 4f))
                    return rp.Value;
            }
            return null;
        }

        public override void Reset()
        {
        }

        public override void UpdateTransitions(Dictionary<PetrinetTransition, bool> placeActive, Dictionary<PetrinetTransition, bool> readyToFire)
        {
            foreach (var pa in placeActive)
                _transitionToViz[pa.Key].SetActive(pa.Value);
            _readyToFire = readyToFire;
            if (!placeActive.Values.Any(t => t))
            {
                _transitionToViz.Values.First().SetActive(true);
            }
        }

        public override void TransitionFired(PetrinetTransition transition)
        {
        }

        public override LinkInformation GetLinkInformation()
        {
            return LinkInformation.Default;
        }
    }
}
