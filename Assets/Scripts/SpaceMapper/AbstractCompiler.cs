using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Petrinet;
using UnityEngine;

namespace Assets.Scripts.SpaceMapper
{
    public abstract class AbstractCompiler : Builder
    {
        protected internal HardwareRequirements HardwareRequirements;

        private Vector3 _compiledPosition;

        public enum LinkInformation
        {
            ShowNoWalls,
            ShowAsUnoccupied,
            Default
        }

        protected internal List<PetrinetTransition> Transitions = new List<PetrinetTransition>();

        private void AddListeners()
        {
            //Transition.HardwareRequirements.ReAssigned.AddListener(Recompile);
            foreach (var transition in Transitions)
            {
                PetrinetTransition.TriedToFire.AddListener(Fired);
                transition.InConditionsChanged.AddListener(CheckVisualization);
            }
        }

        private void Fired(PetrinetTransition transition, bool successfully)
        {
            if(successfully && Transitions.Contains(transition))
                TransitionFired(transition);
        }

        private void RemoveListeners()
        {
            //Transition.HardwareRequirements.ReAssigned.RemoveListener(Recompile);
            foreach (var transition in Transitions)
            {
                PetrinetTransition.TriedToFire.RemoveListener(Fired);
                transition.InConditionsChanged.RemoveListener(CheckVisualization);
            }
        }

        internal void StartCompile(TrackingSpaceRoot ram)
        {
            HardwareRequirements = gameObject.GetComponentInParent<HardwareRequirements>();
            Transitions = HardwareRequirements.gameObject.GetComponents<PetrinetTransition>().ToList();
            RemoveListeners();
            AddListeners();
            Recompile(ram);
            _compiledPosition = transform.position;
            CheckVisualization();
        }

        internal void ApplyOffset(Vector3 offset)
        {
            transform.position = _compiledPosition + offset;
        }

        private void CheckVisualization()
        {
            var transitions = HardwareRequirements.gameObject.GetComponentsInChildren<PetrinetTransition>();

            var shouldViz = transitions.ToDictionary(t => t, t => CheckVisualizationSingle(t.GetConditionsFulfilled()));
            
            var anyShouldViz = shouldViz.Any(sv => sv.Value);
            gameObject.SetActive(anyShouldViz);
            if (anyShouldViz)
                transform.position = _compiledPosition;
            else
            {
                //var firstPlace = transitions.SelectMany(d => d.In)
                //                     .FirstOrDefault(c => c.Type == PetrinetCondition.ConditionType.Place) ??
                //                 transitions.SelectMany(d => d.Out)
                //                     .FirstOrDefault(c => c.Type == PetrinetCondition.ConditionType.Place);
                //if (firstPlace != null)
                //    transform.position = _compiledPosition +
                //                         firstPlace.GetComponent<AbstractLinker>().GetBuildObject().transform.position;
            }

            var readyToFire = transitions.ToDictionary(t => t, t => t.GetConditionsFulfilled().Values.All(v => v));

            UpdateTransitions(shouldViz, readyToFire);
        }

        private static bool CheckVisualizationSingle(IDictionary<PetrinetCondition, bool> isConditionFulfilled)
        {
            //set visuals on/off
            var shouldViz = isConditionFulfilled.Any(c =>
                c.Key.Type == PetrinetCondition.ConditionType.Place &&
                isConditionFulfilled[c.Key]);
            return shouldViz;
        }

        public abstract void Reset();

        public abstract void TransitionFired(PetrinetTransition transition);

        public abstract void Recompile(TrackingSpaceRoot ram);

        public abstract void UpdateTransitions(Dictionary<PetrinetTransition, bool> placeActive, Dictionary<PetrinetTransition, bool> readyToFire);

        public abstract LinkInformation GetLinkInformation();
    }
}