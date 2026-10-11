using UnityEngine;

namespace TheLastPatient
{
    [CreateAssetMenu(menuName = "The Last Patient/Objective")]
    public sealed class ObjectiveDefinition : ScriptableObject
    {
        public string id;
        [TextArea] public string description;
    }
}
