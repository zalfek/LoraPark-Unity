using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace ARLocation
{
    public class Navigate : MonoBehaviour
    {
        private GameObject arrow;
        private Camera camera; //Camera to use
        private Transform target; //Target to point at (you could set this to any gameObject dynamically)
        private Vector3 targetPos; //Target position on screen
        private Vector3 screenMiddle; //Middle of the screen
        private GameObject destinationObject;
        private MeshRenderer renderer;
        private TextMesh textmesh;
        private MeshRenderer meeshRenderer;

        // Start is called before the first frame update
        void Start()
        {
            camera = Camera.main;
            Vector3 cameraBottomMiddle = camera.ScreenToWorldPoint(new Vector3(Screen.width*0.5f, Screen.height *0.2f, 10f));
            this.gameObject.transform.position = cameraBottomMiddle;

            arrow = this.gameObject.transform.GetChild(0).gameObject;
            renderer = arrow.GetComponentsInChildren<MeshRenderer>(true)[0];

            textmesh = this.gameObject.transform.GetChild(1).gameObject.GetComponentsInChildren<TextMesh>(true)[0]; ;
            meeshRenderer = textmesh.GetComponentsInChildren<MeshRenderer>(true)[0];

            renderer.enabled = false;
            meeshRenderer.enabled = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (destinationObject != null) {
                Debug.Log("Update");
                arrow.transform.rotation = Quaternion.Slerp(arrow.transform.rotation, Quaternion.LookRotation(target.position), 10 * Time.deltaTime);
                textmesh.text = string.Format("{0:N0}", destinationObject.GetComponent<PlaceAtLocation>().SceneDistance) + " m";
            }
        }

        public void NavigateToSensor(string message)
        {
            StopNavigation(message);
            Debug.Log(message);
            var objects = JArray.Parse(message);
            foreach (JObject location in objects)
            {
                Debug.Log(location);

                double lat = double.Parse((string)location["location"]["latitude"]);
                double lng = double.Parse((string)location["location"]["longitude"]);

                var entry = new Location()
                {
                    Latitude = lat,
                    Longitude = lng,
                    Altitude = 0,
                    AltitudeMode = AltitudeMode.GroundRelative
                };
                Debug.Log($"{lat}, {lng}");
                destinationObject = getDestination(entry);
                Debug.Log("transform");
                target = destinationObject.transform;
               
            }
            renderer.enabled = true;
            meeshRenderer.enabled = true;
        }

        private GameObject getDestination(Location location)
        {
            Dictionary<Location, GameObject> gameObjects = ObjectLocationLoader.gameObjects;
            foreach (KeyValuePair<Location, GameObject> kvp in gameObjects)
            {
                if(Location.Equal(location, kvp.Key))
                {
                    Debug.Log("getDestination return");
                    return kvp.Value;
                }
            }
            Debug.Log("getDestination null");
            return null;
        }

        public void StopNavigation(string message)
        {
            meeshRenderer.enabled = false;
            renderer.enabled = false;
            destinationObject = null;
            target = null;
        }

    }
}