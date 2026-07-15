using UnityEngine;

namespace Helpers
{
    public class ObjectToggler : MonoBehaviour
    {
        public void ToggleEnabled() => gameObject.SetActive(!gameObject.activeSelf);
    }
}
