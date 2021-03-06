﻿using UnityEngine;
using System.Collections;

public class bl_CameraOrbit : bl_CameraBase
{
    [HideInInspector]
    public bool m_Interact = true;

    [Header("Target")]
    public Transform Target;
    [Header("Settings")]
    public bool isForMobile = false;
    public bool AutoTakeInfo = true;
    public float Distance = 5f;
    [Range(0.01f, 5)]public float SwichtSpeed = 2;
    public Vector2 DistanceClamp = new Vector2(1.5f, 5);
    public Vector2 YLimitClamp = new Vector2(-20, 80);
    public Vector2 XLimitClamp = new Vector2(360, 360); //Clamp the horizontal angle from the start position (max left = x, max right = y) >= 360 = not limit
    public Vector2 SpeedAxis = new Vector2(100, 100);
    public bool LockCursorOnRotate = true;
    [Header("Input")]
    public bool RequieredInput = true;
    public CameraMouseInputType RotateInputKey = CameraMouseInputType.LeftAndRight;
    [Range(0.001f, 0.07f)]
    public float InputMultiplier = 0.02f;
    [Range(0.1f, 15)]
    public float InputLerp = 7;
    public bool useKeys = false;
    [Header("Movement")]
    public CameraMovementType MovementType = CameraMovementType.Normal;
    [Range(-90, 90)]
    public float PuwFogAmount = -5;
    [Range(0.1f, 20)]
    public float LerpSpeed = 7;
    [Range(1f, 100)]
    public float OutInputSpeed = 20;
    [Header("Fog")]
    [Range(5, 179)]
    public float FogStart = 100;
    [Range(0.1f, 15)]
    public float FogLerp = 7;
    [Range(0.0f, 7)]
    public float DelayStartFog = 2;
    [Range(1, 10)]
    public float ScrollSensitivity = 5;
    [Range(1, 25)]
    public float ZoomSpeed = 7;
    [Header("Auto Rotation")]
    public bool AutoRotate = true;
    public CameraAutoRotationType AutoRotationType = CameraAutoRotationType.Dinamicaly;
    [Range(0, 20)]
    public float AutoRotSpeed = 5;
    [Header("Collision")]
    public bool DetectCollision = true;
    public bool TeleporOnHit = true;
    [Range(0.01f, 4)]
    public float CollisionRadius = 2;
    public LayerMask DetectCollisionLayers;
    [Header("Fade")]
    public bool FadeOnStart = true;
    [Range(0.01f, 5)]public float FadeSpeed = 2;
    [SerializeField]
    private Texture2D FadeTexture;

    private float y;
    private float x;
    private Ray Ray;
    private bool LastHaveInput = false;
    private float distance = 0;
    private float currentFog = 60;
    private float defaultFog;
    float horizontal;
    float vertical;
    private float defaultAutoSpeed;
    private float lastHorizontal;
    private bool canFogControl = false;
    private bool haveHit = false;
    private float LastDistance;
    private bool m_CanRotate = true;
    private Vector3 ZoomVector;
    private Quaternion CurrentRotation;
    private Vector3 CurrentPosition;
    private float FadeAlpha = 1;
    private bool isSwitchingTarget = false;
    private bool isDetectingHit = false;
    private float initXRotation;

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        SetUp();
    }

    /// <summary>
    /// 
    /// </summary>
    void SetUp()
    {
        //SetUp default position for camera
        //For avoid the effect of 'teleportation' in the first movement
        if (AutoTakeInfo)
        {
            distance = Vector3.Distance(transform.position, Target.position);
            Distance = distance;
            Vector3 eulerAngles = Transform.eulerAngles;
            x = eulerAngles.y;
            y = eulerAngles.x;
            initXRotation = eulerAngles.y;
            horizontal = x;
            vertical = y;
        }
        else
        {
            distance = Distance;
        }
        currentFog = GetCamera.fieldOfView;
        defaultFog = currentFog;
        GetCamera.fieldOfView = FogStart;
        defaultAutoSpeed = AutoRotSpeed;
        StartCoroutine(IEDelayFog());
        if (RotateInputKey == CameraMouseInputType.MobileTouch && FindObjectOfType<bl_OrbitTouchPad>() == null)
        {
            Debug.LogWarning("For use  mobile touched be sure to put the 'OrbitTouchArea in the canvas of scene");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void LateUpdate()
    {
        if (Target == null)
        {
            Debug.LogWarning("Target is not assigned to orbit camera!", this);
            return;
        }
        if (isSwitchingTarget)
            return;

        if (CanRotate)
        {
            //Calculate the distance of camera
            ZoomControll(false);

            //Control rotation of camera
            OrbitControll();

            //Auto rotate the camera when key is not pressed.
            if (AutoRotate && !isInputKeyRotate) { AutoRotation(); }
        }
        else
        {
            //Calculate the distance of camera
            ZoomControll(true);
        }

        //When can't interact with inputs not need continue here.
        if (!m_Interact)
            return;

        //Control fog effect in camera.
        FogControl();

        //Control all input for apply to the rotation.
        InputControl();
    }

    /// <summary>
    /// 
    /// </summary>
    void InputControl()
    {
        if (LockCursorOnRotate && !useKeys)
        {
            if (!isForMobile)
            {
                if (!isInputKeyRotate && LastHaveInput)
                {
                    if (LockCursorOnRotate && Interact) { bl_CameraUtils.LockCursor(false); }
                    LastHaveInput = false;
                    if (lastHorizontal >= 0) { AutoRotSpeed = OutInputSpeed; } else { AutoRotSpeed = -OutInputSpeed; }
                }
                if (isInputKeyRotate && !LastHaveInput)
                {
                    if (LockCursorOnRotate && Interact) { bl_CameraUtils.LockCursor(true); }
                    LastHaveInput = true;
                }
            }
        }

        if (isInputUpKeyRotate)
        {
            currentFog -= PuwFogAmount;
        }
    }

    /// <summary>
    /// Rotate auto when any key is pressed.
    /// </summary>
    void AutoRotation()
    {
        switch (AutoRotationType)
        {
            case CameraAutoRotationType.Dinamicaly:
                AutoRotSpeed = (lastHorizontal > 0) ? Mathf.Lerp(AutoRotSpeed, defaultAutoSpeed, Time.deltaTime / 2) : Mathf.Lerp(AutoRotSpeed, -defaultAutoSpeed, Time.deltaTime / 2);
                break;
            case CameraAutoRotationType.Left:
                AutoRotSpeed = Mathf.Lerp(AutoRotSpeed, defaultAutoSpeed, Time.deltaTime / 2);
                break;
            case CameraAutoRotationType.Right:
                AutoRotSpeed = Mathf.Lerp(AutoRotSpeed, -defaultAutoSpeed, Time.deltaTime / 2);
                break;

        }
        horizontal += Time.deltaTime * AutoRotSpeed;
    }

    /// <summary>
    /// 
    /// </summary>
    void FogControl()
    {
        if (!canFogControl)
            return;

        //Control the 'puw' effect of fog camera.
        currentFog = Mathf.SmoothStep(currentFog, defaultFog, Time.deltaTime * FogLerp);
        //smooth transition with lerp
        GetCamera.fieldOfView = Mathf.Lerp(GetCamera.fieldOfView, currentFog, Time.deltaTime * FogLerp);
    }


    /// <summary>
    /// 
    /// </summary>
    void OrbitControll()
    {
        if (m_Interact)
        {
            if (!isForMobile)
            {
                if (RequieredInput && !useKeys && isInputKeyRotate || !RequieredInput)
                {
                    horizontal += ((SpeedAxis.x) * InputMultiplier) * AxisX;
                    vertical -= (SpeedAxis.y * InputMultiplier) * AxisY;
                    lastHorizontal = AxisX;
                }
                else if (useKeys)
                {
                    horizontal -= ((KeyAxisX * SpeedAxis.x) ) * InputMultiplier;
                    vertical += (KeyAxisY * SpeedAxis.y) * InputMultiplier;
                    lastHorizontal = KeyAxisX;
                }
            }
        }

        //clamp 'vertical' angle
        vertical = bl_CameraUtils.ClampAngle(vertical, YLimitClamp.x, YLimitClamp.y);
        if (XLimitClamp.x < 360 && XLimitClamp.y < 360)
        {
            horizontal = bl_CameraUtils.ClampAngle(horizontal, (initXRotation - XLimitClamp.y), (XLimitClamp.x + initXRotation));
        }
        //smooth movement of responsiveness input.
        x = Mathf.Lerp(x, horizontal, Time.deltaTime * InputLerp);
        y = Mathf.Lerp(y, vertical, Time.deltaTime * InputLerp);

        //clamp 'y' angle
        y = bl_CameraUtils.ClampAngle(y, YLimitClamp.x, YLimitClamp.y);

        //convert vector to quaternion for apply to rotation
        CurrentRotation = Quaternion.Euler(y, x, 0f);

        //calculate the position and clamp on a circle
        CurrentPosition = ((CurrentRotation * ZoomVector)) + Target.position;

        //switch in the movement select
        switch (MovementType)
        {
            case CameraMovementType.Dynamic:
                Transform.position = Vector3.Lerp(Transform.position, CurrentPosition, (LerpSpeed) * Time.deltaTime);
                Transform.rotation = Quaternion.Lerp(Transform.rotation, CurrentRotation, (LerpSpeed * 2) * Time.deltaTime);
                break;
            case CameraMovementType.Normal:
                Transform.rotation = CurrentRotation;
                Transform.position = CurrentPosition;
                break;
            case CameraMovementType.Towars:
                Transform.rotation = Quaternion.RotateTowards(Transform.rotation, CurrentRotation, (LerpSpeed));
                Transform.position = Vector3.MoveTowards(Transform.position, CurrentPosition, (LerpSpeed));
                break;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void ZoomControll(bool autoApply)
    {
        bool isHit = false;
        //clamp distance and check this.
        distance = Mathf.Clamp(distance - (MouseScrollWheel * ScrollSensitivity), DistanceClamp.x, DistanceClamp.y);
        //Collision detector with a simple raycast
        if (DetectCollision)
        {
            //Calculate direction from target
            Vector3 forward = Transform.position - Target.position;
            //create a ray from transform to target
            Ray = new Ray(Target.position, forward.normalized);
            RaycastHit hit;
            //if ray detect a an obstacle in between the point of origin and the target
            if (Physics.SphereCast(Ray.origin, CollisionRadius, Ray.direction, out hit, distance, DetectCollisionLayers))
            {
                if (!haveHit) { LastDistance = distance; haveHit = true; }
                distance = Mathf.Clamp(hit.distance, DistanceClamp.x, DistanceClamp.y);
                if (TeleporOnHit) { Distance = distance; }
                isHit = true;
            }
            else
            {
                if (!isDetectingHit) { StartCoroutine(DetectHit()); }
            }
            distance = (distance < 1) ? 1 : distance;// distance is recommendable never is least than 1
            if (!haveHit || !TeleporOnHit)
            {
                float s = (isHit) ? Mathf.PI : 1;
                Distance = Mathf.SmoothStep(Distance, distance, Time.deltaTime * (ZoomSpeed * s));
            }
        }
        else
        {
            distance = (distance < 1) ? 1 : distance;// distance is recommendable never is least than 1
            Distance = Mathf.SmoothStep(Distance, distance, Time.deltaTime * ZoomSpeed);
        }

        //apply distance to vector depth z
        ZoomVector = new Vector3(0f, 0f, -this.Distance);

        if (autoApply)
        {
            //calculate the position and clamp on a circle
            CurrentPosition = ((CurrentRotation * ZoomVector)) + Target.position;

            //switch in the movement select
            switch (MovementType)
            {
                case CameraMovementType.Dynamic:
                    Transform.position = Vector3.Lerp(Transform.position, CurrentPosition, (LerpSpeed) * Time.deltaTime);
                    Transform.rotation = Quaternion.Lerp(Transform.rotation, CurrentRotation, (LerpSpeed * 2) * Time.deltaTime);
                    break;
                case CameraMovementType.Normal:
                    Transform.rotation = CurrentRotation;
                    Transform.position = CurrentPosition;
                    break;
                case CameraMovementType.Towars:
                    Transform.rotation = Quaternion.RotateTowards(Transform.rotation, CurrentRotation, (LerpSpeed));
                    Transform.position = Vector3.MoveTowards(Transform.position, CurrentPosition, (LerpSpeed));
                    break;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private bool isInputKeyRotate
    {
        get
        {
            switch (RotateInputKey)
            {
                case CameraMouseInputType.All:
                    return (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1) || Input.GetKey(KeyCode.Mouse2) || Input.GetMouseButton(0));
                case CameraMouseInputType.LeftAndRight:
                    return (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1));
                case CameraMouseInputType.LeftMouse:
                    return (Input.GetKey(KeyCode.Mouse0));
                case CameraMouseInputType.RightMouse:
                    return (Input.GetKey(KeyCode.Mouse1));
                case CameraMouseInputType.MouseScroll:
                    return (Input.GetKey(KeyCode.Mouse2));
                case CameraMouseInputType.MobileTouch:
                    return (Input.GetMouseButton(0) || Input.GetMouseButton(1));
                default:
                    return (Input.GetKey(KeyCode.Mouse0));
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void OnGUI()
    {
        if (isSwitchingTarget)
        {
            GUI.color = new Color(1, 1, 1, FadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), FadeTexture, ScaleMode.StretchToFill);
            return;
        }

        if (FadeOnStart && FadeAlpha > 0)
        {
            FadeAlpha -= Time.deltaTime * FadeSpeed;
            GUI.color = new Color(1, 1, 1, FadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), FadeTexture, ScaleMode.StretchToFill);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private bool isInputUpKeyRotate
    {
        get
        {
            switch (RotateInputKey)
            {
                case CameraMouseInputType.All:
                    return (Input.GetKeyUp(KeyCode.Mouse0) || Input.GetKeyUp(KeyCode.Mouse1) || Input.GetKeyUp(KeyCode.Mouse2) || Input.GetMouseButtonUp(0));
                case CameraMouseInputType.LeftAndRight:
                    return (Input.GetKeyUp(KeyCode.Mouse0) || Input.GetKeyUp(KeyCode.Mouse1));
                case CameraMouseInputType.LeftMouse:
                    return (Input.GetKeyUp(KeyCode.Mouse0) || Input.GetMouseButtonUp(0));
                case CameraMouseInputType.RightMouse:
                    return (Input.GetKeyUp(KeyCode.Mouse1));
                case CameraMouseInputType.MouseScroll:
                    return (Input.GetKeyUp(KeyCode.Mouse2));
                case CameraMouseInputType.MobileTouch:
                    return (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1));
                default:
                    return (Input.GetKey(KeyCode.Mouse0) || Input.GetMouseButton(0));
            }
        }
    }

    /// <summary>
    /// Call this function for change the target to orbit
    /// the change will be by a smooth fade effect
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        StopCoroutine("TranslateTarget");
        StartCoroutine("TranslateTarget", newTarget);
    }

    /// <summary>
    /// 
    /// </summary>
    IEnumerator TranslateTarget(Transform newTarget)
    {
        isSwitchingTarget = true;
        while (FadeAlpha < 1)
        {
            transform.position = Vector3.Lerp(transform.position, transform.position + new Vector3(0, 2, -2), Time.deltaTime );
            FadeAlpha += Time.smoothDeltaTime * SwichtSpeed;
            yield return null;
        }
        Target = newTarget;
        isSwitchingTarget = false;
        while (FadeAlpha > 0)
        {
            FadeAlpha -= Time.smoothDeltaTime * SwichtSpeed;
            yield return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator DetectHit()
    {
        isDetectingHit = true;
        yield return new WaitForSeconds(0.4f);
        if (haveHit) { distance = LastDistance; haveHit = false; }
        isDetectingHit = false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator IEDelayFog()
    {
        yield return new WaitForSeconds(DelayStartFog);
        canFogControl = true;
    }

    /// <summary>
    /// 
    /// </summary>
    public float Horizontal
    {
        get
        {
            return horizontal;
        }
        set
        {
            horizontal += value;
            lastHorizontal = horizontal;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public float Vertical
    {
        get
        {
            return vertical;
        }
        set
        {
            vertical += value;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public bool Interact
    {
        get
        {
            return m_Interact;
        }
        set
        {
            m_Interact = value;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public bool CanRotate
    {
        get
        {
            return m_CanRotate;
        }
        set
        {
            m_CanRotate = value;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public float AutoRotationSpeed
    {
        get
        {
            return defaultAutoSpeed;
        }
        set
        {
            defaultAutoSpeed = value;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public void SetZoom(float value)
    {
        distance += (-(value * 0.5f) * ScrollSensitivity);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public void SetStaticZoom(float value)
    {
        distance += value;
    }

    /// <summary>
    /// 
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color32(0, 221, 221, 255);
        if (Target != null)
        {
            Gizmos.DrawLine(transform.position, Target.position);
            Gizmos.matrix = Matrix4x4.TRS(Target.position, transform.rotation, new Vector3(1f, 0, 1f));
            Gizmos.DrawWireSphere(Target.position, Distance);
            Gizmos.matrix = Matrix4x4.identity;
        }
        Gizmos.DrawCube(transform.position, new Vector3(1, 0.2f, 0.2f));
        Gizmos.DrawCube(transform.position, Vector3.one / 2);
    }
}