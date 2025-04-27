using System;
using TMPro;
using UniRx;
using UnityEditor;
using UnityEngine;

namespace DatAR.Widgets.ManipulationMinMaxFilter
{
    public class FilterSliderControllerComponent : MonoBehaviour
    {
        [MinMaxSlider (0f, 1f)] [SerializeField] private Vector2 _selectedRange = new Vector2(0, 1); // x is min; y is max
        public readonly BehaviorSubject<Vector2> selectedRange;
    
        private Transform _rangeTopTransform;
        private Transform _rangeMiddleTransform;
        private Transform _rangeBottomTransform;
    
        private Transform _handleTopTransform;
        private Transform _handleBottomTransform;

        public TextMeshPro TopValueText;
        public TextMeshPro BottomValueText;
        public TextMeshPro LabelText;

        // Don't allow more value updates than one per frame
        // Otherwise, expect bottlenecks.
        private bool _valuesUpdatedThisFrame;

        private float _baseScale = 10;

        public void LateUpdate()
        {
            if (_valuesUpdatedThisFrame)
            {
                UpdateSlider();
            }
            _valuesUpdatedThisFrame = false;
        }

        public void SetMinValue(float minValue)
        {
            _selectedRange.x = Mathf.Clamp(minValue, 0, _selectedRange.y);
            _valuesUpdatedThisFrame = true;
        }
    
        public void SetMaxValue(float maxValue)
        {
            _selectedRange.y = Mathf.Clamp(maxValue, _selectedRange.x, 1);
            _valuesUpdatedThisFrame = true;
        }

        public void SetMinMaxValues(Vector2 minMaxValue)
        {
            _selectedRange.x = Mathf.Clamp(minMaxValue.x, 0, _selectedRange.y);
            _selectedRange.y = Mathf.Clamp(minMaxValue.y, _selectedRange.x, 1);
            _valuesUpdatedThisFrame = true;
        }

        FilterSliderControllerComponent()
        {
            selectedRange = new BehaviorSubject<Vector2>(
                _selectedRange);
        }
    
        private void Awake()
        {
            // Set game objects; always the same in the prefab
            _rangeTopTransform = gameObject.transform.Find("FilterHandleTop/FilterRangeTop");
            _rangeMiddleTransform = gameObject.transform.Find("FilterHandleBottom/FilterRangeMiddle");
            _rangeBottomTransform = gameObject.transform.Find("FilterRangeBottom");
            _handleTopTransform = gameObject.transform.Find("FilterHandleTop");
            _handleBottomTransform = gameObject.transform.Find("FilterHandleBottom");

            if (_rangeTopTransform == null
                || _rangeMiddleTransform == null
                || _rangeMiddleTransform == null
                || _rangeBottomTransform == null
                || _handleTopTransform == null
                || _handleBottomTransform == null)
            {
                throw new Exception("Could not find at least one of the slider elements. Did you rename any?");
            }
        
            UpdateSlider();
        }

#if UNITY_EDITOR
        // Runs on changes through editor when in play mode
        private void OnValidate()
        {
            if (EditorApplication.isPlaying && _rangeTopTransform != null)
            {
                UpdateSlider();
            }
        }
#endif
    
        // Take in provided range, and visualise slider. After this, update observable.
        private void UpdateSlider()
        {
            var rangeTopLocalScale = _rangeTopTransform.localScale;
            _rangeTopTransform.localScale = new Vector3(
                rangeTopLocalScale.x,
                (1 - _selectedRange.y) * _baseScale,
                rangeTopLocalScale.z);

            var rangeMiddleLocalScale = _rangeMiddleTransform.localScale;
            _rangeMiddleTransform.localScale = new Vector3(
                rangeMiddleLocalScale.x,
                (_selectedRange.y - _selectedRange.x) * _baseScale,
                rangeMiddleLocalScale.z);
        
            var rangeBottomLocalScale = _rangeBottomTransform.localScale;
            _rangeBottomTransform.localScale = new Vector3(
                rangeBottomLocalScale.x,
                _selectedRange.x * _baseScale,
                rangeBottomLocalScale.z);

            var handleTopLocalPosition = _handleTopTransform.localPosition;
            _handleTopTransform.localPosition = new Vector3(
                handleTopLocalPosition.x,
                _selectedRange.y * _baseScale,
                handleTopLocalPosition.z);
        
            var handleBottomLocalPosition = _handleBottomTransform.localPosition;
            _handleBottomTransform.localPosition = new Vector3(
                handleBottomLocalPosition.x,
                _selectedRange.x * _baseScale,
                handleBottomLocalPosition.z);

            selectedRange.OnNext(_selectedRange);
        }
    }
}
