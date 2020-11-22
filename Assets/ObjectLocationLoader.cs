using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using UnityEditor;
using TMPro;

namespace ARLocation
{
    public class ObjectLocationLoader : MonoBehaviour, IEventSystemHandler
    {
        private SpriteRenderer spriteRenderer;
        private MeshRenderer meeshRenderer;
        public static Dictionary<Location, GameObject> gameObjects;
        
        // Start is called before the first frame update
        void Start()
        {
            gameObjects = new Dictionary<Location, GameObject>();
        }

        // Update is called once per frame
        void Update()
        {
               if (gameObjects != null && gameObjects.Count != 0)
             {
                foreach (KeyValuePair<Location, GameObject> gameObject in gameObjects)
                {
                    gameObject.Value.transform.LookAt(Camera.main.transform);
                    spriteRenderer = gameObject.Value.GetComponentsInChildren<SpriteRenderer>(true)[0];
                    meeshRenderer = gameObject.Value.GetComponentsInChildren<MeshRenderer>(true)[0];
                    var place = gameObject.Value.GetComponent<PlaceAtLocation>();
                    if (place.SceneDistance > 15)
                    {
                        spriteRenderer.enabled = false;
                        meeshRenderer.enabled = false;
                    }
                              else if (place.SceneDistance <= 15)
                    {
                        spriteRenderer.enabled = true;
                        meeshRenderer.enabled = true;
                    }
                }
            }
        }

        public void LoadLocationObjects(string message)
        {

            var opts = new PlaceAtLocation.PlaceAtOptions()
            {
                HideObjectUntilItIsPlaced = true,
                MaxNumberOfLocationUpdates = 0,
                MovementSmoothing = 0.05f,
                UseMovingAverage = false
            };

            Debug.Log(message);
            var objects = JArray.Parse(message);
            foreach (JObject location in objects)
            {
                Debug.Log(location);

                double lat = double.Parse((string)location["location"]["latitude"]);
                double lng = double.Parse((string)location["location"]["longitude"]);
                JToken sensorData = location["data"];

                var entry = new Location()
                {
                    Latitude = lat,
                    Longitude = lng,
                    Altitude = 0,
                    AltitudeMode = AltitudeMode.GroundRelative
                };

                //Create Game object
                GameObject newGameObject = Instantiate(Resources.Load("POIPrefab")) as GameObject;
                newGameObject.transform.SetParent(this.transform);


                //Create 3D text
                TextMeshPro textMesh = newGameObject.GetComponentsInChildren<TextMeshPro>(true)[0];
                foreach (JObject data in sensorData)
                {
                    textMesh.text = textMesh.text
                    + (string)data["name"]
                    + ": "
                    + "<color=yellow>"
                    + (string)data["value"]
                    + "</color>"
                    + System.Environment.NewLine;
                }

                //Put new Game Object to the Scene
                PlaceAtLocation.AddPlaceAtComponent(newGameObject, entry, opts);
                gameObjects.Add(entry, newGameObject);
                Debug.Log($"{lat}, {lng}, {name}");
            }
        }

    }
}