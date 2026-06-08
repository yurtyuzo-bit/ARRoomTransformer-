using System;
using UnityEngine;

namespace ARRoomTransformer
{
    /// <summary>
    /// Dokunmatik girdi yöneticisi. Tek parmak dokunma, çift parmak pinch/rotate
    /// hareketlerini algılar ve ilgili event'leri tetikler.
    /// </summary>
    public class TouchInputManager : MonoBehaviour
    {
        [Header("Ayarlar")]
        [SerializeField] private float tapMaxDuration = 0.3f;
        [SerializeField] private float tapMaxMovement = 10f;
        [SerializeField] private float longPressThreshold = 0.6f;
        [SerializeField] private float doubleTapMaxInterval = 0.3f;
        [SerializeField] private bool ignoreUITouches = true;

        // Tek parmak
        private Vector2 _touchStartPos;
        private float _touchStartTime;
        private bool _isDragging;
        private float _lastTapTime;

        // Çift parmak
        private float _initialPinchDistance;
        private float _initialRotationAngle;
        private bool _isPinching;

        // ================================================================
        // Events
        // ================================================================

        /// <summary>Ekrana tek dokunma (tap). Parametre: ekran pozisyonu.</summary>
        public event Action<Vector2> OnTap;

        /// <summary>Çift dokunma (double tap). Parametre: ekran pozisyonu.</summary>
        public event Action<Vector2> OnDoubleTap;

        /// <summary>Uzun basma. Parametre: ekran pozisyonu.</summary>
        public event Action<Vector2> OnLongPress;

        /// <summary>Sürükleme başladı. Parametre: başlangıç pozisyonu.</summary>
        public event Action<Vector2> OnDragStart;

        /// <summary>Sürükleme devam ediyor. Parametreler: mevcut pozisyon, delta.</summary>
        public event Action<Vector2, Vector2> OnDrag;

        /// <summary>Sürükleme bitti. Parametre: bitiş pozisyonu.</summary>
        public event Action<Vector2> OnDragEnd;

        /// <summary>Pinch (ölçekleme). Parametre: ölçek faktörü (1.0 = değişim yok).</summary>
        public event Action<float> OnPinch;

        /// <summary>İki parmak döndürme. Parametre: derece cinsinden açı değişimi.</summary>
        public event Action<float> OnTwoFingerRotate;

        /// <summary>Pinch/rotate hareketi bitti.</summary>
        public event Action OnPinchEnd;

        /// <summary>Şu an sürükleme yapılıyor mu?</summary>
        public bool IsDragging => _isDragging;

        /// <summary>Şu an pinch yapılıyor mu?</summary>
        public bool IsPinching => _isPinching;

        /// <summary>Dokunma sayısı.</summary>
        public int TouchCount => Input.touchCount;

        private void Update()
        {
            int touchCount = Input.touchCount;

            if (touchCount == 0)
            {
                if (_isDragging)
                {
                    _isDragging = false;
                    OnDragEnd?.Invoke(_touchStartPos);
                }
                if (_isPinching)
                {
                    _isPinching = false;
                    OnPinchEnd?.Invoke();
                }
                return;
            }

            if (touchCount == 1)
            {
                HandleSingleTouch(Input.GetTouch(0));
            }
            else if (touchCount == 2)
            {
                HandleDoubleTouch(Input.GetTouch(0), Input.GetTouch(1));
            }
        }

        private void HandleSingleTouch(Touch touch)
        {
            // UI üzerindeyse atla
            if (ignoreUITouches && IsPointerOverUI(touch.position)) return;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _touchStartPos = touch.position;
                    _touchStartTime = Time.time;
                    _isDragging = false;
                    break;

                case TouchPhase.Moved:
                    float moveDistance = Vector2.Distance(touch.position, _touchStartPos);

                    if (!_isDragging && moveDistance > tapMaxMovement)
                    {
                        _isDragging = true;
                        OnDragStart?.Invoke(_touchStartPos);
                    }

                    if (_isDragging)
                    {
                        Vector2 delta = touch.deltaPosition;
                        OnDrag?.Invoke(touch.position, delta);
                    }
                    break;

                case TouchPhase.Stationary:
                    // Uzun basma kontrolü
                    if (!_isDragging && Time.time - _touchStartTime >= longPressThreshold)
                    {
                        OnLongPress?.Invoke(touch.position);
                        _touchStartTime = float.MaxValue; // Tekrar tetiklemeyi önle
                    }
                    break;

                case TouchPhase.Ended:
                    if (_isDragging)
                    {
                        _isDragging = false;
                        OnDragEnd?.Invoke(touch.position);
                    }
                    else
                    {
                        float duration = Time.time - _touchStartTime;
                        float totalMove = Vector2.Distance(touch.position, _touchStartPos);

                        if (duration <= tapMaxDuration && totalMove <= tapMaxMovement)
                        {
                            // Double tap kontrolü
                            if (Time.time - _lastTapTime <= doubleTapMaxInterval)
                            {
                                OnDoubleTap?.Invoke(touch.position);
                                _lastTapTime = 0f;
                            }
                            else
                            {
                                OnTap?.Invoke(touch.position);
                                _lastTapTime = Time.time;
                            }
                        }
                    }
                    break;

                case TouchPhase.Canceled:
                    _isDragging = false;
                    break;
            }
        }

        private void HandleDoubleTouch(Touch touch0, Touch touch1)
        {
            // Tek parmak sürüklemeyi durdur
            if (_isDragging)
            {
                _isDragging = false;
                OnDragEnd?.Invoke(touch0.position);
            }

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                // Pinch/rotate başlangıcı
                _initialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                _initialRotationAngle = GetAngleBetweenTouches(touch0.position, touch1.position);
                _isPinching = true;
                return;
            }

            if (_isPinching && (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved))
            {
                // Pinch (ölçek)
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                if (_initialPinchDistance > 0.01f)
                {
                    float scaleFactor = currentDistance / _initialPinchDistance;
                    OnPinch?.Invoke(scaleFactor);
                    _initialPinchDistance = currentDistance;
                }

                // Rotation
                float currentAngle = GetAngleBetweenTouches(touch0.position, touch1.position);
                float angleDelta = Mathf.DeltaAngle(_initialRotationAngle, currentAngle);
                if (Mathf.Abs(angleDelta) > 0.5f)
                {
                    OnTwoFingerRotate?.Invoke(angleDelta);
                    _initialRotationAngle = currentAngle;
                }
            }

            if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
            {
                _isPinching = false;
                OnPinchEnd?.Invoke();
            }
        }

        private float GetAngleBetweenTouches(Vector2 pos0, Vector2 pos1)
        {
            Vector2 diff = pos1 - pos0;
            return Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        }

        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null) return false;

            var pointerData = new UnityEngine.EventSystems.PointerEventData(eventSystem)
            {
                position = screenPosition
            };

            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);
            return results.Count > 0;
        }
    }
}
