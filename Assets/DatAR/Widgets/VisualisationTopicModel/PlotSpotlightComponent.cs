using UnityEngine;

namespace DatAR.Widgets.VisualisationTopicModel
{
    public class PlotSpotlightComponent : MonoBehaviour
    {
        private float _speed = .05f;

        [SerializeField] private Transform _sphere;

        private VisualisationTopicModel _dataPlot;
    
        // Update is called once per frame
        void Awake()
        {
            _dataPlot = gameObject.GetComponentInParent<VisualisationTopicModel>();
        }
    
        void OnTriggerStay(Collider other)
        {
            if (!other.tag.Equals("spotlight"))
            {
                return;
            }
            // other.gameObject.transform.position = other.gameObject.transform.position + new Vector3(0, .5f, 0);
//        var prevRotate = transform.rotation;

            var originalLocLocal = _dataPlot.pointsLocation[$"{name}"];
            var originalLoc = transform.parent.TransformPoint(originalLocLocal);
            var move = originalLoc - other.transform.position;
                   
            float step =  _speed * Time.deltaTime; // calculate distance to move
        
            // check if position is not outside of sphere
            if (Vector3.Distance(Vector3.MoveTowards(transform.position, originalLoc + move, step), other.transform.position) >= _sphere.lossyScale.x / 2)
            {
                return;
            }
            transform.position = Vector3.MoveTowards(transform.position, originalLoc + move, step);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.tag.Equals("spotlight"))
            {
                return;
            }
            // TODO: animate
            var originalLocLocal = _dataPlot.pointsLocation[$"{name}"];
            var originalLoc = transform.parent.TransformPoint(originalLocLocal);
            transform.position = originalLoc;
        }
    }
}
