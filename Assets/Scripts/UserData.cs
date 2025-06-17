using UnityEngine;

namespace Test
{
    public class UserData : MonoBehaviour
    {
        private static UserData _instance;
        public static UserData Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("UserData");
                    _instance = go.AddComponent<UserData>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        [field: SerializeField] public long Suid { get; set; }
        [field: SerializeField] public string AuthToken { get; set; }
        public string Id { get; set; }
        public string Password { get; set; }
        public bool IsAdmin { get; set; }
    }
}