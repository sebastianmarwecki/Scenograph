using SpaceMapper;
using UnityEngine;

namespace Assets.Scripts.SpaceMapper
{
    public class Builder : MonoBehaviour {

        private GameObject _build;
    
        internal GameObject GetBuildObject()
        {
            if (!Application.isPlaying)
                return null;
            if (_build == null)
            {
                _build = new GameObject();
                _build.transform.parent = transform;
                _build.transform.position = Vector3.zero;
                _build.name = "buildObject_" + gameObject.name;
            }
            return _build;
        }

        protected GameObject CompileLibraryObject(GameObject libraryObject, Vector2 tilePosition, float yRotation, string id)
        {
            var libraryObjectInstance = Instantiate(libraryObject);
            libraryObjectInstance.transform.parent = GetBuildObject().transform;
            libraryObjectInstance.transform.localPosition = new Vector3(tilePosition.x, 0, tilePosition.y);
            libraryObjectInstance.transform.localRotation = Quaternion.identity;
           // libraryObjectInstance.transform.position = ;
           // libraryObjectInstance.transform.parent = GetBuildObject().transform;
           // libraryObjectInstance.transform.localScale = new Vector3(tileSize.x, 1f, tileSize.y);
            libraryObjectInstance.transform.rotation = Quaternion.Euler(new Vector3(0f, yRotation, 0f));
            libraryObjectInstance.name = id;
            foreach (var post in libraryObjectInstance.GetComponentsInChildren<AbstractPostLinkage>())
            {
                post.Call();
            }
            return libraryObjectInstance;
        }

        internal void DeleteKids()
        {
            var build = GetBuildObject();
            if (build == null)
                return;
            var kids = build.GetComponentsInChildren<Transform>();
            foreach (var kid in kids)
            {
                if (kid == build.transform)
                    continue;
                kid.SetParent(null);
                Destroy(kid.gameObject);
            }
        }
    }
}
