
using UnityEngine;

namespace Assets.Scripts.Game
{
    public class ChairBroken : MonoBehaviour
    {
        public Transform ChairRef;
        public GameObject BrokenChairPrefab;

        private GameObject _instantiatedPrefab;

        private void OnEnable()
        {
            _instantiatedPrefab = Instantiate(BrokenChairPrefab);
            _instantiatedPrefab.transform.position = ChairRef.transform.position;
            _instantiatedPrefab.transform.rotation = ChairRef.transform.rotation;
        }

        private void OnDisable()
        {
            if (_instantiatedPrefab != null)
                Destroy(_instantiatedPrefab);
        }
    }
}
