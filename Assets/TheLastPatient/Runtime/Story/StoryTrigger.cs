using UnityEngine;

namespace TheLastPatient
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class StoryTrigger : MonoBehaviour
    {
        public StoryEventDefinition storyEvent;
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<FirstPersonController>() != null && GameDirector.Instance != null)
                GameDirector.Instance.story.Raise(storyEvent);
        }
    }
}
